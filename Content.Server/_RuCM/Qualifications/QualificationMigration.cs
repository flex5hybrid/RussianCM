using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Shared._RuCM.Qualifications;

namespace Content.Server._RuCM.Qualifications;

public sealed record MigrationCandidate(Guid Player, Dictionary<string, double> TrackerHours);
public sealed record MigrationPlan(string Key, long Revision, DateTimeOffset At, Dictionary<Guid, HashSet<string>> Grants);
public sealed record QualificationMigrationSettings(Dictionary<string, HashSet<string>> Groups, Dictionary<string, string> Aliases);

public sealed partial class QualificationService
{
    public const string MigrationKey = "govfor-training-v1";

    /// <summary>No writes. Recent GOVFOR evidence is job-specific, never inferred from account last-seen.</summary>
    public MigrationPlan MigrationDryRun(IEnumerable<MigrationCandidate> candidates, DateTimeOffset at)
    {
        var s = VolatileSnapshot();
        var grants = new Dictionary<Guid, HashSet<string>>();
        if (s.Migrations.Contains(MigrationKey)) return new(MigrationKey, s.Revision, at, grants);
        foreach (var candidate in candidates.GroupBy(c => c.Player).Select(g => g.First()))
        {
            var earned = new HashSet<string>();
            var roles = s.Roles.Values.Where(r => r.Enabled && !s.CommandingOfficerJobs.Contains(r.JobId)).ToArray();
            var canonicalHours = candidate.TrackerHours.GroupBy(t => s.TrackerAliases.GetValueOrDefault(t.Key, t.Key)).ToDictionary(g => g.Key, g => g.Sum(t => Math.Max(0, t.Value)));
            double Hours(string groupId, IEnumerable<RoleRequirement> fallback)
            {
                var group = s.MigrationGroups.TryGetValue(groupId, out var jobs) ? s.Roles.Values.Where(r => jobs.Contains(r.JobId) && !s.CommandingOfficerJobs.Contains(r.JobId)) : fallback;
                return group.Select(r => r.Tracker).Where(t => t.Length > 0).Distinct().Sum(t => canonicalHours.GetValueOrDefault(t));
            }
            if (s.Participation.Any(p => p.Player == candidate.Player && p.At >= at.AddDays(-14) && p.At <= at && s.Roles.TryGetValue(p.Job, out var participated) && participated.Govfor)) earned.Add("enlisted");
            if (Hours("sergeant", roles.Where(r => r.MinimumLevel == MilitaryLevel.Sergeant)) >= 5) earned.UnionWith(new[] { "enlisted", "sergeant" });
            if (Hours("officer", roles.Where(r => r.MinimumLevel == MilitaryLevel.Officer)) >= 10) earned.UnionWith(QualificationRules.Levels);
            foreach (var definition in s.Definitions.Values.Where(d => d.Enabled && !QualificationRules.Levels.Contains(d.Id) && d.Id != "commanding_officer"))
                if (Hours(definition.Id, roles.Where(r => r.Professional.Contains(definition.Id))) >= 5) earned.Add(definition.Id);
            // Never restore an existing suspension/revocation through migration.
            if (s.Players.TryGetValue(candidate.Player, out var player)) earned.ExceptWith(player.Grants.Keys);
            if (candidate.Player != Guid.Empty && earned.Count > 0) grants[candidate.Player] = earned;
        }
        return new(MigrationKey, s.Revision, at, grants);
    }
    private QualificationStore VolatileSnapshot() => System.Threading.Volatile.Read(ref _cache);

    public async Task ExecuteMigration(QualificationAuthority actor, MigrationPlan plan)
    {
        var granted = new List<QualificationMutationEvent>();
        await _mutations.WaitAsync();
        try
        {
            if (!_loaded) throw new QualificationValidationException("storage");
            var current = VolatileSnapshot();
            if (!IsManagement(actor, current)) throw new QualificationPermissionException();
            if (current.Migrations.Contains(plan.Key)) return;
            if (plan.Key != MigrationKey || plan.Revision != current.Revision || actor.Context.At - plan.At > TimeSpan.FromMinutes(10))
                throw new QualificationConflictException();
            var next = current.Clone();
            foreach (var (id, qualifications) in plan.Grants)
            {
                var player = Player(next, id, actor.Context.At);
                foreach (var qualification in qualifications.OrderBy(id => Array.IndexOf(QualificationRules.Levels, id) is var level && level >= 0 ? level : 3).ThenBy(id => id, StringComparer.Ordinal))
                {
                    if (player.Grants.ContainsKey(qualification)) continue;
                    Award(next, player, qualification, actor.Context, MigrationKey);
                    next.Audit.Add(new(Guid.NewGuid(), "MigrationGrant", actor.Context.Actor, id, actor.Context.At,
                        actor.Context.Round, actor.Context.Server, "", qualification, MigrationKey, JsonSerializer.Serialize(plan)));
                    granted.Add(new("Grant", id, qualification, actor.Context.Actor));
                }
            }
            next.Migrations.Add(plan.Key);
            next.Audit.Add(new(Guid.NewGuid(), "MigrationExecute", actor.Context.Actor, null, actor.Context.At,
                actor.Context.Round, actor.Context.Server, "", JsonSerializer.Serialize(plan.Grants), MigrationKey, plan.Key));
            next.Revision = current.Revision + 1;
            await _repository.Save(next, current.Revision);
            System.Threading.Volatile.Write(ref _cache, next);
            Available = true;
        }
        catch (Exception e) when (e is not QualificationPermissionException && e is not QualificationValidationException && e is not QualificationConflictException)
        { Available = false; StorageFailure?.Invoke(e); throw; }
        finally { _mutations.Release(); }
        foreach (var grant in granted) Changed?.Invoke(grant);
    }

    public async Task RecordParticipation(GovforParticipation participation)
    {
        await _mutations.WaitAsync();
        try
        {
            if (!_loaded) return;
            var current = VolatileSnapshot();
            if (!current.Roles.TryGetValue(participation.Job, out var role) || !role.Govfor || current.Participation.Any(p => p.Player == participation.Player && p.Round == participation.Round && p.Server == participation.Server && p.Job == participation.Job)) return;
            var next = current.Clone();
            next.Participation.Add(participation);
            Player(next, participation.Player, participation.At);
            next.Revision++;
            await _repository.Save(next, current.Revision);
            System.Threading.Volatile.Write(ref _cache, next);
            Available = true;
        }
        catch (Exception e) { Available = false; StorageFailure?.Invoke(e); }
        finally { _mutations.Release(); }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared._RuCM.Qualifications;

namespace Content.Server._RuCM.Qualifications;

public sealed record MigrationCandidate(Guid Player, Dictionary<string, double> TrackerHours);
public sealed record MigrationScan(int AccountsScanned, IReadOnlyList<MigrationCandidate> Candidates);
public sealed record MigrationPlan(string Key, long Revision, DateTimeOffset At, int AccountsScanned, Dictionary<Guid, HashSet<string>> Grants);
public sealed record QualificationMigrationSettings(Dictionary<string, HashSet<string>> Groups, Dictionary<string, string> Aliases);

public sealed partial class QualificationService
{
    public const string MigrationKey = "govfor-training-v1";

    /// <summary>
    /// One-time soft migration from historical role timers. Human GOVFOR service over three hours
    /// grants Enlisted; higher levels and professional clearances use their configured role groups.
    /// Synthetic evidence is rejected before aliases are folded.
    /// </summary>
    public MigrationPlan MigrationDryRun(IEnumerable<MigrationCandidate> candidates, DateTimeOffset at, int? accountsScanned = null)
    {
        var s = VolatileSnapshot();
        var unique = candidates.Where(c => c.Player != Guid.Empty).GroupBy(c => c.Player).Select(g => g.First()).ToArray();
        var grants = new Dictionary<Guid, HashSet<string>>();
        var scanned = accountsScanned ?? unique.Length;
        if (s.Migrations.Contains(MigrationKey)) return new(MigrationKey, s.Revision, at, scanned, grants);

        var humanGovforRoles = s.Roles.Values.Where(r => r.Govfor && !r.Synthetic).ToArray();
        var migrationRoles = humanGovforRoles.Where(r => r.Enabled && !s.CommandingOfficerJobs.Contains(r.JobId)).ToArray();

        // Reject synthetic evidence before alias folding, including aliases to human trackers.
        // Trackers shared by human and synthetic jobs cannot prove human service.
        var syntheticTrackers = s.Roles.Values.Where(r => r.Synthetic).Select(r => r.Tracker).Where(t => t.Length > 0).ToHashSet();
        var syntheticCanonical = syntheticTrackers.Select(t => s.TrackerAliases.GetValueOrDefault(t, t)).ToHashSet();

        foreach (var candidate in unique)
        {
            var earned = new HashSet<string>();
            var canonicalHours = candidate.TrackerHours
                .Where(t => !syntheticTrackers.Contains(t.Key) &&
                    !syntheticCanonical.Contains(s.TrackerAliases.GetValueOrDefault(t.Key, t.Key)))
                .GroupBy(t => s.TrackerAliases.GetValueOrDefault(t.Key, t.Key))
                .ToDictionary(g => g.Key, g => g.Sum(t => Math.Max(0, t.Value)));

            double Hours(IEnumerable<RoleRequirement> group)
            {
                return group.Select(r => r.Tracker)
                    .Where(t => t.Length > 0)
                    .Select(t => s.TrackerAliases.GetValueOrDefault(t, t))
                    .Distinct()
                    .Sum(t => canonicalHours.GetValueOrDefault(t));
            }

            IEnumerable<RoleRequirement> MigrationGroup(string groupId, IEnumerable<RoleRequirement> fallback)
            {
                if (!s.MigrationGroups.TryGetValue(groupId, out var jobs))
                    return fallback;
                return s.Roles.Values.Where(r => jobs.Contains(r.JobId) && !r.Synthetic && !s.CommandingOfficerJobs.Contains(r.JobId));
            }

            // Strictly more than three hours, summed across all human GOVFOR role trackers.
            if (Hours(humanGovforRoles) > 3)
                earned.Add("enlisted");

            if (Hours(MigrationGroup("sergeant", migrationRoles.Where(r => r.MinimumLevel == MilitaryLevel.Sergeant))) >= 5)
                earned.UnionWith(new[] { "enlisted", "sergeant" });

            if (Hours(MigrationGroup("officer", migrationRoles.Where(r => r.MinimumLevel == MilitaryLevel.Officer))) >= 10)
                earned.UnionWith(QualificationRules.Levels);

            foreach (var definition in s.Definitions.Values.Where(d =>
                         d.Enabled && !QualificationRules.Levels.Contains(d.Id) && d.Id != "commanding_officer"))
            {
                if (Hours(MigrationGroup(definition.Id, migrationRoles.Where(r => r.Professional.Contains(definition.Id)))) >= 5)
                    earned.Add(definition.Id);
            }

            // Never restore an existing active/suspended/revoked record through migration.
            if (s.Players.TryGetValue(candidate.Player, out var player))
                earned.ExceptWith(player.Grants.Keys);

            if (earned.Count > 0)
                grants[candidate.Player] = earned;
        }

        return new(MigrationKey, s.Revision, at, scanned, grants);
    }

    public Task<MigrationScan> ScanMigrationCandidates(CancellationToken cancel = default) =>
        _repository.ScanMigrationCandidates(cancel);

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
                        actor.Context.Round, actor.Context.Server, "", qualification, MigrationKey, plan.Key));
                    granted.Add(new("Grant", id, qualification, actor.Context.Actor));
                }
            }
            next.Migrations.Add(plan.Key);
            var counts = plan.Grants.Values.SelectMany(x => x).GroupBy(x => x)
                .ToDictionary(g => g.Key, g => g.Count());
            var summary = JsonSerializer.Serialize(new
            {
                plan.AccountsScanned,
                EligibleAccounts = plan.Grants.Count,
                Records = plan.Grants.Sum(x => x.Value.Count),
                Counts = counts
            });
            next.Audit.Add(new(Guid.NewGuid(), "MigrationExecute", actor.Context.Actor, null, actor.Context.At,
                actor.Context.Round, actor.Context.Server, "", summary, MigrationKey, plan.Key));
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
            if (!current.Roles.TryGetValue(participation.Job, out var role) || !role.Govfor || role.Synthetic || current.Participation.Any(p => p.Player == participation.Player && p.Round == participation.Round && p.Server == participation.Server && p.Job == participation.Job)) return;
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

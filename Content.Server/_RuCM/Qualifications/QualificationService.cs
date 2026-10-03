using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared._RuCM.Qualifications;

namespace Content.Server._RuCM.Qualifications;

public sealed record QualificationAuthority(TrainingContext Context, bool Administrator, bool Management,
    bool CurrentOfficer, bool CurrentCo, bool CurrentParticipant, Guid? VerifiedInitiator = null);
public sealed record QualificationMutationEvent(string Action, Guid Target, string Qualification, Guid Actor);

/// <summary>All mutations copy-on-write, serialize under a lock, persist atomically, then publish cache.</summary>
public sealed partial class QualificationService
{
    private readonly IRuCMQualificationRepository _repository;
    private readonly SemaphoreSlim _mutations = new(1, 1);
    private QualificationStore _cache = new();
    private QualificationStore _seed = new();
    private bool _loaded;
    public bool Available { get; private set; }
    public event Action<QualificationMutationEvent>? Changed;
    public event Action<Exception>? StorageFailure;
    public QualificationService(IRuCMQualificationRepository repository) { _repository = repository; }
    public QualificationStore Snapshot() => Volatile.Read(ref _cache).Clone();

    public async Task Initialize(QualificationStore seed)
    {
        _seed = seed.Clone();
        await _mutations.WaitAsync();
        try
        {
            var loaded = await _repository.Load();
            if (loaded == null)
            {
                loaded = seed.Clone();
                loaded.Revision = 1;
                await _repository.Save(loaded, 0);
            }
            Volatile.Write(ref _cache, loaded);
            _loaded = true;
            Available = true;
        }
        catch (Exception e) { Available = false; StorageFailure?.Invoke(e); }
        finally { _mutations.Release(); }
    }

    public async Task Refresh()
    {
        if (!_loaded) { await Initialize(_seed); return; }
        await _mutations.WaitAsync();
        try
        {
            var loaded = await _repository.Load() ?? throw new QualificationValidationException("storage");
            Volatile.Write(ref _cache, loaded); Available = true;
        }
        catch (Exception e) { Available = false; StorageFailure?.Invoke(e); }
        finally { _mutations.Release(); }
    }

    public JobEligibility CanTakeJob(Guid player, string job)
    {
        var cache = Volatile.Read(ref _cache);
        cache.Players.TryGetValue(player, out var state);
        cache.Roles.TryGetValue(job, out var requirement);
        return QualificationRules.CanTakeJob(state, requirement);
    }
    public bool HasCachedPlayer(Guid player) => Volatile.Read(ref _cache).Players.ContainsKey(player);
    public PlayerTrainingState? GetPlayerTrainingState(Guid player) => Snapshot().Players.GetValueOrDefault(player);

    public bool IsManagement(QualificationAuthority actor, QualificationStore? store = null) =>
        actor.Administrator || actor.Management || (store ?? Volatile.Read(ref _cache)).Management.Contains(actor.Context.Actor);
    private bool CanTrain(QualificationAuthority actor, QualificationStore store, string id) => IsManagement(actor, store) ||
        actor.CurrentParticipant && QualificationRules.CanTrain(store.Instructors.GetValueOrDefault(actor.Context.Actor), id);

    private static PlayerTrainingState Player(QualificationStore store, Guid target, DateTimeOffset at)
    {
        if (target == Guid.Empty) throw new QualificationValidationException("target");
        if (!store.Players.TryGetValue(target, out var player))
            store.Players.Add(target, player = new PlayerTrainingState { Player = target, FirstTrainingAt = at });
        return player;
    }
    private static void Reason(string text)
    { if (string.IsNullOrWhiteSpace(text) || text.Length > 2000) throw new QualificationValidationException("reason"); }
    private static void Id(string id)
    { if (!Regex.IsMatch(id, "^[a-z][a-z0-9_]{0,63}$")) throw new QualificationValidationException("id"); }

    public async Task Apply(QualificationAuthority actor, QualificationAction action, QualificationRequest request)
    {
        if (request.Qualification is null || request.Item is null || request.Reason is null || request.Payload is null)
            throw new QualificationValidationException("request");
        await _mutations.WaitAsync();
        QualificationMutationEvent? changed = null;
        try
        {
            if (!_loaded) throw new QualificationValidationException("storage");
            var current = Volatile.Read(ref _cache);
            var next = current.Clone();
            var context = actor.Context;
            var manager = IsManagement(actor, next);
            var training = CanTrain(actor, next, request.Qualification);
            var oldState = JsonSerializer.Serialize(current.Players.GetValueOrDefault(request.Target));
            var metadata = request.Qualification;
            switch (action)
            {
                case QualificationAction.Complete:
                case QualificationAction.Certify:
                    if (!training || request.Target == context.Actor) throw new QualificationPermissionException();
                    if (!next.Definitions.TryGetValue(request.Qualification, out var definition) || !definition.Enabled)
                        throw new QualificationValidationException("definition");
                    var student = Player(next, request.Target, context.At);
                    if (action == QualificationAction.Complete)
                    {
                        if (!definition.Items.Any(i => i.Id == request.Item && i.Enabled)) throw new QualificationValidationException("item");
                        if (request.Reason.Length > 2000) throw new QualificationValidationException("reason");
                        if (!student.Progress.TryGetValue(definition.Id, out var progress))
                            student.Progress.Add(definition.Id, progress = new());
                        if (!progress.TryAdd(request.Item, new(definition.Id, request.Item, context, request.Reason))) return;
                        metadata = JsonSerializer.Serialize(progress[request.Item]);
                    }
                    else
                    {
                        if (!QualificationRules.ChecklistComplete(student, definition)) throw new QualificationValidationException("checklist");
                        if (student.Grants.ContainsKey(definition.Id)) throw new QualificationValidationException("already_granted");
                        Award(next, student, definition.Id, context, request.Reason);
                    }
                    break;
                case QualificationAction.Note:
                    if (!manager && !(actor.CurrentParticipant && next.Instructors.TryGetValue(context.Actor, out var instructor) && instructor.Active))
                        throw new QualificationPermissionException();
                    Reason(request.Reason);
                    Player(next, request.Target, context.At);
                    next.Notes.Add(new(Guid.NewGuid(), request.Target, context, request.Reason));
                    break;
                case QualificationAction.Grant:
                case QualificationAction.Revoke:
                case QualificationAction.Restore:
                    if (!manager) throw new QualificationPermissionException();
                    Reason(request.Reason);
                    var target = Player(next, request.Target, context.At);
                    if (!next.Definitions.ContainsKey(request.Qualification)) throw new QualificationValidationException("definition");
                    if (action == QualificationAction.Grant)
                    {
                        if (target.Grants.ContainsKey(request.Qualification)) throw new QualificationValidationException("already_granted");
                        Award(next, target, request.Qualification, context, request.Reason);
                    }
                    else
                    {
                        if (!target.Grants.TryGetValue(request.Qualification, out var grant)) throw new QualificationValidationException("grant");
                        target.Grants[request.Qualification] = grant with { Status = action == QualificationAction.Revoke ? QualificationStatus.Revoked : QualificationStatus.Active };
                        foreach (var suspension in next.Suspensions.Where(s => s.Target == request.Target && s.Qualification == request.Qualification && s.Status is "active" or "pending"))
                        {
                            suspension.Status = action == QualificationAction.Restore ? "resolved" : "revoked";
                            suspension.ResolvedAt = context.At; suspension.ResolvedBy = context.Actor; suspension.ResolutionReason = request.Reason;
                        }
                    }
                    break;
                case QualificationAction.Suspend:
                    if (!actor.Administrator && !actor.CurrentCo && !actor.CurrentOfficer) throw new QualificationPermissionException();
                    Reason(request.Reason);
                    if (request.Target == context.Actor) throw new QualificationPermissionException();
                    var suspended = Player(next, request.Target, context.At);
                    if (!suspended.Grants.TryGetValue(request.Qualification, out var active) || active.Status != QualificationStatus.Active)
                        throw new QualificationValidationException("grant");
                    foreach (var expired in next.Suspensions.Where(s => s.Target == request.Target && s.Qualification == request.Qualification && s.Status == "pending" && (s.Initiator.Round != context.Round || s.Initiator.Server != context.Server)))
                    {
                        expired.Status = "expired"; expired.ResolvedAt = context.At; expired.ResolvedBy = context.Actor;
                        expired.ResolutionReason = "round_changed";
                        next.Audit.Add(new(Guid.NewGuid(), "SuspensionExpired", context.Actor, request.Target, context.At, context.Round, context.Server, "pending", "expired", "round_changed", expired.Id.ToString()));
                    }
                    if (next.Suspensions.Any(s => s.Target == request.Target && s.Qualification == request.Qualification && s.Status is "pending" or "active")) return;
                    var record = new Suspension { Target = request.Target, Qualification = request.Qualification, Initiator = context,
                        Reason = request.Reason, Status = actor.Administrator || actor.CurrentCo ? "active" : "pending" };
                    next.Suspensions.Add(record);
                    metadata = JsonSerializer.Serialize(record);
                    if (record.Status == "active") suspended.Grants[request.Qualification] = active with { Status = QualificationStatus.Suspended };
                    break;
                case QualificationAction.ConfirmSuspension:
                    if (!actor.CurrentOfficer && !actor.CurrentCo) throw new QualificationPermissionException();
                    var pending = next.Suspensions.SingleOrDefault(s => s.Id == request.Suspension);
                    if (pending == null || pending.Status != "pending" || pending.Initiator.Actor == context.Actor || pending.Target == context.Actor)
                        throw new QualificationPermissionException();
                    if (actor.VerifiedInitiator != pending.Initiator.Actor) throw new QualificationPermissionException();
                    // Integration re-resolves initiator's current-round authority before allowing this action.
                    if (pending.Initiator.Round != context.Round || pending.Initiator.Server != context.Server)
                        throw new QualificationPermissionException();
                    var pendingPlayer = Player(next, pending.Target, context.At);
                    if (!pendingPlayer.Grants.TryGetValue(pending.Qualification, out var pendingGrant) || pendingGrant.Status != QualificationStatus.Active)
                        throw new QualificationValidationException("grant");
                    pending.Second = context; pending.Status = "active";
                    pendingPlayer.Grants[pending.Qualification] = pendingGrant with { Status = QualificationStatus.Suspended };
                    request.Target = pending.Target; request.Qualification = pending.Qualification;
                    oldState = JsonSerializer.Serialize(current.Players.GetValueOrDefault(pending.Target));
                    metadata = JsonSerializer.Serialize(pending);
                    break;
                case QualificationAction.SaveRole:
                case QualificationAction.SaveDefinition:
                case QualificationAction.SaveInstructor:
                case QualificationAction.SaveManagement:
                case QualificationAction.SaveCommandJobs:
                case QualificationAction.SaveMigrationSettings:
                case QualificationAction.CorrectProgress:
                    if (!manager) throw new QualificationPermissionException();
                    Reason(request.Reason);
                    if (request.Revision != current.Revision) throw new QualificationConflictException();
                    Configure(next, actor, action, request);
                    oldState = JsonSerializer.Serialize(new { current.Roles, current.Definitions, current.Instructors, current.Management, current.OfficerJobs, current.CommandingOfficerJobs, current.MigrationGroups, current.TrackerAliases });
                    metadata = request.Payload;
                    break;
                default: throw new QualificationValidationException("action");
            }
            var newState = JsonSerializer.Serialize(next.Players.GetValueOrDefault(request.Target));
            if (action is QualificationAction.SaveRole or QualificationAction.SaveDefinition or QualificationAction.SaveInstructor or QualificationAction.SaveManagement or QualificationAction.SaveCommandJobs or QualificationAction.SaveMigrationSettings)
                newState = JsonSerializer.Serialize(new { next.Roles, next.Definitions, next.Instructors, next.Management, next.OfficerJobs, next.CommandingOfficerJobs, next.MigrationGroups, next.TrackerAliases });
            next.Audit.Add(new(Guid.NewGuid(), action.ToString(), context.Actor, request.Target == Guid.Empty ? null : request.Target,
                context.At, context.Round, context.Server, oldState, newState, request.Reason, metadata));
            next.Revision = current.Revision + 1;
            await _repository.Save(next, current.Revision);
            Volatile.Write(ref _cache, next);
            Available = true;
            var eventAction = action == QualificationAction.Suspend && next.Suspensions.Last().Status == "pending" ? "SuspensionRequested" : action.ToString();
            changed = new(eventAction, request.Target, request.Qualification, context.Actor);
        }
        catch (QualificationConflictException)
        {
            // Another server won the CAS: refresh, never overwrite it silently.
            try { var fresh = await _repository.Load(); if (fresh != null) Volatile.Write(ref _cache, fresh); }
            catch (Exception e) { StorageFailure?.Invoke(e); }
            throw;
        }
        catch (Exception e) when (e is not QualificationPermissionException && e is not QualificationValidationException)
        { Available = false; StorageFailure?.Invoke(e); throw; }
        finally { _mutations.Release(); }
        if (changed != null) Changed?.Invoke(changed);
    }

    private static void Award(QualificationStore store, PlayerTrainingState player, string id, TrainingContext context, string reason)
    {
        var definition = store.Definitions[id];
        player.Grants[id] = new(id, QualificationStatus.Active, definition.Version,
            definition.Items.Where(i => i.Required && i.Enabled).Select(i => i.Id).ToArray(), context, reason);
        var level = Array.IndexOf(QualificationRules.Levels, id);
        for (var i = 0; i < level; i++)
        {
            var lower = QualificationRules.Levels[i];
            if (!player.Grants.ContainsKey(lower)) Award(store, player, lower, context, reason);
        }
    }

    private static void Configure(QualificationStore next, QualificationAuthority actor, QualificationAction action, QualificationRequest request)
    {
        switch (action)
        {
            case QualificationAction.SaveRole:
                var role = JsonSerializer.Deserialize<RoleRequirement>(request.Payload) ?? throw new QualificationValidationException("role");
                if (string.IsNullOrWhiteSpace(role.JobId) || role.Professional is null || !Enum.IsDefined(role.MinimumLevel) || role.Professional.Any(id => !next.Definitions.ContainsKey(id) || QualificationRules.Levels.Contains(id)))
                    throw new QualificationValidationException("role");
                next.Roles[role.JobId] = role;
                break;
            case QualificationAction.SaveDefinition:
                var definition = JsonSerializer.Deserialize<QualificationDefinition>(request.Payload) ?? throw new QualificationValidationException("definition");
                if (definition.Id is null || definition.Name is null || definition.Description is null || definition.Items is null || definition.Items.Any(i => i is null || i.Id is null || i.Name is null || i.Description is null)) throw new QualificationValidationException("definition");
                Id(definition.Id);
                if (definition.Name.Length > 200 || definition.Description.Length > 4000 || definition.Items.Count > 200 || definition.Items.Select(i => i.Id).Distinct().Count() != definition.Items.Count)
                    throw new QualificationValidationException("definition");
                foreach (var item in definition.Items) { Id(item.Id); if (item.Name.Length > 200 || item.Description.Length > 4000) throw new QualificationValidationException("item"); }
                if (next.Definitions.TryGetValue(definition.Id, out var previous))
                {
                    definition.Version = previous.Version + 1;
                    // Retain removed stable IDs, disabled, for history and rename-safe completion.
                    foreach (var old in previous.Items.Where(i => definition.Items.All(n => n.Id != i.Id)))
                    { old.Enabled = false; definition.Items.Add(old); }
                }
                else if (QualificationRules.Levels.Contains(definition.Id)) throw new QualificationValidationException("definition");
                else definition.Version = 1;
                next.Definitions[definition.Id] = definition;
                break;
            case QualificationAction.SaveInstructor:
                var accreditation = JsonSerializer.Deserialize<InstructorAccreditation>(request.Payload) ?? throw new QualificationValidationException("instructor");
                if (accreditation.Professional is null || accreditation.Professional.Any(id => !next.Definitions.ContainsKey(id) || QualificationRules.Levels.Contains(id) || id == "commanding_officer"))
                    throw new QualificationValidationException("instructor");
                var existing = next.Instructors.GetValueOrDefault(request.Target);
                next.Instructors[request.Target] = accreditation with { GrantedBy = existing?.GrantedBy ?? actor.Context.Actor,
                    GrantedAt = existing?.GrantedAt ?? actor.Context.At, ModifiedBy = actor.Context.Actor, ModifiedAt = actor.Context.At };
                break;
            case QualificationAction.SaveManagement:
                var acl = JsonSerializer.Deserialize<HashSet<Guid>>(request.Payload) ?? throw new QualificationValidationException("acl");
                if (acl.Contains(Guid.Empty)) throw new QualificationValidationException("acl");
                next.Management = acl;
                break;
            case QualificationAction.SaveCommandJobs:
                var jobs = JsonSerializer.Deserialize<CommandJobs>(request.Payload) ?? throw new QualificationValidationException("jobs");
                if (jobs.Officer is null || jobs.CommandingOfficer is null) throw new QualificationValidationException("jobs");
                next.OfficerJobs = jobs.Officer; next.CommandingOfficerJobs = jobs.CommandingOfficer;
                break;
            case QualificationAction.SaveMigrationSettings:
                var settings = JsonSerializer.Deserialize<QualificationMigrationSettings>(request.Payload) ?? throw new QualificationValidationException("migration_settings");
                if (settings.Groups is null || settings.Aliases is null || settings.Groups.Values.Any(g => g is null)) throw new QualificationValidationException("migration_settings");
                if (settings.Groups.Keys.Any(id => id != "sergeant" && id != "officer" && (!next.Definitions.ContainsKey(id) || QualificationRules.Levels.Contains(id))) ||
                    settings.Groups.Values.SelectMany(x => x).Any(j => !next.Roles.ContainsKey(j)) || settings.Aliases.Any(a => string.IsNullOrWhiteSpace(a.Key) || string.IsNullOrWhiteSpace(a.Value) || a.Key.Length > 200 || a.Value.Length > 200))
                    throw new QualificationValidationException("migration_settings");
                next.MigrationGroups = settings.Groups; next.TrackerAliases = settings.Aliases;
                break;
            case QualificationAction.CorrectProgress:
                if (!next.Definitions.TryGetValue(request.Qualification, out var checklist) || checklist.Items.All(i => i.Id != request.Item))
                    throw new QualificationValidationException("item");
                Player(next, request.Target, actor.Context.At).Progress.GetValueOrDefault(request.Qualification)?.Remove(request.Item);
                break;
        }
    }

    public Dictionary<string, double> Metrics()
    {
        var s = Volatile.Read(ref _cache);
        var result = new Dictionary<string, double>();
        foreach (var level in Enum.GetValues<MilitaryLevel>()) result["level_" + level] = s.Players.Values.Count(p => QualificationRules.EffectiveLevel(p) == level);
        foreach (var id in s.Definitions.Keys.Except(QualificationRules.Levels))
            result["professional_" + id] = s.Players.Values.Count(p => p.Grants.TryGetValue(id, out var g) && g.Status == QualificationStatus.Active);
        result["instructors"] = s.Instructors.Values.Count(i => i.Active);
        result["completions"] = s.Audit.Count(a => a.Action == QualificationAction.Complete.ToString());
        result["certifications"] = s.Audit.Count(a => a.Action == QualificationAction.Certify.ToString());
        result["suspensions"] = s.Suspensions.Count(x => x.Status == "active");
        foreach (var id in s.Definitions.Keys) result["suspensions_" + id] = s.Suspensions.Count(x => x.Qualification == id && x.Status == "active");
        result["restorations"] = s.Audit.Count(a => a.Action == QualificationAction.Restore.ToString());
        result["revocations"] = s.Audit.Count(a => a.Action == QualificationAction.Revoke.ToString());
        result["migration_grants"] = s.Audit.Count(a => a.Action == "MigrationGrant");
        result["started_training"] = s.Players.Values.Count(p => p.Progress.Count > 0);
        result["completed_training"] = s.Players.Values.Count(p => p.Grants.Count > 0);
        var enlisted = s.Players.Values.Where(p => p.Grants.ContainsKey("enlisted") && p.Progress.Count > 0).ToArray();
        result["average_enlisted_hours"] = enlisted.Length == 0 ? 0 : enlisted.Average(p => Math.Max(0, (p.Grants["enlisted"].Issuer.At - p.FirstTrainingAt).TotalHours));
        result["average_enlisted_rounds"] = enlisted.Length == 0 ? 0 : enlisted.Average(p => (double) p.Progress.Values.SelectMany(x => x.Values).Select(x => (x.By.Server, x.By.Round)).Distinct().Count());
        return result;
    }
}

public sealed record CommandJobs(HashSet<string> Officer, HashSet<string> CommandingOfficer);
public sealed class QualificationPermissionException : Exception;
public sealed class QualificationValidationException : Exception
{ public string Code { get; } public QualificationValidationException(string code) { Code = code; } }

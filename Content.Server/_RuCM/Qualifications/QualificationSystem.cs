using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Station.Events;
using Content.Shared._RuCM.Qualifications;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.CMU14.Round.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;
using Robust.Shared.Log;
using static Content.Server.GameTicking.GameTicker;

namespace Content.Server._RuCM.Qualifications;

public sealed class QualificationSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IResourceManager _resources = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private IAdminManager _admins = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private EuiManager _euis = default!;
    [Dependency] private SharedMindSystem _minds = default!;
    [Dependency] private SharedJobSystem _jobs = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private GameTicker _ticker = default!;

    public QualificationService Service { get; private set; } = default!;
    private readonly Queue<Action> _queue = new();
    private readonly Dictionary<Guid, DateTimeOffset> _lastRequest = new();
    private readonly Dictionary<Guid, (string Token, MigrationPlan Plan)> _previews = new();
    private readonly List<QualificationEui> _open = new();
    private readonly Dictionary<EntityUid, Guid> _boundTargets = new();
    private readonly System.Collections.Concurrent.ConcurrentQueue<QualificationMutationEvent> _events = new();
    private Task? _running;
    private Action? _finished;
    private bool _ready;
    private ISawmill _log = default!;
    private DateTimeOffset _nextRefresh;
    private bool _refreshStorage;
    private bool _warnedUnavailable;

    public QualificationMode Mode => !_cfg.GetCVar(QualificationCVars.Enabled) ? QualificationMode.Disabled :
        _cfg.GetCVar(QualificationCVars.Enforce) ? QualificationMode.Enforce : QualificationMode.Warn;

    public override void Initialize()
    {
        base.Initialize();
        _log = Logger.GetSawmill("rucm.qualifications");
        SubscribeLocalEvent<StationJobsGetCandidatesEvent>(OnCandidates);
        SubscribeLocalEvent<GetDisallowedJobsEvent>(OnDisallowed);
        SubscribeLocalEvent<IsRoleAllowedEvent>(OnAllowed);
        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(OnBeforeSpawn);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawnComplete);
        SubscribeLocalEvent<QualificationUiComponent, QualificationBoundRequest>(OnBoundRequest);
        SubscribeLocalEvent<QualificationUiComponent, BoundUserInterfaceMessageAttempt>(OnBoundAttempt);
        // No connection is exposed by upstream's repository API; use a confidential RuCM configuration.
        var connection = _cfg.GetCVar(QualificationCVars.Connection);
        _refreshStorage = !string.IsNullOrWhiteSpace(connection);
        Service = new(new PostgresQualificationRepository(connection));
        Service.StorageFailure += _ => _log.Fatal("RuCM qualification storage unavailable; last-known cache retained. Check RuCM database configuration.");
        Service.Changed += e => _events.Enqueue(e);
        _running = InitializeStorage();
        _finished = () => _ready = true;
    }

    private async Task InitializeStorage()
    {
        try
        {
            using var reader = _resources.ContentFileReadText(new ResPath("/RuCM/Qualifications/seed.json"));
            var seed = JsonSerializer.Deserialize<QualificationStore>(reader.ReadToEnd()) ?? new();
            // Derive trackers and validate every seed role against actual loaded prototypes.
            foreach (var role in seed.Roles.Values)
            {
                if (!ProtoMan.TryIndex<JobPrototype>(role.JobId, out var job)) throw new QualificationValidationException("seed_job");
                role.Tracker = job.PlayTimeTracker.Id;
                role.Govfor = job.RoundSide == RoundJobSide.Govfor;
            }
            foreach (var job in ProtoMan.EnumeratePrototypes<JobPrototype>())
                seed.Roles.TryAdd(job.ID, new RoleRequirement { JobId = job.ID, Enabled = false, Tracker = job.PlayTimeTracker.Id, Govfor = job.RoundSide == RoundJobSide.Govfor });
            if (string.IsNullOrWhiteSpace(_cfg.GetCVar(QualificationCVars.Connection)))
            {
                if (Mode == QualificationMode.Disabled) _log.Info("RuCM qualification persistence is not configured; feature disabled.");
                else _log.Fatal("RuCM qualification persistence is not configured. Following configured failure policy.");
                return;
            }
            await Service.Initialize(seed);
        }
        catch { _log.Fatal("RuCM qualifications seed/storage initialization failed. Existing jobs continue under configured failure policy."); }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (_running is { IsCompleted: true })
        {
            if (_running.IsFaulted) Log.Error("RuCM qualifications operation failed; no uncommitted state published.");
            _running = null;
            var callback = _finished; _finished = null; callback?.Invoke();
            foreach (var eui in _open.ToArray()) if (!eui.IsShutDown) eui.StateDirty();
        }
        while (_events.TryDequeue(out var e))
        {
            Log.Info($"RuCM qualifications {e.Action}: actor={e.Actor} target={e.Target} qualification={e.Qualification}");
            switch (e.Action)
            {
                case "Grant": case "Certify": RaiseLocalEvent(new RuCMQualificationGrantedEvent(e.Target, e.Qualification, e.Actor)); break;
                case "Suspend": case "ConfirmSuspension": RaiseLocalEvent(new RuCMQualificationSuspendedEvent(e.Target, e.Qualification, e.Actor)); break;
                case "Restore": RaiseLocalEvent(new RuCMQualificationRestoredEvent(e.Target, e.Qualification, e.Actor)); break;
                case "Complete": RaiseLocalEvent(new RuCMChecklistItemCompletedEvent(e.Target, e.Qualification, e.Actor)); break;
            }
        }
        if (_ready && _running == null && _queue.TryDequeue(out var action)) action();
        if (_ready && _running == null && _refreshStorage && DateTimeOffset.UtcNow >= _nextRefresh)
        {
            _nextRefresh = DateTimeOffset.UtcNow.AddSeconds(30);
            _running = Service.Refresh();
        }
    }

    public bool CanTakeJob(Guid player, string job)
    {
        if (Mode == QualificationMode.Disabled) return true;
        if (Service.Available) _warnedUnavailable = false;
        if (!Service.Available && !Service.HasCachedPlayer(player))
        {
            if (!_warnedUnavailable) { _warnedUnavailable = true; _log.Fatal("RuCM qualification gate has no cached user data; following configured failure policy."); }
            return _cfg.GetCVar(QualificationCVars.FailOpen);
        }
        return Mode != QualificationMode.Enforce || Service.CanTakeJob(player, job).Allowed;
    }
    private void OnCandidates(ref StationJobsGetCandidatesEvent ev)
    { var player = ev.Player; if (Mode == QualificationMode.Enforce) ev.Jobs.RemoveAll(j => !CanTakeJob(player.UserId, j.Id)); }
    private void OnDisallowed(ref GetDisallowedJobsEvent ev)
    {
        if (Mode != QualificationMode.Enforce) return;
        foreach (var job in ProtoMan.EnumeratePrototypes<JobPrototype>())
            if (!CanTakeJob(ev.Player.UserId, job.ID)) ev.Jobs.Add(job.ID);
    }
    private void OnAllowed(ref IsRoleAllowedEvent ev)
    {
        if (ev.Jobs == null || Mode != QualificationMode.Enforce) return;
        foreach (var job in ev.Jobs) if (!CanTakeJob(ev.Player.UserId, job.Id)) ev.Cancelled = true;
    }
    private void OnBeforeSpawn(PlayerBeforeSpawnEvent ev)
    {
        if (Mode == QualificationMode.Disabled || ev.JobId == null) return;
        var eligibility = Service.CanTakeJob(ev.Player.UserId, ev.JobId);
        if (eligibility.Allowed || !Service.Available && !Service.HasCachedPlayer(ev.Player.UserId) && _cfg.GetCVar(QualificationCVars.FailOpen)) return;
        _chat.DispatchServerMessage(ev.Player, Loc.GetString("rucm-qualifications-job-denied", ("requirements", string.Join(", ", eligibility.Missing.Select(DisplayQualification)))));
        Log.Warning($"RuCM qualifications job violation: player={ev.Player.UserId}, job={ev.JobId}, mode={Mode}");
        if (Mode == QualificationMode.Enforce) ev.JobId = null;
    }
    private string DisplayQualification(string id)
    {
        var definition = Service.Snapshot().Definitions.GetValueOrDefault(id);
        return definition == null ? id : Loc.TryGetString(definition.Name, out var name) ? name : definition.Name;
    }
    private void OnSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (ev.JobId == null || _queue.Count >= 1024) return;
        var participation = new GovforParticipation(ev.Player.UserId, ev.JobId, DateTimeOffset.UtcNow,
            GameTicker.GetRoundId(EntityManager.EntitySysManager), _cfg.GetCVar(QualificationCVars.ServerId));
        _queue.Enqueue(() => _running = Service.RecordParticipation(participation));
    }

    public QualificationAuthority Authority(ICommonSession player, Guid? verified = null)
    {
        var snapshot = Service.Snapshot();
        var job = "";
        if (_minds.TryGetMind(player.UserId, out var mind, out var mindComp) && _jobs.MindTryGetJobId(mind, out var prototype)) job = prototype?.Id ?? "";
        var participant = _ticker.RunLevel == GameRunLevel.InRound && player.AttachedEntity != null && mindComp?.OwnedEntity == player.AttachedEntity;
        return new(new(player.UserId, _minds.GetCharacterName(player.UserId) ?? "", job, GameTicker.GetRoundId(EntityManager.EntitySysManager),
            _cfg.GetCVar(QualificationCVars.ServerId), DateTimeOffset.UtcNow), _admins.HasAdminFlag(player, AdminFlags.Host),
            snapshot.Management.Contains(player.UserId), participant && snapshot.OfficerJobs.Contains(job),
            participant && snapshot.CommandingOfficerJobs.Contains(job), participant, verified);
    }

    /// <summary>Server-side repository injection for isolated hosts and integration fixtures. No client route.</summary>
    public void ConfigureRepository(IRuCMQualificationRepository repository, QualificationStore seed)
    {
        if (_running is { IsCompleted: false }) throw new InvalidOperationException("Qualification operation in progress");
        _ready = false;
        _refreshStorage = false;
        Service = new(repository);
        Service.StorageFailure += _ => _log.Fatal("RuCM qualification storage unavailable; last-known cache retained.");
        Service.Changed += e => _events.Enqueue(e);
        _running = Service.Initialize(seed);
        _finished = () => _ready = true;
    }

    public void BootstrapManagement(Guid target, bool enabled, string reason, Action<string> done)
    {
        if (_queue.Count >= 1024) { done("rate_limit"); return; }
        _queue.Enqueue(() =>
        {
            var snapshot = Service.Snapshot();
            if (enabled) snapshot.Management.Add(target); else snapshot.Management.Remove(target);
            var authority = new QualificationAuthority(new(Guid.Empty, "server console", "", GameTicker.GetRoundId(EntityManager.EntitySysManager),
                _cfg.GetCVar(QualificationCVars.ServerId), DateTimeOffset.UtcNow), true, false, false, false, false);
            var request = new QualificationRequest { Target = target, Revision = snapshot.Revision, Reason = reason, Payload = JsonSerializer.Serialize(snapshot.Management) };
            var error = "";
            _running = Save(); _finished = () => done(error);
            async Task Save() { try { await Service.Apply(authority, QualificationAction.SaveManagement, request); } catch (Exception e) { error = Error(e); } }
        });
    }

    public QualificationView View(ICommonSession player, Guid target, string error = "")
    {
        var actor = Authority(player);
        var manager = Service.IsManagement(actor);
        var s = Service.Snapshot();
        var accreditation = s.Instructors.GetValueOrDefault(player.UserId);
        var instructor = actor.CurrentParticipant && accreditation is { Active: true };
        if (!manager && !instructor && !actor.CurrentOfficer && !actor.CurrentCo) target = player.UserId;
        if (target == Guid.Empty) target = player.UserId;
        var result = new QualificationView { Viewer = player.UserId, Target = target, Management = manager, Instructor = instructor,
            Officer = actor.CurrentOfficer, CommandingOfficer = actor.CurrentCo, Administrator = actor.Administrator, Available = Service.Available, Error = error };
        foreach (var job in ProtoMan.EnumeratePrototypes<JobPrototype>()) result.Jobs[job.ID] = job.LocalizedName;
        if (manager)
        {
            result.Store = s; result.Metrics = Service.Metrics();
            // Send only the selected player's private history, not the entire player database.
            result.Store.Players = s.Players.Where(p => p.Key == target).ToDictionary(p => p.Key, p => p.Value);
            result.Store.Notes = s.Notes.Where(n => n.Target == target).ToList();
            result.Store.Suspensions = s.Suspensions.Where(x => x.Target == target || x.Status == "pending").ToList();
            result.Store.Audit = s.Audit.Where(a => a.Target == target || a.Target == null).TakeLast(100).ToList();
            result.Store.Participation.Clear();
            if (_previews.TryGetValue(player.UserId, out var preview))
            { result.PreviewToken = preview.Token; result.Preview = new() { Counts = preview.Plan.Grants.Values.SelectMany(x => x).GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count()), Records = preview.Plan.Grants.Sum(x => x.Value.Count) }; }
        }
        else
        {
            result.Store.Revision = s.Revision;
            result.Store.Definitions = s.Definitions;
            result.Store.Roles = s.Roles;
            if (s.Players.TryGetValue(target, out var training)) result.Store.Players[target] = training;
            result.Store.Suspensions = s.Suspensions.Where(x => x.Target == target || (actor.CurrentOfficer || actor.CurrentCo) && x.Status == "pending").ToList();
            if (instructor) result.Store.Instructors[player.UserId] = accreditation!;
            if (instructor) result.Store.Notes = s.Notes.Where(n => n.Target == target).ToList();
        }
        if (manager || instructor || actor.CurrentOfficer || actor.CurrentCo)
            foreach (var online in _players.Sessions.Where(p => p.AttachedEntity != null)) result.OnlinePlayers[online.UserId] = online.Name;
        return result;
    }

    public void Open(ICommonSession player, bool bound = false)
    {
        if (bound && player.AttachedEntity is { } entity)
        {
            EnsureComp<QualificationUiComponent>(entity);
            _ui.SetUi(entity, QualificationUiKey.Main, new InterfaceData("Content.Client._RuCM.Qualifications.QualificationBoundUserInterface"));
            _boundTargets[entity] = player.UserId;
            _ui.OpenUi(entity, QualificationUiKey.Main, entity);
            return;
        }
        var eui = new QualificationEui(this); _open.Add(eui); _euis.OpenEui(eui, player);
    }
    public void Remove(QualificationEui eui) => _open.Remove(eui);
    private void OnBoundAttempt(EntityUid uid, QualificationUiComponent comp, BoundUserInterfaceMessageAttempt message)
    { if (Equals(message.UiKey, QualificationUiKey.Main) && message.Actor != uid) message.Cancel(); }
    private void OnBoundRequest(EntityUid uid, QualificationUiComponent comp, QualificationBoundRequest message)
    {
        if (message.Actor != uid || !TryComp<ActorComponent>(message.Actor, out var actor)) return;
        if (message.Request is { } parsed && parsed.Target != Guid.Empty) _boundTargets[uid] = parsed.Target;
        Submit(actor.PlayerSession, message.Action, message.Request, error =>
        {
            // Confidential training notes/audit never enter PVS-replicated BUI component state.
            if (!Deleted(uid)) _ui.ServerSendUiMessage(uid, QualificationUiKey.Main,
                new QualificationBoundView(View(actor.PlayerSession, _boundTargets.GetValueOrDefault(uid), error)), actor.PlayerSession);
        });
    }

    /// <summary>Normalize untrusted engine DTOs at the server boundary; authorization stays in Submit/Apply.</summary>
    public void Submit(ICommonSession player, QualificationAction action, QualificationRequest? request, Action<string> done)
    {
        if (request == null) { done("request"); return; }
        string json;
        try
        {
            var payload = request.Payload;
            if (request.Configuration is { } config)
            {
                payload = action switch
                {
                    QualificationAction.SaveRole => JsonSerializer.Serialize(config.Role),
                    QualificationAction.SaveDefinition => JsonSerializer.Serialize(config.Definition),
                    QualificationAction.SaveInstructor => JsonSerializer.Serialize(config.Instructor),
                    QualificationAction.SaveManagement => JsonSerializer.Serialize(config.Management),
                    QualificationAction.SaveCommandJobs => JsonSerializer.Serialize(new CommandJobs(config.OfficerJobs!, config.CommandingOfficerJobs!)),
                    QualificationAction.SaveMigrationSettings => JsonSerializer.Serialize(new QualificationMigrationSettings(config.MigrationGroups!, config.TrackerAliases!)),
                    QualificationAction.MigrationPreview => JsonSerializer.Serialize(config.Roster),
                    _ => payload
                };
            }
            // Snapshot requests before queueing, retaining the existing size/rate/permission checks.
            json = JsonSerializer.Serialize(new QualificationRequest
            {
                Target = request.Target, Qualification = request.Qualification, Item = request.Item, Reason = request.Reason,
                Suspension = request.Suspension, Revision = request.Revision, Payload = payload, PreviewToken = request.PreviewToken
            });
        }
        catch (Exception e) { done(Error(e)); return; }
        Submit(player, action, json, done);
    }

    public void Submit(ICommonSession player, QualificationAction action, string json, Action<string> done)
    {
        var now = DateTimeOffset.UtcNow;
        if (json.Length > 128000 || _queue.Count >= 1024 || _lastRequest.TryGetValue(player.UserId, out var previous) && now - previous < TimeSpan.FromMilliseconds(200))
        { done("rate_limit"); return; }
        _lastRequest[player.UserId] = now;
        _queue.Enqueue(() =>
        {
            if (player.Status == SessionStatus.Disconnected) return;
            string error = "";
            try
            {
                var request = JsonSerializer.Deserialize<QualificationRequest>(json) ?? throw new QualificationValidationException("request");
                if (request.Qualification is null || request.Item is null || request.Reason is null || request.Payload is null || request.PreviewToken is null || request.Qualification.Length > 64 || request.Item.Length > 64 || request.Reason.Length > 2000)
                    throw new QualificationValidationException("request");
                if (action == QualificationAction.View) { done(""); return; }
                var authority = Authority(player);
                if (action is QualificationAction.SaveRole or QualificationAction.SaveDefinition or QualificationAction.SaveInstructor or QualificationAction.SaveManagement or QualificationAction.SaveCommandJobs or QualificationAction.SaveMigrationSettings or QualificationAction.CorrectProgress or QualificationAction.Grant or QualificationAction.Restore or QualificationAction.Revoke or QualificationAction.MigrationPreview or QualificationAction.MigrationExecute)
                    if (!Service.IsManagement(authority)) throw new QualificationPermissionException();
                if (action == QualificationAction.SaveRole)
                {
                    var role = JsonSerializer.Deserialize<RoleRequirement>(request.Payload) ?? throw new QualificationValidationException("role");
                    if (!ProtoMan.TryIndex<JobPrototype>(role.JobId, out var job)) throw new QualificationValidationException("job");
                    role.Tracker = job.PlayTimeTracker.Id; request.Payload = JsonSerializer.Serialize(role);
                    role.Govfor = job.RoundSide == RoundJobSide.Govfor; request.Payload = JsonSerializer.Serialize(role);
                }
                if (action == QualificationAction.SaveMigrationSettings)
                {
                    var settings = JsonSerializer.Deserialize<QualificationMigrationSettings>(request.Payload) ?? throw new QualificationValidationException("migration_settings");
                    if (settings.Groups is null || settings.Aliases is null || settings.Groups.Values.Any(g => g is null)) throw new QualificationValidationException("migration_settings");
                    if (settings.Groups.Values.SelectMany(x => x).Any(j => !ProtoMan.HasIndex<JobPrototype>(j))) throw new QualificationValidationException("job");
                }
                if (action == QualificationAction.SaveCommandJobs)
                {
                    var jobs = JsonSerializer.Deserialize<CommandJobs>(request.Payload) ?? throw new QualificationValidationException("jobs");
                    if (jobs.Officer is null || jobs.CommandingOfficer is null) throw new QualificationValidationException("jobs");
                    if (jobs.Officer.Concat(jobs.CommandingOfficer).Any(j => !ProtoMan.HasIndex<JobPrototype>(j))) throw new QualificationValidationException("job");
                }
                if (action == QualificationAction.Complete || action == QualificationAction.Certify || action == QualificationAction.Note)
                    if (!Service.IsManagement(authority) && !_players.Sessions.Any(p => p.UserId == request.Target && Authority(p).CurrentParticipant)) throw new QualificationPermissionException();
                if (action == QualificationAction.ConfirmSuspension)
                {
                    var pending = Service.Snapshot().Suspensions.SingleOrDefault(s => s.Id == request.Suspension);
                    var initiator = _players.Sessions.FirstOrDefault(p => p.UserId == pending?.Initiator.Actor);
                    if (initiator == null || !Authority(initiator).CurrentOfficer) throw new QualificationPermissionException();
                    authority = Authority(player, initiator.UserId);
                }
                _running = Execute(authority, action, request);
            }
            catch (Exception e) { error = Error(e); }
            if (_running == null) { _finished = null; done(error); return; }

            async Task Execute(QualificationAuthority actor, QualificationAction operation, QualificationRequest request)
            {
                try
                {
                    if (operation == QualificationAction.MigrationPreview)
                    {
                        if (!Service.IsManagement(actor)) throw new QualificationPermissionException();
                        var roster = JsonSerializer.Deserialize<HashSet<Guid>>(request.Payload) ?? throw new QualificationValidationException("roster");
                        if (roster.Count > 10000) throw new QualificationValidationException("roster");
                        var candidates = new List<MigrationCandidate>();
                        foreach (var id in roster)
                        {
                            var times = await _db.GetPlayTimes(id);
                            candidates.Add(new(id, times.GroupBy(t => t.Tracker).ToDictionary(g => g.Key, g => g.Sum(t => t.TimeSpent.TotalHours))));
                        }
                        var plan = Service.MigrationDryRun(candidates, actor.Context.At);
                        _previews[player.UserId] = (Guid.NewGuid().ToString("N"), plan);
                    }
                    else if (operation == QualificationAction.MigrationExecute)
                    {
                        if (!_previews.TryGetValue(player.UserId, out var preview) || preview.Token != request.PreviewToken) throw new QualificationPermissionException();
                        await Service.ExecuteMigration(actor, preview.Plan); _previews.Remove(player.UserId);
                    }
                    else await Service.Apply(actor, operation, request);
                }
                catch (Exception e) { error = Error(e); }
            }
            _finished = () => done(error);
        });
    }
    private string Error(Exception e)
    {
        if (e is QualificationPermissionException) { _log.Warning("RuCM qualification authorization failure."); return "permission"; }
        if (e is QualificationConflictException) return "conflict";
        if (e is QualificationValidationException validation) return validation.Code;
        if (e is JsonException || e is ArgumentException) return "request";
        _log.Error($"RuCM qualification request failed ({e.GetType().Name}); stored state retained."); return "storage";
    }
}

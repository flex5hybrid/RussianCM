using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.GameTicking;
using Content.Server.Players.RateLimiting;
using Content.Shared.Administration;
using Content.Shared.Administration.AdminAI;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Guidebook;
using Content.Shared.Players.RateLimiting;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.CMU14.AdminAI;

public sealed class AdminAHelpAcceptedEvent(Guid channel, int round, bool fromAdmin, string text, bool adminOnly) : EntityEventArgs
{
    public readonly Guid Channel = channel;
    public readonly int Round = round;
    public readonly bool FromAdmin = fromAdmin, AdminOnly = adminOnly;
    public readonly string Text = text;
}

public sealed class AdminAHelpDeliveredEvent(AdminAHelpAcceptedEvent message) : EntityEventArgs
{
    public readonly AdminAHelpAcceptedEvent Message = message;
}

/// <summary>Private drafts and manually requested evidence reports. No disciplinary APIs or automatic draft publication.</summary>
public sealed partial class AdminAIDraftSystem : EntitySystem
{
    private const string RateKey = "AdminAIManualDraft";
    [Dependency] private IAdminManager _admins = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private IAdminAIService _inference = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IResourceManager _resources = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private ITaskManager _tasks = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IAdminAIRoundContext _round = default!;
    [Dependency] private PlayerRateLimitManager _rateLimit = default!;
    private AIDraftSessionStore _store = new(new());
    private AIRuleKnowledgeBase? _knowledge;
    private Task? _knowledgeTask;
    private ISawmill _audit = default!;
    private bool _dirty, _shuttingDown;
    private long _knowledgeGeneration;
    private TimeSpan _nextState, _nextDenialLog;
    private Action<bool> _boolChanged = default!;
    private Action<int> _intChanged = default!;
    private Action<float> _floatChanged = default!;
    private Action<string> _stringChanged = default!;
    // Trusted delivery seams for focused tests; production always uses the authenticated session channel.
    internal Action<ICommonSession, AdminAIDraftState>? StateDelivery;
    internal Action<ICommonSession>? ResetDelivery;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AdminAHelpAcceptedEvent>(Accepted);
        SubscribeNetworkEvent<AdminAIDraftRequest>(Requested);
        SubscribeNetworkEvent<AdminAIContextRequest>((message, args) => HandleContext(message, args.SenderSession));
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => Reset());
        SubscribeLocalEvent<GameRunLevelChangedEvent>(_ => Reset());
        InitializeCoordinator();
    }

    internal void InitializeCoordinator()
    {
        _audit = LogManager.GetSawmill("cmu.admin_ai.audit");
        _store = CreateStore();
        _admins.OnPermsChanged += PermissionsChanged;
        _players.PlayerStatusChanged += SessionChanged;
        _prototypes.PrototypesReloaded += PrototypesChanged;
        _boolChanged = _ => Volatile.Write(ref _dirty, true); _intChanged = _ => Volatile.Write(ref _dirty, true);
        _floatChanged = _ => Volatile.Write(ref _dirty, true); _stringChanged = _ => Volatile.Write(ref _dirty, true);
        foreach (var cvar in AdminAICVars.RuntimeBools) _config.OnValueChanged(cvar, _boolChanged);
        foreach (var cvar in AdminAICVars.RuntimeInts) _config.OnValueChanged(cvar, _intChanged);
        foreach (var cvar in AdminAICVars.RuntimeFloats) _config.OnValueChanged(cvar, _floatChanged);
        foreach (var cvar in AdminAICVars.RuntimeStrings) _config.OnValueChanged(cvar, _stringChanged);
        // Separate bucket: AI requests cannot consume the ordinary ahelp message quota.
        _rateLimit.Register(RateKey, new RateLimitRegistration(CCVars.AhelpRateLimitPeriod, CCVars.AhelpRateLimitCount, _ => { }));
    }

    private bool Enabled => _config.GetCVar(AdminAICVars.Enabled) && AdminAICVars.ReadContext(_config).IsValid();
    private AIDraftSessionStore CreateStore()
    {
        var store = new AIDraftSessionStore(AdminAICVars.ReadContext(_config));
        // Capacity/TTL eviction must revoke the already delivered client view as well as the server ticket.
        store.ViewRemoved += view =>
        {
            try
            {
                if (!_shuttingDown && _players.TryGetSessionById(new(view.Owner), out var session)
                    && session.Status != SessionStatus.Disconnected && Authorized(session))
                    Deliver(session, new(new(view.Channel), view.SelectionId, Guid.Empty, view.CaseId, Guid.Empty,
                        AdminAIDraftStatus.Stale, "", Array.Empty<AdminAISource>(), "", 0));
            }
            catch (Exception) { _audit.Warning("AI view eviction delivery failed; ordinary ahelp remains available."); }
        };
        return store;
    }
    private bool Authorized(ICommonSession session) => _admins.HasAdminFlag(session, AdminFlags.Adminhelp);
    internal void Accepted(AdminAHelpAcceptedEvent message)
    {
        if (!Enabled || _shuttingDown) return;
        if (Volatile.Read(ref _dirty)) Reconfigure();
        try
        {
            _store.Accept(message.Channel, message.Round, message.FromAdmin, message.Text, message.AdminOnly, _timing.RealTime);
            foreach (var owner in _store.Owners)
                if (_store.Peek(owner) is { } view && view.Channel == message.Channel && _players.TryGetSessionById(new(owner), out var session)) Send(session, view);
        }
        catch (Exception) { _audit.Warning("Accepted ahelp AI context failed; ordinary ahelp delivery continues."); }
    }

    private void Requested(AdminAIDraftRequest message, EntitySessionEventArgs args)
        => HandleRequest(message, args.SenderSession);

    internal void HandleRequest(AdminAIDraftRequest message, ICommonSession session)
    {
        if (!Authorized(session))
        {
            _store.Revoke(session.UserId.UserId);
            if (_timing.RealTime >= _nextDenialLog)
            { _nextDenialLog = _timing.RealTime + TimeSpan.FromSeconds(1); _audit.Info($"AI access denied actor={session.UserId}"); }
            SendReset(session); return;
        }
        if (_shuttingDown || !Enum.IsDefined(message.Action) || message.SelectionId == Guid.Empty) return;
        if (_rateLimit.CountAction(session, RateKey) != RateLimitStatus.Allowed) { SendEmpty(session, message, AdminAIDraftStatus.RateLimited); return; }
        if (Volatile.Read(ref _dirty)) Reconfigure();
        if (!Enabled) { _store.Revoke(session.UserId.UserId); SendEmpty(session, message, AdminAIDraftStatus.Disabled); return; }
        var owner = session.UserId.UserId; var channel = message.Channel.UserId;
        var view = message.Action == AdminAIDraftAction.Open
            ? _store.Open(owner, channel, message.SelectionId, _round.CurrentRoundId, _timing.RealTime, true)
            : _store.Get(owner, channel, message.ViewId, message.SelectionId, _round.CurrentRoundId, _timing.RealTime, true);
        if (view == null) { SendEmpty(session, message, AdminAIDraftStatus.NoConversation); return; }
        switch (message.Action)
        {
            case AdminAIDraftAction.Close: _store.Revoke(owner); SendEmpty(session, message, AdminAIDraftStatus.Cancelled); return;
            case AdminAIDraftAction.Cancel: _store.Cancel(view); view.LastResult = AIInferenceStatus.Cancelled; break;
            case AdminAIDraftAction.Prepare: Prepare(session, view); return;
        }
        EnsureKnowledge(); Send(session, view);
    }

    private void Prepare(ICommonSession session, AIDraftSessionStore.View view)
    {
        EnsureKnowledge();
        if (_knowledge == null) { Send(session, view, AdminAIDraftStatus.KnowledgeLoading); return; }
        var infrastructure = _inference.Snapshot();
        if (!infrastructure.Enabled) { Send(session, view, AdminAIDraftStatus.Disabled); return; }
        if (infrastructure.Health != AIWorkerHealth.Ready) { Send(session, view, AdminAIDraftStatus.Unavailable); return; }
        if (infrastructure.State == AIResourceState.Paused) { Send(session, view, AdminAIDraftStatus.Paused); return; }
        var outputTokens = _config.GetCVar(AdminAICVars.DraftOutputTokens);
        if (outputTokens is < 32 or > 256) { Send(session, view, AdminAIDraftStatus.Disabled); return; }
        var maxChars = Math.Min(2300, _config.GetCVar(infrastructure.State == AIResourceState.Degraded ? AdminAICVars.DegradedInputChars : AdminAICVars.MaxInputChars));
        var ticket = _store.Begin(view, _timing.RealTime, Math.Min(1200, maxChars - 350));
        if (ticket == null) { Send(session, view, view.Pending == null ? AdminAIDraftStatus.RateLimited : AdminAIDraftStatus.Working); return; }
        var knowledge = _knowledge;
        var publicMap = AIResponsePolicy.IsMapQuestion(ticket.Question) ? _round.PublicMapDescription : null;
        Send(session, view, AdminAIDraftStatus.Working);
        _audit.Info($"AI requested actor={session.UserId} case={ticket.CaseId} round={ticket.Round} request={ticket.Id} mode=DraftOnly");
        _ = Task.Run(async () =>
        {
            var sources = Array.Empty<AIRulePassage>(); var result = new AIInferenceResult(ticket.Id, AIInferenceStatus.InvalidRequest);
            try
            {
                sources = AIResponsePolicy.FindAnswer(ticket.Question, knowledge);
                if (publicMap != null || sources.Length > 0 && !ticket.HasHistory)
                {
                    // Fast public facts retain the same private view/authentication/revision checks.
                    result = new(ticket.Id, AIInferenceStatus.Success, publicMap ?? AIResponsePolicy.Excerpt(sources));
                }
                else
                {
                    sources = knowledge.SearchDraft(ticket, maxChars - ticket.Conversation.Length - 350);
                    var request = AIDraftPrompt.Request(ticket, sources, maxChars, outputTokens);
                    if (request != null) result = await _inference.EnqueueAsync(request, AIRequestClass.LightManual, ticket.Token).ConfigureAwait(false);
                }
            }
            catch (Exception) { result = new(ticket.Id, AIInferenceStatus.Unavailable); }
            var completed = result; var selected = sources;
            _tasks.RunOnMainThread(() => Complete(session, ticket, completed, selected));
        });
    }

    private void Complete(ICommonSession original, AIDraftTicket ticket, AIInferenceResult result, AIRulePassage[] sources)
    {
        if (Volatile.Read(ref _dirty)) Reconfigure();
        if (_shuttingDown || !Enabled || !_players.TryGetSessionById(new(ticket.Owner), out var current) || !ReferenceEquals(original, current)) return;
        if (!Authorized(current)) { _store.Revoke(ticket.Owner); SendReset(current); return; }
        var draft = result.CorrelationId == ticket.Id && result.Status == AIInferenceStatus.Success
            ? AdminAIDraftText.Sanitize(result.UntrustedText ?? "") : "";
        var status = result.CorrelationId != ticket.Id || result.Status == AIInferenceStatus.Success && draft.Length == 0
            ? AIInferenceStatus.InvalidResponse : result.Status;
        if (_inference.Snapshot().State == AIResourceState.Paused)
        { status = AIInferenceStatus.Paused; draft = ""; }
        var view = _store.Complete(ticket, _round.CurrentRoundId, _timing.RealTime, true, status, draft);
        _audit.Info($"AI completed actor={current.UserId} case={ticket.CaseId} round={ticket.Round} request={ticket.Id} result={status} current={view != null} sources={string.Join(',', sources.Select(source => source.GuideId))}");
        if (view == null) return;
        view.Sources = status == AIInferenceStatus.Success ? sources : Array.Empty<AIRulePassage>(); Send(current, view);
    }

    private void EnsureKnowledge()
    {
        if (_knowledge != null || _knowledgeTask is { IsCompleted: false }) return;
        var generation = _knowledgeGeneration;
        var documents = _prototypes.EnumeratePrototypes<GuideEntryPrototype>().Where(entry => AIRuleKnowledgeBase.IsAllowedPath(entry.Text.ToString()))
            .OrderBy(entry => entry.ID, StringComparer.Ordinal).Take(AIRuleKnowledgeBase.MaxDocuments).Select(entry => (entry.ID, entry.Name, Path: entry.Text.ToString())).ToArray();
        _knowledgeTask = Task.Run(() =>
        {
            var loaded = new List<AIRuleDocument>();
            foreach (var document in documents)
            {
                try
                {
                    using var stream = _resources.ContentFileRead(document.Path);
                    var buffer = new byte[AIRuleKnowledgeBase.MaxDocumentBytes + 1]; var count = 0;
                    while (count < buffer.Length) { var read = stream.Read(buffer, count, buffer.Length - count); if (read == 0) break; count += read; }
                    if (count <= AIRuleKnowledgeBase.MaxDocumentBytes) loaded.Add(new(document.ID, document.Name, document.Path, buffer[..count]));
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
            }
            AIRuleKnowledgeBase knowledge;
            try { knowledge = new(loaded); } catch (Exception) { knowledge = new(Array.Empty<AIRuleDocument>()); }
            _tasks.RunOnMainThread(() =>
            {
                if (_shuttingDown || generation != _knowledgeGeneration) return;
                _knowledge = knowledge;
                _audit.Info($"AI public knowledge ready: documents={knowledge.Documents} passages={knowledge.Count}");
            });
        });
    }

    internal AIRuleKnowledgeBase? GetKnowledge()
    {
        if (Volatile.Read(ref _dirty)) Reconfigure();
        if (!Enabled || _shuttingDown) return null;
        EnsureKnowledge(); return _knowledge;
    }
    internal Guid? GetPublicCaseId(Guid channel) => _store.CaseId(channel, _round.CurrentRoundId, _timing.RealTime);

    private void Send(ICommonSession session, AIDraftSessionStore.View view, AdminAIDraftStatus? status = null)
    {
        if (!Authorized(session) || !Enabled || view.Owner != session.UserId.UserId) return;
        var snapshot = _inference.Snapshot();
        status ??= view.Pending != null ? AdminAIDraftStatus.Working : view.Draft.Length > 0 ? AdminAIDraftStatus.Draft
            : view.LastResult.HasValue ? Map(view.LastResult.Value) : !snapshot.Enabled ? AdminAIDraftStatus.Disabled
            : snapshot.Health != AIWorkerHealth.Ready ? AdminAIDraftStatus.Unavailable : snapshot.State == AIResourceState.Paused ? AdminAIDraftStatus.Paused : AdminAIDraftStatus.Ready;
        var sources = view.Sources.Select(source => new AdminAISource(source.GuideId, source.Title, source.Version, source.Fragment)).ToArray();
        Deliver(session, new AdminAIDraftState(new(view.Channel), view.SelectionId, view.Id, view.CaseId, view.Pending?.Id ?? Guid.Empty,
            status.Value, view.Draft, sources, $"{snapshot.State}/{snapshot.Health}", snapshot.Queue?.Pending ?? 0,
            view.Draft.Length > 0 ? Math.Max(0, (int)(view.DraftExpires - _timing.RealTime).TotalSeconds) : 0,
            CanInvestigate(session), view.Round));
        SendCase(session, view);
    }
    private void SendEmpty(ICommonSession session, AdminAIDraftRequest message, AdminAIDraftStatus status)
        => Deliver(session, new AdminAIDraftState(message.Channel, message.SelectionId, Guid.Empty, Guid.Empty, Guid.Empty,
            status, "", Array.Empty<AdminAISource>(), "", 0));
    private void Deliver(ICommonSession session, AdminAIDraftState state)
    { if (StateDelivery != null) StateDelivery(session, state); else RaiseNetworkEvent(state, session); }
    private void SendReset(ICommonSession session)
    { if (ResetDelivery != null) ResetDelivery(session); else RaiseNetworkEvent(new AdminAIDraftReset(), session); }
    private static AdminAIDraftStatus Map(AIInferenceStatus status) => status switch
    {
        AIInferenceStatus.Disabled => AdminAIDraftStatus.Disabled, AIInferenceStatus.Unavailable => AdminAIDraftStatus.Unavailable,
        AIInferenceStatus.Paused => AdminAIDraftStatus.Paused, AIInferenceStatus.QueueFull => AdminAIDraftStatus.QueueFull,
        AIInferenceStatus.Cancelled => AdminAIDraftStatus.Cancelled, AIInferenceStatus.Expired or AIInferenceStatus.TimedOut => AdminAIDraftStatus.Expired,
        _ => AdminAIDraftStatus.InvalidResponse,
    };
    private void PermissionsChanged(AdminPermsChangedEventArgs args)
    { _store.Revoke(args.Player.UserId.UserId); SendReset(args.Player); }
    private void SessionChanged(object? sender, SessionStatusEventArgs args)
    { if (args.NewStatus == SessionStatus.Disconnected) _store.Revoke(args.Session.UserId.UserId); }
    private void PrototypesChanged(PrototypesReloadedEventArgs args) { Volatile.Write(ref _dirty, true); }
    private void Reset()
    {
        foreach (var owner in _store.Owners)
            if (_players.TryGetSessionById(new(owner), out var session)) SendReset(session);
        _store.Reset();
    }
    private void Reconfigure()
    {
        Volatile.Write(ref _dirty, false); Reset(); _store = CreateStore();
        _knowledgeGeneration++; _knowledge = null;
    }
    public override void Update(float frameTime)
    {
        if (Volatile.Read(ref _dirty)) Reconfigure();
        if (_timing.RealTime < _nextState) return;
        _nextState = _timing.RealTime + TimeSpan.FromSeconds(5);
        if (Enabled) EnsureKnowledge();
        var owners = _store.Owners.ToArray(); _store.Sweep(_timing.RealTime);
        foreach (var owner in owners)
        {
            if (!_players.TryGetSessionById(new(owner), out var session)) { _store.Revoke(owner); continue; }
            if (_store.Peek(owner) is { } view) Send(session, view);
            // ViewRemoved already sent a scoped stale state; a global reset could erase a newly selected channel.
        }
    }
    public override void Shutdown()
    {
        _shuttingDown = true; _store.Reset(); _knowledgeGeneration++;
        _admins.OnPermsChanged -= PermissionsChanged; _players.PlayerStatusChanged -= SessionChanged; _prototypes.PrototypesReloaded -= PrototypesChanged;
        foreach (var cvar in AdminAICVars.RuntimeBools) _config.UnsubValueChanged(cvar, _boolChanged);
        foreach (var cvar in AdminAICVars.RuntimeInts) _config.UnsubValueChanged(cvar, _intChanged);
        foreach (var cvar in AdminAICVars.RuntimeFloats) _config.UnsubValueChanged(cvar, _floatChanged);
        foreach (var cvar in AdminAICVars.RuntimeStrings) _config.UnsubValueChanged(cvar, _stringChanged);
        base.Shutdown();
    }
}

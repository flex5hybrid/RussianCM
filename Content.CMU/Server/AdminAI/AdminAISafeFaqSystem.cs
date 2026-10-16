using System;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.Administration.AdminAI;
using Content.Shared.GameTicking;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.CMU14.AdminAI;

/// <summary>Opt-in public FAQ only when no administrator is connected. No model output is sent automatically.</summary>
public sealed class AdminAISafeFaqSystem : EntitySystem
{
    [Dependency] private IAdminManager _admins = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private IAdminAIService _inference = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private ITaskManager _tasks = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IAdminAIRoundContext _round = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    private readonly AISafeFaqSessionStore _store = new();
    private ISawmill _audit = default!;
    private bool _shuttingDown;
    private Action<bool> _changed = default!;
    private TimeSpan _nextSweep;
    private int _inProgress;
    internal Func<AIRuleKnowledgeBase?>? KnowledgeLookup;
    internal Func<Guid, Guid?>? CaseLookup;
    internal Action<ICommonSession, string>? ReplyDelivery;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AdminAHelpAcceptedEvent>(message => _store.Invalidate(message.Channel));
        SubscribeLocalEvent<AdminAHelpDeliveredEvent>(message => Accepted(message.Message));
        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => _store.Reset());
        SubscribeLocalEvent<GameRunLevelChangedEvent>(_ => _store.Reset());
        InitializeCoordinator();
    }

    internal void InitializeCoordinator()
    {
        _audit = LogManager.GetSawmill("cmu.admin_ai.faq");
        _changed = _ => _store.Reset(false);
        _config.OnValueChanged(AdminAICVars.Enabled, _changed);
        _config.OnValueChanged(AdminAICVars.SafeFAQ, _changed);
        _config.OnValueChanged(AdminAICVars.Clarification, _changed);
        _admins.OnPermsChanged += PermissionsChanged;
        _players.PlayerStatusChanged += SessionChanged;
        _prototypes.PrototypesReloaded += PrototypesChanged;
    }

    // AllAdmins includes hidden, AFK and de-adminned staff. Their names/status are never revealed to the player.
    private bool Eligible()
    {
        if (_shuttingDown || !_config.GetCVar(AdminAICVars.Enabled)
            || !(_config.GetCVar(AdminAICVars.SafeFAQ) || _config.GetCVar(AdminAICVars.Clarification))
            || _admins.AllAdmins.Any()) return false;
        var snapshot = _inference.Snapshot();
        return snapshot.Enabled && snapshot.Health == AIWorkerHealth.Ready && snapshot.State == AIResourceState.Normal;
    }

    internal void Accepted(AdminAHelpAcceptedEvent message)
    {
        try { AcceptSafe(message); }
        catch (Exception) { _audit.Warning("AI FAQ observer failed; ordinary ahelp delivery continues."); }
    }

    private void AcceptSafe(AdminAHelpAcceptedEvent message)
    {
        _store.Invalidate(message.Channel); // New input/admin intervention invalidates an older pending answer.
        if (message.FromAdmin || message.AdminOnly || _inProgress >= 8 || !Eligible() || message.Round != _round.CurrentRoundId
            || !_players.TryGetSessionById(new(message.Channel), out var original) || original.Status != SessionStatus.InGame) return;
        var mapQuestion = AIResponsePolicy.IsMapQuestion(message.Text);
        var clarify = _config.GetCVar(AdminAICVars.Clarification) && AIResponsePolicy.NeedsClarification(message.Text);
        if (!clarify && (!_config.GetCVar(AdminAICVars.SafeFAQ) || !mapQuestion && !AIResponsePolicy.IsInformationalQuestion(message.Text))) return;
        var clarificationCase = clarify
            ? (CaseLookup != null ? CaseLookup(message.Channel) : EntityManager.System<AdminAIDraftSystem>().GetPublicCaseId(message.Channel)) : null;
        if (clarify && !clarificationCase.HasValue) return;
        var publicMap = mapQuestion ? _round.PublicMapDescription : null;
        var knowledge = clarify ? null : KnowledgeLookup != null ? KnowledgeLookup() : EntityManager.System<AdminAIDraftSystem>().GetKnowledge();
        if (!clarify && publicMap == null && knowledge == null) return;
        var ticket = _store.Begin(message.Channel, message.Round, _timing.RealTime, clarificationCase ?? Guid.Empty);
        if (ticket == null) return;
        _inProgress++;
        var question = message.Text;
        var neutral = clarify ? Loc.GetString("cmu-admin-ai-clarification-question") : null;
        _ = Task.Run(() =>
        {
            try
            {
                var sources = knowledge == null ? Array.Empty<AIRulePassage>() : AIResponsePolicy.FindAnswer(question, knowledge);
                var text = neutral ?? publicMap ?? (sources.Length == 0 ? "" : AIResponsePolicy.Excerpt(sources));
                // The background operation sees only this player's accepted public question and public documents.
                _tasks.RunOnMainThread(() =>
                {
                    try { Complete(original, ticket, text, sources, publicMap != null); }
                    catch (Exception) { _audit.Warning("AI FAQ delivery failed; ordinary ahelp remains available."); }
                    finally { _inProgress--; }
                });
            }
            catch (Exception) { _tasks.RunOnMainThread(() => { _store.Discard(ticket); _inProgress--; }); }
        });
    }

    internal void Complete(ICommonSession original, AISafeFaqTicket ticket, string text, AIRulePassage[] sources, bool publicMap)
    {
        var modeEnabled = _config.GetCVar(ticket.ClarificationCase == Guid.Empty ? AdminAICVars.SafeFAQ : AdminAICVars.Clarification);
        if (!_players.TryGetSessionById(new(ticket.Channel), out var current) || !ReferenceEquals(original, current)
            || current.Status != SessionStatus.InGame || !_store.Complete(ticket, _round.CurrentRoundId, _timing.RealTime, modeEnabled && Eligible())) return;
        text = AdminAIDraftText.Sanitize(text);
        if (text.Length == 0) { _audit.Info($"AI FAQ no-source actor={current.UserId} request={ticket.Id}"); return; }
        var label = Loc.GetString("cmu-admin-ai-faq-label");
        var sourceLabel = ticket.ClarificationCase != Guid.Empty ? Loc.GetString("cmu-admin-ai-clarification-footer")
            : publicMap ? Loc.GetString("cmu-admin-ai-faq-server-source") : Loc.GetString("cmu-admin-ai-faq-guide-source");
        var reply = $"{label}\n{text}\n{sourceLabel}";
        // Only server-created plain text. No TrueSender supplied by the player/model and no generated markup.
        if (ReplyDelivery != null) ReplyDelivery(current, reply);
        else RaiseNetworkEvent(new SharedBwoinkSystem.BwoinkTextMessage(current.UserId, SharedBwoinkSystem.SystemUserId,
            FormattedMessage.EscapeText(reply), playSound: false), current);
        _audit.Info($"AI FAQ sent actor={current.UserId} round={ticket.Round} request={ticket.Id} sources={string.Join(',', sources.Select(source => source.GuideId))} public-map={publicMap}");
    }

    private void PermissionsChanged(AdminPermsChangedEventArgs args) => _store.Reset(false);
    private void PrototypesChanged(PrototypesReloadedEventArgs args) => _store.Reset(false);
    private void SessionChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus == SessionStatus.Disconnected) _store.Invalidate(args.Session.UserId.UserId);
        if (_admins.AllAdmins.Any()) _store.Reset(false);
    }

    public override void Update(float frameTime)
    {
        if (_timing.RealTime < _nextSweep) return;
        _nextSweep = _timing.RealTime + TimeSpan.FromSeconds(1);
        _store.Sweep(_timing.RealTime);
    }
    public override void Shutdown()
    {
        _shuttingDown = true; _store.Reset();
        _config.UnsubValueChanged(AdminAICVars.Enabled, _changed);
        _config.UnsubValueChanged(AdminAICVars.SafeFAQ, _changed);
        _config.UnsubValueChanged(AdminAICVars.Clarification, _changed);
        _admins.OnPermsChanged -= PermissionsChanged;
        _players.PlayerStatusChanged -= SessionChanged;
        _prototypes.PrototypesReloaded -= PrototypesChanged;
        base.Shutdown();
    }
}

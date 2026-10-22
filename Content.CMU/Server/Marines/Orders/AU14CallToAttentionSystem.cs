using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Server._RMC14.Marines.Roles.Ranks;
using Content.Shared.ActionBlocker;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.CMU14.Marines.Orders;
using Content.Shared.CMU14.Round.Roles;
using Content.Shared.CMU14.Squads;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Roles;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.CMU14.Marines.Orders;

public sealed partial class AU14CallToAttentionSystem : EntitySystem
{
    private static readonly TimeSpan StandAtAttentionDuration = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan OrderAbilitySyncInterval = TimeSpan.FromSeconds(1);
    private const string OrdersBuiName = "AU14MilitaryOrdersBoundUserInterface";

    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private SharedBuckleSystem _buckle = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private SharedCMChatSystem _cmChat = default!;
    [Dependency] private EntityLookupSystem _entityLookup = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private RankSystem _rank = default!;
    [Dependency] private RotateToFaceSystem _rotateToFace = default!;
    [Dependency] private AU14SilenceOrderSystem _silence = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly HashSet<Entity<HumanoidProfileComponent>> _receivers = new();
    private TimeSpan _nextAbilitySync;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AU14CallToAttentionAbilityComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AU14CallToAttentionAbilityComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AU14CallToAttentionAbilityComponent, AU14CallToAttentionActionEvent>(OnOpenOrders);

        Subs.BuiEvents<AU14CallToAttentionAbilityComponent>(AU14MilitaryOrderUiKey.Key, subs =>
        {
            subs.Event<AU14MilitaryOrderSelectedBuiMsg>(OnOrderSelected);
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var frozen = EntityQueryEnumerator<AU14StandAtAttentionComponent>();
        while (frozen.MoveNext(out var uid, out var component))
        {
            if (!IsPaused(uid) && component.ExpiresAt <= time)
                RemCompDeferred<AU14StandAtAttentionComponent>(uid);
        }

        if (time < _nextAbilitySync)
            return;

        _nextAbilitySync = time + OrderAbilitySyncInterval;
        SyncOrderAbilities();
    }

    private void OnStartup(Entity<AU14CallToAttentionAbilityComponent> ent, ref ComponentStartup args)
    {
        var ui = EnsureComp<UserInterfaceComponent>(ent);
        _ui.SetUi((ent, ui), AU14MilitaryOrderUiKey.Key, new InterfaceData(OrdersBuiName));

        if (ent.Comp.CanCall)
            AddOrdersAction(ent);
    }

    private void OnShutdown(Entity<AU14CallToAttentionAbilityComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnOpenOrders(
        Entity<AU14CallToAttentionAbilityComponent> ent,
        ref AU14CallToAttentionActionEvent args)
    {
        if (args.Handled || !ent.Comp.CanCall || _mobState.IsDead(ent))
            return;

        args.Handled = true;
        _ui.TryOpenUi(ent.Owner, AU14MilitaryOrderUiKey.Key, ent.Owner);
    }

    private void OnOrderSelected(
        Entity<AU14CallToAttentionAbilityComponent> ent,
        ref AU14MilitaryOrderSelectedBuiMsg args)
    {
        if (args.Actor != ent.Owner ||
            !_ui.IsUiOpen(ent.Owner, AU14MilitaryOrderUiKey.Key, args.Actor) ||
            !Enum.IsDefined(args.Order) ||
            !TryGetOrderContext(ent.Owner, out var job, out var rankScore, out var paygrade, out var rankId))
        {
            return;
        }

        var isOfficer = IsOfficerRank(paygrade, rankId);
        var isLeader = IsOrderLeader(ent.Owner, job);
        if (!ent.Comp.CanCall || (!isOfficer && !isLeader) || _mobState.IsDead(ent))
            return;

        switch (args.Order)
        {
            case AU14MilitaryOrder.Silence:
                if (isOfficer || isLeader)
                    IssueSilenceOrder(ent.Owner, ent.Comp, job, rankScore);
                break;
            case AU14MilitaryOrder.Salute:
                if (isOfficer)
                    IssueSaluteOrder(ent.Owner, ent.Comp, job, rankScore);
                break;
            case AU14MilitaryOrder.Attention:
                IssueAttentionOrder(ent.Owner, ent.Comp, job, rankScore);
                break;
        }
    }

    private void IssueSilenceOrder(
        EntityUid caller,
        AU14CallToAttentionAbilityComponent ability,
        JobPrototype callerJob,
        int callerRankScore)
    {
        TryComp<AU14SilenceOrderAbilityComponent>(caller, out var silenceAbility);
        var duration = silenceAbility?.Duration ?? TimeSpan.FromSeconds(15);
        var range = silenceAbility?.Range ?? 7f;
        var cooldown = silenceAbility?.Cooldown ?? TimeSpan.FromSeconds(10);
        if (ability.SilenceReadyAt > _timing.CurTime)
            return;

        ability.SilenceReadyAt = _timing.CurTime + cooldown;
        Dirty(caller, ability);

        SendCallout(caller, ability.SilenceCallouts);
        var expiresAt = _timing.CurTime + duration;
        var notice = Loc.GetString("au14-silence-order-notice");

        foreach (var target in GetSubordinates(caller, callerJob, callerRankScore, range))
        {
            _silence.ApplySilenceUntil(target, expiresAt);
            _popup.PopupEntity(notice, target, target, PopupType.Small);
            _cmChat.ChatMessageToOne(notice, target, ChatChannel.Local, colorOverride: new Color(0.75f, 0.75f, 1f, 1f));
        }
    }

    private void IssueSaluteOrder(
        EntityUid caller,
        AU14CallToAttentionAbilityComponent ability,
        JobPrototype callerJob,
        int callerRankScore)
    {
        if (ability.SaluteReadyAt > _timing.CurTime)
            return;

        ability.SaluteReadyAt = _timing.CurTime + ability.Cooldown;
        Dirty(caller, ability);
        SendCallout(caller, ability.SaluteCallouts);

        _receivers.Clear();
        if (!TryComp(caller, out TransformComponent? xform))
            return;

        _entityLookup.GetEntitiesInRange(xform.Coordinates, ability.Range, _receivers);
        var targets = new List<EntityUid>();
        foreach (var receiver in _receivers)
        {
            if (IsValidSubordinate(caller, receiver.Owner, callerJob, callerRankScore) &&
                _examine.InRangeUnOccluded(caller, receiver.Owner, ability.Range, uid => uid == caller || uid == receiver.Owner))
                targets.Add(receiver.Owner);
        }

        var focus = GetAttentionFocus(caller, callerJob, ability.Range);
        foreach (var target in targets)
        {
            var delay = GetResponseDelay(ability.ResponseStagger);
            Timer.Spawn(delay, () => RespondToOrder(
                caller,
                target,
                focus,
                ability.SaluteEmote,
                "au14-salute-order-notice",
                false,
                callerJob,
                callerRankScore));
        }
    }

    private void IssueAttentionOrder(
        EntityUid caller,
        AU14CallToAttentionAbilityComponent ability,
        JobPrototype callerJob,
        int callerRankScore)
    {
        if (ability.AttentionReadyAt > _timing.CurTime)
            return;

        ability.AttentionReadyAt = _timing.CurTime + ability.Cooldown;
        Dirty(caller, ability);

        var officerNearby = HasOfficerNearby(caller, callerJob, callerRankScore, ability.OfficerNearbyRange);
        var callouts = new List<LocId>(ability.AttentionCallouts);
        if (officerNearby)
            callouts.Add("au14-call-to-attention-callout-commander-on-deck");
        SendCallout(caller, callouts);

        _receivers.Clear();
        if (!TryComp(caller, out TransformComponent? xform))
            return;

        _entityLookup.GetEntitiesInRange(xform.Coordinates, ability.Range, _receivers);
        var targets = new List<EntityUid>();
        foreach (var receiver in _receivers)
        {
            if (IsValidSubordinate(caller, receiver.Owner, callerJob, callerRankScore) &&
                _examine.InRangeUnOccluded(caller, receiver.Owner, ability.Range, uid => uid == caller || uid == receiver.Owner))
                targets.Add(receiver.Owner);
        }

        var focus = GetAttentionFocus(caller, callerJob, ability.Range);
        foreach (var target in targets)
        {
            var delay = GetResponseDelay(ability.ResponseStagger);
            Timer.Spawn(delay, () => RespondToOrder(
                caller,
                target,
                focus,
                ability.AttentionEmote,
                "au14-call-to-attention-notice",
                true,
                callerJob,
                callerRankScore));
        }
    }

    private void RespondToOrder(
        EntityUid caller,
        EntityUid target,
        EntityUid attentionFocus,
        ProtoId<EmotePrototype> emote,
        string notice,
        bool freeze,
        JobPrototype callerJob,
        int callerRankScore)
    {
        if (TerminatingOrDeleted(caller) ||
            _mobState.IsDead(caller) ||
            TerminatingOrDeleted(target) ||
            _mobState.IsDead(target) ||
            !IsValidSubordinate(caller, target, callerJob, callerRankScore))
        {
            return;
        }

        StandFromSeat(target);
        FaceAttentionFocus(target, attentionFocus);
        _popup.PopupEntity(Loc.GetString(notice), target, target, PopupType.Small);
        _chat.TryEmoteWithChat(target, emote, ignoreActionBlocker: true, forceEmote: true);

        if (freeze)
            ApplyStandAtAttention(target);
    }

    private void ApplyStandAtAttention(EntityUid target)
    {
        var attention = EnsureComp<AU14StandAtAttentionComponent>(target);
        attention.ExpiresAt = _timing.CurTime + StandAtAttentionDuration;
        Dirty(target, attention);
        _actionBlocker.UpdateCanMove(target);
    }

    private IEnumerable<EntityUid> GetSubordinates(
        EntityUid caller,
        JobPrototype callerJob,
        int callerRankScore,
        float range)
    {
        _receivers.Clear();
        if (!TryComp(caller, out TransformComponent? xform))
            return Array.Empty<EntityUid>();

        _entityLookup.GetEntitiesInRange(xform.Coordinates, range, _receivers);
        var targets = new List<EntityUid>();
        foreach (var receiver in _receivers)
        {
            if (IsValidSubordinate(caller, receiver.Owner, callerJob, callerRankScore) &&
                _examine.InRangeUnOccluded(caller, receiver.Owner, range, uid => uid == caller || uid == receiver.Owner))
                targets.Add(receiver.Owner);
        }

        return targets;
    }

    private bool IsValidSubordinate(
        EntityUid caller,
        EntityUid target,
        JobPrototype callerJob,
        int callerRankScore)
    {
        if (caller == target ||
            _mobState.IsDead(target) ||
            HasComp<AU14CallToAttentionImmuneComponent>(target) ||
            !HasComp<MarineComponent>(target) ||
            !TryGetRoleAndRank(target, out var targetJob, out var targetRankScore, out _, out _))
        {
            return false;
        }

        return targetRankScore < callerRankScore && IsSameForce(caller, target, callerJob, targetJob);
    }

    private bool HasOfficerNearby(EntityUid caller, JobPrototype callerJob, int callerRankScore, float range)
    {
        if (!TryComp(caller, out TransformComponent? xform))
            return false;

        _receivers.Clear();
        _entityLookup.GetEntitiesInRange(xform.Coordinates, range, _receivers);
        foreach (var receiver in _receivers)
        {
            if (receiver.Owner == caller ||
                !TryGetRoleAndRank(receiver.Owner, out var job, out var score, out var paygrade, out var rankId) ||
                score <= callerRankScore ||
                !IsOfficerRank(paygrade, rankId) ||
                !IsSameForce(caller, receiver.Owner, callerJob, job) ||
                !_examine.InRangeUnOccluded(
                    caller,
                    receiver.Owner,
                    range,
                    uid => uid == caller || uid == receiver.Owner))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private EntityUid GetAttentionFocus(EntityUid caller, JobPrototype callerJob, float range)
    {
        var focus = caller;
        var highestPriority = 0;
        var shortestDistance = float.MaxValue;
        var callerCoords = _transform.GetMapCoordinates(caller);

        _receivers.Clear();
        if (!TryComp(caller, out TransformComponent? xform))
            return focus;

        _entityLookup.GetEntitiesInRange(xform.Coordinates, range, _receivers);
        foreach (var receiver in _receivers)
        {
            var candidate = receiver.Owner;
            if (TerminatingOrDeleted(candidate) ||
                _mobState.IsDead(candidate) ||
                !TryComp(candidate, out AU14CallToAttentionAbilityComponent? ability) ||
                ability.AttentionFocusPriority <= 0)
            {
                continue;
            }

            if (candidate != caller)
            {
                if (!TryGetRoleAndRank(candidate, out var candidateJob, out _, out var paygrade, out var rankId) ||
                    (!IsOfficerRank(paygrade, rankId) && !IsOrderLeader(candidate, candidateJob)) ||
                    !IsSameForce(caller, candidate, callerJob, candidateJob))
                {
                    continue;
                }
            }

            if (!_examine.InRangeUnOccluded(caller, candidate, range, uid => uid == caller || uid == candidate))
                continue;

            var candidateCoords = _transform.GetMapCoordinates(candidate);
            if (candidateCoords.MapId != callerCoords.MapId)
                continue;

            var distance = (candidateCoords.Position - callerCoords.Position).LengthSquared();
            if (ability.AttentionFocusPriority < highestPriority ||
                ability.AttentionFocusPriority == highestPriority && distance >= shortestDistance)
            {
                continue;
            }

            focus = candidate;
            highestPriority = ability.AttentionFocusPriority;
            shortestDistance = distance;
        }

        return focus;
    }

    private void FaceAttentionFocus(EntityUid target, EntityUid attentionFocus)
    {
        if (target == attentionFocus || TerminatingOrDeleted(attentionFocus))
            return;

        var focusCoords = _transform.GetMapCoordinates(attentionFocus);
        var targetCoords = _transform.GetMapCoordinates(target);

        if (focusCoords.MapId != targetCoords.MapId)
            return;

        _rotateToFace.TryFaceCoordinates(target, focusCoords.Position);
    }

    private void StandFromSeat(EntityUid target)
    {
        if (TryComp(target, out BuckleComponent? buckle) && buckle.Buckled)
            _buckle.TryUnbuckle(target, target, buckle, popup: false);
    }

    private void SendCallout(EntityUid caller, IReadOnlyList<LocId>? callouts)
    {
        if (callouts == null || callouts.Count == 0)
            return;

        var callout = _random.Pick(callouts);
        _chat.TrySendInGameICMessage(caller, Loc.GetString(callout), InGameICChatType.Speak, false, ignoreActionBlocker: true);
    }

    private TimeSpan GetResponseDelay(TimeSpan maxDelay)
    {
        return maxDelay <= TimeSpan.Zero
            ? TimeSpan.Zero
            : TimeSpan.FromSeconds(_random.NextDouble() * maxDelay.TotalSeconds);
    }

    private void SyncOrderAbilities()
    {
        var query = EntityQueryEnumerator<MarineComponent, OriginalRoleComponent, RankComponent>();
        while (query.MoveNext(out var uid, out _, out var role, out _))
        {
            if (role.Job is not { } jobId ||
                !_proto.TryIndex(jobId, out JobPrototype? job) ||
                !TryGetRoleAndRank(uid, out _, out _, out var paygrade, out var rankId) ||
                (!IsOfficerRank(paygrade, rankId) && !IsOrderLeader(uid, job)))
            {
                continue;
            }

            var ability = EnsureComp<AU14CallToAttentionAbilityComponent>(uid);
            if (!ability.CanCall)
            {
                ability.CanCall = true;
                Dirty(uid, ability);
            }

            if (ability.ActionEntity == null)
                AddOrdersAction((uid, ability));
        }
    }

    private void AddOrdersAction(Entity<AU14CallToAttentionAbilityComponent> ent)
    {
        if (ent.Comp.ActionEntity != null)
            return;

        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action);
        _actions.SetUseDelay(ent.Comp.ActionEntity, TimeSpan.Zero);
    }

    private bool TryGetOrderContext(
        EntityUid uid,
        out JobPrototype job,
        out int rankScore,
        out string? paygrade,
        out string? rankId)
    {
        if (TryGetRoleAndRank(uid, out job!, out rankScore, out paygrade, out rankId) &&
            HasComp<MarineComponent>(uid))
        {
            return true;
        }

        job = default!;
        rankScore = -1;
        paygrade = null;
        rankId = null;
        return false;
    }

    private bool TryGetRoleAndRank(
        EntityUid uid,
        out JobPrototype job,
        out int rankScore,
        out string? paygrade,
        out string? rankId)
    {
        job = default!;
        rankScore = -1;
        paygrade = null;
        rankId = null;

        if (!TryComp<OriginalRoleComponent>(uid, out var original) ||
            original.Job is not { } jobId ||
            !_proto.TryIndex(jobId, out JobPrototype? jobProto) ||
            _rank.GetRank(uid) is not { } rank)
        {
            return false;
        }

        job = jobProto;
        paygrade = rank.Paygrade;
        rankId = rank.ID;
        rankScore = GetOrderRankScore(paygrade, rankId);
        return rankScore >= 0;
    }

    private bool IsSameForce(EntityUid caller, EntityUid target, JobPrototype callerJob, JobPrototype targetJob)
    {
        if (!string.IsNullOrWhiteSpace(callerJob.RoundForce) ||
            !string.IsNullOrWhiteSpace(targetJob.RoundForce))
        {
            return !string.IsNullOrWhiteSpace(callerJob.RoundForce) &&
                   !string.IsNullOrWhiteSpace(targetJob.RoundForce) &&
                   callerJob.RoundForce.Equals(targetJob.RoundForce, StringComparison.OrdinalIgnoreCase);
        }

        if (callerJob.RoundSide != RoundJobSide.None || targetJob.RoundSide != RoundJobSide.None)
        {
            return callerJob.RoundSide != RoundJobSide.None &&
                   callerJob.RoundSide == targetJob.RoundSide;
        }

        return TryComp<SquadMemberComponent>(caller, out var callerSquad) &&
               TryComp<SquadMemberComponent>(target, out var targetSquad) &&
               callerSquad.Squad != null &&
               callerSquad.Squad == targetSquad.Squad;
    }

    private bool IsOrderLeader(EntityUid uid, JobPrototype job)
    {
        if (HasComp<SquadLeaderComponent>(uid))
            return true;

        var roundRole = job.RoundRole;
        var jobId = job.ID;
        return ContainsRoleWord(roundRole, "Sergeant") ||
               ContainsRoleWord(roundRole, "Instructor") ||
               ContainsRoleWord(roundRole, "Commander") ||
               ContainsRoleWord(jobId, "Sergeant") ||
               ContainsRoleWord(jobId, "Instructor") ||
               ContainsRoleWord(jobId, "Commander");
    }

    private static bool ContainsRoleWord(string? value, string word)
        => value?.Contains(word, StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsOfficerRank(string? paygrade, string? rankId)
    {
        if (rankId == "AU14RankCMBMarshal")
            return true;

        if (paygrade == null)
            return false;

        return paygrade.StartsWith("O", StringComparison.OrdinalIgnoreCase) ||
               paygrade.StartsWith("PMC-S", StringComparison.OrdinalIgnoreCase) ||
               paygrade.StartsWith("PMC-C", StringComparison.OrdinalIgnoreCase) ||
               paygrade.StartsWith("PSC-S", StringComparison.OrdinalIgnoreCase) ||
               paygrade.StartsWith("PSC-C", StringComparison.OrdinalIgnoreCase) ||
               paygrade.StartsWith("PMC-AVO", StringComparison.OrdinalIgnoreCase) ||
               paygrade.StartsWith("PSC-AVO", StringComparison.OrdinalIgnoreCase) ||
               paygrade is "PMC-FCO" or "PMC-FL" or "PMC-FOL" or "PMC-DIR" ||
               paygrade is "VASGC" or "VASGA" or "VASGCA" or "VADPTLDR";
    }

    private static int GetOrderRankScore(string? paygrade, string? rankId)
    {
        var score = CMURankScore.FromPaygrade(paygrade);
        if (score >= 0)
            return score;

        var customRankScore = rankId switch
        {
            "AU14RankCMBMarshal" => 3000,
            "AU14RankCMBChiefDeputy" => 1900,
            "AU14RankCMBSrDeputy" => 1600,
            "AU14RankCMBDeputy" => 1300,
            "AU14RankCMBProbDeputy" => 1000,
            _ => -1,
        };
        if (customRankScore >= 0 || paygrade == null)
            return customRankScore;

        if (paygrade.StartsWith("PMC-C", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(paygrade.AsSpan("PMC-C".Length), out var corporateGrade))
        {
            return 3000 + corporateGrade * 10;
        }

        if (paygrade.StartsWith("PMC-S", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(paygrade.AsSpan("PMC-S".Length), out var supervisoryGrade))
        {
            return 2000 + supervisoryGrade * 10;
        }

        if (paygrade.StartsWith("PMC-F", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(paygrade.AsSpan("PMC-F".Length), out var fieldGrade))
        {
            return 100 + fieldGrade * 10;
        }

        if (paygrade.StartsWith("PSC-C", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(paygrade.AsSpan("PSC-C".Length), out var provostCommandGrade))
        {
            return 3000 + provostCommandGrade * 10;
        }

        if (paygrade.StartsWith("PSC-S", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(paygrade.AsSpan("PSC-S".Length), out var provostSupervisoryGrade))
        {
            return 2000 + provostSupervisoryGrade * 10;
        }

        return paygrade switch
        {
            "PMC-OP" or "PMC-EN" or "PMC-VS" or "PMC-SS" or "PMC-MS" or "PMC-WS" or
                "PMC-XS" or "PMC-DOC" or "PMC-TEC" or "PMC-ELR" or "PMC-ELM" or
                "PMC-ELG" or "PMC-SYN" => 100,
            "PMC-FCO" => 2500,
            "PMC-FL" => 2400,
            "PMC-FOL" => 2300,
            "PMC-DIR" => 3100,
            "PMC-TL" => 600,
            "PMC-ETL" => 700,
            "PMC-AVC3" => 500,
            "PMC-AVC4" => 600,
            "PMC-AVO1" => 2010,
            "PMC-AVO2" => 2020,
            "PSC-R0" or "PSC-R1" or "PSC-R2" or "PSC-R3" or "PSC-R4" or
                "PSC-R5" or "PSC-R6" or "PSC-RUSYN1" or "PSC-RUSYN2" or
                "PSC-RUSYN3" or "PSC-RUSYN4" or "PSC-RUSYN5" => 100,
            "PSC-AVC1" => 500,
            "PSC-AVC2" => 600,
            "PSC-AVO1" => 2010,
            "PSC-AVO2" => 2020,
            "VASGC" => 2200,
            "VASGA" => 2100,
            "VASGCA" => 2150,
            "VADPTLDR" => 2300,
            "VASCTL" or "VASCE" or "VASCMG" or "VASCM" or "VASC" or "VASCS" or "VASPC" => 100,
            _ => -1,
        };
    }
}

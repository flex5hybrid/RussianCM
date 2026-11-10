using Content.Server._RuMC14.Engineering;
using Content.Server.Chat.Systems;
using Content.Shared._RuMC14.Reinforcements;
using Content.Shared.Chat;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RuMC14.Reinforcements;

public sealed class RuMCReinforcementHolopadSystem : EntitySystem
{
    private static readonly EntProtoId FoldedHolopad = "RuMCReinforcementHolopadFolded";

    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RuMCThirdPartySpawnedEvent>(OnThirdPartySpawned);
        SubscribeLocalEvent<RuMCReinforcementCallComponent, RuMCSpawnAfterInteractSpawnedEvent>(OnUnfolded);
        SubscribeLocalEvent<RuMCReinforcementHolopadComponent, ComponentShutdown>(OnHolopadShutdown);
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<RuMCReinforcementHolopadComponent, RuMCReinforcementCallComponent>();
        while (query.MoveNext(out var uid, out var holopad, out var callComp))
        {
            if (callComp.Call is { } call && holopad.NextStepAt is { } next && now >= next)
                Advance((uid, holopad), call, now);
        }
    }

    private void OnThirdPartySpawned(ref RuMCThirdPartySpawnedEvent ev)
    {
        if (PickCarrier(ev.Leaders, ev.Members) is not { } carrier)
            return;

        var holopad = Spawn(FoldedHolopad, Transform(carrier).Coordinates);
        EnsureComp<RuMCReinforcementCallComponent>(holopad).Call = ev.Call;

        _hands.PickupOrDrop(carrier, holopad, checkActionBlocker: false);
    }

    private void OnUnfolded(Entity<RuMCReinforcementCallComponent> folded, ref RuMCSpawnAfterInteractSpawnedEvent args)
    {
        if (!TryComp(args.Spawned, out RuMCReinforcementHolopadComponent? holopad))
            return;

        EnsureComp<RuMCReinforcementCallComponent>(args.Spawned).Call = folded.Comp.Call;

        holopad.Hologram = Spawn(holopad.HologramProto, Transform(args.Spawned).Coordinates);
        holopad.NextStepAt = _timing.CurTime + holopad.StepDelay;
    }

    private void OnHolopadShutdown(Entity<RuMCReinforcementHolopadComponent> ent, ref ComponentShutdown args)
    {
        QueueDel(ent.Comp.Hologram);
    }

    private void Advance(Entity<RuMCReinforcementHolopadComponent> ent, RuMCReinforcementCall call, TimeSpan now)
    {
        switch (ent.Comp.Step)
        {
            case RuMCReinforcementStep.Greeting:
                Say(ent.Comp, Loc.GetString("rumc-reinforcement-greeting", ("side", call.Side)));
                ent.Comp.Step = RuMCReinforcementStep.Message;
                break;
            case RuMCReinforcementStep.Message:
                Say(ent.Comp, call.Message);
                ent.Comp.Step = RuMCReinforcementStep.Vanish;
                break;
            case RuMCReinforcementStep.Vanish:
                Vanish(ent);
                return;
        }

        ent.Comp.NextStepAt = now + ent.Comp.StepDelay;
    }

    private void Say(RuMCReinforcementHolopadComponent holopad, string text)
    {
        if (holopad.Hologram is not { } hologram)
            return;

        // No radio prefixes: the text is the caller's
        _chat.TrySendInGameICMessage(
            hologram,
            text,
            InGameICChatType.Speak,
            ChatTransmitRange.Normal,
            checkRadioPrefix: false,
            ignoreActionBlocker: true);
    }

    private void Vanish(Entity<RuMCReinforcementHolopadComponent> ent)
    {
        var coordinates = Transform(ent).Coordinates;
        Spawn(ent.Comp.SparksProto, coordinates);
        _audio.PlayPvs(ent.Comp.VanishSound, coordinates);
        QueueDel(ent);
    }

    // First body with hands, leaders first; else the first body
    private EntityUid? PickCarrier(IReadOnlyList<EntityUid> leaders, IReadOnlyList<EntityUid> members)
    {
        foreach (var body in leaders)
        {
            if (HasComp<HandsComponent>(body))
                return body;
        }

        foreach (var body in members)
        {
            if (HasComp<HandsComponent>(body))
                return body;
        }

        if (leaders.Count > 0)
            return leaders[0];

        return members.Count > 0 ? members[0] : null;
    }
}

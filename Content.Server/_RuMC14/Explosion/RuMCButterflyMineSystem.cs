using Content.Server.Explosion.EntitySystems;
using Content.Shared._CMU14.Medical.BodyPart.Events;
using Content.Shared._RuMC14.Explosion;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Random;

namespace Content.Server._RuMC14.Explosion;

public sealed partial class RuMCButterflyMineSystem : SharedRuMCButterflyMineSystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RuMCButterflyMineComponent, StepTriggeredOffEvent>(HandleStepOffTriggered);
        SubscribeLocalEvent<RuMCButterflyMineComponent, StepTriggerAttemptEvent>(HandleStepTriggerAttempt);
    }

    private void HandleStepOffTriggered(Entity<RuMCButterflyMineComponent> ent, ref StepTriggeredOffEvent args)
    {
        if (_mobState.IsDead(args.Tripper))
            return;

        TrySeverRandomLeg(args.Tripper);
        _trigger.Trigger(ent, args.Tripper);
    }

    private void HandleStepTriggerAttempt(Entity<RuMCButterflyMineComponent> ent, ref StepTriggerAttemptEvent args)
    {
        args.Continue = true;

        if (!ent.Comp.Armed)
        {
            args.Cancelled = true;
            return;
        }

        if (ent.Comp.Installer == args.Tripper &&
            Timing.CurTime < ent.Comp.InstallerImmunityUntil)
        {
            args.Cancelled = true;
            return;
        }
    }

    private void TrySeverRandomLeg(EntityUid body)
    {
        var first = _random.Next(2) == 0 ? BodyPartSymmetry.Left : BodyPartSymmetry.Right;
        var second = first == BodyPartSymmetry.Left
            ? BodyPartSymmetry.Right
            : BodyPartSymmetry.Left;

        if (!TrySeverLeg(body, first))
            TrySeverLeg(body, second);
    }

    private bool TrySeverLeg(EntityUid body, BodyPartSymmetry symmetry)
    {
        foreach (var (partUid, part) in _body.GetBodyChildren(body))
        {
            if (part.PartType != BodyPartType.Leg || part.Symmetry != symmetry)
                continue;

            var ev = new BodyPartSeveredEvent(body, partUid, BodyPartType.Leg);
            RaiseLocalEvent(partUid, ref ev);
            return true;
        }
        return false;
    }
}

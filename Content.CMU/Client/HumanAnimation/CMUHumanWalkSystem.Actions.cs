using Content.Shared.Body;
using Content.Shared.Body.Part;
using Content.Shared.CMU14.HumanAnimation;
using Content.Shared.CMU14.Medical.Anatomy.Bones;
using Content.Shared.CMU14.Movement;
using Content.Shared.DoAfter;
using Content.Shared.Movement.Components;

namespace Content.Client.CMU14.HumanAnimation;

public sealed partial class CMUHumanWalkSystem
{
    private readonly record struct Clip(string Name, int Frame);

    private void OnAction(CMUHumanActionEvent ev)
    {
        if (!_enabled || !TryGetEntity(ev.Actor, out var uid) || !_states.TryGetValue(uid.Value, out var state))
            return;

        if (state.Action is CMUHumanAction.Fall or CMUHumanAction.Rise)
            return;

        // Manual reach should never cancel an attack pose that is still playing.
        if (ev.Action == CMUHumanAction.Reach
            && state.Action is CMUHumanAction.Melee or CMUHumanAction.Shoot
            && (_timing.CurTime - state.ActionStarted).TotalSeconds < ActionDuration(state.Action))
            return;

        state.Action = ev.Action;
        state.ActionStarted = _timing.CurTime;
    }

    private static float ActionDuration(CMUHumanAction action) => action switch
    {
        CMUHumanAction.Reach => .36f,
        CMUHumanAction.Melee => .28f,
        CMUHumanAction.Shoot => .16f,
        CMUHumanAction.Fall => .24f,
        CMUHumanAction.Rise => .30f,
        _ => 0f,
    };

    private static Clip? ActionClip(WalkState state, TimeSpan now, bool alive, bool standing, bool held)
    {
        var duration = ActionDuration(state.Action);
        var elapsed = (now - state.ActionStarted).TotalSeconds;
        if (elapsed < 0 || elapsed >= duration
            || (state.Action != CMUHumanAction.Fall && (!alive || !standing)))
        {
            state.Action = CMUHumanAction.None;
            return null;
        }

        var name = state.Action switch
        {
            CMUHumanAction.Reach => held ? "reach-held" : "reach",
            CMUHumanAction.Melee => held ? "melee-held" : "melee",
            CMUHumanAction.Shoot => "shoot",
            CMUHumanAction.Fall => "fall",
            CMUHumanAction.Rise => "rise",
            _ => throw new InvalidOperationException(),
        };
        return new Clip(name, Math.Clamp((int) (elapsed / duration * 8), 0, 7));
    }

    private Clip IdleClip(EntityUid uid, TimeSpan now, bool held)
    {
        if (TryComp<DoAfterComponent>(uid, out var work))
        {
            foreach (var action in work.DoAfters.Values)
            {
                if (!action.Completed && !action.Cancelled && action.Args.NeedHand)
                    return new Clip(held ? "reach-held" : "reach", LoopFrame(uid, now, .86));
            }
        }

        return new Clip("idle", LoopFrame(uid, now, 3.1));
    }

    private static int LoopFrame(EntityUid uid, TimeSpan now, double duration)
    {
        var time = now.TotalSeconds + (uid.GetHashCode() & 31) * .13;
        return (int) (time % duration / duration * 8);
    }

    private string WalkingClip(EntityUid uid, InputMoverComponent input, BodyComponent? body, bool held)
    {
        if (input.Sprinting)
            return held ? "run-held" : "run";

        if (body != null)
        {
            foreach (var leg in body.LegEntities)
            {
                if (!TryComp<FractureComponent>(leg, out var fracture)
                    || !fracture.Severity.IsAtLeast(FractureSeverity.Simple)
                    || !TryComp<BodyPartComponent>(leg, out var part))
                    continue;

                if (part.Symmetry == BodyPartSymmetry.Left)
                    return held ? "limp-left-held" : "limp-left";
                if (part.Symmetry == BodyPartSymmetry.Right)
                    return held ? "limp-right-held" : "limp-right";
            }
        }

        if (input.HumanWalkPace == CMUHumanMovementPace.Quiet)
            return held ? "quiet-held" : "quiet";
        if (input.HumanWalkPace == CMUHumanMovementPace.Tactical)
            return "tactical";
        if (TryComp<MovementSpeedModifierComponent>(uid, out var speed)
            && speed.CurrentSprintSpeed < speed.BaseSprintSpeed * .8f)
            return held ? "heavy-held" : "heavy";

        return held ? "held" : "walk";
    }
}

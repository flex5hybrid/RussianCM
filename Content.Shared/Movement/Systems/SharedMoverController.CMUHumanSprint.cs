using System.Numerics;
using Content.Shared._RMC14.Stamina;
using Content.Shared._RMC14.Synth;
using Content.Shared.Humanoid;
using Content.Shared.Movement.Components;
using Content.Shared.Standing;
using Robust.Shared.Map;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedMoverController
{
    private const double HumanSprintCostPerSecond = 8;

    [Dependency] private RMCStaminaSystem _sprintStamina = default!;
    [Dependency] private StandingStateSystem _sprintStanding = default!;

    private readonly Dictionary<EntityUid, (EntityCoordinates Start, float Fraction)> _humanSprintStarts = new();

    private void UpdateHumanSprint(Entity<InputMoverComponent> ent, bool grounded = true)
    {
        if (!_net.IsServer)
            return;

        var stamina = CompOrNull<RMCStaminaComponent>(ent);
        var sprintOnModifier = stamina != null &&
            TryComp<HumanoidProfileComponent>(ent, out var profile) && profile.Species.Id == "Human" &&
            !HasComp<SynthComponent>(ent);
        var blocked = sprintOnModifier &&
            (!_sprintStamina.CanSprint(stamina!) || !grounded || _sprintStanding.IsDown(ent.Owner));

        if (ent.Comp.SprintOnModifier == sprintOnModifier && ent.Comp.SprintBlocked == blocked)
            return;

        ent.Comp.SprintOnModifier = sprintOnModifier;
        ent.Comp.SprintBlocked = blocked;
        Dirty(ent);
    }

    private void TrackHumanSprint(Entity<InputMoverComponent> ent, TransformComponent xform, float fraction)
    {
        if (_net.IsServer && ent.Comp.SprintOnModifier && fraction > 0)
            _humanSprintStarts[ent.Owner] = (xform.Coordinates, MathF.Min(fraction, 1f));
    }

    private void SpendHumanSprintStamina(float frameTime)
    {
        foreach (var (uid, (start, fraction)) in _humanSprintStarts)
        {
            if (!XformQuery.TryComp(uid, out var xform) || xform.ParentUid != start.EntityId ||
                !TryComp<RMCStaminaComponent>(uid, out var stamina))
                continue;

            // Compare after collision resolution in parent coordinates so running into a wall
            // or being carried along by a moving grid doesn't spend stamina.
            var distanceSquared = Vector2.DistanceSquared(start.Position, xform.LocalPosition);
            if (distanceSquared is > 0.000001f and < 1f)
                _sprintStamina.SpendForSprint((uid, stamina), fraction * frameTime * HumanSprintCostPerSecond);
        }
        _humanSprintStarts.Clear();
    }
}

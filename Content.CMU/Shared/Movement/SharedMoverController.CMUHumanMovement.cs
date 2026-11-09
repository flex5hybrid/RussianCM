using System.Numerics;
using Content.Shared._RMC14.Stamina;
using Content.Shared._RMC14.Synth;
using Content.Shared.CMU14.Movement;
using Content.Shared.CombatMode;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Map;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedMoverController
{
    private const double HumanSprintCostPerSecond = 8;

    [Dependency] private RMCStaminaSystem _sprintStamina = default!;
    [Dependency] private StandingStateSystem _sprintStanding = default!;
    [Dependency] private SharedCombatModeSystem _paceCombat = default!;
    [Dependency] private SharedHandsSystem _paceHands = default!;
    [Dependency] private SharedPopupSystem _pacePopup = default!;

    private readonly Dictionary<EntityUid, (EntityCoordinates Start, float Fraction)> _humanSprintStarts = new();

    private void ToggleQuietWalk(EntityUid uid)
    {
        if (!MoverQuery.TryComp(uid, out var mover)
            || !mover.SprintOnModifier
            || !_mobState.IsAlive(uid)
            || RelayQuery.HasComp(uid)
            || _container.IsEntityInContainer(uid))
            return;

        mover.QuietWalk = !mover.QuietWalk;
        Dirty(uid, mover);
        _pacePopup.PopupPredicted(Loc.GetString(mover.QuietWalk ? "cmu-quiet-walk-on" : "cmu-quiet-walk-off"), uid, uid);
    }

    private void UpdateHumanMovement(Entity<InputMoverComponent> ent, bool grounded = true)
    {
        if (!_net.IsServer)
        {
            if (ent.Comp.SprintOnModifier)
                ent.Comp.HumanWalkPace = GetHumanWalkPace(ent, grounded && !_sprintStanding.IsDown(ent.Owner));
            return;
        }

        var stamina = CompOrNull<RMCStaminaComponent>(ent);
        var sprintOnModifier = stamina != null &&
            TryComp<HumanoidProfileComponent>(ent, out var profile) && profile.Species.Id == "Human" &&
            !HasComp<SynthComponent>(ent);
        var onFoot = sprintOnModifier && grounded && !_sprintStanding.IsDown(ent.Owner);
        var blocked = sprintOnModifier && (!_sprintStamina.CanSprint(stamina!) || !onFoot);
        var pace = GetHumanWalkPace(ent, onFoot);

        if (ent.Comp.SprintOnModifier == sprintOnModifier
            && ent.Comp.SprintBlocked == blocked
            && ent.Comp.HumanWalkPace == pace)
            return;

        ent.Comp.SprintOnModifier = sprintOnModifier;
        ent.Comp.SprintBlocked = blocked;
        ent.Comp.HumanWalkPace = pace;
        Dirty(ent);
    }

    private CMUHumanMovementPace GetHumanWalkPace(Entity<InputMoverComponent> ent, bool onFoot)
    {
        if (!onFoot)
            return CMUHumanMovementPace.Walk;

        if (ent.Comp.QuietWalk)
            return CMUHumanMovementPace.Quiet;

        return _paceCombat.IsInCombatMode(ent.Owner)
            && _paceHands.TryGetActiveItem(ent.Owner, out var held)
            && HasComp<GunComponent>(held)
            ? CMUHumanMovementPace.Tactical
            : CMUHumanMovementPace.Walk;
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

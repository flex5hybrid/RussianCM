using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Movement.Systems;
using Content.Shared.RuMC.Vehicles.Components;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Shared.RuMC.Vehicles.Systems;

/// <summary>
/// Система танка
/// </summary>
public sealed class TankSystem : EntitySystem
{
    [Dependency] private readonly SharedMoverController _mover = null!;
    [Dependency] private readonly SharedInteractionSystem _interaction = null!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = null!;
    [Dependency] private readonly SharedContainerSystem _container = null!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = null!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TankComponent, MechEntryEvent>(OnMechEntry);
        SubscribeLocalEvent<TankComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
        SubscribeLocalEvent<TankComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, TankComponent component, ComponentStartup args)
    {
        component.PilotSlot = _container.EnsureContainer<ContainerSlot>(uid, component.PilotSlotId);
    }

    private void OnMechEntry(EntityUid uid, TankComponent component, MechEntryEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        TryInsert(uid, args.Args.User, component);
        _actionBlocker.UpdateCanMove(uid);

        args.Handled = true;
    }

    private void TryInsert(EntityUid uid, EntityUid? toInsert, TankComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (toInsert == null || component.PilotSlot.ContainedEntity == toInsert)
            return;

        SetupUser(uid, toInsert.Value);
        _container.Insert(toInsert.Value, component.PilotSlot);
    }

    private void SetupUser(EntityUid mech, EntityUid pilot, TankComponent? component = null)
    {
        if (!Resolve(mech, ref component))
            return;

        var rider = EnsureComp<ActiveTankPilotComponent>(pilot);

        var irelay = EnsureComp<InteractionRelayComponent>(pilot);

        _mover.SetRelay(pilot, mech);
        _interaction.SetRelay(pilot, mech, irelay);
        rider.Mech = mech;
        Dirty(pilot, rider);
    }

    private bool CanInsert(EntityUid uid, EntityUid toInsert, TankComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        return IsEmpty(component) && _actionBlocker.CanMove(toInsert);
    }

    private static bool IsEmpty(TankComponent component)
    {
        return component.PilotSlot.ContainedEntity == null;
    }

    private void OnAlternativeVerb(EntityUid uid, TankComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!CanInsert(uid, args.User, component))
            return;
        var enterVerb = new AlternativeVerb
        {
            Text = Loc.GetString("mech-verb-enter"),
            Act = () =>
            {
                var doAfterEventArgs = new DoAfterArgs(EntityManager,
                    args.User,
                    component.EntryDelay,
                    new MechEntryEvent(),
                    uid,
                    target: uid)
                {
                    BreakOnMove = true,
                };

                _doAfter.TryStartDoAfter(doAfterEventArgs);
            },
        };
        args.Verbs.Add(enterVerb);
    }
}

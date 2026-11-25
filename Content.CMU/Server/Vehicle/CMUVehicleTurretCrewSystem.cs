using Content.Shared._RMC14.Vehicle;
using Content.Shared.Buckle.Components;
using Content.Shared.CMU14.Vehicle;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.CMU14.Vehicle;

// CMU14 class
// A homemade turret needs somewhere for its gunner to sit and to load it from. Civilian vehicles have no such seat, so
// while a turret with CMUVehicleTurretCrew is fitted, the first passenger seat of the interior is swapped for its gunner
// seat; taking the turret off puts the passenger seat back. The gunner seat also loads the mounted gun's magazine.
public sealed class CMUVehicleTurretCrewSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private VehicleSystem _vehicle = default!;
    [Dependency] private VehicleTopologySystem _topology = default!;
    [Dependency] private VehicleTurretSystem _turret = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HardpointSlotsChangedEvent>(OnSlotsChanged);
        SubscribeLocalEvent<CMUVehicleMagazineLoaderComponent, InteractUsingEvent>(OnLoaderInteractUsing);
    }

    private void OnSlotsChanged(HardpointSlotsChangedEvent args)
    {
        var vehicle = args.Vehicle;
        if (TerminatingOrDeleted(vehicle) || !HasComp<VehicleEnterComponent>(vehicle))
            return;

        EntProtoId? gunnerSeat = null;
        foreach (var mounted in _topology.GetMountedSlots(vehicle))
        {
            if (mounted.Item is { } item && TryComp(item, out CMUVehicleTurretCrewComponent? crew))
            {
                gunnerSeat = crew.GunnerSeat;
                PlaceTurret(vehicle, item);
                break;
            }
        }

        TryComp(vehicle, out CMUVehicleGunnerSeatStateComponent? state);
        if (gunnerSeat is { } seatProto)
        {
            if (state?.GunnerSeat is not { } existing || TerminatingOrDeleted(existing))
                InstallGunnerSeat(vehicle, seatProto);
        }
        else if (state != null)
        {
            RestorePassengerSeat((vehicle, state));
        }
    }

    // Puts the turret's rotating overlay where this vehicle's roof is.
    private void PlaceTurret(EntityUid vehicle, EntityUid turret)
    {
        if (!TryComp(vehicle, out CMUVehicleTurretMountComponent? mount) || !TryComp(turret, out VehicleTurretComponent? comp))
            return;

        _turret.CMUSetMountOffsets(turret, comp, mount.North, mount.East, mount.South, mount.West);
    }

    private void InstallGunnerSeat(EntityUid vehicle, EntProtoId seatProto)
    {
        // Builds the interior if nobody has been inside yet; entry 0 is on the interior's grid.
        if (!_vehicle.TryGetInteriorEntryCoordinates(vehicle, 0, out var entry))
            return;

        var state = EnsureComp<CMUVehicleGunnerSeatStateComponent>(vehicle);
        state.OriginalPrototype = null;
        state.Coordinates = entry;
        state.Rotation = Angle.Zero;

        if (FindPassengerSeat(entry.EntityId) is { } passenger)
        {
            var xform = Transform(passenger);
            state.Coordinates = xform.Coordinates;
            state.Rotation = xform.LocalRotation;
            state.OriginalPrototype = MetaData(passenger).EntityPrototype?.ID;
            Del(passenger);
        }

        var seat = Spawn(seatProto, state.Coordinates);
        _transform.SetLocalRotation(seat, state.Rotation);
        state.GunnerSeat = seat;
    }

    private void RestorePassengerSeat(Entity<CMUVehicleGunnerSeatStateComponent> vehicle)
    {
        if (vehicle.Comp.GunnerSeat is { } seat && !TerminatingOrDeleted(seat))
            Del(seat);

        if (vehicle.Comp.OriginalPrototype is { } original && !TerminatingOrDeleted(vehicle.Comp.Coordinates.EntityId))
        {
            var passenger = Spawn(original, vehicle.Comp.Coordinates);
            _transform.SetLocalRotation(passenger, vehicle.Comp.Rotation);
        }

        RemComp<CMUVehicleGunnerSeatStateComponent>(vehicle);
    }

    private EntityUid? FindPassengerSeat(EntityUid grid)
    {
        EntityUid? found = null;
        var children = Transform(grid).ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            if (!HasComp<StrapComponent>(child) ||
                HasComp<VehicleDriverSeatComponent>(child) ||
                HasComp<VehicleWeaponsSeatComponent>(child))
            {
                continue;
            }

            // The lowest uid is the one mapped first, so the same seat is picked every time.
            if (found == null || child.Id < found.Value.Id)
                found = child;
        }

        return found;
    }

    private void OnLoaderInteractUsing(Entity<CMUVehicleMagazineLoaderComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled ||
            !_vehicle.TryGetVehicleFromInterior(ent.Owner, out var found) ||
            found is not { } vehicle)
        {
            return;
        }

        foreach (var mounted in _topology.GetMountedSlots(vehicle))
        {
            if (mounted.Item is not { } gun ||
                !_itemSlots.TryGetSlot(gun, ent.Comp.Slot, out var slot) ||
                !_itemSlots.CanInsert(gun, slot, args.Used, args.User, swap: true))
            {
                continue;
            }

            args.Handled = true;
            if (slot.HasItem)
                _itemSlots.TryEjectToHands(gun, slot, args.User, excludeUserAudio: true);

            if (_itemSlots.TryInsertFromHand(gun, slot, args.User))
                _popup.PopupEntity(Loc.GetString("cmu-vehicle-gunner-seat-loaded", ("gun", gun)), ent.Owner, args.User);

            return;
        }
    }
}

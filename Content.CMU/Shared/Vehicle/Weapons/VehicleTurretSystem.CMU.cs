using System.Numerics;

namespace Content.Shared._RMC14.Vehicle;

public sealed partial class VehicleTurretSystem
{
    /// <summary>
    /// CMU14: places a turret's overlay on the vehicle it was fitted to, one pixel offset for each way the vehicle faces.
    /// A homemade turret fits several civilian vehicles, so the offsets come from the vehicle, not the turret prototype.
    /// </summary>
    public void CMUSetMountOffsets(EntityUid uid, VehicleTurretComponent turret, Vector2 north, Vector2 east, Vector2 south, Vector2 west)
    {
        turret.UseDirectionalOffsets = true;
        turret.PixelOffset = Vector2.Zero;
        turret.PixelOffsetNorth = north;
        turret.PixelOffsetEast = east;
        turret.PixelOffsetSouth = south;
        turret.PixelOffsetWest = west;
        Dirty(uid, turret);
    }

    /// <summary>
    /// CMU14: keeps a turret's vehicle-relative aim inside its arc around the vehicle's front.
    /// </summary>
    private Angle CMUClampYaw(VehicleTurretComponent turret, EntityUid vehicle, Angle desired)
    {
        if (turret.CMUMaxYawDegrees <= 0f)
            return desired;

        if (!turret.StabilizedRotation)
            return CMUClampLocalYaw(turret, desired);

        var vehicleRot = _transform.GetWorldRotation(vehicle);
        return (CMUClampLocalYaw(turret, desired - vehicleRot) + vehicleRot).Reduced();
    }

    /// <summary>
    /// CMU14: clamps a vehicle-relative angle to the turret's arc. Reduced() keeps angles anywhere in
    /// (-2π, 2π), so the angle is first wrapped to the shortest turn from the front, or aim past 180°
    /// would snap to the wrong edge.
    /// </summary>
    private Angle CMUClampLocalYaw(VehicleTurretComponent turret, Angle local)
    {
        if (turret.CMUMaxYawDegrees <= 0f)
            return local;

        var max = MathHelper.DegreesToRadians(turret.CMUMaxYawDegrees);
        return new Angle(Math.Clamp(Angle.ShortestDistance(Angle.Zero, local).Theta, -max, max));
    }
}

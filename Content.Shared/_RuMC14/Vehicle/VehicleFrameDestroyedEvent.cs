using Robust.Shared.GameObjects;

namespace Content.Shared._RuMC14.Vehicle;

/// <summary>
/// Raised on the vehicle entity when its frame integrity drops to zero.
/// </summary>
public sealed class VehicleFrameDestroyedEvent : EntityEventArgs
{
    public readonly EntityUid Vehicle;

    public VehicleFrameDestroyedEvent(EntityUid vehicle)
    {
        Vehicle = vehicle;
    }
}

using Content.Shared.CMU14.Vehicle;
using Content.Shared.Containers.ItemSlots;

namespace Content.Shared._RMC14.Vehicle;

public sealed partial class HardpointSystem
{
    // CMU14 method
    // A working slowdown-compensation module (the heavy-fuel engine) cancels its share of the total slowdown; damage
    // scales its share down the same way it scales every module's effect.
    private float CMUCompensateSlowdown(
        EntityUid vehicle,
        HardpointSlotsComponent hardpoints,
        ItemSlotsComponent itemSlots,
        float speedMultiplier)
    {
        if (speedMultiplier >= 1f)
            return speedMultiplier;

        var fraction = 0f;
        foreach (var mounted in _topology.GetMountedSlots(vehicle, hardpoints, itemSlots))
        {
            if (mounted.Item is not { } item ||
                !TryComp(item, out CMUVehicleSlowdownCompensationComponent? compensation))
            {
                continue;
            }

            fraction = MathF.Max(fraction, compensation.Fraction * GetHardpointPerformanceMultiplier(item));
        }

        fraction = Math.Clamp(fraction, 0f, 1f);
        return 1f - (1f - speedMultiplier) * (1f - fraction);
    }
}

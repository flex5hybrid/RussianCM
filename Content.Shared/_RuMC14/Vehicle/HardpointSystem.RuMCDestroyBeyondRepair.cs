namespace Content.Shared._RMC14.Vehicle;

// RuMC edit: destroying a vehicle frame for good without a damage hit.
public sealed partial class HardpointSystem
{
    /// <summary>
    /// Wrecks a vehicle frame permanently, the same way a catastrophic hit does: its hull drops to zero, it
    /// cannot run and nothing on it can be repaired or replaced. Surviving parts do not bring it back.
    /// </summary>
    public void RuMCDestroyBeyondRepair(EntityUid vehicle)
    {
        if (_net.IsClient || !TryComp(vehicle, out HardpointIntegrityComponent? frame) || frame.DestroyedBeyondRepair)
            return;

        frame.DestroyedBeyondRepair = true;
        frame.Integrity = 0f;
        Dirty(vehicle, frame);
        UpdateFrameDamageAppearance(vehicle, frame);
        RaiseIntegrityChanged(vehicle);
        RaiseFrameIntegrityChanged(vehicle, false);
        RefreshCanRun(vehicle);
        _lock.RefreshForcedOpen(vehicle);
        UpdateHardpointUi(vehicle);
    }
}

using System;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedMoverController
{
    /// <summary>
    /// Applies first-person yaw immediately instead of feeding it through the legacy
    /// smooth 2D camera rotation path. Movement and rendering must see the exact same
    /// angle every frame or they can disagree when the target crosses 180 degrees.
    /// </summary>
    public void SetFirstPersonCameraRotation(EntityUid uid, Angle angle)
    {
        if (CameraRotationLocked ||
            !double.IsFinite(angle.Theta) ||
            !MoverQuery.TryGetComponent(uid, out var mover))
        {
            return;
        }

        var reduced = angle.Reduced();
        if (mover.RelativeRotation.Equals(reduced) &&
            mover.TargetRelativeRotation.Equals(reduced))
        {
            return;
        }

        mover.RelativeRotation = reduced;
        mover.TargetRelativeRotation = reduced;
        mover.LerpTarget = TimeSpan.Zero;
        Dirty(uid, mover);
    }
}

/// <summary>
/// Receives coalesced first-person yaw updates. This is deliberately a normal
/// network event rather than a predictive event because mouse motion originates
/// from the render/input frame, not from a predicted simulation tick.
/// </summary>
public sealed class FirstPersonLookNetworkSystem : EntitySystem
{
    [Dependency] private readonly SharedMoverController _mover = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvent<FirstPersonLookSyncEvent>(OnFirstPersonLook);
    }

    private void OnFirstPersonLook(FirstPersonLookSyncEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is { } uid)
            _mover.SetFirstPersonCameraRotation(uid, msg.Yaw);
    }
}

[Serializable, NetSerializable]
public sealed class FirstPersonLookSyncEvent : EntityEventArgs
{
    public Angle Yaw;
}

using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Rotation;
using Content.Shared.Standing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics3D;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Buckle;

public abstract partial class SharedBuckleSystem : EntitySystem
{
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private ISharedPlayerManager _playerManager = default!;

    [Dependency] protected ActionBlockerSystem ActionBlocker = default!;
    [Dependency] protected SharedAppearanceSystem Appearance = default!;

    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedJointSystem _joints = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedPhysics3DSystem _physics3D = default!;
    [Dependency] private SharedTransform3DSystem _transform3D = default!;
    [Dependency] private SharedRotationVisualsSystem _rotationVisuals = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(SharedInteractionSystem));
        UpdatesAfter.Add(typeof(SharedInputSystem));

        InitializeBuckle();
        InitializeStrap();
        InitializeInteraction();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BuckleComponent, Transform3DComponent>();
        while (query.MoveNext(out var uid, out var buckle, out _))
        {
            if (buckle.BuckledTo is not { } strap || !HasComp<PhysicsBody3DComponent>(uid))
                continue;

            var position = _transform3D.GetWorldPosition3D(uid);
            var rotation = _transform3D.GetWorldRotation3D(uid);
            _physics3D.TeleportBody(uid, position, rotation);
            if (_physics3D.TryGetVelocity(strap, out var linear, out var angular))
                _physics3D.SetVelocity(uid, linear, angular);
        }
    }
}

using System.Numerics;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Physics3D;

namespace Content.Server.Physics.Controllers;

/// <summary>
/// Content input adapter for the engine-owned 3D character controller. Existing movement key transport remains
/// useful, but no 2D velocity, tile collision or transform rotation is applied to migrated characters.
/// </summary>
public sealed class CharacterController3DSystem : EntitySystem
{
    [Dependency] private SharedPhysics3DSystem _physics3D = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesBefore.Add(typeof(SharedPhysics3DSystem));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<InputMoverComponent, CharacterController3DComponent>();
        while (query.MoveNext(out var uid, out var mover, out _))
        {
            var wishDirection = mover.CanMove
                ? GetWorldWishDirection(mover.HeldMoveButtons, (float) mover.FirstPersonYaw.Theta)
                : Vector2.Zero;
            _physics3D.DriveCharacter(uid, wishDirection, mover.Sprinting, frameTime);
        }
    }

    private static Vector2 GetWorldWishDirection(MoveButtons buttons, float yaw)
    {
        var local = Vector2.Zero;
        if ((buttons & MoveButtons.Up) != 0)
            local.Y += 1f;
        if ((buttons & MoveButtons.Down) != 0)
            local.Y -= 1f;
        if ((buttons & MoveButtons.Right) != 0)
            local.X += 1f;
        if ((buttons & MoveButtons.Left) != 0)
            local.X -= 1f;

        if (local.LengthSquared() > 1f)
            local = Vector2.Normalize(local);

        var forward = new Vector2(MathF.Sin(yaw), MathF.Cos(yaw));
        var right = new Vector2(MathF.Cos(yaw), -MathF.Sin(yaw));
        return right * local.X + forward * local.Y;
    }
}

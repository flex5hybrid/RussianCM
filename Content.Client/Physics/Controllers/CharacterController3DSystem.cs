using System.Numerics;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.Player;
using Robust.Shared.Physics3D;
using Robust.Shared.Player;

namespace Content.Client.PhysicsSystem.Controllers;

/// <summary>
/// Drives the local predicted Bepu character with the same movement intent used by the server. Robust prediction
/// restores authoritative component states, then replays pending movement commands through this system.
/// </summary>
public sealed class CharacterController3DSystem : EntitySystem
{
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private SharedPhysics3DSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesBefore.Add(typeof(SharedPhysics3DSystem));
        SubscribeLocalEvent<CharacterController3DComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<CharacterController3DComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<CharacterController3DComponent, ComponentStartup>(OnCharacterStartup);
        SubscribeLocalEvent<CharacterController3DComponent, ComponentShutdown>(OnCharacterShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (_players.LocalEntity is not { Valid: true } player ||
            !TryComp(player, out InputMoverComponent? mover) ||
            !HasComp<CharacterController3DComponent>(player))
        {
            return;
        }

        var wishDirection = mover.CanMove
            ? GetWorldWishDirection(mover.HeldMoveButtons, (float) mover.FirstPersonYaw.Theta)
            : Vector2.Zero;
        _physics.DriveCharacter(player, wishDirection, mover.Sprinting, frameTime);
    }

    private void OnPlayerAttached(Entity<CharacterController3DComponent> entity, ref LocalPlayerAttachedEvent args)
    {
        EnsureComp<PredictedPhysics3DComponent>(entity.Owner);
    }

    private void OnPlayerDetached(Entity<CharacterController3DComponent> entity, ref LocalPlayerDetachedEvent args)
    {
        RemComp<PredictedPhysics3DComponent>(entity.Owner);
    }

    private void OnCharacterStartup(Entity<CharacterController3DComponent> entity, ref ComponentStartup args)
    {
        if (_players.LocalEntity == entity.Owner)
            EnsureComp<PredictedPhysics3DComponent>(entity.Owner);
    }

    private void OnCharacterShutdown(Entity<CharacterController3DComponent> entity, ref ComponentShutdown args)
    {
        RemComp<PredictedPhysics3DComponent>(entity.Owner);
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

using Content.Server.Vehicles.Components;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.GameObjects;

namespace Content.Server.Vehicles.Systems;

public sealed class TankControllerSystem : EntitySystem
{
    [Dependency] private readonly TankGunSystem _gunSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Используем PointerInputCmdHandler вместо абстрактного InputCmdHandler
        CommandBinds.Builder
            .Bind(EngineKeyFunctions.Use,
                new PointerInputCmdHandler(HandleShoot))
            .Register<TankControllerSystem>();
    }

    private bool HandleShoot(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.Session?.AttachedEntity is not { } user)
            return false;

        if (!TryComp<TankControllerComponent>(user, out var controller) ||
            controller.Controller is not { } tank)
            return false;

        if (TryComp<TankGunComponent>(tank, out var gun))
            _gunSystem.TryFire(tank, user, gun);

        return true;
    }

    public void AssignController(EntityUid uid, EntityUid user, TankControllerComponent? controller = null)
    {
        if (!Resolve(uid, ref controller))
            return;

        controller.Controller = user;
    }

    public void UnassignController(EntityUid uid, TankControllerComponent? controller = null)
    {
        if (!Resolve(uid, ref controller))
            return;

        controller.Controller = null;
    }
}

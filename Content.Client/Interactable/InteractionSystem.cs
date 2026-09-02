using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Client.Interactable;

// TODO Remove Shared prefix
public sealed class InteractionSystem : SharedInteractionSystem
{
    public override void Initialize()
    {
        // First-person world actions carry no legacy pointer coordinates. The shared handler reconstructs an
        // authoritative 3D ray from the simulation's character pose and sequenced look state.
        CommandBinds.Builder
            .BindBefore(
                EngineKeyFunctions.Use,
                new PointerInputCmdHandler(HandleFirstPersonUse),
                typeof(SharedInteractionSystem))
            .BindBefore(
                ContentKeyFunctions.ActivateItemInWorld,
                new PointerInputCmdHandler(HandleFirstPersonActivate),
                typeof(SharedInteractionSystem))
            .BindBefore(
                ContentKeyFunctions.AltActivateItemInWorld,
                new PointerInputCmdHandler(HandleFirstPersonAltActivate),
                typeof(SharedInteractionSystem))
            .BindBefore(
                ContentKeyFunctions.TryPullObject,
                new PointerInputCmdHandler(HandleFirstPersonPull),
                typeof(SharedInteractionSystem))
            .Register<InteractionSystem>();

        base.Initialize();
    }

    public override void Shutdown()
    {
        CommandBinds.Unregister<InteractionSystem>();
        base.Shutdown();
    }

    private bool HandleFirstPersonUse(in PointerInputCmdHandler.PointerInputCmdArgs args)
        => SendFirstPersonInteraction(InteractionAction3D.Use, in args);

    private bool HandleFirstPersonActivate(in PointerInputCmdHandler.PointerInputCmdArgs args)
        => SendFirstPersonInteraction(InteractionAction3D.Activate, in args);

    private bool HandleFirstPersonAltActivate(in PointerInputCmdHandler.PointerInputCmdArgs args)
        => SendFirstPersonInteraction(InteractionAction3D.AltActivate, in args);

    private bool HandleFirstPersonPull(in PointerInputCmdHandler.PointerInputCmdArgs args)
        => SendFirstPersonInteraction(InteractionAction3D.Pull, in args);

    private bool SendFirstPersonInteraction(
        InteractionAction3D action,
        in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (!IsFirstPersonSession(args.Session))
            return false;

        if (args.State == BoundKeyState.Down)
            RaisePredictiveEvent(new Interaction3DRequestEvent(action));

        // Consume both edges so no hidden 2D pointer state is serialized.
        return true;
    }

    private bool IsFirstPersonSession(ICommonSession? session)
    {
        return session?.AttachedEntity is { } player &&
               TryComp(player, out InputMoverComponent? mover) &&
               mover.FirstPersonMode;
    }

}

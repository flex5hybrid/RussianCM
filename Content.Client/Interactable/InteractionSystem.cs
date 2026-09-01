using Content.Shared.Input;
using Content.Shared.Interaction;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;

namespace Content.Client.Interactable;

// TODO Remove Shared prefix
public sealed class InteractionSystem : SharedInteractionSystem
{
    private const float FirstPersonInteractionRayRange = InteractionRange + 0.5f;

    [Dependency] private InputSystem _input = default!;
    [Dependency] private World3DGridRenderingSystem _world3D = default!;

    private bool _reinjectingFirstPersonInput;

    public override void Initialize()
    {
        // Resolve the center-screen 3D target before the legacy shared interaction handlers run.
        // The modified input is then replayed through those handlers, so prediction and the server receive
        // the same EntityUid while all existing access/range/LOS validation remains authoritative.
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
        => RedirectFirstPersonInteraction(EngineKeyFunctions.Use, in args);

    private bool HandleFirstPersonActivate(in PointerInputCmdHandler.PointerInputCmdArgs args)
        => RedirectFirstPersonInteraction(ContentKeyFunctions.ActivateItemInWorld, in args);

    private bool HandleFirstPersonAltActivate(in PointerInputCmdHandler.PointerInputCmdArgs args)
        => RedirectFirstPersonInteraction(ContentKeyFunctions.AltActivateItemInWorld, in args);

    private bool HandleFirstPersonPull(in PointerInputCmdHandler.PointerInputCmdArgs args)
        => RedirectFirstPersonInteraction(ContentKeyFunctions.TryPullObject, in args);

    private bool RedirectFirstPersonInteraction(
        BoundKeyFunction function,
        in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (_reinjectingFirstPersonInput ||
            args.State != BoundKeyState.Down ||
            args.OriginalMessage is not ClientFullInputCmdMessage original ||
            !_world3D.TryRaycastFirstPerson(FirstPersonInteractionRayRange, out var hit) ||
            !TryComp(hit.Entity, out TransformComponent? targetTransform))
        {
            return false;
        }

        var replacement = new ClientFullInputCmdMessage(
            original.Tick,
            original.SubTick,
            original.InputFunctionId,
            targetTransform.Coordinates,
            original.ScreenCoordinates,
            original.State,
            hit.Entity)
        {
            InputSequence = original.InputSequence,
        };

        _reinjectingFirstPersonInput = true;
        try
        {
            // replay=true skips the already-applied key-state transition while still running the normal
            // content handlers and dispatching the replacement target to the server exactly once.
            _input.HandleInputCommand(args.Session, function, replacement, replay: true);
        }
        finally
        {
            _reinjectingFirstPersonInput = false;
        }

        // Consume the original 2D pointer command so it cannot also interact with whatever is under the hidden cursor.
        return true;
    }
}

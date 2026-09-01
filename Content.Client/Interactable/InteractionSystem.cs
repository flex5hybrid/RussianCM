using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Item;
using Content.Shared.Movement.Components;
using Content.Shared.UserInterface;
using Content.Shared.Wall;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Player;

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
            !IsFirstPersonSession(args.Session))
        {
            return false;
        }

        if (_world3D.TryRaycastFirstPerson(
                FirstPersonInteractionRayRange,
                IsSpriteBackedInteractionCandidate,
                out var hit) &&
            TryComp(hit.Entity, out TransformComponent? targetTransform))
        {
            ReinjectFirstPersonInput(
                args.Session,
                function,
                original,
                targetTransform.Coordinates,
                hit.Entity);
            return true;
        }

        // Pulling requires an entity. A first-person miss must therefore stop here instead of falling through
        // to the entity that happens to be under the now-hidden 2D cursor.
        if (function == ContentKeyFunctions.TryPullObject)
            return true;

        // World-use actions are still allowed to target empty space. Feed the existing interaction pipeline
        // an XY compatibility coordinate sampled from the actual 3D centre-screen ray, never the hidden cursor.
        if (_world3D.TryGetFirstPersonAimCoordinates(FirstPersonInteractionRayRange, out var aimCoordinates))
        {
            ReinjectFirstPersonInput(
                args.Session,
                function,
                original,
                aimCoordinates,
                EntityUid.Invalid);
        }

        // Even if the 3D camera cannot currently produce a ray, consume first-person pointer input. Falling
        // through would silently restore legacy cursor targeting, which is explicitly forbidden in FPS mode.
        return true;
    }

    private bool IsFirstPersonSession(ICommonSession? session)
    {
        return session?.AttachedEntity is { } player &&
               TryComp(player, out InputMoverComponent? mover) &&
               mover.FirstPersonMode;
    }

    /// <summary>
    /// Hard physical entities are always considered by the Robust 3D ray as blockers/targets. This predicate
    /// only decides which entities without hard fixtures should receive a sprite-derived interaction volume.
    /// </summary>
    private bool IsSpriteBackedInteractionCandidate(EntityUid uid)
    {
        return HasComp<ItemComponent>(uid) ||
               HasComp<ActivatableUIComponent>(uid) ||
               HasComp<InteractionRelayComponent>(uid) ||
               HasComp<WallMountComponent>(uid);
    }

    private void ReinjectFirstPersonInput(
        ICommonSession? session,
        BoundKeyFunction function,
        ClientFullInputCmdMessage original,
        EntityCoordinates coordinates,
        EntityUid target)
    {
        var replacement = new ClientFullInputCmdMessage(
            original.Tick,
            original.SubTick,
            original.InputFunctionId,
            coordinates,
            original.ScreenCoordinates,
            original.State,
            target)
        {
            InputSequence = original.InputSequence,
        };

        _reinjectingFirstPersonInput = true;
        try
        {
            // replay=true skips the already-applied key-state transition while still running the normal
            // content handlers and dispatching the replacement target to the server exactly once.
            _input.HandleInputCommand(session, function, replacement, replay: true);
        }
        finally
        {
            _reinjectingFirstPersonInput = false;
        }
    }
}

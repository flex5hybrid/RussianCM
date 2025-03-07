using System.Linq;
using Content.Shared.Imperial.Medieval.Magic.Minigames;
using Content.Shared.Imperial.Medieval.Magic.Minigames.Events;
using Content.Shared.Imperial.Minigames.Events;
using Content.Shared.Input;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client.Imperial.Medieval.Magic.Minigames;


public sealed partial class MedievalArrowsMinigameSystem : SharedMedievalArrowsMinigameSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private MedievalArrowsMinigameOverlay _overlay = default!;


    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();

        SubscribeNetworkEvent<StartMinigameEvent>(OnMinigameStart);
        SubscribeLocalEvent<MedievalArrowsMinigameComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<MedievalArrowsMinigameComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<MedievalArrowsMinigameComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ArcadeUp, new PointerInputCmdHandler((session, _, _) => ChangeSequence(session, ArrowsTypes.ArrowUp)))
            .Bind(ContentKeyFunctions.ArcadeDown, new PointerInputCmdHandler((session, _, _) => ChangeSequence(session, ArrowsTypes.ArrowDown)))
            .Bind(ContentKeyFunctions.ArcadeLeft, new PointerInputCmdHandler((session, _, _) => ChangeSequence(session, ArrowsTypes.ArrowLeft)))
            .Bind(ContentKeyFunctions.ArcadeRight, new PointerInputCmdHandler((session, _, _) => ChangeSequence(session, ArrowsTypes.ArrowRight)))
            .Register<MedievalArrowsMinigameSystem>();
    }

    private void OnMinigameStart(StartMinigameEvent args)
    {
        var uid = GetEntity(args.Player);

        if (_player.LocalEntity != uid) return;
        if (_overlayManager.AllOverlays.Contains(_overlay)) return;

        if (!TryComp<MedievalArrowsMinigameComponent>(uid, out var component)) return;

        _overlay.Combination = component.Combination;
        _overlay.PlayerCombination = new();

        _overlayManager.AddOverlay(_overlay);
    }

    private void OnShutdown(EntityUid uid, MedievalArrowsMinigameComponent component, ComponentShutdown args)
    {
        if (_player.LocalEntity != uid) return;

        _overlayManager.RemoveOverlay(_overlay);
    }

    private void OnPlayerAttached(EntityUid uid, MedievalArrowsMinigameComponent component, LocalPlayerAttachedEvent args)
    {
        if (_player.LocalEntity != uid) return;
        if (_overlayManager.AllOverlays.Contains(_overlay)) return;

        _overlayManager.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, MedievalArrowsMinigameComponent component, LocalPlayerDetachedEvent args)
    {
        if (_player.LocalEntity != uid) return;

        _overlayManager.RemoveOverlay(_overlay);
    }

    #region Key Binds

    private bool ChangeSequence(ICommonSession? playerSession, ArrowsTypes type)
    {
        if (!_timing.IsFirstTimePredicted) return false;
        if (playerSession?.AttachedEntity is not { Valid: true } player || !Exists(player)) return false;

        if (!TryComp<MedievalArrowsMinigameComponent>(player, out var component)) return false;
        if (component.NextProbTIme >= _timing.CurTime) return false;

        component.PlayerCombination.Add(type);

        if (!IsValidCombination(component.Combination, component.PlayerCombination))
        {
            component.PlayerCombination.Clear();

            foreach (var _ in _overlay.PlayerCombination.ToList())
                _overlay.TryRemoveLastPlayerCombination();

            component.NextProbTIme = _timing.CurTime + component.ProbDelay;

            RaiseNetworkEvent(new MedievalArrowInvalidCombination()
            {
                Player = GetNetEntity(player)
            });

            return false;
        }

        if (component.Combination.Count == component.PlayerCombination.Count)
        {
            _overlay.TryAddCorrectCombination();
            component.PlayerCombination.Clear();

            RaiseNetworkEvent(new MedievalArrowValidCombination()
            {
                Player = GetNetEntity(player)
            });

            return false;
        }

        _overlay.TryAddCorrectCombination();

        return false;
    }

    #endregion

    #region Helpers

    private bool IsValidCombination(List<ArrowsTypes> combination, List<ArrowsTypes> playerCombination)
    {
        for (var i = 0; i < playerCombination.Count; i++)
            if (playerCombination[i] != combination[i]) return false;

        return true;
    }

    #endregion
}

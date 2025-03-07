using Content.Shared.Imperial.MouseInput;
using Content.Shared.Imperial.MouseInput.Events;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Map;

namespace Content.Client.Imperial.MouseInput;


public sealed partial class MouseInputSystem : SharedMouseInputSystem
{
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<MousePositionRequestEvent>(OnMousePositionRequest);
    }

    private void OnMousePositionRequest(MousePositionRequestEvent args)
    {
        var player = GetEntity(args.Player);

        if (player != _playerManager.LocalEntity) return;
        if (!TryGetCursorEntityCoordinates(out var coords)) return;

        RaisePredictiveEvent(new MousePositionResponseEvent()
        {
            Coordinates = GetNetCoordinates(coords!.Value),
            Performer = args.Performer
        });
    }

    #region Public API

    public override void CreateMousePositionRequest(EntityUid player, EntityUid? performer = null, TimeSpan? bandwidth = null)
    {
        if (!TryGetCursorEntityCoordinates(out var coords)) return;

        RaiseLocalEvent(performer ?? player, new MousePositionRefreshEvent()
        {
            MousePosition = coords!.Value,
            Player = player
        });
    }

    #endregion

    #region Helpers

    private bool TryGetCursorEntityCoordinates(out EntityCoordinates? coords)
    {
        coords = null;

        var mapPosition = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition);

        if (mapPosition.MapId == MapId.Nullspace) return false;
        if (!_mapSystem.TryGetMap(mapPosition.MapId, out var mapUid)) return false;

        coords = _transformSystem.ToCoordinates(mapUid.Value, mapPosition);

        return true;
    }

    #endregion
}

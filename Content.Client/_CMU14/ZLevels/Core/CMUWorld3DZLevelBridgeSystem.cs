using Content.Shared._CMU14.ZLevels.Core.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Client._CMU14.ZLevels.Core;

/// <summary>
/// Exposes all maps in the local player's CMU z-level network to Robust's perspective renderer.
/// This keeps the legacy MapId separation for simulation/PVS while composing the maps into one 3D scene.
/// </summary>
public sealed class CMUWorld3DZLevelBridgeSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private World3DGridRenderingSystem _world3D = default!;

    private EntityQuery<TransformComponent> _transformQuery;
    private EntityQuery<CMUZLevelMapComponent> _zMapQuery;
    private EntityQuery<CMUZLevelsNetworkComponent> _networkQuery;
    private EntityQuery<MapComponent> _mapQuery;
    private readonly List<MapId> _renderMaps = new();

    public override void Initialize()
    {
        base.Initialize();

        _transformQuery = GetEntityQuery<TransformComponent>();
        _zMapQuery = GetEntityQuery<CMUZLevelMapComponent>();
        _networkQuery = GetEntityQuery<CMUZLevelsNetworkComponent>();
        _mapQuery = GetEntityQuery<MapComponent>();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        _renderMaps.Clear();

        if (_player.LocalEntity is not { Valid: true } player ||
            !_transformQuery.TryGetComponent(player, out var playerTransform) ||
            playerTransform.MapUid is not { } playerMap ||
            !_zMapQuery.TryGetComponent(playerMap, out var zMap) ||
            !zMap.NetworkUid.IsValid() ||
            !_networkQuery.TryGetComponent(zMap.NetworkUid, out var network))
        {
            _world3D.ClearRenderMaps();
            return;
        }

        foreach (var mapUid in network.ZLevels.Values)
        {
            if (mapUid is not { } map ||
                !_mapQuery.TryGetComponent(map, out var mapComponent) ||
                mapComponent.MapId == MapId.Nullspace)
            {
                continue;
            }

            _renderMaps.Add(mapComponent.MapId);
        }

        if (_renderMaps.Count == 0)
            _world3D.ClearRenderMaps();
        else
            _world3D.SetRenderMaps(_renderMaps);
    }

    public override void Shutdown()
    {
        _world3D.ClearRenderMaps();
        base.Shutdown();
    }
}

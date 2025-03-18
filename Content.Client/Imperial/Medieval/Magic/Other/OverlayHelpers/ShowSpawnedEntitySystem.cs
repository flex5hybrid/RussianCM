using System.Linq;
using Content.Client.Imperial.OverlayTargetingHelpers.PrespawnOverlayHelper;
using Content.Shared.Imperial.Medieval.Magic.Overlays;
using Content.Shared.Input;
using Robust.Client.Graphics;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client.Imperial.Medieval.Magic.Overlays;


public sealed partial class ShowSpawnedEntitySystem : SharedShowSpawnedEntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private Dictionary<EntityUid, ShowSpawnedEntityOverlay> _overlay = default!;


    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();

        SubscribeLocalEvent<ShowSpawnedEntityComponent, MedievalActionStartTargetingEvent>(OnStartTargeting);
        SubscribeLocalEvent<ShowSpawnedEntityComponent, MedievalActionStopTargetingEvent>(OnStopTargeting);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.MouseMiddle, new PointerInputCmdHandler(MiddleMousePressed))
            .Register<ShowSpawnedEntitySystem>();
    }

    private void OnStartTargeting(EntityUid uid, ShowSpawnedEntityComponent component, MedievalActionStartTargetingEvent args)
    {
        if (!component.Sprites.Any()) return;

        if (!_overlay.TryGetValue(uid, out var _))
            _overlay.Add(uid, new());

        component.IsActive = true;

        _overlay[uid].Sprites = component.Sprites;

        _overlayManager.AddOverlay(_overlay[uid]);
    }

    private void OnStopTargeting(EntityUid uid, ShowSpawnedEntityComponent component, MedievalActionStopTargetingEvent args)
    {
        component.IsActive = false;

        _overlayManager.RemoveOverlay(_overlay[uid]);
    }

    private bool MiddleMousePressed(ICommonSession? playerSession, EntityCoordinates coordinates, EntityUid entity)
    {
        if (!_timing.IsFirstTimePredicted) return false;
        if (playerSession?.AttachedEntity is not { Valid: true } player || !Exists(player)) return false;

        var enumerator = EntityQueryEnumerator<ShowSpawnedEntityComponent>();

        while (enumerator.MoveNext(out var uid, out var component))
        {
            if (!component.IsActive) continue;
            if (!_overlay.TryGetValue(uid, out var _)) continue;

            _overlay[uid].Rotation -= component.RotationPerClick;

            RaiseNetworkEvent(new SpawnedEntitySpriteRotateEvent()
            {
                Action = GetNetEntity(uid),
                Rotation = _overlay[uid].Rotation
            });
        }

        return false;
    }
}

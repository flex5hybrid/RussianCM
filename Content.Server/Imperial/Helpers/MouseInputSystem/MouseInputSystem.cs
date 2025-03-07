
using Content.Shared.Imperial.MouseInput;
using Content.Shared.Imperial.MouseInput.Events;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.Imperial.MouseInput;


public sealed partial class MouseInputSystem : SharedMouseInputSystem
{
    #region Public API

    public override void CreateMousePositionRequest(EntityUid player, EntityUid? performer = null, TimeSpan? bandwidth = null)
    {
        if (!HasComp<ActorComponent>(player)) return;

        if (TryGetCache(player, out var cachedCoords))
        {
            UpdateCache(player, cachedCoords!.Value);
            RaiseLocalEvent(performer ?? player, new MousePositionRefreshEvent()
            {
                MousePosition = cachedCoords!.Value,
                Player = player,
            });
        }

        UpdateCache(player, EntityCoordinates.Invalid, bandwidth);
        RaiseNetworkEvent(new MousePositionRequestEvent()
        {
            Performer = GetNetEntity(performer ?? player),
            Player = GetNetEntity(player)
        }, player);
    }

    #endregion
}

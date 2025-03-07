using Robust.Shared.Map;

namespace Content.Shared.Imperial.MouseInput.Events;


public sealed class MousePositionRefreshEvent : EntityEventArgs
{
    public EntityCoordinates MousePosition;

    public EntityUid Player;
}

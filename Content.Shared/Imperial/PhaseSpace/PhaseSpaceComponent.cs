using Content.Shared.Physics;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Imperial.PhaseSpace;


/// <summary>
/// Sends an entity into phase space
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PhaseSpaceComponent : Component
{
    /// <summary>
    /// Cached layers value that will be returned to the entity when the component is removed.
    /// </summary>
    [ViewVariables]
    public Dictionary<string, (int, int)> СachedCollisionLayers = new();
}

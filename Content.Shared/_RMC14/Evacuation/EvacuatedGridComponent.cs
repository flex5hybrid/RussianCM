using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Evacuation;

/// <summary>
/// Marker component added to grids that have been launched via evacuation (lifeboat or escape pod).
/// Used by kill-all rules to exclude evacuated entities from victory condition counts.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EvacuatedGridComponent : Component
{
}


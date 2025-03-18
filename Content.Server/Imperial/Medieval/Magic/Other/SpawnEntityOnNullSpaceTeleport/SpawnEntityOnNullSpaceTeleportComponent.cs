using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.Medieval.Magic.SpawnEntityOnNullSpaceTeleport;


/// <summary>
/// Spawn entity when this component remove
/// </summary>
[RegisterComponent]
public sealed partial class SpawnEntityOnNullSpaceTeleportComponent : Component
{
    [DataField]
    public EntProtoId Spawn = "";
}

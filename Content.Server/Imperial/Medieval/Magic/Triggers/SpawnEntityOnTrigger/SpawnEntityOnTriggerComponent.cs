using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.Medieval.Magic.Triggers;

/// <summary>
/// Spawn entity on trigger
/// </summary>
[RegisterComponent, Access(typeof(SpawnEntityOnTriggerSystem))]
public sealed partial class SpawnEntityOnTriggerComponent : Component
{
    [DataField(required: true)]
    public List<EntProtoId> SpawnedEntitiesPrototype;
}

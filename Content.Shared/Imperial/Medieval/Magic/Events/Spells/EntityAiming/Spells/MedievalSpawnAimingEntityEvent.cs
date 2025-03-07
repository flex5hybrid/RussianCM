using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Medieval.Magic;


/// <summary>
/// Spawn a entities from <see cref="ProjectilePrototype" /> and rise specified event to each entity with target counts
/// </summary>
public sealed partial class MedievalSpawnAimingEntityEvent : MedievalEntityAimingSpellEvent
{
    /// <summary>
    /// What entity should be spawned.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId SpawnedEntity = new();
}

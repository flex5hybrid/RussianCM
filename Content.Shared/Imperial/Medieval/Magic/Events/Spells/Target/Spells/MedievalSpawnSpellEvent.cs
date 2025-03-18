using System.Numerics;
using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Medieval.Magic;


/// <summary>
/// Called to cast a spell with enity spawn
/// </summary>
public sealed partial class MedievalSpawnSpellEvent : MedievalTargetSpellEvent
{
    /// <summary>
    /// What entity should be spawned.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId SpawnedEntityPrototype;

    /// <summary>
    /// Spawn type. Either by mouse cursor or from the entity itself
    /// </summary>
    [DataField(required: true)]
    public SpawnSpellType SpawnType;

    /// <summary>
    /// Tiles that will be subtracted from the selected position. Allows, for example, to spawn a wall in front of the entity, and not in it
    /// </summary>
    [DataField]
    public Vector2 RelativePosition = new Vector2(0, 0);
}

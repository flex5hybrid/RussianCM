namespace Content.Server.Imperial.Medieval.Magic.MedievalSpellRandomRelativeSpawn;


/// <summary>
///
/// </summary>
[RegisterComponent]
public sealed partial class MedievalSpellRandomRelativeSpawnComponent : Component
{
    /// <summary>
    /// A radius in tiles when we can spawn entity
    /// </summary>
    [DataField]
    public float MaxSpawnRadius = 2f;

    /// <summary>
    /// A minimum radius in tiles when we can spawn entity
    /// </summary>
    [DataField]
    public float MinSpawnRadius = 0f;
}

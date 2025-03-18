using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Medieval.Magic;


/// <summary>
/// Called to cast a spell with a projectile
/// </summary>
public sealed partial class MedievalEntityTargetProjectileSpellEvent : MedievalEntityAimingSpellEvent
{
    /// <summary>
    /// What entity should be spawned.
    /// </summary>
    [DataField]
    public EntProtoId ProjectilePrototype = new();

    /// <summary>
    /// Start projectile speed
    /// </summary>
    [DataField]
    public float ProjectileSpeed = 20f;
}

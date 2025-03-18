using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Medieval.Magic;


/// <summary>
/// Called to cast a spell with a projectile
/// </summary>
public sealed partial class MedievalHomingProjectilesSpellEvent : MedievalEntityAimingSpellEvent
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

    /// <summary>
    /// A max spread
    /// </summary>
    [DataField]
    public Angle Spread = Angle.FromDegrees(180);

    [DataField]
    public float LinearVelocityIntensy = 1.0f;

    [DataField]
    public Angle RelativeAngle = Angle.Zero;
}

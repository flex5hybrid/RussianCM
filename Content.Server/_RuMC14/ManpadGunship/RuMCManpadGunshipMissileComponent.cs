using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._RuMC14.ManpadGunship;

/// <summary>A missile in flight.</summary>
[RegisterComponent, UnsavedComponent]
public sealed partial class RuMCManpadGunshipMissileComponent : Component
{
    public EntityUid Shooter;
    public EntityUid Target;
    public TimeSpan ImpactAt;
    public float Damage;
    public EntProtoId HitEffect;
    public int HitEffectCount;
    public float HitEffectSpread;
    public SoundSpecifier? ImpactSound;
    public float SoundRange;
}

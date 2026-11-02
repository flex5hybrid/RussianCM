using Content.Shared._RuMC14.ManpadLowAltitude;
using Robust.Shared.Audio;

namespace Content.Server._RuMC14.ManpadLowAltitude;

/// <summary>A missile in flight.</summary>
[RegisterComponent, UnsavedComponent]
public sealed partial class RuMCManpadLowAltitudeMissileComponent : Component
{
    public EntityUid Shooter;
    public EntityUid Target;
    public TimeSpan ImpactAt;
    public RuMCManpadLowAltitudeHitProfile Hit = default!;
    public SoundSpecifier? ImpactSound;
    public float SoundRange;
}

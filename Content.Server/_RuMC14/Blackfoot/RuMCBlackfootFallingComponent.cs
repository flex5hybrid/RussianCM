namespace Content.Server._RuMC14.Blackfoot;

/// <summary>A destroyed Blackfoot spinning out of control until <see cref="CrashAt"/>.</summary>
[RegisterComponent, UnsavedComponent]
public sealed partial class RuMCBlackfootFallingComponent : Component
{
    public TimeSpan CrashAt;
    public float AngularVelocityDegrees;
}

namespace Content.Server._RuMC14.ManpadLowAltitude;

[RegisterComponent, UnsavedComponent]
public sealed partial class RuMCManpadLowAltitudeAimingComponent : Component
{
    public EntityUid? Target;
    public TimeSpan NextScan;
    public TimeSpan LockStartedAt;
    public TimeSpan LockCompleteAt;
    public TimeSpan NextBeep;
    public TimeSpan NextAcquire;
}

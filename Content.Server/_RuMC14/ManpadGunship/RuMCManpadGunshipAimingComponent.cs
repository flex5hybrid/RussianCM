namespace Content.Server._RuMC14.ManpadGunship;

[RegisterComponent, UnsavedComponent]
public sealed partial class RuMCManpadGunshipAimingComponent : Component
{
    public EntityUid? Target;
    public TimeSpan NextScan;
    public TimeSpan LockStartedAt;
    public TimeSpan LockCompleteAt;
    public TimeSpan NextBeep;
    public TimeSpan NextAcquire;
}

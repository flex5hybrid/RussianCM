namespace Content.Shared._RuMC14.NightVision;

/// <summary>
/// Broadcast before a night vision item applies its vision to the user.
/// </summary>
[ByRefEvent]
public readonly record struct RuMCNightVisionItemEnablingEvent(EntityUid Item, EntityUid User);

/// <summary>
/// Raised on an active night vision item before another source switches it off.
/// </summary>
[ByRefEvent]
public readonly record struct RuMCNightVisionItemDisplacedEvent(EntityUid User);

 namespace Content.Shared._RMC14.Evacuation;

/// <summary>
/// Broadcast event raised when an evacuation grid (lifeboat or escape pod) launches via FTL.
/// Used by kill-all rules to re-check victory conditions so the round doesn't softlock.
/// </summary>
[ByRefEvent]
public readonly record struct EvacuationLaunchedEvent(EntityUid Grid);


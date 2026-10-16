namespace Content.Shared._RuMC14.Vehicle;

/// <summary>
/// Raised on a mob after it was forced out of <paramref name="Vehicle"/> and thrown clear
/// </summary>
[ByRefEvent]
public readonly record struct RuMCVehicleOccupantEjectedEvent(EntityUid Vehicle, bool CookOff);

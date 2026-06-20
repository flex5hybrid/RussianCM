namespace Content.Server.AU14.Round;

/// <summary>
/// Raised after all faction ships (govfor/opfor) have been loaded and
/// ShipFactionComponent has been attached to their grids.
/// AddJobsRuleSystem subscribes to this to add ship-side jobs after ships exist.
/// </summary>
public sealed class AU14ShipsLoadedEvent { }

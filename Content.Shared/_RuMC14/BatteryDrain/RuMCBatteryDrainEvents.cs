namespace Content.Shared._RuMC14.BatteryDrain;

/// <summary>Raised on the item when it is turned on or off.</summary>
[ByRefEvent]
public readonly record struct RuMCBatteryDrainActiveChangedEvent(bool Active);

/// <summary>Raised on the server on an active item whose battery just ran out.</summary>
[ByRefEvent]
public readonly record struct RuMCBatteryDepletedEvent;

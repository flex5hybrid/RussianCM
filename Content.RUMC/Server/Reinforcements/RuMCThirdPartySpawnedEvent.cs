using Content.Shared._RuMC14.Reinforcements;

namespace Content.Server._RuMC14.Reinforcements;

[ByRefEvent]
public readonly record struct RuMCThirdPartySpawnedEvent(
    RuMCReinforcementCall Call,
    IReadOnlyList<EntityUid> Leaders,
    IReadOnlyList<EntityUid> Members);

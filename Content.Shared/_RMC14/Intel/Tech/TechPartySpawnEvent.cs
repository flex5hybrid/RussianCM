using Content.Shared._RuMC14.Reinforcements; // RuMC edit
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Intel.Tech;

[DataRecord]
[Serializable, NetSerializable]
public sealed partial record TechPartySpawnEvent(string ThirdPartyId)
{
    public string Team { get; init; } = string.Empty;

    public RuMCReinforcementCall? Call { get; init; } // RuMC edit
}

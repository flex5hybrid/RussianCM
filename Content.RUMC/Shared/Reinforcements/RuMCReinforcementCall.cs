using Robust.Shared.Serialization;

namespace Content.Shared._RuMC14.Reinforcements;

[DataRecord]
[Serializable, NetSerializable]
public sealed partial record RuMCReinforcementCall(string Side, string Message);

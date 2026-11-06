using Content.Shared._RMC14.Dialog;
using Robust.Shared.Serialization;

namespace Content.Shared._RuMC14.Reinforcements;

[Serializable, NetSerializable]
public sealed record RuMCReinforcementMessageEvent(string Message = "") : DialogInputEvent(Message);

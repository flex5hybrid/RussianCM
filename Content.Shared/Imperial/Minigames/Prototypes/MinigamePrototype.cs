using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Minigames;


[Prototype("minigame")]
[DataDefinition, Serializable, NetSerializable]
public sealed partial class MinigamePrototype : MinigameData, IPrototype;

using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Medieval.Magic.Minigames.Events;


[Serializable, NetSerializable]
public sealed partial class MedievalArrowValidCombination : EntityEventArgs
{
    public NetEntity Player;
}

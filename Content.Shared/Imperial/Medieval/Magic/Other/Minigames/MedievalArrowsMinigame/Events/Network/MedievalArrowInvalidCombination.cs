using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Medieval.Magic.Minigames.Events;


[Serializable, NetSerializable]
public sealed partial class MedievalArrowInvalidCombination : EntityEventArgs
{
    public NetEntity Player;
}

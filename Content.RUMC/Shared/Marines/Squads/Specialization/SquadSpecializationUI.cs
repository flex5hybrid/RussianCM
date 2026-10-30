using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Marines.Squads.Specialization;

[Serializable, NetSerializable]
public enum SquadSpecializationUI : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class SquadSpecializationBuiState : BoundUserInterfaceState
{
    public readonly string SquadName;
    public readonly Color SquadColor;
    public readonly ProtoId<SquadSpecializationPrototype>? CurrentSpecialization;
    public readonly bool IsLocked;
    public readonly List<ProtoId<SquadSpecializationPrototype>> AvailableSpecializations;
    public readonly Dictionary<ProtoId<SquadSpecializationPrototype>, string> TakenSpecializations;

    public SquadSpecializationBuiState(
        string squadName,
        Color squadColor,
        ProtoId<SquadSpecializationPrototype>? currentSpecialization,
        bool isLocked,
        List<ProtoId<SquadSpecializationPrototype>> availableSpecializations,
        Dictionary<ProtoId<SquadSpecializationPrototype>, string> takenSpecializations)
    {
        SquadName = squadName;
        SquadColor = squadColor;
        CurrentSpecialization = currentSpecialization;
        IsLocked = isLocked;
        AvailableSpecializations = availableSpecializations;
        TakenSpecializations = takenSpecializations;
    }
}

[Serializable, NetSerializable]
public sealed class SquadSpecializationSelectBuiMsg : BoundUserInterfaceMessage
{
    public readonly ProtoId<SquadSpecializationPrototype> Specialization;

    public SquadSpecializationSelectBuiMsg(ProtoId<SquadSpecializationPrototype> specialization)
    {
        Specialization = specialization;
    }
}

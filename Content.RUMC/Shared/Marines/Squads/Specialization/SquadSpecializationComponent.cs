using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Squads.Specialization;

/// <summary>
/// Attached to a squad team entity to store its chosen doctrine/specialization.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SquadSpecializationComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<SquadSpecializationPrototype>? Specialization;

    [DataField, AutoNetworkedField]
    public bool Locked;

    [DataField, AutoNetworkedField]
    public TimeSpan? SelectedAt;
}

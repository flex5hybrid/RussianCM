using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Marines.Squads.Specialization;

/// <summary>
/// Component for handheld doctrine specialization tablet.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SquadSpecializationTabletComponent : Component
{
    /// <summary>
    /// Faction identifier (e.g. "UPP", "USCM") to filter allowed specializations.
    /// If null, matches any faction or neutral specializations.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? Faction;
}

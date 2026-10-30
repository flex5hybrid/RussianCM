using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Marines.Squads.Specialization;

/// <summary>
/// Prototype defining a squad specialization doctrine (e.g. VDV, SOF, Recon).
/// </summary>
[Prototype("squadSpecialization")]
public sealed partial class SquadSpecializationPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Description { get; private set; } = default!;

    [DataField]
    public LocId? Summary { get; private set; }

    [DataField]
    public SpriteSpecifier? Icon { get; private set; }

    /// <summary>
    /// Optional faction filter, e.g. "UPP", "USCM". If null, available to all factions.
    /// </summary>
    [DataField]
    public string? Faction { get; private set; }

    /// <summary>
    /// Custom radio announcement key sent to squad channel when this doctrine is confirmed.
    /// </summary>
    [DataField]
    public LocId? Announcement { get; private set; }
}

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Requisitions;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class RequisitionsEntry
{
    [DataField]
    public string? Name;

    [DataField(required: true)]
    public int Cost;

    [DataField(required: true)]
    public EntProtoId Crate;

    [DataField]
    public List<EntProtoId> Entities = new();

    // --- Department order metadata (set at runtime, not from YAML) ---

    /// <summary>Who ordered this (auto-filled from ID card).</summary>
    [NonSerialized]
    public string? DeptOrderedBy;

    /// <summary>User-provided reason for the order.</summary>
    [NonSerialized]
    public string? DeptReason;

    /// <summary>User-provided delivery location.</summary>
    [NonSerialized]
    public string? DeptDeliverTo;

    /// <summary>Access level to lock the crate to, or null for no lock.</summary>
    [NonSerialized]
    public string? DeptAccessLevel;

    /// <summary>Name of the ordering department.</summary>
    [NonSerialized]
    public string? DeptName;
}

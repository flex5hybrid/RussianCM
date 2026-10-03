using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Preferences.Loadouts;

/// <summary>
/// Specifies the selected prototype and custom data for a loadout.
/// </summary>
[Serializable, NetSerializable, DataDefinition]
public sealed partial class Loadout : IEquatable<Loadout>
{
    [DataField]
    public ProtoId<LoadoutPrototype> Prototype;

    // Player-chosen item, name and color, used by loadouts that allow a free choice.
    [DataField]
    public string? CustomEntity;

    [DataField]
    public string? CustomName;

    [DataField]
    public Color? CustomColor;

    public bool Equals(Loadout? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Prototype.Equals(other.Prototype) &&
               CustomEntity == other.CustomEntity &&
               CustomName == other.CustomName &&
               CustomColor == other.CustomColor;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is Loadout other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Prototype, CustomEntity, CustomName, CustomColor);
    }
}

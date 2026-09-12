using Robust.Shared.Prototypes;

namespace Content.Shared.Radio;

/// <summary>
/// Defines a radio channel and its transmission properties.
/// </summary>
[Prototype]
public sealed partial class RadioChannelPrototype : IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Human-readable name for the channel.
    /// </summary>
    [DataField]
    public LocId Name { get; private set; } = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedName => Loc.GetString(Name);

    /// <summary>
    /// Upstream keycode kept in prototype data for compatibility/debugging.
    /// RuCM-facing code should use <see cref="KeyCode"/> instead.
    /// </summary>
    [DataField("keycode"), ViewVariables(VVAccess.ReadOnly)]
    public char CanonicalKeyCode { get; private set; } = '\0';

    /// <summary>
    /// Optional RuCM-facing keycode. When present, this is the primary keycode shown to players
    /// and used by normal chat/UI code.
    /// </summary>
    [DataField("localizedKeycode"), ViewVariables(VVAccess.ReadOnly)]
    public char LocalizedKeyCode { get; private set; } = '\0';

    /// <summary>
    /// Player-facing keycode for this channel.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public char KeyCode => LocalizedKeyCode == '\0' ? CanonicalKeyCode : LocalizedKeyCode;

    [ViewVariables(VVAccess.ReadOnly)]
    public char DisplayKeyCode => KeyCode;

    public bool MatchesKeyCode(char keyCode)
    {
        var normalized = char.ToLowerInvariant(keyCode);
        return char.ToLowerInvariant(KeyCode) == normalized ||
               char.ToLowerInvariant(CanonicalKeyCode) == normalized;
    }

    /// <summary>
    /// Frequency used by the channel.
    /// </summary>
    [DataField]
    public RadioFrequency Frequency { get; private set; } = RadioFrequency.Off;

    /// <summary>
    /// Color used to display the channel.
    /// </summary>
    [DataField]
    public Color Color { get; private set; } = Color.Lime;

    /// <summary>
    /// Whether the channel can transmit across different stations without a telecommunications server.
    /// </summary>
    [DataField]
    public bool LongRange;

    [DataField]
    public bool Tower;

    [DataField]
    public bool Planet = true;


    [DataField]
    public string Faction = string.Empty;

    // AU14: gated combat nets need relay coverage on the map, ungated channels keep stock behavior
    [DataField]
    public bool AnchorGated;
}

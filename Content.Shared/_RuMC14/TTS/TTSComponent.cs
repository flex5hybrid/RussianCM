using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Corvax.TTS;

/// <summary>
/// Apply TTS for entity chat say messages
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TTSComponent : Component
{
    /// <summary>
    /// Prototype of used voice for TTS.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("voice", customTypeSerializer: typeof(PrototypeIdSerializer<TTSVoicePrototype>))]
    public string? VoicePrototypeId { get; set; }

    /// <summary>
    /// Фракция говорящего (ксено / люди)
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("faction")]
    public HearingFaction Faction { get; set; } = HearingFaction.Human;
}

public enum HearingFaction
{
    Human,
    Xeno
}

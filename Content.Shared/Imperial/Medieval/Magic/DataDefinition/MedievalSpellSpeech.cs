using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Medieval.Magic;


/// <summary>
/// The word that the caster will say when casting
/// </summary>
[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public sealed partial class MedievalSpellSpeech
{
    /// <summary>
    /// Try guess
    /// </summary>
    [DataField(required: true)]
    public LocId Speech;

    /// <summary>
    /// the type that will be pronounced
    /// <para>
    /// (Emote/Speak/Wishper)
    /// </para>
    /// </summary>
    [DataField]
    public SpellSpeechType SpeechType = SpellSpeechType.Speak;

    /// <summary>
    /// Color of the message to be colored. Colors the entire message.
    /// </summary>
    [DataField]
    public Color? Color;

    /// <summary>
    /// Doesn't show message in right chat.
    /// </summary>
    /// <remarks>
    /// WARNING: If this parameter is true, then whisper will not work.
    /// </remarks>
    [DataField]
    public bool HideChat = false;
}

using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Medieval.Magic;


/// <summary>
/// Replicated version of <see cref="TransformToChatEnum" /> in Shared.
/// <para>
/// (Thanks Wizards)
/// </para>
/// </summary>
[Serializable, NetSerializable]
public enum SpellSpeechType : byte
{
    Speak,
    Emote,
    Whisper
}

/// <summary>
/// Entity spawn type
/// </summary>
[Serializable, NetSerializable]
public enum SpawnSpellType : byte
{
    SpawnOnMousePosition,
    SpawnOnCaster
}


/// <summary>
/// Target event spawn type
/// </summary>
[Serializable, NetSerializable]
public enum TargetSpellType : byte
{
    /// <summary>
    ///  Apply the spell effect to the current cursor position
    /// </summary>
    TargetOnCurrentMousePosition,
    /// <summary>
    /// Apply the spell effect to the position we chose DoAfter
    /// </summary>
    TargetOnLastMousePosition
}

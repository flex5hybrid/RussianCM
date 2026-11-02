using System.Linq;
using Content.Shared.Humanoid;

namespace Content.Shared.Corvax.TTS;

/// <summary>
/// Shared rules for the character editor and automatic voice assignment.
/// Custom recordings have no sex metadata and are selected explicitly.
/// </summary>
public static class CMUTTSVoiceSelection
{
    public const string RandomVoice = "random";
    public const string DefaultYautjaVoice = "REXXAR";

    /// <summary>
    /// The curated masculine voices available to every Yautja, regardless of sex.
    /// Shared by the editor, profile persistence and server-side checks.
    /// </summary>
    public static readonly string[] YautjaVoiceIds =
    [
        "REXXAR",
        "GARROSH",
        "ILLIDAN",
        "ARTHAS",
        "LORD_HARKON",
        "SUPER_MUTANT_FL3",
    ];

    public static bool IsYautjaVoice(string? voiceId)
    {
        return voiceId != null && YautjaVoiceIds.Contains(voiceId, StringComparer.Ordinal);
    }

    public static bool IsYautjaSelectable(TTSVoicePrototype voice)
    {
        return IsYautjaVoice(voice.ID)
            && voice.Sex == Sex.Male
            && voice.RoundStart
            && !voice.SponsorOnly;
    }

    public static string SanitizeYautjaVoice(string? voiceId)
    {
        return IsYautjaVoice(voiceId) ? voiceId! : DefaultYautjaVoice;
    }

    public static bool MatchesSex(Sex voiceSex, Sex characterSex)
    {
        return characterSex == Sex.Unsexed || voiceSex == Sex.Unsexed || voiceSex == characterSex;
    }

    public static bool IsSelectable(TTSVoicePrototype voice, Sex? sex = null)
    {
        return voice.RoundStart && !voice.SponsorOnly && (sex == null || MatchesSex(voice.Sex, sex.Value));
    }

    public static TTSVoicePrototype[] GetRandomVoices(IEnumerable<TTSVoicePrototype> voices, Sex sex)
    {
        var available = voices.Where(voice => IsSelectable(voice, sex)).OrderBy(voice => voice.ID).ToArray();
        // Prefer the character's own sex; neutral voices are a fallback for sexed humanoids.
        var matching = available.Where(voice => voice.Sex == sex).ToArray();
        return sex != Sex.Unsexed && matching.Length > 0 ? matching : available;
    }
}

/// <summary>
/// Raised after the profile's sex and TTS preference have been applied.
/// Only the server resolves a random preference to an actual voice.
/// </summary>
[ByRefEvent]
public readonly record struct CMUTTSVoiceAppliedEvent;

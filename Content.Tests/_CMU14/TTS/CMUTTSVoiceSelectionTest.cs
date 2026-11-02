using System.Linq;
using Content.Shared.Corvax.TTS;
using Content.Shared.Humanoid;
using NUnit.Framework;

namespace Content.Tests.Shared.TTS;

[TestFixture]
public sealed class CMUTTSVoiceSelectionTest
{
    [TestCase(Sex.Male, Sex.Male, true)]
    [TestCase(Sex.Female, Sex.Female, true)]
    [TestCase(Sex.Male, Sex.Female, false)]
    [TestCase(Sex.Female, Sex.Male, false)]
    [TestCase(Sex.Unsexed, Sex.Male, true)]
    [TestCase(Sex.Unsexed, Sex.Female, true)]
    [TestCase(Sex.Male, Sex.Unsexed, true)]
    [TestCase(Sex.Female, Sex.Unsexed, true)]
    public void VoicesMatchCharacterSex(Sex voice, Sex character, bool matches)
    {
        Assert.That(CMUTTSVoiceSelection.MatchesSex(voice, character), Is.EqualTo(matches));
    }

    [TestCase(Sex.Male, "male")]
    [TestCase(Sex.Female, "female")]
    public void AutomaticVoicesExcludeOtherSexAndRestrictedVoices(Sex sex, string expected)
    {
        var voices = new[]
        {
            Voice("male", Sex.Male), Voice("female", Sex.Female), Voice("neutral", Sex.Unsexed),
            Voice("sponsor", sex, sponsor: true), Voice("hidden", sex, roundStart: false),
        };
        Assert.That(CMUTTSVoiceSelection.GetRandomVoices(voices, sex).Select(voice => voice.ID), Is.EqualTo(new[] { expected }));
    }

    [Test]
    public void NeutralFallbackDoesNotUseOppositeSex()
    {
        var voices = new[] { Voice("male", Sex.Male), Voice("neutral", Sex.Unsexed) };
        Assert.That(CMUTTSVoiceSelection.GetRandomVoices(voices, Sex.Female).Select(voice => voice.ID), Is.EqualTo(new[] { "neutral" }));
        Assert.That(CMUTTSVoiceSelection.GetRandomVoices(new[] { voices[0] }, Sex.Female), Is.Empty);
    }

    [Test]
    public void UnsexedCharactersMayUseAllUnrestrictedVoices()
    {
        var voices = new[] { Voice("male", Sex.Male), Voice("female", Sex.Female), Voice("neutral", Sex.Unsexed) };
        Assert.That(CMUTTSVoiceSelection.GetRandomVoices(voices, Sex.Unsexed), Has.Length.EqualTo(3));
    }

    [Test]
    public void YautjaWhitelistIsStrictAndHasSafeFallback()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CMUTTSVoiceSelection.IsYautjaVoice("REXXAR"), Is.True);
            Assert.That(CMUTTSVoiceSelection.IsYautjaVoice("PUCHKOW"), Is.False);
            Assert.That(CMUTTSVoiceSelection.IsYautjaVoice("custom:MyVoice"), Is.False);
            Assert.That(CMUTTSVoiceSelection.SanitizeYautjaVoice("custom:MyVoice"),
                Is.EqualTo(CMUTTSVoiceSelection.DefaultYautjaVoice));
            Assert.That(CMUTTSVoiceSelection.SanitizeYautjaVoice(null),
                Is.EqualTo(CMUTTSVoiceSelection.DefaultYautjaVoice));
            Assert.That(CMUTTSVoiceSelection.IsYautjaSelectable(Voice("GARROSH", Sex.Male)), Is.True);
            Assert.That(CMUTTSVoiceSelection.IsYautjaSelectable(Voice("GARROSH", Sex.Female)), Is.False);
            Assert.That(CMUTTSVoiceSelection.IsYautjaSelectable(Voice("GARROSH", Sex.Male, sponsor: true)), Is.False);
            Assert.That(CMUTTSVoiceSelection.IsYautjaSelectable(Voice("GARROSH", Sex.Male, roundStart: false)), Is.False);
        });
    }

    private static TTSVoicePrototype Voice(string id, Sex sex, bool sponsor = false, bool roundStart = true)
    {
        // Prototype fields have private setters; construct minimal fixtures without loading game resources.
        var voice = new TTSVoicePrototype();
        typeof(TTSVoicePrototype).GetProperty(nameof(TTSVoicePrototype.ID))!.SetValue(voice, id);
        typeof(TTSVoicePrototype).GetProperty(nameof(TTSVoicePrototype.Sex))!.SetValue(voice, sex);
        typeof(TTSVoicePrototype).GetProperty(nameof(TTSVoicePrototype.SponsorOnly))!.SetValue(voice, sponsor);
        typeof(TTSVoicePrototype).GetProperty(nameof(TTSVoicePrototype.RoundStart))!.SetValue(voice, roundStart);
        return voice;
    }
}

using Content.IntegrationTests.Fixtures;
using Content.Server.CMU14.Yautja;
using Content.Server.Corvax.TTS;
using Content.Shared.CMU14.Yautja;
using Content.Shared.Corvax.TTS;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.CMU14.TTS;

[TestFixture]
public sealed class CMUTTSVoiceAssignmentTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = """
        - type: entity
          id: CMUTTSFemaleTest
          components:
          - type: TTS
          - type: HumanoidProfile
            sex: Female

        - type: entity
          id: CMUTTSFixedTest
          components:
          - type: TTS
            voice: TURRET_FLOOR
        """;

    [Test]
    public async Task RandomGhostRoleProfilesReceiveVoicesMatchingTheirSex()
    {
        await Server.WaitAssertion(() =>
        {
            for (var i = 0; i < 32; i++)
            {
                var profile = HumanoidCharacterProfile.RandomWithSpecies("Human");
                var voice = SProtoMan.Index<TTSVoicePrototype>(profile.TTSVoice);
                Assert.That(CMUTTSVoiceSelection.IsSelectable(voice, profile.Sex), Is.True);
                Assert.That(voice.Sex, Is.EqualTo(profile.Sex));
            }
        });
    }

    [Test]
    public async Task MapInitializationAssignsFemaleVoiceAndPreservesFixedVoices()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var female = SEntMan.SpawnEntity("CMUTTSFemaleTest", map.GridCoords);
            var fixedVoice = SEntMan.SpawnEntity("CMUTTSFixedTest", map.GridCoords);
            try
            {
                var voice = SProtoMan.Index<TTSVoicePrototype>(SEntMan.GetComponent<TTSComponent>(female).VoicePrototypeId!);
                Assert.That(voice.Sex, Is.EqualTo(Sex.Female));
                Assert.That(SEntMan.GetComponent<TTSComponent>(fixedVoice).VoicePrototypeId, Is.EqualTo("TURRET_FLOOR"));
            }
            finally
            {
                SEntMan.DeleteEntity(female);
                SEntMan.DeleteEntity(fixedVoice);
            }
        });
    }

    [TestCase("CMUMobYautja")]
    [TestCase("CMUMobYautjaYoungblood")]
    public async Task YautjaVoicesAreAvailableAndEnforcedRegardlessOfSex(string prototype)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            foreach (var id in CMUTTSVoiceSelection.YautjaVoiceIds)
            {
                var voicePrototype = SProtoMan.Index<TTSVoicePrototype>(id);
                Assert.That(CMUTTSVoiceSelection.IsYautjaSelectable(voicePrototype), Is.True, id);
            }

            var entity = SEntMan.SpawnEntity(prototype, map.GridCoords);
            try
            {
                var profiles = SEntMan.System<YautjaProfileApplySystem>();
                var tts = SEntMan.System<TTSSystem>();
                var hunter = YautjaCharacterProfile.Default
                    .WithSex(Sex.Female)
                    .WithTTSVoice("GARROSH");

                // The real Yautja application path must restore the choice after humanoid voice validation.
                profiles.ApplyProfile(entity, hunter, equipProfileGear: false);
                Assert.That(SEntMan.GetComponent<TTSComponent>(entity).VoicePrototypeId, Is.EqualTo("GARROSH"));

                // Reapplying the hunter profile must preserve the selected voice.
                profiles.ApplyProfile(entity, hunter, equipProfileGear: false);
                Assert.That(SEntMan.GetComponent<TTSComponent>(entity).VoicePrototypeId, Is.EqualTo("GARROSH"));

                profiles.ApplyProfile(entity, hunter.WithTTSVoice("PUCHKOW"), equipProfileGear: false);
                Assert.That(SEntMan.GetComponent<TTSComponent>(entity).VoicePrototypeId,
                    Is.EqualTo(CMUTTSVoiceSelection.DefaultYautjaVoice));

                // Server-side enforcement must not trust a caller even if it bypasses profile sanitization.
                tts.ApplyYautjaProfileVoice(entity, "custom:MyVoice");
                Assert.That(SEntMan.GetComponent<TTSComponent>(entity).VoicePrototypeId,
                    Is.EqualTo(CMUTTSVoiceSelection.DefaultYautjaVoice));
            }
            finally
            {
                SEntMan.DeleteEntity(entity);
            }
        });
    }

    [Test]
    public async Task ProfileApplicationResolvesRandomUsingNewSexAndKeepsExplicitChoice()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var entity = SEntMan.SpawnEntity("CMUTTSFemaleTest", map.GridCoords);
            try
            {
                var profiles = SEntMan.System<HumanoidProfileSystem>();
                var profile = HumanoidCharacterProfile.DefaultWithSpecies().WithSex(Sex.Male);
                profiles.ApplyProfileTo(entity, profile.WithTTSVoice(CMUTTSVoiceSelection.RandomVoice));
                var randomVoice = SEntMan.GetComponent<TTSComponent>(entity).VoicePrototypeId!;
                Assert.That(SProtoMan.Index<TTSVoicePrototype>(randomVoice).Sex, Is.EqualTo(Sex.Male));

                profiles.ApplyProfileTo(entity, profile.WithTTSVoice("PUCHKOW"));
                Assert.That(SEntMan.GetComponent<TTSComponent>(entity).VoicePrototypeId, Is.EqualTo("PUCHKOW"));

                profiles.ApplyProfileTo(entity, profile.WithSex(Sex.Female).WithTTSVoice("PUCHKOW"));
                var corrected = SEntMan.GetComponent<TTSComponent>(entity).VoicePrototypeId!;
                Assert.That(SProtoMan.Index<TTSVoicePrototype>(corrected).Sex, Is.EqualTo(Sex.Female));
            }
            finally
            {
                SEntMan.DeleteEntity(entity);
            }
        });
    }
}

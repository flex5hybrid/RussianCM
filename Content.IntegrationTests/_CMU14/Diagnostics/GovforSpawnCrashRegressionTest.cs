using Content.IntegrationTests.Fixtures;
using Content.Shared.CMU14.CCVar;
using Content.Shared.CMU14.Callsigns;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Corvax.TTS;
using Content.Shared.GameTicking;
using Content.Shared.Station;
using Content.Shared.Preferences;
using Content.Shared._RMC14.Marines.Squads;
using Robust.Shared.Configuration;

namespace Content.IntegrationTests.CMU14.Diagnostics;

[TestFixture]
public sealed class GovforSpawnCrashRegressionTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };

    [TestCase(true)]
    [TestCase(false)]
    public async Task GovforRiflemanSpawnCompletes(bool ttsEnabled)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            Server.ResolveDependency<IConfigurationManager>().SetCVar(CCCVars.TTSEnabled, ttsEnabled);
            Server.ResolveDependency<IConfigurationManager>().SetCVar(AU14CCVars.NewCommsSystem, true);
            var profile = HumanoidCharacterProfile.DefaultWithSpecies().WithTTSVoice(CMUTTSVoiceSelection.RandomVoice);
            var station = Server.System<StationSystem>().InitializeNewStation(
                new StationConfig { StationPrototype = "StandardUNMCWarship", StationComponentOverrides = new() },
                new[] { map.GridCoords.EntityId });
            var mob = Server.System<StationSpawningSystem>().SpawnPlayerMob(
                map.GridCoords, "AU14JobGOVFORSquadRifleman", profile, station);
            Server.PlayerMan.SetAttachedEntity(ServerSession!, mob);
            var completed = new PlayerSpawnCompleteEvent(mob, ServerSession!, "AU14JobGOVFORSquadRifleman", true, true, 1, station, profile);
            SEntMan.EventBus.RaiseEvent(EventSource.Local, completed);
            Assert.That(SEntMan.GetComponent<SquadMemberComponent>(mob).Squad, Is.Not.Null);
            var callsign = SEntMan.GetComponent<AU14CallsignComponent>(mob);
            Assert.That(callsign.Faction, Is.EqualTo("govfor"));
            Assert.That(callsign.Callsign, Is.Not.Empty);
            Assert.That(callsign.Suffix, Is.Not.Empty);
            Assert.That(SEntMan.GetComponent<TTSComponent>(mob).VoicePrototypeId, Is.Not.EqualTo(CMUTTSVoiceSelection.RandomVoice));
        });
        await Pair.RunTicksSync(5);
    }
}

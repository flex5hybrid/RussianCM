using System.Reflection;
using Content.IntegrationTests.Fixtures;
using Content.Server.Corvax.TTS;
using Content.Shared.CMU14.CCVar;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Corvax.TTS;
using Robust.Shared.Configuration;
using ServerTTSSystem = Content.Server.Corvax.TTS.TTSSystem;
using ClientTTSSystem = Content.Client.Corvax.TTS.TTSSystem;

namespace Content.IntegrationTests.CMU14.Diagnostics;

[TestFixture]
[EnsureCVar(Side.Server, typeof(CCCVars), nameof(CCCVars.TTSEnabled), false)]
public sealed class CMUTTSPreviewDeliveryTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, InLobby = true, Dirty = true };

    [TestCase(true)]
    [TestCase(false)]
    public async Task LobbyPreviewReachesClientWithOrWithoutCallsigns(bool callsignsEnabled)
    {
        var audio = new byte[48044];
        using (var stream = new System.IO.MemoryStream(audio))
        using (var writer = new System.IO.BinaryWriter(stream))
        {
            writer.Write("RIFF"u8);
            writer.Write((uint) audio.Length - 8);
            writer.Write("WAVEfmt "u8);
            writer.Write(16u);
            writer.Write((ushort) 1);
            writer.Write((ushort) 1);
            writer.Write(24000u);
            writer.Write(48000u);
            writer.Write((ushort) 2);
            writer.Write((ushort) 16);
            writer.Write("data"u8);
            writer.Write((uint) audio.Length - 44);
        }
        var requests = 0;
        await Server.WaitAssertion(() =>
        {
            Assert.That(ServerSession!.AttachedEntity, Is.Null, "Preview must run without a spawned character.");
            // Pair startup opens the lobby before EnsureCVar applies. Only this known,
            // non-exception configuration error is allowed before the offline fixture is installed.
            var startupLogs = Pair.ServerLogHandler.FailingLogs;
            Assert.That(startupLogs.Count, Is.LessThanOrEqualTo(1));
            Assert.That(startupLogs.All(log => log.EndsWith(
                "[ERRO] tts: TTS API token is empty Exception: ", StringComparison.Ordinal)), Is.True);
            var logOutput = Pair.ServerLogHandler.ActiveContext!;
            Pair.ServerLogHandler.ClearContext();
            Pair.ServerLogHandler.ActivateContext(logOutput);
            var cfg = Server.ResolveDependency<IConfigurationManager>();
            // The lobby requests its catalog independently of preview; keep that request offline too.
            typeof(ServerTTSSystem).GetField("_catalogLoaded", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(Server.System<ServerTTSSystem>(), true);
            cfg.SetCVar(CCCVars.TTSEnabled, true);
            cfg.SetCVar(AU14CCVars.NewCommsSystem, callsignsEnabled);
            // Exercise the real queue and network path without production credentials or HTTP.
            var queue = new CMUTTSSynthesisQueue(async (_, _, _) =>
            {
                requests++;
                await Task.Yield();
                return audio;
            }, () => TimeSpan.Zero);
            typeof(ServerTTSSystem).GetField("_speechQueue", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(Server.System<ServerTTSSystem>(), queue);
        });
        await Client.WaitPost(() =>
        {
            // Audio decoding is outside this server/network regression's scope.
            Client.ResolveDependency<IConfigurationManager>().SetCVar(CCCVars.TTSVolume, 0f);
            Client.System<CMUTTSPreviewProbeSystem>().Events.Clear();
            Client.System<ClientTTSSystem>().RequestPreviewTTS("PUCHKOW");
        });
        await Pair.RunTicksSync(60);
        await Server.WaitAssertion(() => Assert.That(requests, Is.EqualTo(1)));
        await Client.WaitAssertion(() =>
        {
            var events = Client.System<CMUTTSPreviewProbeSystem>().Events;
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].Data, Is.EqualTo(audio));
            Assert.That(events[0].SourceUid, Is.Null);
        });
    }
}

public sealed class CMUTTSPreviewProbeSystem : EntitySystem
{
    public readonly List<PlayTTSEvent> Events = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<PlayTTSEvent>(args => Events.Add(args));
    }
}

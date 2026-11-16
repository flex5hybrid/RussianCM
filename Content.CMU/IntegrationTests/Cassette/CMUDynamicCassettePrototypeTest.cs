using Content.Shared.CMU14.Cassette;
using Content.Shared._RMC14.Cassette;
using Robust.Shared.Map;

namespace Content.IntegrationTests.CMU14.Cassette;

[TestFixture]
public sealed class CMUDynamicCassettePrototypeTest
{
    [Test]
    public async Task CustomTapeAndPortableSpeakerHaveRequiredComponents()
    {
        var (server, _) = await PoolManager.GenerateServer(new PoolSettings(), TestContext.Out);

        try
        {
            await server.WaitAssertion(() =>
            {
                var entities = server.EntMan;
                var originalCustomTape = entities.SpawnEntity("RMCCassetteTapeCustom", MapCoordinates.Nullspace);
                Assert.That(entities.HasComponent<CMUDynamicCassetteComponent>(originalCustomTape), Is.False,
                    "The original custom cassette must keep its client-local behavior.");

                var tape = entities.SpawnEntity("CMUCassetteTapeCustomRitoka", MapCoordinates.Nullspace);
                var dynamicCassette = entities.GetComponent<CMUDynamicCassetteComponent>(tape);
                var cassetteTape = entities.GetComponent<CassetteTapeComponent>(tape);
                Assert.Multiple(() =>
                {
                    Assert.That(dynamicCassette.MaxTracks, Is.EqualTo(CMUDynamicCassetteLimits.MaxTracksPerCassette));
                    Assert.That(dynamicCassette.Tracks, Is.Empty);
                    Assert.That(dynamicCassette.Titles, Is.Empty);
                    Assert.That(cassetteTape.Songs, Is.Empty);
                });

                var speaker = entities.SpawnEntity("CMUCassetteSpeakerPortable", MapCoordinates.Nullspace);
                Assert.Multiple(() =>
                {
                    Assert.That(entities.HasComponent<CassettePlayerComponent>(speaker), Is.True);
                    Assert.That(entities.HasComponent<CMUPublicCassettePlayerComponent>(speaker), Is.True);
                    Assert.That(entities.HasComponent<Content.Shared.Clothing.Components.ClothingComponent>(speaker), Is.False,
                        "The boombox must be usable in the world instead of being worn in an equipment slot.");
                });
            });
        }
        finally
        {
            server.Dispose();
        }
    }
}

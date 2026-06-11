using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Shared._RMC14.Power;
using NUnit.Framework;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests._RMC14;

[TestFixture]
public sealed class WallApcConstructionTest
{
    [Test]
    public async Task ConstructedWallApcKeepsConstructionGraphState()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        var changed = false;

        await server.WaitPost(() =>
        {
            var entMan = server.EntMan;
            var frame = entMan.SpawnEntity("CMApcFrame", map.GridCoords);
            var frameConstruction = entMan.GetComponent<ConstructionComponent>(frame);

            Assert.Multiple(() =>
            {
                Assert.That(frameConstruction.Graph, Is.EqualTo("CMApc"));
                Assert.That(frameConstruction.Node, Is.EqualTo("apcFrame"));
            });

            changed = entMan.System<ConstructionSystem>().ChangeNode(frame, null, "apc");
        });

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var constructedCount = 0;
            var query = entMan.EntityQueryEnumerator<RMCApcComponent, MetaDataComponent, ConstructionComponent>();

            while (query.MoveNext(out var uid, out _, out var meta, out var construction))
            {
                if (meta.EntityPrototype?.ID != "CMApcConstructed")
                    continue;

                constructedCount++;

                Assert.Multiple(() =>
                {
                    Assert.That(entMan.Deleted(uid), Is.False);
                    Assert.That(construction.Graph, Is.EqualTo("CMApc"));
                    Assert.That(construction.Node, Is.EqualTo("apc"));
                });
            }

            Assert.Multiple(() =>
            {
                Assert.That(changed, Is.True);
                Assert.That(constructedCount, Is.EqualTo(1));
            });
        });

        await pair.CleanReturnAsync();
    }
}

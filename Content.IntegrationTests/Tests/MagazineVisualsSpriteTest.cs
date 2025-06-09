using System.Collections.Generic;
using Content.Client.Weapons.Ranged.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests;

/// <summary>
/// Tests all entity prototypes with the MagazineVisualsComponent.
/// </summary>
[TestFixture]
public sealed class MagazineVisualsSpriteTest
{
    [Test]
    public async Task MagazineVisualsSpritesExist()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var client = pair.Client;
        var toTest = new List<(int, string)>();
        var protos = pair.GetPrototypesWithComponent<MagazineVisualsComponent>();
        var spriteSys = client.System<SpriteSystem>();

        await client.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
<<<<<<< HEAD
                foreach (var (proto, _) in protos)
                {
                    var uid = client.EntMan.Spawn(proto.ID);
                    var visuals = client.EntMan.GetComponent<MagazineVisualsComponent>(uid);

                    Assert.That(client.EntMan.TryGetComponent(uid, out SpriteComponent sprite),
                        @$"{proto.ID} has MagazineVisualsComponent but no SpriteComponent.");
                    Assert.That(client.EntMan.HasComponent<AppearanceComponent>(uid),
                        @$"{proto.ID} has MagazineVisualsComponent but no AppearanceComponent.");

                    toTest.Clear();
                    if (spriteSys.LayerMapTryGet((uid, sprite), GunVisualLayers.Mag, out var magLayerId, false))
                        toTest.Add((magLayerId, ""));
                    if (spriteSys.LayerMapTryGet((uid, sprite), GunVisualLayers.MagUnshaded, out var magUnshadedLayerId, false))
                        toTest.Add((magUnshadedLayerId, "-unshaded"));

                    Assert.That(
                        toTest,
                        Is.Not.Empty,
=======
                foreach (var proto in protoMan.EnumeratePrototypes<EntityPrototype>())
                {
                    if (proto.Abstract || pair.IsTestPrototype(proto))
                        continue;

                    if (!proto.TryGetComponent<MagazineVisualsComponent>(out var visuals, componentFactory))
                        continue;

                    Assert.That(proto.TryGetComponent<SpriteComponent>(out var sprite, componentFactory),
                        @$"{proto.ID} has MagazineVisualsComponent but no SpriteComponent.");
                    Assert.That(proto.HasComponent<AppearanceComponent>(componentFactory),
                        @$"{proto.ID} has MagazineVisualsComponent but no AppearanceComponent.");

                    var toTest = new List<(int, string)>();
                    if (sprite.LayerMapTryGet(GunVisualLayers.Mag, out var magLayerId))
                        toTest.Add((magLayerId, ""));
                    if (sprite.LayerMapTryGet(GunVisualLayers.MagUnshaded, out var magUnshadedLayerId))
                        toTest.Add((magUnshadedLayerId, "-unshaded"));

                    Assert.That(toTest, Is.Not.Empty,
>>>>>>> master
                        @$"{proto.ID} has MagazineVisualsComponent but no Mag or MagUnshaded layer map.");

                    var start = visuals.ZeroVisible ? 0 : 1;
                    foreach (var (id, midfix) in toTest)
                    {
<<<<<<< HEAD
                        Assert.That(spriteSys.TryGetLayer((uid, sprite), id, out var layer, false));
=======
                        Assert.That(sprite.TryGetLayer(id, out var layer));
>>>>>>> master
                        var rsi = layer.ActualRsi;
                        for (var i = start; i < visuals.MagSteps; i++)
                        {
                            var state = $"{visuals.MagState}{midfix}-{i}";
                            Assert.That(rsi.TryGetState(state, out _),
                                @$"{proto.ID} has MagazineVisualsComponent with MagSteps = {visuals.MagSteps}, but {rsi.Path} doesn't have state {state}!");
                        }

                        // MagSteps includes the 0th step, so sometimes people are off by one.
                        var extraState = $"{visuals.MagState}{midfix}-{visuals.MagSteps}";
                        Assert.That(rsi.TryGetState(extraState, out _), Is.False,
                            @$"{proto.ID} has MagazineVisualsComponent with MagSteps = {visuals.MagSteps}, but more states exist!");
<<<<<<< HEAD

                        client.EntMan.DeleteEntity(uid);
=======
>>>>>>> master
                    }
                }
            });
        });

        await pair.CleanReturnAsync();
    }
}

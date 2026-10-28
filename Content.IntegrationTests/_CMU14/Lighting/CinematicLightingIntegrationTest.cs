using System.Numerics;
using Content.Client.CMU14.Lighting;
using Content.Client.Power.Components;
using Content.Client.Viewport;
using Content.IntegrationTests.Fixtures;
using Content.Shared.CCVar;
using Content.Shared.CMU14.Lighting;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using ClientEye = Robust.Shared.Graphics.Eye;
using Content.Client.Graphics;
using Content.Client.CMU14.Lighting.Configuration;
using Robust.Client.ResourceManagement;
using Moq;

namespace Content.IntegrationTests.CMU14.Lighting;

// CMU14 class: content lighting, visibility boundaries, and resource ownership regressions.
[TestFixture]
public sealed class CinematicLightingIntegrationTest : GameTest
{
    [Test]
    public async Task ShaderSourcesAndInheritedEmitterDataLoad()
    {
        await Client.WaitAssertion(() =>
        {
            string[] shaders = ["CMUCinematicEmission", "CMUCinematicDownsample", "CMUCinematicSpill",
                "CMUCinematicAtmosphere", "CMUCinematicComposite"];
            foreach (var id in shaders)
            {
                using var shader = CProtoMan.Index<ShaderPrototype>(id).InstanceUnique();
                Assert.That(shader.Mutable, Is.True, id);
            }
            using (var pool = new CinematicShaderPool(CProtoMan.Index<ShaderPrototype>("CMUCinematicEmission"), 2))
            {
                var first = pool.Get(0);
                var second = pool.Get(1);
                Assert.That(first, Is.Not.SameAs(second), "Queued sources must not share mutable uniforms.");
                Assert.That(pool.Get(0), Is.SameAs(first), "Steady frames reuse the same source slot.");
                pool.Dispose();
            }
            string[] prototypes = ["RMCLightFixtureAlwaysPowered", "RMCSmallFloodlight", "RMCTallFloodlight",
                "AU14TallFloodlight", "CMUWarshipAlertBeaconRed", "RMCPropCommunicationsConsole", "ExplosionLight"];
            foreach (var id in prototypes)
            {
                var entity = CProtoMan.Index<EntityPrototype>(id);
                Assert.That(entity.Components.TryGetValue("CMUCinematicEmitter", out var entry), Is.True, id);
                var emitter = (CMUCinematicEmitterComponent) entry.Component;
                Assert.That(CProtoMan.HasIndex(emitter.Profile), Is.True, id);
            }
            Assert.That(CProtoMan.Index<EntityPrototype>("CMWindowReinforcedAlmayer")
                .Components.ContainsKey("CMUCinematicMaterial"), Is.True);
        });
    }

    [Test]
    public async Task PresetsSwitchLiveAndViewportScopeCannotLeakIntoAnotherCameraOrZLevel()
    {
        await Client.WaitAssertion(() =>
        {
            var config = Client.ResolveDependency<IConfigurationManager>();
            var clyde = Client.ResolveDependency<IClyde>();
            var overlays = Client.ResolveDependency<IOverlayManager>();
            var lights = Client.ResolveDependency<ILightManager>();
            var system = CEntMan.System<CinematicLightingSystem>();
            var original = config.GetCVar(CCVars.CMUCinematicLighting);
            var drawLighting = lights.DrawLighting;
            using var first = clyde.CreateViewport(new Vector2i(65, 33));
            using var second = clyde.CreateViewport(new Vector2i(33, 17));
            first.Eye = new ClientEye();
            second.Eye = new ClientEye();
            try
            {
                lights.DrawLighting = true;
                foreach (var quality in new[] { 1, 2, 3, 0, 3, 1, 0 })
                {
                    config.SetCVar(CCVars.CMUCinematicLighting, quality);
                    Assert.That(overlays.HasOverlay<CinematicLightingOverlay>(), Is.EqualTo(quality != 0));
                    Assert.That(system.IsEligible(first, lights), Is.False);
                    using (system.BeginViewport(first))
                    {
                        Assert.That(system.IsEligible(first, lights), Is.EqualTo(quality != 0));
                        Assert.That(system.IsEligible(second, lights), Is.False);
                        using (system.BeginViewport(second))
                            Assert.That(system.IsEligible(second, lights), Is.EqualTo(quality != 0));
                        Assert.That(system.IsEligible(first, lights), Is.EqualTo(quality != 0));
                        first.Eye = new ScalingViewport.ZEye { Depth = -1 };
                        Assert.That(system.IsEligible(first, lights), Is.False);
                        first.Eye = new ClientEye();
                    }
                    Assert.That(system.IsEligible(first, lights), Is.False);
                }
                config.SetCVar(CCVars.CMUCinematicLighting, 2);
                using (system.BeginViewport(first))
                {
                    lights.DrawLighting = false;
                    Assert.That(system.IsEligible(first, lights), Is.False, "Full night vision retains native presentation.");
                }
            }
            finally
            {
                lights.DrawLighting = drawLighting;
                config.SetCVar(CCVars.CMUCinematicLighting, original);
            }
        });
    }

    [Test]
    public async Task ExistingLightAndPowerStatesImmediatelyGateEmissionIncludingChildBeacons()
    {
        var testMap = await Pair.CreateTestMap();
        NetEntity fixtureNet = default;
        await Server.WaitPost(() =>
        {
            Server.PlayerMan.SetAttachedEntity(ServerSession!, testMap.Grid.Owner);
            var fixture = SEntMan.SpawnEntity("RMCLightFixtureAlwaysPowered", testMap.GridCoords);
            fixtureNet = SEntMan.GetNetEntity(fixture);
        });
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            var config = Client.ResolveDependency<IConfigurationManager>();
            var original = config.GetCVar(CCVars.CMUCinematicLighting);
            config.SetCVar(CCVars.CMUCinematicLighting, 2);
            var fixture = CEntMan.GetEntity(fixtureNet);
            var xform = CEntMan.GetComponent<TransformComponent>(fixture);
            var transform = CEntMan.System<SharedTransformSystem>();
            var coords = transform.GetMapCoordinates(fixture);
            var eye = new ClientEye { Position = coords };
            var sources = new List<CinematicLightSource>();
            var system = CEntMan.System<CinematicLightingSystem>();
            var lights = CEntMan.System<SharedPointLightSystem>();
            var native = CEntMan.GetComponent<PointLightComponent>(fixture);
            var radius = native.Radius;
            var beacon = CEntMan.SpawnEntity("CMUWarshipAlertBeaconRed", coords);
            transform.SetParent(beacon, fixture);
            transform.SetLocalPosition(beacon, Vector2.Zero);
            var receiver = CEntMan.AddComponent<ApcPowerReceiverComponent>(fixture);
            receiver.NetSyncEnabled = false;
            receiver.NeedsPower = true;
            receiver.Powered = true;
            var renderedSprite = CEntMan.GetComponent<SpriteComponent>(fixture);
            var spriteColor = renderedSprite.Color;
            var sprites = CEntMan.System<SpriteSystem>();
            try
            {
                Collect();
                Assert.That(sources.Exists(source => source.Uid == fixture), Is.True);
                Assert.That(sources.Exists(source => source.Uid == beacon), Is.True);
                var rotation = transform.GetWorldRotation(fixture);
                var spriteRotation = renderedSprite.NoRotation ? -eye.Rotation : rotation;
                var expectedEmission = coords.Position + spriteRotation.RotateVec(renderedSprite.Offset);
                Assert.That(sources.Find(source => source.Uid == fixture).EmissionPosition,
                    Is.EqualTo(expectedEmission), "RMC wall-light sprite offsets must not detach bloom from the lamp.");
                sprites.SetColor((fixture, renderedSprite), spriteColor.WithAlpha(0f));
                Collect();
                Assert.That(sources.Exists(source => source.Uid == fixture || source.Uid == beacon), Is.False,
                    "A hidden parent must not disclose itself through a child emitter.");
                sprites.SetColor((fixture, renderedSprite), spriteColor);
                lights.SetEnabled(fixture, false, native);
                Collect();
                Assert.That(sources.Exists(source => source.Uid == fixture || source.Uid == beacon), Is.False);
                lights.SetEnabled(fixture, true, native);
                receiver.Powered = false;
                Collect();
                Assert.That(sources.Exists(source => source.Uid == fixture || source.Uid == beacon), Is.False);
                receiver.Powered = true;
                Collect();
                Assert.That(sources.Exists(source => source.Uid == fixture), Is.True);
                system.CollectSources(new MapId(int.MaxValue), Box2.CenteredAround(coords.Position, new Vector2(20)), eye, sources);
                Assert.That(sources, Is.Empty, "A viewport never reads sources from another map or Z level.");
                Assert.That(native.Radius, Is.EqualTo(radius), "Cosmetic presets must not modify native light radius.");
            }
            finally
            {
                CEntMan.DeleteEntity(beacon);
                CEntMan.RemoveComponent<ApcPowerReceiverComponent>(fixture);
                lights.SetEnabled(fixture, true, native);
                sprites.SetColor((fixture, renderedSprite), spriteColor);
                config.SetCVar(CCVars.CMUCinematicLighting, original);
            }

            void Collect() => system.CollectSources(xform.MapID,
                Box2.CenteredAround(coords.Position, new Vector2(20)), eye, sources);
        });
        await Server.WaitPost(() =>
        {
            SEntMan.DeleteEntity(SEntMan.GetEntity(fixtureNet));
            Server.PlayerMan.SetAttachedEntity(ServerSession!, null);
        });
    }

    [Test]
    public async Task OddSizeResizeViewportPurgeAndDisableReleaseEveryOwnedTargetExactlyOnce()
    {
        await Client.WaitAssertion(() =>
        {
            var clyde = Client.ResolveDependency<IClyde>();
            var targets = new List<Mock<IRenderTexture>>();
            var counted = new Mock<IClyde>();
            counted.Setup(c => c.CreateRenderTarget(It.IsAny<Vector2i>(), It.IsAny<RenderTargetFormatParameters>(),
                It.IsAny<TextureSampleParameters?>(), It.IsAny<string>())).Returns(
                (Vector2i size, RenderTargetFormatParameters format, TextureSampleParameters? sample, string name) =>
                {
                    var real = clyde.CreateRenderTarget(size, format, sample, name);
                    var target = new Mock<IRenderTexture>();
                    target.SetupGet(t => t.Size).Returns(size);
                    target.SetupGet(t => t.Texture).Returns(real.Texture);
                    target.Setup(t => t.Dispose()).Callback(real.Dispose);
                    targets.Add(target);
                    return target.Object;
                });
            using var cache = new OverlayResourceCache<CinematicViewportResources>();
            using var viewport = clyde.CreateViewport(new Vector2i(65, 33));
            var resources = cache.GetForViewport(viewport, _ => new CinematicViewportResources(counted.Object,
                CEntMan, CProtoMan, Client.ResolveDependency<IResourceCache>(),
                Client.ResolveDependency<ITileDefinitionManager>(), Client.ResolveDependency<IConfigurationManager>(),
                CinematicLightingQuality.For(2)));
            resources.EnsureSize(new Vector2i(65, 33), new MapId(1));
            Assert.That(resources.Pyramid[0].Size, Is.EqualTo(new Vector2i(33, 17)));
            var clears = new List<Color?>();
            var drawing = new Mock<DrawingHandleWorld>(MockBehavior.Loose, Texture.White);
            drawing.Setup(h => h.RenderInRenderTarget(It.IsAny<IRenderTarget>(), It.IsAny<Action>(), It.IsAny<Color?>()))
                .Callback((IRenderTarget _, Action _, Color? clear) => clears.Add(clear));
            resources.Frame.Handle = drawing.Object;
            resources.Occlusion.RenderBlockers(resources.Blockers);
            resources.Bloom.Render(resources.Pyramid, resources.Blockers.Texture);
            resources.Material!.Render(resources.Materials!);
            resources.Spill!.Render(resources.Effects!, resources.Materials!.Texture);
            Assert.That(clears.Count, Is.EqualTo(resources.Pyramid.Length + 3));
            foreach (var clear in clears)
                Assert.That(clear, Is.EqualTo(new Color(0f, 0f, 0f, 0f)),
                    "RGB data masks must not retain the white RGB channels of Color.Transparent.");
            var count = targets.Count;
            resources.EnsureSize(new Vector2i(65, 33), new MapId(1));
            Assert.That(targets.Count, Is.EqualTo(count), "Steady rendering reuses its targets.");
            resources.EnsureSize(new Vector2i(37, 19), new MapId(2));
            for (var i = 0; i < count; i++)
                targets[i].Verify(t => t.Dispose(), Times.Once);
            viewport.Dispose();
            cache.Dispose();
            Assert.That(resources.TargetCount, Is.Zero);
            Assert.That(resources.TargetBytes, Is.Zero);
            foreach (var target in targets)
                target.Verify(t => t.Dispose(), Times.Once);
        });
    }
}

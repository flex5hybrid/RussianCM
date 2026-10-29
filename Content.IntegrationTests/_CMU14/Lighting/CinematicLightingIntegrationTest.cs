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
                "CMUCinematicAtmosphere", "CMUCinematicComposite", "CMUCinematicSurfaceComposite", "CMUCinematicLightComposite", "CMUCinematicLightCopy", "CMUCinematicWallReceiver", "CMUCinematicContact"];
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
    public async Task AuthoredMasksAndIndependentSurfaceResponsesAreValid()
    {
        await Client.WaitAssertion(() =>
        {
            var cache = Client.ResolveDependency<IResourceCache>();
            var tube = CProtoMan.Index<CMUCinematicEmissionMaskPrototype>("CMUTubeMask");
            var rsi = cache.GetResource<RSIResource>(new Robust.Shared.Utility.ResPath(tube.Rsi)).RSI;
            Assert.That(rsi.Size, Is.EqualTo(tube.FrameSize));
            Assert.That(tube.Directions.Count, Is.EqualTo(4));
            foreach (var region in tube.Directions.Values)
            {
                Assert.That(region.X, Is.GreaterThanOrEqualTo(0));
                Assert.That(region.Y, Is.GreaterThanOrEqualTo(0));
                Assert.That(region.Z, Is.LessThanOrEqualTo(1));
                Assert.That(region.W, Is.LessThanOrEqualTo(1));
                Assert.That((region.Z - region.X) * (region.W - region.Y), Is.InRange(0.001f, 0.04f),
                    "A tube mask must isolate the luminous element, not the full RSI rectangle.");
            }
            var fixture = CProtoMan.Index<CMUCinematicLightProfilePrototype>("CMUFixture");
            Assert.That(fixture.Shape, Is.EqualTo(CMUCinematicSourceShape.Linear));
            Assert.That(fixture.WallMounted, Is.True);
            Assert.That(fixture.Indirect, Is.GreaterThan(0));
            Assert.That(fixture.InnerGlow, Is.GreaterThan(fixture.OuterGlow));
            var matte = CProtoMan.Index<CMUCinematicMaterialPrototype>("CMUMatteMetal");
            var polished = CProtoMan.Index<CMUCinematicMaterialPrototype>("CMUPolishedMetal");
            var wet = CProtoMan.Index<CMUCinematicMaterialPrototype>("CMUWetFloor");
            Assert.That(polished.Reflectivity * polished.SpecularStrength,
                Is.GreaterThan(matte.Reflectivity * matte.SpecularStrength * 4));
            Assert.That(polished.Roughness, Is.LessThan(matte.Roughness));
            Assert.That(wet.Stretch, Is.GreaterThan(polished.Stretch));
            var controls = CEntMan.System<CinematicLightingSystem>().Controls;
            controls.Indirect = false;
            controls.Reflections = false;
            Assert.That(controls.Direct && controls.Glow && controls.Atmosphere, Is.True);
            controls.Reset();
            Assert.That(controls.EnabledMask, Is.EqualTo(63));
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
                for (var cycle = 0; cycle < 10; cycle++)
                foreach (var quality in new[] { 1, 2, 3, 0 })
                {
                    config.SetCVar(CCVars.CMUCinematicLighting, quality);
                    Assert.That(overlays.HasOverlay<CinematicLightingOverlay>(), Is.EqualTo(quality != 0));
                    Assert.That(overlays.HasOverlay<CinematicSurfaceLightingOverlay>(), Is.EqualTo(quality >= 2));
                    Assert.That(overlays.HasOverlay<CinematicIrradianceOverlay>(), Is.EqualTo(quality >= 2));
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
            var materialFrame = new CinematicRenderContext(CProtoMan, Client.ResolveDependency<IResourceCache>());
            materialFrame.Map = coords.MapId;
            var materials = new CinematicMaterialRenderer(CEntMan, CProtoMan,
                Client.ResolveDependency<ITileDefinitionManager>(), materialFrame);
            var beaconTransform = CEntMan.GetComponent<TransformComponent>(beacon);
            try
            {
                Collect();
                Assert.That(sources.Exists(source => source.Uid == fixture), Is.True);
                Assert.That(sources.Exists(source => source.Uid == beacon), Is.True);
                var beaconLight = CEntMan.GetComponent<PointLightComponent>(beacon);
#pragma warning disable RA0002 // Set animation-owned state explicitly in this headless regression fixture.
                beaconLight.Rotation = Angle.FromDegrees(37);
#pragma warning restore RA0002
                Collect();
                Assert.That(sources.Find(source => source.Uid == beacon).Rotation, Is.EqualTo(beaconLight.Rotation),
                    "Cinematic beams must follow the live native animation, not just the entity transform.");
#pragma warning disable RA0002 // Exercise the native mount-relative mask mode without changing its production prototype.
                beaconLight.MaskAutoRotate = true;
#pragma warning restore RA0002
                transform.SetLocalRotation(beacon, Angle.FromDegrees(29));
                Collect();
                Assert.That(sources.Find(source => source.Uid == beacon).Rotation,
                    Is.EqualTo(beaconLight.Rotation + transform.GetWorldRotation(beacon)),
                    "A directional native mask must retain its own rotation plus the mounted pose.");
                Assert.That(materials.VisibleSurface(beacon, beaconTransform), Is.True);
                var modelSource = sources.Find(source => source.Uid == fixture);
                Assert.That(modelSource.NativeColor, Is.EqualTo(native.Color));
                Assert.That(modelSource.NativeEnergy, Is.EqualTo(native.Energy));
                Assert.That(modelSource.NativeRadius, Is.EqualTo(native.Radius));
                sprites.SetContainerOccluded((fixture, renderedSprite), true);
                Collect();
                Assert.That(sources.Exists(source => source.Uid == fixture), Is.True,
                    "A container can pass native light while hiding the standalone item sprite.");
                Assert.That(sources.Find(source => source.Uid == fixture).Sprite, Is.Null,
                    "A held light must not duplicate its item sprite over the actor.");
                lights.SetContainerOccluded(fixture, true, native);
                Collect();
                Assert.That(sources.Exists(source => source.Uid == fixture || source.Uid == beacon), Is.False);
                lights.SetContainerOccluded(fixture, false, native);
                sprites.SetContainerOccluded((fixture, renderedSprite), false);
                Collect();
                var rotation = transform.GetWorldRotation(fixture);
                Assert.That(sources.Find(source => source.Uid == fixture).Position,
                    Is.EqualTo(coords.Position + rotation.RotateVec(native.Offset)),
                    "Match Clyde's light origin exactly: a 0.016m bias can move RMC fixtures inside a wall.");
                var spriteRotation = renderedSprite.NoRotation ? -eye.Rotation : rotation;
                var expectedEmission = coords.Position + spriteRotation.RotateVec(renderedSprite.Offset);
                Assert.That(sources.Find(source => source.Uid == fixture).EmissionPosition,
                    Is.EqualTo(expectedEmission), "RMC wall-light sprite offsets must not detach bloom from the lamp.");
                sprites.SetColor((fixture, renderedSprite), spriteColor.WithAlpha(0f));
                Collect();
                Assert.That(sources.Exists(source => source.Uid == fixture || source.Uid == beacon), Is.False,
                    "A hidden parent must not disclose itself through a child emitter.");
                Assert.That(materials.VisibleSurface(beacon, beaconTransform), Is.False,
                    "A hidden parent must not disclose itself through a child material footprint either.");
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
                lights.SetContainerOccluded(fixture, false, native);
                sprites.SetContainerOccluded((fixture, renderedSprite), false);
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
                    if (name != "cmu-cinematic-blockers" && name != "cmu-cinematic-materials")
                        Assert.That(format.ColorFormat, Is.EqualTo(RenderTargetColorFormat.R11FG11FB10F),
                            "Clyde Add uses DstAlpha: radiance must use HDR RGB so overlapping sources cannot amplify alpha.");
                    if (name == "cmu-cinematic-emission")
                        Assert.That(sample?.Filter, Is.False, "The reduced core retains the original RSI pixel edges.");
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
            Assert.That(resources.Emission.Size, Is.EqualTo(new Vector2i(33, 17)));
            var clears = new List<Color?>();
            var drawing = new Mock<DrawingHandleWorld>(MockBehavior.Loose, Texture.White);
            drawing.Setup(h => h.RenderInRenderTarget(It.IsAny<IRenderTarget>(), It.IsAny<Action>(), It.IsAny<Color?>()))
                .Callback((IRenderTarget _, Action _, Color? clear) => clears.Add(clear));
            resources.Frame.Handle = drawing.Object;
            resources.Occlusion.RenderBlockers(resources.Blockers);
            resources.Bloom.Render(resources.Emission, resources.Pyramid, resources.Blockers.Texture,
                resources.InnerSeeds, resources.OuterSeeds);
            resources.Material!.Render(resources.Materials!);
            resources.Contact!.Render(resources.Blockers);
            resources.Spill!.RenderIrradiance(resources.Irradiance!);
            resources.Spill.RenderReflections(resources.Reflections!, resources.Materials!.Texture);
            resources.Atmosphere!.Render(resources.AtmosphereTarget!);
            Assert.That(clears.Count, Is.EqualTo(resources.Pyramid.Length + 9));
            Assert.That(clears.Count(clear => clear == null), Is.EqualTo(1),
                "The contact B contribution must preserve the blocker R and receiver G channels.");
            foreach (var clear in clears)
                Assert.That(clear, Is.AnyOf(null, CinematicRenderContext.ClearColor, CinematicRenderContext.RadianceClearColor),
                    "RGB data masks must not retain the white RGB channels of Color.Transparent.");
            Assert.That(clears.Count(clear => clear == CinematicRenderContext.RadianceClearColor), Is.EqualTo(6),
                "Three optical seeds, irradiance, reflections and atmosphere use HDR RGB.");
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

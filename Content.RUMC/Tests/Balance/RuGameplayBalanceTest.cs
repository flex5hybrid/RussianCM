using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server.CMU14.Threats;
using Content.Shared.CMU14.Threats;
using Content.Shared.CMU14.util;
using Content.Shared.CMU14.Medical.Anatomy.BodyParts;
using Content.Shared.CMU14.Medical.Core;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Xenonids.Maturing;
using Content.Shared._RMC14.Xenonids.Projectile.Spit.Standard;
using Content.Shared._RMC14.Xenonids.Projectile.Spit.Queen;
using Content.Shared._RMC14.Xenonids.SpitToggle;
using Content.Shared.Body.Part;
using Content.Shared.FixedPoint;
using Content.Shared._RMC14.Xenonids.Screech;
using Content.Shared.Stunnable;
using Content.Shared.StatusEffectNew;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Actions.Components;
using Content.Shared.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests._RuCM.Balance;

public sealed class RuGameplayBalanceTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = false, Dirty = true };

    [TestCase("XenoThreat")]
    [TestCase("XenoThreatCF")]
    public async Task SpawnPlanAndPlayerBudgetRemainConsistentAndMonotone(string id)
    {
        await Server.WaitAssertion(() =>
        {
            var threat = SProtoMan.Index<ThreatPrototype>(id);
            var spawn = SProtoMan.Index(threat.RoundStartSpawn);
            var previous = 0;
            for (var players = 0; players <= 200; players++)
            {
                var count = ThreatVoteSelection.CalculateBodyCount(spawn, players, threat);
                var leaders = ThreatVoteSelection.GetScaledBodies(spawn.LeadersToSpawn, spawn.Scaling, players).ToDictionary();
                var members = ThreatVoteSelection.GetScaledBodies(spawn.GruntsToSpawn, spawn.Scaling, players).ToDictionary();
                ThreatVoteSelection.LimitBodies(leaders, members, ThreatVoteSelection.CalculateThreatPlayerBudget(players, threat));
                Assert.That(count.Total, Is.EqualTo(leaders.Values.Sum() + members.Values.Sum()));
                Assert.That(count.Total, Is.InRange(previous, players));
                previous = count.Total;
            }
        });
    }

    [Test]
    public async Task MatureQueenUsesExistingSingleSpitAndToggleInsteadOfVolley()
    {
        var map = await Pair.CreateTestMap();
        EntityUid queen = default;
        await Server.WaitPost(() =>
        {
            queen = SSpawnAtPosition("CMXenoQueen", map.GridCoords);
            Server.System<XenoMaturingSystem>().Mature((queen, SEntMan.GetComponent<XenoMaturingComponent>(queen)));
        });
        await Server.WaitRunTicks(1);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<XenoQueenSpitComponent>(queen), Is.False);
            var spit = SEntMan.GetComponent<XenoSpitComponent>(queen);
            var toggle = SEntMan.GetComponent<XenoToggleSpitComponent>(queen);
            Assert.That(spit.ProjectileId.Id, Is.EqualTo("XenoQueenNeuroSpitProjectile"));
            Assert.That(spit.PlasmaCost, Is.EqualTo(FixedPoint2.New(50)));
            Assert.That(spit.Speed, Is.EqualTo(30));
            Assert.That(toggle.AcidProto.Id, Is.EqualTo("XenoChargedSpitProjectile"));
            Assert.That(toggle.AcidCost, Is.EqualTo(FixedPoint2.New(25)));
            var spitAction = SSpawnAtPosition("ActionXenoSpitQueen", map.GridCoords);
            Assert.That(SEntMan.GetComponent<ActionComponent>(spitAction).UseDelay, Is.EqualTo(TimeSpan.FromSeconds(2.5)));
            var screechAction = SSpawnAtPosition("ActionXenoScreech", map.GridCoords);
            Assert.That(SEntMan.GetComponent<ActionComponent>(screechAction).UseDelay, Is.EqualTo(TimeSpan.FromSeconds(50)));
            Assert.That(spit.ProjectileId, Is.EqualTo(toggle.NeuroProto));
            var change = new XenoSpitToggleActionEvent();
            SEntMan.EventBus.RaiseLocalEvent(queen, change);
            Assert.That(change.Handled, Is.True);
            Assert.That(spit.ProjectileId, Is.EqualTo(toggle.AcidProto));
            Assert.That(spit.PlasmaCost, Is.EqualTo(toggle.AcidCost));
            change = new XenoSpitToggleActionEvent();
            SEntMan.EventBus.RaiseLocalEvent(queen, change);
            Assert.That(spit.ProjectileId, Is.EqualTo(toggle.NeuroProto));
            Assert.That(spit.PlasmaCost, Is.EqualTo(toggle.NeuroCost));
        });
    }

    [Test]
    public async Task ScreechMatchesRmcCloseParalysisOuterStunAndParasiteRange()
    {
        var map = await Pair.CreateTestMap();
        EntityUid queen = default;
        EntityUid near = default;
        EntityUid far = default;
        EntityUid outside = default;
        EntityUid parasite = default;
        EntityUid outsideParasite = default;
        await Server.WaitPost(() =>
        {
            queen = SSpawnAtPosition("CMXenoQueen", map.GridCoords);
            near = SSpawnAtPosition("CMMobHuman", new EntityCoordinates(map.GridCoords.EntityId, map.GridCoords.Position + new Vector2(1, 0)));
            far = SSpawnAtPosition("CMMobHuman", new EntityCoordinates(map.GridCoords.EntityId, map.GridCoords.Position + new Vector2(5, 0)));
            outside = SSpawnAtPosition("CMMobHuman", new EntityCoordinates(map.GridCoords.EntityId, map.GridCoords.Position + new Vector2(9, 0)));
            parasite = SSpawnAtPosition("CMXenoParasite", new EntityCoordinates(map.GridCoords.EntityId, map.GridCoords.Position + new Vector2(0, 10)));
            outsideParasite = SSpawnAtPosition("CMXenoParasite", new EntityCoordinates(map.GridCoords.EntityId, map.GridCoords.Position + new Vector2(0, 13)));
            Server.System<XenoMaturingSystem>().Mature((queen, SEntMan.GetComponent<XenoMaturingComponent>(queen)));
        });
        await Server.WaitRunTicks(1);
        await Server.WaitAssertion(() =>
        {
            var plasma = SEntMan.GetComponent<XenoPlasmaComponent>(queen);
            var before = plasma.Plasma;
            var scream = new XenoScreechActionEvent { Performer = queen };
            SEntMan.EventBus.RaiseLocalEvent(queen, scream);
            Assert.That(scream.Handled, Is.True);
            Assert.That(SEntMan.HasComponent<StunnedComponent>(near), Is.True);
            Assert.That(SEntMan.HasComponent<StunnedComponent>(far), Is.True);
            Assert.That(plasma.Plasma, Is.EqualTo(before - FixedPoint2.New(250)));
            Assert.That(SEntMan.HasComponent<KnockedDownComponent>(near), Is.True);
            Assert.That(SEntMan.HasComponent<KnockedDownComponent>(far), Is.False);
            Assert.That(SEntMan.HasComponent<StunnedComponent>(outside), Is.False);
            Assert.That(SEntMan.HasComponent<StunnedComponent>(parasite), Is.True);
            Assert.That(SEntMan.HasComponent<StunnedComponent>(outsideParasite), Is.False);
            var status = Server.System<StatusEffectsSystem>();
            Assert.That(status.TryGetTime(near, SharedStunSystem.ParalyzeId, out var closeTime), Is.True);
            Assert.That(closeTime.EndEffectTime - SGameTiming.CurTime, Is.EqualTo(TimeSpan.FromSeconds(8)));
            Assert.That(status.TryGetTime(far, SharedStunSystem.StunId, out var farTime), Is.True);
            Assert.That(farTime.EndEffectTime - SGameTiming.CurTime, Is.EqualTo(TimeSpan.FromSeconds(6)));
            Assert.That(status.TryGetTime(parasite, SharedStunSystem.StunId, out var parasiteTime), Is.True);
            // RMC applies its existing xeno stun-duration multiplier once, including to parasites.
            Assert.That(parasiteTime.EndEffectTime - SGameTiming.CurTime, Is.EqualTo(TimeSpan.FromSeconds(8) * 0.667));
            Assert.That(SEntMan.HasComponent<ScreechBlindComponent>(near), Is.False);
            Assert.That(SEntMan.HasComponent<ScreechBlindComponent>(far), Is.False);
        });
    }

    [Test]
    public async Task OrdinaryTraumaRetainsBaselineThresholdAndAllowsConsecutiveLimbLoss()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var human = SSpawnAtPosition("CMMobHuman", map.GridCoords);
            var index = Server.System<CMUMedicalBodyIndexSystem>();
            var parts = Server.System<SharedBodyPartHealthSystem>();
            var impact = DamageImpact.MeleeSlash;
            var onePoint = new DamageSpecifier { DamageDict = { ["Slash"] = FixedPoint2.New(1) } };
            var perPoint = DamageImpactSeverance.Calculate(onePoint, impact).Float()
                * Server.CfgMan.GetCVar(CMUMedicalCCVars.BodyPartDamagePropagation);

            foreach (var symmetry in new[] { BodyPartSymmetry.Left, BodyPartSymmetry.Right })
            {
                var key = new CMUMedicalBodyPartKey(BodyPartType.Arm, symmetry);
                Assert.That(index.TryGetBodyPart(human, key, out var arm), Is.True);
                var health = SEntMan.GetComponent<BodyPartHealthComponent>(arm);
                var threshold = health.Max + health.SeveranceThreshold;
                var damage = new DamageSpecifier
                {
                    DamageDict = { ["Slash"] = FixedPoint2.New(threshold.Float() * 1.1f / perPoint) },
                };
                Assert.That(parts.TryApplyPartDamage(human, arm, damage,
                    impact: impact, ignoreResistance: true), Is.True);
                Assert.That(index.TryGetBodyPart(human, key, out _), Is.False,
                    "ordinary trauma must retain its original threshold without repeat-loss protection");
            }
        });
    }

    [Test]
    public async Task DistressCatalogExcludesLacnAndRetainsOtherPlatoons()
    {
        await Server.WaitAssertion(() =>
        {
            var catalog = Server.System<Content.Server.CMU14.Round.AuRoundSystem>().BuildKabanCatalog(50);
            var distress = catalog.Where(option => option.Mode == "DistressSignal").ToArray();
            Assert.That(distress, Is.Not.Empty);
            Assert.That(distress.All(option => !option.Govfor.ContainsKey("LACN") && option.Govfor.Count > 0), Is.True);
            Assert.That(SProtoMan.HasIndex<PlatoonPrototype>("LACN"), Is.True);
            Assert.That(SProtoMan.Index<Content.Server.GameTicking.Presets.GamePresetPrototype>("Insurgency").ExcludedPlatoons,
                Does.Not.Contain("LACN"));
        });
    }

    [TestCase("DistressSignal", true)]
    [TestCase("Insurgency", false)]
    public async Task VaraderoFallbackRespectsModeExclusionWithoutChangingOtherModes(string mode, bool excluded)
    {
        await Server.WaitAssertion(() =>
        {
            var factory = Server.ResolveDependency<IComponentFactory>();
            Assert.That(SProtoMan.Index<EntityPrototype>("CMUPlanetVaradero").TryGetComponent(
                out RMCPlanetMapPrototypeComponent? planet, factory), Is.True);
            var preset = SProtoMan.Index<Content.Server.GameTicking.Presets.GamePresetPrototype>(mode);
            var platoons = Server.System<Content.Server.CMU14.Round.PlatoonSpawnRuleSystem>();
            var platoon = platoons.ResolveDefaultPlatoon(planet!.DefaultGovforPlatoon!, planet.PlatoonsGovfor, preset);
            Assert.That(platoon, Is.Not.Null);
            Assert.That(platoon!.ID, excluded ? Is.Not.EqualTo("LACN") : Is.EqualTo("LACN"));

            var round = Server.System<Content.Server.CMU14.Round.AuRoundSystem>();
            Assert.That(round.SetPlanet("CMUPlanetVaradero"), Is.True);
            round.SetGovforShip(string.Empty);
            platoons.SelectedGovforPlatoon = null;
            Assert.That(round.ResolveGovforShip(preset), Is.EqualTo(platoon.PossibleShips.FirstOrDefault()),
                "the loaded ship and the spawned fallback faction must agree");

            var manual = SProtoMan.Index<PlatoonPrototype>("LACN");
            platoons.SelectedGovforPlatoon = manual;
            Assert.That(round.ResolveGovforShip(preset), Is.EqualTo(manual.PossibleShips.FirstOrDefault()),
                "an explicit admin selection must retain priority");
            platoons.SelectedGovforPlatoon = null;
        });
    }

}

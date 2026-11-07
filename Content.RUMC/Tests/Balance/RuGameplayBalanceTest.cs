using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server.CMU14.Threats;
using Content.Shared.CMU14.Threats;
using Content.Shared.CMU14.util;
using Content.Shared.CMU14.Medical.Anatomy.BodyParts;
using Content.Shared.CMU14.Medical.Anatomy.BodyParts.Events;
using Content.Shared.CMU14.Medical.Core;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Xenonids.Maturing;
using Content.Shared._RMC14.Xenonids.Projectile.Spit.Standard;
using Content.Shared._RMC14.Xenonids.Projectile.Spit.Queen;
using Content.Shared._RMC14.Xenonids.SpitToggle;
using Content.Shared._RuCM.Balance;
using Content.Shared.Body.Part;
using Content.Shared.FixedPoint;
using Content.Shared._RMC14.Xenonids.Screech;
using Content.Shared.Stunnable;
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
    public async Task ScreechRestoresShortCloseControlWithoutBlindnessOrLongRangeStun()
    {
        var map = await Pair.CreateTestMap();
        EntityUid queen = default;
        EntityUid near = default;
        EntityUid far = default;
        await Server.WaitPost(() =>
        {
            queen = SSpawnAtPosition("CMXenoQueen", map.GridCoords);
            near = SSpawnAtPosition("CMMobHuman", new EntityCoordinates(map.GridCoords.EntityId, map.GridCoords.Position + new Vector2(1, 0)));
            far = SSpawnAtPosition("CMMobHuman", new EntityCoordinates(map.GridCoords.EntityId, map.GridCoords.Position + new Vector2(5, 0)));
            Server.System<XenoMaturingSystem>().Mature((queen, SEntMan.GetComponent<XenoMaturingComponent>(queen)));
        });
        await Server.WaitRunTicks(1);
        await Server.WaitAssertion(() =>
        {
            var scream = new XenoScreechActionEvent { Performer = queen };
            SEntMan.EventBus.RaiseLocalEvent(queen, scream);
            Assert.That(scream.Handled, Is.True);
            Assert.That(SEntMan.HasComponent<StunnedComponent>(near), Is.True);
            Assert.That(SEntMan.HasComponent<StunnedComponent>(far), Is.False);
            Assert.That(SEntMan.HasComponent<ScreechBlindComponent>(near), Is.False);
            Assert.That(SEntMan.HasComponent<ScreechBlindComponent>(far), Is.False);
        });
    }

    [Test]
    public async Task RaisedThresholdKeepsADamagedLimbAttachedUntilSevereTrauma()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            Server.CfgMan.SetCVar(RuBalanceCVars.LimbSeveranceThresholdMultiplier, 1.5f);
            var human = SSpawnAtPosition("CMMobHuman", map.GridCoords);
            var index = Server.System<CMUMedicalBodyIndexSystem>();
            var key = new CMUMedicalBodyPartKey(BodyPartType.Arm, BodyPartSymmetry.Left);
            Assert.That(index.TryGetBodyPart(human, key, out var arm), Is.True);
            var health = SEntMan.GetComponent<BodyPartHealthComponent>(arm);
            var legacyThreshold = health.Max + health.SeveranceThreshold;
            var impact = DamageImpact.MeleeSlash;
            var onePoint = new DamageSpecifier { DamageDict = { ["Slash"] = FixedPoint2.New(1) } };
            var perPoint = DamageImpactSeverance.Calculate(onePoint, impact).Float()
                * Server.CfgMan.GetCVar(CMUMedicalCCVars.BodyPartDamagePropagation);
            var damage = new DamageSpecifier
            {
                DamageDict = { ["Slash"] = FixedPoint2.New(legacyThreshold.Float() * 1.2f / perPoint) },
            };
            var parts = Server.System<SharedBodyPartHealthSystem>();
            Assert.That(parts.TryApplyPartDamage(human, arm, damage, impact: impact, ignoreResistance: true), Is.True);
            Assert.That(health.SeveranceDamage, Is.GreaterThan(legacyThreshold));
            Assert.That(index.TryGetBodyPart(human, key, out _), Is.True,
                "crossing the legacy threshold alone must no longer detach the limb");
            Assert.That(parts.TryApplyPartDamage(human, arm, damage, impact: impact, ignoreResistance: true), Is.True);
            Assert.That(index.TryGetBodyPart(human, key, out _), Is.False,
                "the higher threshold must remain finite");
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

    [TestCase(false, false, false)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(false, false, true)]
    public async Task RepeatTraumaticSeveranceRequiresCatastrophicDamageButSurgeryStillWorks(bool catastrophic, bool surgical, bool expired)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var human = SSpawnAtPosition("CMMobHuman", map.GridCoords);
            var index = Server.System<CMUMedicalBodyIndexSystem>();
            Assert.That(index.TryGetBodyPart(human, new(BodyPartType.Arm, BodyPartSymmetry.Left), out var first), Is.True);
            Assert.That(index.TryGetBodyPart(human, new(BodyPartType.Arm, BodyPartSymmetry.Right), out var second), Is.True);
            var sever = new BodyPartSeverAttemptEvent(human, first, BodyPartType.Arm);
            SEntMan.EventBus.RaiseLocalEvent(first, ref sever);
            Assert.That(sever.Succeeded, Is.True);

            if (expired)
                SEntMan.GetComponent<Content.Server._RuCM.Balance.RepeatSeveranceComponent>(human).LastSeverance
                    -= TimeSpan.FromSeconds(Server.CfgMan.GetCVar(RuBalanceCVars.RepeatSeveranceSeconds) + 1);

            if (catastrophic)
            {
                var damage = new DamageSpecifier { DamageDict = { ["Slash"] = FixedPoint2.New(2000) } };
                Assert.That(Server.System<SharedBodyPartHealthSystem>().TryApplyPartDamage(
                    human, second, damage, impact: DamageImpact.MeleeSlash, ignoreResistance: true), Is.True);
                Assert.That(index.TryGetBodyPart(human, new(BodyPartType.Arm, BodyPartSymmetry.Right), out _), Is.False);
                return;
            }
            sever = new(human, second, BodyPartType.Arm, Surgical: surgical);
            SEntMan.EventBus.RaiseLocalEvent(second, ref sever);
            Assert.That(sever.Succeeded, Is.EqualTo(surgical || expired));
            Assert.That(sever.Cancelled, Is.EqualTo(!surgical && !expired));
        });
    }
}

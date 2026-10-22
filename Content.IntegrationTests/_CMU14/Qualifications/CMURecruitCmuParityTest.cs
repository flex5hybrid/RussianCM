using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Content.IntegrationTests.Fixtures;
using Content.Server._RuCM.Qualifications;
using Content.Server.CMU14.Round;
using Content.Server.CMU14.Marines.Roles.Chevrons;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.GameTicking.Presets;
using Content.Server.Maps;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Station.Systems;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.UniformAccessories;
using Content.Shared._RuCM.Qualifications;
using Content.Shared.Access.Components;
using Content.Shared.CMU14;
using Content.Shared.CMU14.Marines.Roles.Ranks;
using Content.Shared.CMU14.util;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Station;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.EntitySerialization;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Network;
using RankSystem = Content.Server._RMC14.Marines.Roles.Ranks.RankSystem;

namespace Content.IntegrationTests.CMU14.Qualifications;

[TestFixture]
public sealed class CMURecruitCmuParityTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Destructive = true };
    [TestPrototypes]
    private const string Prototypes = """
        - type: entity
          id: CMUTestRecruitShipPlanet
          components:
          - type: RMCPlanetMapPrototype
            mapId: USSBushRedux
            inRotation: false
            govforinship: true
            opforinship: true
            govfordropships: 0
            opfordropships: 0
        - type: entity
          id: CMUTestRecruitGroundPlanet
          components:
          - type: RMCPlanetMapPrototype
            mapId: USSBushRedux
            inRotation: false
            govforinship: false
            opforinship: true
            govfordropships: 0
            opfordropships: 0
        """;

    [TestCase("USSBush")]
    [TestCase("USSBushRedux")]
    public async Task ActualBushMapsOfferAndSpawnRecruitWithoutSpecialMapMarkers(string mapId)
    {
        var pair = Pair;
        var server = pair.Server;
        await server.WaitAssertion(() =>
        {
            Configure(server, true);
            var entities = server.EntMan;
            var ticker = entities.System<GameTicker>();
            var grids = ticker.LoadGameMap(server.ProtoMan.Index<GameMapPrototype>(mapId),
                out _, DeserializationOptions.Default with { InitializeMaps = true });
            Assert.That(grids, Is.Not.Empty);
            foreach (var grid in grids)
                entities.EnsureComponent<ShipFactionComponent>(grid).Faction = "govfor";
            var station = entities.System<StationSystem>().GetOwningStation(grids.First());
            Assert.That(station, Is.Not.Null);
            RefreshSlots(entities);
            var jobs = entities.System<StationJobsSystem>();
            Assert.That(jobs.TryGetJobSlot(station!.Value, GOVFORRecruitJob.Id, out var slots), Is.True);
            Assert.That(slots, Is.EqualTo(4));
            var recruit = server.ProtoMan.Index<JobPrototype>(GOVFORRecruitJob.Id);
            Assert.That(recruit.CharacterSetupPresets, Is.EquivalentTo(new[] { "Insurgency" }));
            Assert.That(recruit.CanBeAntag, Is.False);
            Assert.That(recruit.IsAvailableInCharacterSetup("ForceOnForce"), Is.False);
            Assert.That(recruit.IsAvailableInCharacterSetup("DistressSignal"), Is.False);
            Assert.That(entities.System<QualificationSystem>().CanTakeJob(ServerSession.UserId, recruit.ID), Is.True);

            var profile = HumanoidCharacterProfile.DefaultWithSpecies();
            var spawn = new PlayerSpawningEvent(GOVFORRecruitJob.Id, profile, station);
            entities.EventBus.RaiseEvent(EventSource.Local, spawn);
            Assert.That(spawn.SpawnResult, Is.Not.Null, $"{mapId}: native generic arrival must be usable");
            var mob = spawn.SpawnResult!.Value;
            var skills = entities.System<SkillsSystem>();
            Assert.That(skills.GetSkill(mob, "RMCSkillFirearms"), Is.EqualTo(1));
            Assert.That(skills.GetSkill(mob, "RMCSkillConstruction"), Is.EqualTo(1));
            Assert.That(skills.GetSkill(mob, "RMCSkillMedical"), Is.Zero);
            Assert.That(entities.System<StationSystem>().GetOwningStation(mob), Is.EqualTo(station));
            Assert.That(entities.HasComponent<SquadMemberComponent>(mob), Is.False, "No temporary combat squad assignment");
            Assert.That(entities.System<RankSystem>().GetRank(mob)!.Paygrade, Is.EqualTo("E0"));
            var inventory = entities.System<InventorySystem>();
            Assert.That(inventory.TryGetSlotEntity(mob, "id", out var card), Is.True);
            Assert.That(entities.GetComponent<AccessComponent>(card!.Value).Tags.Select(t => t.Id),
                Is.EquivalentTo(new[] { "AU14AccessGovfor" }));
            Assert.That(inventory.TryGetSlotEntity(mob, "outerClothing", out _), Is.False);
            Assert.That(inventory.TryGetSlotEntity(mob, "belt", out _), Is.False);
            var participant = ServerSession;
            Assert.That(jobs.TryAssignJob(station.Value, recruit, participant.UserId), Is.True);
            server.ResolveDependency<IConfigurationManager>().SetCVar(GOVFORRecruitJob.Slots, 3);
            RefreshSlots(entities);
            Assert.That(jobs.TryGetJobSlot(station.Value, recruit.ID, out slots), Is.True);
            Assert.That(slots, Is.EqualTo(2), "CVar changes preserve occupied places");

            entities.System<AuRoundSystem>().SetPreset(server.ProtoMan.Index<GamePresetPrototype>("ForceOnForce"));
            RefreshSlots(entities);
            Assert.That(jobs.TryGetJobSlot(station.Value, recruit.ID, out slots), Is.True);
            Assert.That(slots, Is.Zero);
        });
    }

    [Test]
    public async Task GenericArrivalsStayOnGovforCarrierAndGroundRequiresGovforAnchor()
    {
        var pair = Pair;
        var gov = await pair.CreateTestMap();
        var op = await pair.CreateTestMap();
        var colony = await pair.CreateTestMap();
        await pair.Server.WaitAssertion(() =>
        {
            var server = pair.Server;
            var entities = server.EntMan;
            Configure(server, true);
            var stations = entities.System<StationSystem>();
            EntityUid AddStation(EntityUid grid) => stations.InitializeNewStation(
                new StationConfig { StationPrototype = "StandardUNMCWarship", StationComponentOverrides = new() },
                new[] { grid });
            var govStation = AddStation(gov.GridCoords.EntityId);
            var opStation = AddStation(op.GridCoords.EntityId);
            var colonyStation = AddStation(colony.GridCoords.EntityId);
            entities.EnsureComponent<ShipFactionComponent>(gov.GridCoords.EntityId).Faction = "govfor";
            entities.EnsureComponent<ShipFactionComponent>(op.GridCoords.EntityId).Faction = "opfor";
            foreach (var coordinates in new[] { gov.GridCoords, op.GridCoords, colony.GridCoords })
                entities.SpawnEntity("SpawnPointLatejoin", coordinates);
            RefreshSlots(entities);
            var jobs = entities.System<StationJobsSystem>();
            Assert.That(jobs.TryGetJobSlot(govStation, GOVFORRecruitJob.Id, out var slots), Is.True);
            Assert.That(slots, Is.EqualTo(4));
            Assert.That(jobs.TryGetJobSlot(opStation, GOVFORRecruitJob.Id, out slots) && slots != 0, Is.False);
            Assert.That(jobs.TryGetJobSlot(colonyStation, GOVFORRecruitJob.Id, out slots) && slots != 0, Is.False);

            Assert.That(entities.System<AuRoundSystem>().SetPlanet("CMUTestRecruitGroundPlanet"), Is.True);
            typeof(GameTicker).GetProperty(nameof(GameTicker.DefaultMap))!.SetValue(
                entities.System<GameTicker>(), colony.MapId);
            RefreshSlots(entities);
            Assert.That(jobs.TryGetJobSlot(govStation, GOVFORRecruitJob.Id, out slots), Is.True);
            Assert.That(slots, Is.Zero);
            Assert.That(jobs.TryGetJobSlot(colonyStation, GOVFORRecruitJob.Id, out slots) && slots != 0, Is.False,
                "Ordinary colony arrivals must not become a GOVFOR training base");
            entities.SpawnEntity("RuCMSpawnPointGOVFORRecruit", colony.GridCoords);
            RefreshSlots(entities);
            Assert.That(jobs.TryGetJobSlot(colonyStation, GOVFORRecruitJob.Id, out slots), Is.True);
            Assert.That(slots, Is.EqualTo(4));
        });
    }

    [Test]
    public async Task EveryNativePlatoonEquipsRecruitAndInstructorWithoutChangingOtherModes()
    {
        var pair = Pair;
        var map = await pair.CreateTestMap();
        var server = pair.Server;
        await PoolManager.WaitUntil(server, async () =>
        {
            var ready = false;
            await server.WaitPost(() => ready = server.ResolveDependency<PlayTimeTrackingManager>()
                .TryGetTrackerTimes(ServerSession, out _));
            return ready;
        });
        await server.WaitAssertion(() =>
        {
            Configure(server, true);
            var entities = server.EntMan;
            var prototypes = server.ProtoMan;
            var platoons = entities.System<PlatoonSpawnRuleSystem>();
            var spawning = entities.System<StationSpawningSystem>();
            var inventory = entities.System<InventorySystem>();
            var ranks = entities.System<RankSystem>();
            var profile = HumanoidCharacterProfile.DefaultWithSpecies();
            var station = entities.System<StationSystem>().InitializeNewStation(
                new StationConfig { StationPrototype = "StandardUNMCWarship", StationComponentOverrides = new() },
                new[] { map.GridCoords.EntityId });
            var nativePlatoons = prototypes.EnumeratePrototypes<PlatoonPrototype>()
                .Where(p => p.RecruitGear != null).ToArray();
            Assert.That(nativePlatoons.Select(p => p.ID), Is.EquivalentTo(
                new[] { "USCM", "LACN", "UPP", "WEYU", "CMBCIU", "HAZOPS", "ProdigySF", "VAIPO", "RMC" }));
            foreach (var platoon in nativePlatoons)
            {
                platoons.SelectedGovforPlatoon = platoon;
                foreach (var jobId in new[] { GOVFORRecruitJob.Id, "AU14JobGOVFORadvisor" })
                {
                    var isRecruit = jobId == GOVFORRecruitJob.Id;
                    var gearId = isRecruit ? platoon.RecruitGear : platoon.InstructorGear;
                    Assert.That(gearId, Is.Not.Null);
                    var gear = prototypes.Index(gearId!.Value);
                    Assert.That(gear.Inhand, Is.Empty);
                    Assert.That(gear.Storage, Is.Empty);
                    Assert.That(gear.Equipment.Keys, Is.SubsetOf(new[] { "jumpsuit", "shoes", "head" }));
                    var mob = spawning.SpawnPlayerMob(map.GridCoords, jobId, profile, station);
                    var completed = new PlayerSpawnCompleteEvent(mob, ServerSession, jobId, false, true, 1, station, profile);
                    entities.EventBus.RaiseEvent(EventSource.Local, completed);
                    foreach (var slot in new[] { "jumpsuit", "shoes" })
                    {
                        Assert.That(inventory.TryGetSlotEntity(mob, slot, out var item), Is.True, $"{platoon.ID}/{jobId}/{slot}");
                        Assert.That(entities.GetComponent<MetaDataComponent>(item!.Value).EntityPrototype!.ID,
                            Is.EqualTo(gear.Equipment[slot].Id));
                    }
                    Assert.That(entities.HasComponent<SquadMemberComponent>(mob), Is.False);
                    Assert.That(inventory.TryGetSlotEntity(mob, "jumpsuit", out var uniform), Is.True);
                    var holder = entities.GetComponent<UniformAccessoryHolderComponent>(uniform!.Value);
                    var accessories = entities.System<SharedContainerSystem>().GetContainer(uniform.Value, holder.ContainerId);
                    Assert.That(accessories.ContainedEntities, Is.Not.Empty, $"{platoon.ID}/{jobId}: insignia on native uniform");
                    if (isRecruit)
                    {
                        var skills = entities.System<SkillsSystem>();
                        Assert.That(skills.GetSkill(mob, "RMCSkillFirearms"), Is.EqualTo(1), platoon.ID);
                        Assert.That(skills.GetSkill(mob, "RMCSkillConstruction"), Is.EqualTo(1), platoon.ID);
                        Assert.That(skills.GetSkill(mob, "RMCSkillMedical"), Is.Zero, platoon.ID);
                        Assert.That(ranks.GetRank(mob)!.Paygrade, Is.EqualTo("E0"), platoon.ID);
                        Assert.That(accessories.ContainedEntities.Select(e => entities.GetComponent<MetaDataComponent>(e).EntityPrototype!.ID),
                            Does.Contain("CMUChevronRecruit"));
                    }
                    else
                    {
                        server.ResolveDependency<PlayTimeTrackingManager>().TryGetTrackerTimes(ServerSession, out var playTimes);
                        var intended = entities.System<ChevronSystem>().ResolveIntendedRank(mob, jobId, profile, playTimes!);
                        Assert.That(intended, Is.Not.Null, platoon.ID);
                        Assert.That(ranks.GetRank(mob)!.ID, Is.EqualTo(intended!.ID), platoon.ID);
                        Assert.That(entities.GetComponent<JobPrefixComponent>(mob).Prefix.ToString(),
                            Is.EqualTo("cmu-job-prefix-drill-instructor"));
                    }
                    var expectedRank = ranks.GetRank(mob)!.ID;
                    var fallbackRank = isRecruit ? "RMCRankRecruit" : "RMCRankCivilian";
                    Assert.That(inventory.TryUnequip(mob, "jumpsuit", force: true), Is.True);
                    Assert.That(ranks.GetRank(mob)!.ID, Is.EqualTo(fallbackRank), "Removing uniform restores base job rank");
                    Assert.That(inventory.TryEquip(mob, uniform.Value, "jumpsuit", force: true), Is.True);
                    Assert.That(ranks.GetRank(mob)!.ID, Is.EqualTo(expectedRank), "Re-equipping restores platoon insignia rank");
                    var insignia = accessories.ContainedEntities.First(e => entities.HasComponent<RankChangerComponent>(e));
                    Assert.That(entities.System<SharedContainerSystem>().Remove(insignia, accessories), Is.True);
                    Assert.That(ranks.GetRank(mob)!.ID, Is.EqualTo(fallbackRank), "Removing insignia restores base job rank");
                    Assert.That(entities.System<SharedUniformAccessorySystem>().TryInsertUniformAccessory(
                        insignia, uniform.Value, mob), Is.True);
                    Assert.That(ranks.GetRank(mob)!.ID, Is.EqualTo(expectedRank), "Inserting insignia applies its native rank");
                    entities.DeleteEntity(mob);
                }
            }
            entities.System<AuRoundSystem>().SetPreset(prototypes.Index<GamePresetPrototype>("ForceOnForce"));
            var advisor = spawning.SpawnPlayerMob(map.GridCoords, "AU14JobGOVFORadvisor", profile, station);
            entities.EventBus.RaiseEvent(EventSource.Local, new PlayerSpawnCompleteEvent(
                advisor, ServerSession, "AU14JobGOVFORadvisor", false, true, 1, station, profile));
            Assert.That(entities.GetComponent<JobPrefixComponent>(advisor).Prefix.ToString(),
                Is.EqualTo("au14-job-prefix-govforadvisor"));
        });
    }

    private static void Configure(Robust.UnitTesting.RobustIntegrationTest.ServerIntegrationInstance server, bool ship)
    {
        var round = server.System<AuRoundSystem>();
        Assert.That(round.SetPlanet(ship ? "CMUTestRecruitShipPlanet" : "CMUTestRecruitGroundPlanet"), Is.True);
        round.SetPreset(server.ProtoMan.Index<GamePresetPrototype>("Insurgency"));
        typeof(GameTicker).GetField("_runLevel", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(server.System<GameTicker>(), GameRunLevel.PreRoundLobby);
        server.ResolveDependency<IConfigurationManager>().SetCVar(GOVFORRecruitJob.Slots, 4);
        Assert.That(server.System<QualificationSystem>().IsInsurgency, Is.True);
    }

    private static void RefreshSlots(IEntityManager entities) =>
        entities.EventBus.RaiseEvent(EventSource.Local, new RulePlayerSpawningEvent(new(),
            new Dictionary<NetUserId, HumanoidCharacterProfile>(), false));
}

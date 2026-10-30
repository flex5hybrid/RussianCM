using Content.IntegrationTests.Fixtures;
using Content.Server._RMC14.Marines.Roles.Ranks;
using Content.Shared.ActionBlocker;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared._RMC14.Roles;
using Content.Shared.CMU14.Marines.Orders;
using Content.Shared.CMU14.Round.Roles;
using Content.Shared.Movement.Components;
using Content.Shared.Roles;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.IntegrationTests.CMU14.Round;

[TestFixture]
public sealed class AttentionRoleTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = false };

    [Test]
    public async Task ConfiguredMilitaryLeadershipRolesRetainTheirOrderAbility()
    {
        var eligibleRoles = new HashSet<string>
        {
            "Advisor", "EngineeringOfficer", "IntelOfficer", "LogisticsOfficer", "CMO", "ChiefMP",
            "JuniorOfficer", "VehicleCommander", "SectionSergeant", "SquadSergeant", "RadioTelephoneOperator",
            "AdjutantDress", "BrigadierGeneral", "MilitaryPolice",
        };
        await Server.WaitAssertion(() =>
        {
            foreach (var job in SProtoMan.EnumeratePrototypes<JobPrototype>())
            {
                if (job.Abstract || job.RoundRole == null || job.RoundSide is not (RoundJobSide.Govfor or RoundJobSide.Opfor))
                    continue;

                var present = job.RoundComponents.TryGetValue("AU14CallToAttentionAbility", out var entry);
                var seniorOfficer = job.RoundRole is "PlatoonCommander" or "ExecutiveOfficer";
                Assert.That(present, Is.EqualTo(eligibleRoles.Contains(job.RoundRole) || seniorOfficer), job.ID);
                if (present)
                {
                    var ability = (AU14CallToAttentionAbilityComponent) entry!.Component;
                    Assert.That(ability.CanCall, Is.True, job.ID);
                    if (seniorOfficer)
                        Assert.That(ability.AttentionFocusPriority, Is.GreaterThan(0), job.ID);
                }
            }
        });
    }

    [TestCase("AU14JobGOVFORPlatCo")]
    [TestCase("AU14JobGOVFORAdjutant")]
    [TestCase("AU14JobOPFORPlatCo")]
    [TestCase("AU14JobOPFORAdjutant")]
    [TestCase("AU14JobGOVFORPlatCoUPP")]
    [TestCase("AU14JobGOVFORPlatCoWYPMC")]
    [TestCase("AU14JobGOVFORPlatCoCMBCIU")]
    public async Task SeniorOfficerVariantsReceiveTheUnifiedOrdersAction(string jobId)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var officer = SEntMan.SpawnEntity("CMMobHuman", map.GridCoords);
            try
            {
                SEntMan.AddComponents(officer, SProtoMan.Index<JobPrototype>(jobId).RoundComponents);
                var ability = SEntMan.GetComponent<AU14CallToAttentionAbilityComponent>(officer);
                Assert.That(ability.CanCall, Is.True);
                Assert.That(ability.ActionEntity, Is.Not.Null);
                var action = new AU14CallToAttentionActionEvent();
                SEntMan.EventBus.RaiseLocalEvent(officer, action);
                Assert.That(action.Handled, Is.True);
            }
            finally
            {
                SEntMan.DeleteEntity(officer);
            }
        });
    }

    [TestCase("AU14JobGOVFORPlatOp", "RMCRankCaptain")]
    [TestCase("AU14JobGOVFORSquadSergeant", "RMCRankSergeant")]
    [TestCase("AU14JobOPFORSquadSergeant", "RMCRankSergeant")]
    public async Task AuthorizedCallsFocusOnTheSeniorOfficer(string jobId, string rankId)
    {
        var map = await Pair.CreateTestMap();
        EntityUid leader = default, commander = default, marine = default;
        await Server.WaitAssertion(() =>
        {
            leader = SEntMan.SpawnEntity("CMMobHuman", map.GridCoords);
            commander = SEntMan.SpawnEntity("CMMobHuman", map.GridCoords.Offset(new Vector2(1, 2)));
            marine = SEntMan.SpawnEntity("CMMobHuman", map.GridCoords.Offset(new Vector2(1, 0)));
            var callerJob = SProtoMan.Index<JobPrototype>(jobId);
            var force = callerJob.RoundSide == RoundJobSide.Opfor ? "OPFOR" : "GOVFOR";
            var commanderJob = SProtoMan.Index<JobPrototype>($"AU14Job{force}PlatCo");
            var marineJob = SProtoMan.Index<JobPrototype>($"AU14Job{force}SquadRifleman");
            SEntMan.AddComponents(leader, callerJob.RoundComponents);
            SEntMan.EnsureComponent<OriginalRoleComponent>(leader).Job = jobId;
            SEntMan.EnsureComponent<MarineComponent>(leader);
            SEntMan.System<RankSystem>().SetRank(leader, "RMCRankMajor");
            Assert.That(SEntMan.GetComponent<AU14CallToAttentionAbilityComponent>(leader).ActionEntity, Is.Not.Null);
            SEntMan.AddComponents(commander, commanderJob.RoundComponents);
            SEntMan.EnsureComponent<OriginalRoleComponent>(commander).Job = commanderJob.ID;
            SEntMan.AddComponents(marine, marineJob.RoundComponents);
            SEntMan.EnsureComponent<OriginalRoleComponent>(marine).Job = marineJob.ID;
            foreach (var uid in new[] { leader, commander, marine })
                SEntMan.EnsureComponent<MarineComponent>(uid);
            var ranks = SEntMan.System<SharedRankSystem>();
            ranks.SetRank(leader, rankId);
            ranks.SetRank(commander, "RMCRankMajor");
            ranks.SetRank(marine, "RMCRankPrivate");
            SEntMan.GetComponent<AU14CallToAttentionAbilityComponent>(leader).ResponseStagger = TimeSpan.Zero;
            var action = new AU14CallToAttentionActionEvent();
            SEntMan.EventBus.RaiseLocalEvent(leader, action);
            Assert.That(action.Handled, Is.True);
            var ui = SEntMan.System<SharedUserInterfaceSystem>();
            Assert.That(ui.IsUiOpen(leader, AU14MilitaryOrderUiKey.Key, leader), Is.True);
            ui.RaiseUiMessage(leader, AU14MilitaryOrderUiKey.Key,
                new AU14MilitaryOrderSelectedBuiMsg(AU14MilitaryOrder.Attention) { Actor = leader });
        });
        await Pair.RunSeconds(1);
        await Server.WaitAssertion(() =>
        {
            var transform = SEntMan.System<SharedTransformSystem>();
            var towardCommander = transform.GetMapCoordinates(commander).Position - transform.GetMapCoordinates(marine).Position;
            var expected = Angle.FromWorldVec(towardCommander);
            Assert.That(Math.Abs(Angle.ShortestDistance(transform.GetWorldRotation(marine), expected).Degrees),
                Is.LessThan(0.01), "Personnel should face the senior officer even when they are not the caller.");
            foreach (var uid in new[] { leader, commander, marine })
                SEntMan.DeleteEntity(uid);
        });
    }

    [Test]
    public async Task OfficerAttentionOrderReachesSubordinateAndExpiresAfterOneSecond()
    {
        var map = await Pair.CreateTestMap();
        EntityUid officer = default, marine = default;
        await Server.WaitAssertion(() =>
        {
            SEntMan.System<SharedMapSystem>().SetPaused(map.MapUid, false);
            officer = SEntMan.SpawnEntity("CMMobHuman", map.GridCoords);
            marine = SEntMan.SpawnEntity("CMMobHuman", map.GridCoords.Offset(new Vector2(1, 0)));

            var officerJob = SProtoMan.Index<JobPrototype>(AU14JobGOVFORPlatCoPrototype);
            SEntMan.AddComponents(officer, officerJob.RoundComponents);
            SEntMan.EnsureComponent<OriginalRoleComponent>(officer).Job = officerJob.ID;
            SEntMan.EnsureComponent<MarineComponent>(officer);
            SEntMan.System<RankSystem>().SetRank(officer, "RMCRankMajor");

            var marineJob = SProtoMan.Index<JobPrototype>(AU14JobGOVFORSquadRiflemanPrototype);
            SEntMan.AddComponents(marine, marineJob.RoundComponents);
            SEntMan.EnsureComponent<OriginalRoleComponent>(marine).Job = marineJob.ID;
            SEntMan.EnsureComponent<MarineComponent>(marine);
            SEntMan.System<RankSystem>().SetRank(marine, "RMCRankPrivate");

            var ability = SEntMan.GetComponent<AU14CallToAttentionAbilityComponent>(officer);
            ability.ResponseStagger = TimeSpan.Zero;
            Assert.That(ability.CanCall, Is.True);
            Assert.That(SEntMan.System<RankSystem>().GetRank(officer)?.ID, Is.EqualTo("RMCRankMajor"));
            Assert.That(SEntMan.System<RankSystem>().GetRank(marine)?.ID, Is.EqualTo("RMCRankPrivate"));
            Assert.That(officerJob.RoundForce, Is.EqualTo(marineJob.RoundForce));
            var action = new AU14CallToAttentionActionEvent();
            SEntMan.EventBus.RaiseLocalEvent(officer, action);
            var ui = SEntMan.System<SharedUserInterfaceSystem>();
            Assert.That(ui.IsUiOpen(officer, AU14MilitaryOrderUiKey.Key, officer), Is.True);
            ui.RaiseUiMessage(officer, AU14MilitaryOrderUiKey.Key,
                new AU14MilitaryOrderSelectedBuiMsg(AU14MilitaryOrder.Attention) { Actor = officer });
            Assert.That(ability.AttentionReadyAt, Is.GreaterThan(SGameTiming.CurTime),
                $"order was rejected; officer job {SEntMan.System<RankSystem>().GetJobId(officer)}, target job {SEntMan.System<RankSystem>().GetJobId(marine)}");
        });

        await Pair.RunSeconds(0.1f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<AU14StandAtAttentionComponent>(marine), Is.True);
            Assert.That(SEntMan.System<ActionBlockerSystem>().CanMove(marine), Is.False);
        });

        await Pair.RunSeconds(1.1f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<AU14StandAtAttentionComponent>(marine), Is.False);
            SEntMan.DeleteEntity(officer);
            SEntMan.DeleteEntity(marine);
        });
    }

    [Test]
    public async Task StandAtAttentionBlocksMovementForItsDuration()
    {
        var map = await Pair.CreateTestMap();
        EntityUid marine = default;
        TimeSpan expiresAt = default;
        await Server.WaitAssertion(() =>
        {
            marine = SEntMan.SpawnEntity("CMMobHuman", map.GridCoords);
        });
        // Uncontrolled humans lose their input mover after MapInit; this test needs one to check movement.
        await Pair.RunTicksSync(1);
        await Server.WaitAssertion(() =>
        {
            SEntMan.EnsureComponent<InputMoverComponent>(marine);
            var actionBlocker = SEntMan.System<ActionBlockerSystem>();
            actionBlocker.UpdateCanMove(marine);
            Assert.That(actionBlocker.CanMove(marine), Is.True);

            var attention = SEntMan.EnsureComponent<AU14StandAtAttentionComponent>(marine);
            attention.ExpiresAt = SGameTiming.CurTime + TimeSpan.FromSeconds(1);
            expiresAt = attention.ExpiresAt;

            actionBlocker.UpdateCanMove(marine);
            Assert.That(actionBlocker.CanMove(marine), Is.False);
        });

        await Pair.RunSeconds(0.5f);
        await Server.WaitAssertion(() =>
            Assert.That(SEntMan.System<ActionBlockerSystem>().CanMove(marine), Is.False));

        await Pair.RunSeconds(0.75f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<AU14StandAtAttentionComponent>(marine), Is.False);
            Assert.That(SEntMan.System<ActionBlockerSystem>().CanMove(marine), Is.True);
            SEntMan.DeleteEntity(marine);
        });
    }

    private void MakeMovable(EntityUid uid)
    {
        var physics = SEntMan.GetComponent<PhysicsComponent>(uid);
        SEntMan.System<SharedPhysicsSystem>().SetBodyType(uid, BodyType.KinematicController, body: physics);
        SEntMan.EnsureComponent<GodmodeComponent>(uid);
        SEntMan.System<MobStateSystem>().ChangeMobState(uid, MobState.Alive);
        SEntMan.System<ActionBlockerSystem>().UpdateCanMove(uid);
    }
}

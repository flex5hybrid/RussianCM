#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server._RMC14.DonorCapes;
using Content.Server._RMC14.LinkAccount;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Shared._RMC14.DonorCapes;
using Content.Shared._RMC14.LinkAccount;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests._RuMC14.DonorCapes;

[TestFixture]
public sealed class DonorCapeSpawnTest : GameTest
{
    private static readonly ProtoId<RMCDonorCapePrototype> Cape = new("rmc-donor-cape-01");

    public override PoolSettings PoolSettings => new()
    {
        Dirty = true,
        DummyTicker = false,
        Connected = true,
        InLobby = true,
    };

    [Test]
    [RunOnSide(Side.Server)]
    public async Task SubscriberGetsCapeAfterInitialSpawnAndRoleAssignment()
    {
        var (mob, player, profile) = await SpawnControlledPlayer(3);

        await Server.WaitAssertion(() =>
        {
            var uncontrolledBody = SSpawnAtPosition("MobHuman", SComp<TransformComponent>(mob).Coordinates);
            SEntMan.EventBus.RaiseEvent(EventSource.Local, new PlayerSpawnCompleteEvent(
                uncontrolledBody,
                player,
                jobId: null,
                lateJoin: true,
                silent: false,
                joinOrder: 2,
                station: EntityUid.Invalid,
                profile: profile));
            Assert.That(IsHoldingCape(uncontrolledBody), Is.False, "a cape must not go to a body the player does not control");

            RaiseJobsAssigned(player, profile);

            Assert.That(IsHoldingCape(mob), Is.True);
        });
    }

    [RunOnSide(Side.Server)]
    [TestCase(null, TestName = "NoSubscriptionDoesNotGrantCape")]
    [TestCase(2, TestName = "InsufficientTierDoesNotGrantCape")]
    public async Task NoSubscriptionOrInsufficientTierDoesNotGrantCape(int? priority)
    {
        var (mob, player, profile) = await SpawnControlledPlayer(priority);

        await Server.WaitAssertion(() =>
        {
            RaiseJobsAssigned(player, profile);
            Assert.That(IsHoldingCape(mob), Is.False);
        });
    }

    [Test]
    [RunOnSide(Side.Server)]
    public async Task AntagonistAssignedAfterInitialSpawnDoesNotGetCape()
    {
        var (mob, player, profile) = await SpawnControlledPlayer(3);

        await Server.WaitAssertion(() =>
        {
            var minds = Server.System<SharedMindSystem>();
            Assert.That(minds.TryGetMind(mob, out var mindId, out var mind), Is.True);
            Server.System<SharedRoleSystem>().MindAddRole(mindId, "MindRoleTraitor", mind, silent: true);
            Assert.That(Server.System<SharedRoleSystem>().MindIsAntagonist(mindId), Is.True);

            RaiseJobsAssigned(player, profile);

            Assert.That(IsHoldingCape(mob), Is.False);
        });
    }

    private async Task<(EntityUid Mob, ICommonSession Player, HumanoidCharacterProfile Profile)> SpawnControlledPlayer(int? priority)
    {
        var player = ServerSession!;
        var map = await Pair.CreateTestMap();
        var profile = new HumanoidCharacterProfile().WithSelectedDonorCape(Cape);
        EntityUid mob = EntityUid.Invalid;

        await Server.WaitAssertion(() =>
        {
            SetConnectedTier(Server.System<LinkAccountManager>(), player, priority);
            RaisePlayerSpawning(player, profile);

            mob = SSpawnAtPosition("MobHuman", map.GridCoords);
            Server.System<MindSystem>().ControlMob(player.UserId, mob);
            Assert.That(player.AttachedEntity, Is.EqualTo(mob));

            SEntMan.EventBus.RaiseEvent(EventSource.Local, new PlayerSpawnCompleteEvent(
                mob,
                player,
                jobId: null,
                lateJoin: false,
                silent: false,
                joinOrder: 1,
                station: EntityUid.Invalid,
                profile: profile));
            Assert.That(IsHoldingCape(mob), Is.False, "initial spawning waits for role assignment");
        });

        return (mob, player, profile);
    }

    private void RaisePlayerSpawning(ICommonSession player, HumanoidCharacterProfile profile)
    {
        var profiles = new Dictionary<NetUserId, HumanoidCharacterProfile> { [player.UserId] = profile };
        var players = new List<ICommonSession> { player };

        SEntMan.EventBus.RaiseEvent(EventSource.Local, new RulePlayerSpawningEvent(players, profiles, forced: false));
    }

    private void RaiseJobsAssigned(ICommonSession player, HumanoidCharacterProfile profile)
    {
        var profiles = new Dictionary<NetUserId, HumanoidCharacterProfile> { [player.UserId] = profile };
        SEntMan.EventBus.RaiseEvent(EventSource.Local, new RulePlayerJobsAssignedEvent(
            new[] { player },
            profiles,
            forced: false));
    }

    private static void SetConnectedTier(LinkAccountManager manager, ICommonSession player, int? priority)
    {
        var field = typeof(LinkAccountManager).GetField("_connected", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var connected = (Dictionary<NetUserId, SharedRMCPatronFull>) field.GetValue(manager)!;

        if (priority is not { } tierPriority)
        {
            connected[player.UserId] = new SharedRMCPatronFull(null, true, null, null, null);
            return;
        }

        var tier = new SharedRMCPatronTier(false, false, false, false, false, false, "test", tierPriority);
        connected[player.UserId] = new SharedRMCPatronFull(tier, true, null, null, null);
    }

    private bool IsHoldingCape(EntityUid mob)
    {
        var hands = SComp<HandsComponent>(mob);
        var handSystem = SEntMan.System<SharedHandsSystem>();
        return hands.SortedHands.Any(handId =>
        {
            var item = handSystem.GetHeldItem((mob, hands), handId);
            return item is { } entity &&
                   SComp<MetaDataComponent>(entity).EntityPrototype?.ID == "RMCDonorCapeItem01";
        });
    }
}

using Content.Client._RMC14.LinkAccount;
using Content.Client.CMU14.Sponsors;
using Content.IntegrationTests.Fixtures;
using Content.Server.Database;
using Content.Shared.CMU14.Sponsors;
using Content.Server.CMU14.Sponsors;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.UniformAccessories;
using Content.Shared.Preferences;
using Robust.Shared.Containers;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Network;
using System.Net;
using System.Text.Json;

namespace Content.IntegrationTests.Tests.CMU14;

// Each case requires a fresh content pair; serialize initialization of the large prototype catalog.
[TestFixture, NonParallelizable]
public sealed class CMUSponsorIntegrationTests : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };

    private async Task RunServerAsync(Func<Task> action)
    {
        Task pending = null!;
        await Server.WaitPost(() => pending = action());
        for (var i = 0; i < 200 && !pending.IsCompleted; i++)
            await Pair.RunTicksSync(3);
        await pending.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Test]
    public async Task ScoutReceivesChosenCapeAndDogtagAndDowngradeStopsDelivery()
    {
        var user = ServerSession!.UserId;
        var db = Server.ResolveDependency<IServerDbManager>();
        var link = Server.ResolveDependency<Content.Server._RMC14.LinkAccount.LinkAccountManager>();
        var settings = new CMUSponsorSettings("spark", "Remember", "peace", "16", "gold", "branch");
        await RunServerAsync(async () =>
        {
            await db.UpsertPatronTier("SponsorTestScout", 999999998UL, 3, true, true, true, true, true, false);
            Assert.That(await db.SetPatronTier(user.UserId, "SponsorTestScout"), Is.EqualTo(SetPatronTierResult.Success));
            await link.RefreshPatron(user);
            Assert.That(await link.SaveSponsorSettings(user, settings), Is.True);
        });
        await Pair.RunTicksSync(10);
        await Client.WaitAssertion(() => Assert.That(Client.ResolveDependency<LinkAccountManager>().SponsorSettings, Is.EqualTo(settings)));
        await Pair.CreateTestMap();
        EntityUid first = default;
        await Server.WaitAssertion(() =>
        {
            first = SEntMan.SpawnEntity("MobHuman", TestMap!.GridCoords);
            SEntMan.EnsureComponent<MarineComponent>(first);
            var inventory = Server.System<InventorySystem>();
            var tag = SEntMan.SpawnEntity("CMDogtagRifleman", TestMap.GridCoords);
            Assert.That(inventory.TryEquip(first, tag, "id", silent: true), Is.True);
            Server.System<CMUSponsorSystem>().ApplySpawnCosmetics(first, user);
            Assert.That(SEntMan.GetComponent<CMUSponsorEngravingComponent>(tag).Text, Is.EqualTo("Remember"));
            Assert.That(inventory.TryGetSlotEntity(first, "neck", out var cape), Is.True);
            var capeStyle = SEntMan.GetComponent<CMUSponsorCapeComponent>(cape!.Value);
            Assert.That(capeStyle.Trim, Is.EqualTo("gold"));
            Assert.That(capeStyle.Emblem, Is.EqualTo("branch"));
        });
        await RunServerAsync(async () =>
        {
            await db.UpsertPatronTier("SponsorTestColonist", 999999997UL, 7, true, false, false, false, false, false);
            await db.SetPatronTier(user.UserId, "SponsorTestColonist");
            await link.RefreshPatron(user);
            Assert.That(link.GetConnectedPatron(user)!.SponsorSettings, Is.EqualTo(settings));
        });
        await Server.WaitAssertion(() =>
        {
            var next = SEntMan.SpawnEntity("MobHuman", TestMap!.GridCoords);
            SEntMan.EnsureComponent<MarineComponent>(next);
            Server.System<CMUSponsorSystem>().ApplySpawnCosmetics(next, user);
            Assert.That(Server.System<InventorySystem>().TryGetSlotEntity(next, "neck", out _), Is.False);
            SEntMan.DeleteEntity(next);
            SEntMan.DeleteEntity(first);
        });
    }

    [Test]
    public async Task ApprovalChecksTheReviewedDraftAndLobbyMessagesDoNotRepeat()
    {
        var user = ServerSession!.UserId;
        var other = new NetUserId(Guid.NewGuid());
        var db = Server.ResolveDependency<IServerDbManager>();
        await RunServerAsync(async () =>
        {
            await db.UpsertPatronTier("SponsorTestModeration", 999999996UL, 3, true, true, true, true, true, false);
            await db.SetPatronTier(user.UserId, "SponsorTestModeration");
            await db.UpdatePlayerRecordAsync(other, "OtherSponsor", IPAddress.Loopback, null);
            await db.SetPatronTier(other.UserId, "SponsorTestModeration");
            var draft = JsonSerializer.Serialize(new CMUSponsorSettings(FigurineDescription: "Reviewed"));
            await db.SetCMUSponsorSettings(user.UserId, draft);
            Assert.That(await db.ApproveCMUSponsorFigurine(user.UserId, draft + " ", "Wrong"), Is.False);
            Assert.That(await db.ApproveCMUSponsorFigurine(user.UserId, draft, "Reviewed"), Is.True);
            await db.SetCMUSponsorSettings(user.UserId, JsonSerializer.Serialize(new CMUSponsorSettings(FigurineDescription: "Changed")));
            Assert.That(await db.ApproveCMUSponsorFigurine(user.UserId, draft, "Reviewed"), Is.False);
            Assert.That((await db.GetCMUSponsorPreferences(user.UserId))!.ApprovedFigurineDescription, Is.EqualTo("Reviewed"));

            await db.SetLobbyMessage(user.UserId, "First");
            Assert.That(await db.GetRandomLobbyMessage(), Is.Null);
            Assert.That(await db.ApproveCMUSponsorLobby(user.UserId, "Stale"), Is.False);
            Assert.That(await db.ApproveCMUSponsorLobby(user.UserId, "First"), Is.True);
            var first = await db.GetRandomLobbyMessage();
            Assert.That(first!.Value.Message, Is.EqualTo("First"));
            await db.SetLobbyMessage(other.UserId, "Second");
            Assert.That(await db.ApproveCMUSponsorLobby(other.UserId, "Second"), Is.True);
            Assert.That((await db.GetRandomLobbyMessage(first.Value.User))!.Value.User, Is.EqualTo("OtherSponsor"));
            Assert.That((await db.GetRandomLobbyMessage("OtherSponsor"))!.Value.User, Is.EqualTo(first.Value.User));

            await db.SetLobbyMessage(user.UserId, "Edited");
            Assert.That((await db.GetRandomLobbyMessage("OtherSponsor"))!.Value.Message, Is.EqualTo("Second"));
            await db.UpsertPatronTier("SponsorTestNoLobby", 999999995UL, 7, true, false, false, false, false, false);
            await db.SetPatronTier(other.UserId, "SponsorTestNoLobby");
            Assert.That(await db.GetRandomLobbyMessage(), Is.Null);
        });
    }

    [Test]
    public async Task WindowAndAllVisualChoicesInitialize()
    {
        await Client.WaitAssertion(() =>
        {
            var ui = Client.ResolveDependency<Robust.Client.UserInterface.IUserInterfaceManager>();
            var controller = ui.GetUIController<LinkAccountUIController>();
            controller.TogglePatronPerksWindow();
            controller.TogglePatronPerksWindow();
            var visuals = CEntMan.System<CMUSponsorVisualsSystem>();
            foreach (var state in CMUSponsorCatalog.Ghosts.Values)
            {
                var ghost = CEntMan.SpawnEntity("CMUSponsorGhostPreview", MapCoordinates.Nullspace);
                var comp = CEntMan.GetComponent<CMUSponsorGhostComponent>(ghost);
                comp.State = state;
                visuals.UpdateGhost((ghost, comp));
                Assert.That(CEntMan.HasComponent<SpriteComponent>(ghost), Is.True);
                CEntMan.DeleteEntity(ghost);
            }
            for (var i = 1; i <= 16; i++)
            {
                var cape = CEntMan.SpawnEntity($"CMUSponsorCape{i:D2}", MapCoordinates.Nullspace);
                var comp = CEntMan.GetComponent<CMUSponsorCapeComponent>(cape);
                foreach (var trim in CMUSponsorCatalog.Trims)
                foreach (var emblem in CMUSponsorCatalog.Patches.Keys)
                {
                    comp.Trim = trim;
                    comp.Emblem = emblem;
                    visuals.UpdateCape((cape, comp));
                }
                CEntMan.DeleteEntity(cape);
            }
        });
    }

    [Test]
    public async Task UnsubscribedClientCannotSavePerksOverTheNetwork()
    {
        var user = ServerSession!.UserId;
        var link = Client.ResolveDependency<LinkAccountManager>();
        await Client.WaitAssertion(() =>
        {
            Assert.That(link.Tier, Is.Null);
            CEntMan.System<CMUSponsorClientSystem>().Save(new CMUSponsorSettings("flame", "forged", "skull", "16", "gold"));
        });
        await Pair.RunTicksSync(30);
        await Client.WaitAssertion(() => Assert.That(link.SponsorSettings.Ghost, Is.Empty));
        var db = Server.ResolveDependency<IServerDbManager>();
        var saved = await db.GetCMUSponsorPreferences(user.UserId);
        Assert.That(saved, Is.Null);
    }
}

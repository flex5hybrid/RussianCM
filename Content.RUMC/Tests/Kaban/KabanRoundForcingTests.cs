using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Numerics;
using System.Threading.Tasks;
using Content.Client._RuCM.Kaban;
using Content.IntegrationTests.Fixtures;
using Content.Server._RuCM.Kaban;
using Content.Server.CMU14.Round;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Presets;
using Content.Server.Voting.Managers;
using Content.Shared._RuCM.Kaban;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Item;
using Content.Shared.CCVar;
using Content.Shared.Corvax.CCCVars;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests._RuCM.Kaban;

[TestFixture, NonParallelizable]
public sealed class KabanRoundForcingTests : GameTest
{
    private static int _ttsOverrideIndex;
    private HashSet<string> _loaded = new();
    public override PoolSettings PoolSettings => new() { InLobby = true, Fresh = true, Destructive = true };

    [OneTimeSetUp]
    public static void ConfigureLobby()
    {
        _ttsOverrideIndex = PoolManager.Instance.DefaultCvars.Count;
        PoolManager.Instance.DefaultCvars.Add((CCCVars.TTSEnabled.Name, "false"));
        PoolManager.Instance.DefaultCvars.Add((CCVars.VoteStartDelay.Name, "0"));
        PoolManager.Instance.DefaultCvars.Add((RMCCVars.RMCLobbyMinimumPlayers.Name, "0"));
    }

    [OneTimeTearDown]
    public static void RestoreLobby()
    {
        PoolManager.Instance.DefaultCvars.RemoveAt(_ttsOverrideIndex + 2);
        PoolManager.Instance.DefaultCvars.RemoveAt(_ttsOverrideIndex + 1);
        PoolManager.Instance.DefaultCvars.RemoveAt(_ttsOverrideIndex);
    }

    private async Task Until(Func<bool> predicate)
    {
        for (var i = 0; i < 400; i++)
        {
            var ready = false;
            await Server.WaitPost(() => ready = predicate());
            if (ready) return;
            await Pair.RunTicksSync(1);
            await Task.Delay(10);
        }
        Assert.Fail("The round-forcing continuation did not finish within its bounded test wait.");
    }

    private async Task<(KabanPolicy Policy, IServerDbManager Db, KabanForceChoice Choice, KabanRequest Purchase)> SetupPurchase(string product = "full")
    {
        TestContext.Progress.WriteLine("Forcing: configuring small real round maps");
        var changed = new Dictionary<Type, HashSet<string>>();
        Server.ProtoMan.LoadString(Prototypes, changed: changed);
        Guid actor = default;
        string name = "";
        await Server.WaitPost(() =>
        {
            Server.ProtoMan.ReloadPrototypes(changed);
            Server.System<GameTicker>().PauseStart();
            var session = Server.ResolveDependency<IPlayerManager>().Sessions.Single();
            actor = session.UserId;
            name = session.Name;
            Server.CfgMan.SetCVar(KabanCVars.Enabled, true);
            Server.CfgMan.SetCVar(KabanCVars.Shop, true);
            Server.CfgMan.SetCVar(KabanCVars.Forcing, true);
            Server.CfgMan.SetCVar(RMCCVars.RMCLobbyMinimumPlayers, 0);
            Server.CfgMan.SetCVar(CCVars.VoteStartDelay, 0);
            Server.System<GameTicker>().RestartLobbyCountdown();
            Server.System<GameTicker>().PauseStart();
            _loaded = (HashSet<string>) typeof(KabanSystem).GetField("_forceLoadedMaps", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(Server.System<KabanSystem>())!;
        });
        await Until(() => (bool) typeof(KabanSystem).GetField("_ready", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(Server.System<KabanSystem>())!);
        KabanForceOption[] catalog = [];
        await Server.WaitAssertion(() =>
        {
            var resources = Server.ResolveDependency<Robust.Shared.ContentPack.IResourceManager>();
            Assert.That(resources.ContentFileExists(new Robust.Shared.Utility.ResPath("/Maps/Test/empty.yml")), Is.True);
            Assert.That(Server.System<GameTicker>().CanUpdateMap(), Is.True,
                "The fixture must reopen the selection window after removing the minimum-player gate.");
            catalog = Server.System<AuRoundSystem>().KabanCatalog(fresh: true);
        });
        var choice = product switch
        {
            "mode" => new KabanForceChoice(Mode: "KabanTestMode"),
            "map" => new KabanForceChoice(Planet: "KabanTestPlanet"),
            "govfor" => new KabanForceChoice(Govfor: "KabanTestGov"),
            "opfor" => new KabanForceChoice(Opfor: "KabanTestOp"),
            _ => new KabanForceChoice("KabanTestMode", "KabanTestPlanet", "KabanTestGov", "KabanTestOp"),
        };
        Assert.That(catalog.Any(choice.Matches), Is.True, "All fixture resources must pass the production compatibility check.");
        var policy = new KabanPolicy(actor, true, true, true, true, false, false, "RussianCM", "test", true, [], [],
            Forcing: true, ForceOptions: catalog);
        var db = Server.ResolveDependency<IServerDbManager>();
        await db.KabanExecute(policy, new() { Action = KabanAction.CorrectBalance, TargetName = name, Amount = 2000, Confirmed = true }, DateTime.UtcNow);
        var price = product switch { "mode" => 350, "map" => 250, "govfor" or "opfor" => 200, _ => 800 };
        var purchase = new KabanRequest { Action = KabanAction.Purchase, Product = product, Amount = price,
            Text = choice.Encode(), Confirmed = true };
        await db.KabanExecute(policy, purchase, DateTime.UtcNow);
        return (policy, db, choice, purchase);
    }

    [Test]
    public async Task CompletePurchaseAppliesFourSelectionsLoadsAllThreeMapsAndConfirmsAfterInRound()
    {
        var (policy, db, choice, purchase) = await SetupPurchase();
        await Server.WaitAssertion(() =>
        {
            var round = Server.System<AuRoundSystem>();
            var production = round.BuildKabanCatalog(100).Where(o => o.Mode == "ForceOnForce");
            Assert.That(production.Any(o => o.Govfor.Keys.Any(g => o.Opfor.Keys.Any(p => p != g))), Is.True,
                "At a simulated population of 100, current production prototypes/resources must offer a complete FoF plan.");
            Assert.That(round.KabanCatalog(fresh: true).Any(o => o.Mode == "ForceOnForce"), Is.False,
                "The real one-player population must still enforce FoF's minimum.");
        });
        await Client.WaitAssertion(() =>
        {
            KabanRequest? submitted = null;
            using var window = new KabanWindow(request => submitted = request);
            window.Update(new KabanState { Viewer = policy.Actor, Balance = 2000, ForcingEnabled = true,
                Products = [new("full", 800, true, "")], ForceOptions = policy.ForceOptions!.ToList() });
            window.ShowPreviewPage("force-checkout");
            foreach (var field in new[] { "mode", "map", "govfor", "opfor" })
            {
                var select = Descendants(window).OfType<OptionButton>().Single(c => c.Name == "ForceSelect-" + field);
                Assert.That(select.Disabled, Is.False);
                var handler = (Action<OptionButton.ItemSelectedEventArgs>) typeof(OptionButton)
                    .GetField("OnItemSelected", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(select)!;
                handler(new(1, select));
            }
            foreach (var width in new[] { 820, 1100 })
            {
                window.Measure(new Vector2(width, 760));
                window.Arrange(new UIBox2(Vector2.Zero, new Vector2(width, 760)));
                foreach (var select in Descendants(window).OfType<OptionButton>())
                    Assert.That(select.DesiredSize.X, Is.LessThanOrEqualTo(540));
            }
            Click(Descendants(window).OfType<CheckBox>().Single(c => c.Name == "PurchaseConfirm"));
            var button = Descendants(window).OfType<Button>().Single(c => c.Name == "PurchaseSubmit");
            Assert.That(button.Disabled, Is.False);
            Click(button);
            Assert.That(submitted, Is.Not.Null);
            Assert.That(submitted!.Text, Is.EqualTo(choice.Encode()));
            Assert.That(submitted.Amount, Is.EqualTo(800));
            Assert.That(submitted.Confirmed, Is.True);
        });
        TestContext.Progress.WriteLine("Forcing: selecting the durable FIFO head");
        await Server.WaitPost(() => Server.System<AuRoundSystem>().StartVoteSequence());
        await Until(() => Server.System<AuRoundSystem>().KabanSelectionMatches(choice));
        var applying = await db.KabanView(policy);
        Assert.That(applying.Items.Single(i => i.Id == purchase.Operation).Status, Is.EqualTo("applying"));
        await Server.WaitAssertion(() =>
        {
            var round = Server.System<AuRoundSystem>();
            Assert.That(round.GetSelectedGovforShip(), Is.EqualTo("KabanTestGovShip"));
            Assert.That(round.GetSelectedOpforShip(), Is.EqualTo("KabanTestOpShip"));
            Assert.That(Server.System<GameTicker>().RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));
        });
        TestContext.Progress.WriteLine("Forcing: starting GameTicker and loading planet plus both ships");
        await Server.WaitPost(() =>
        {
            // Exercise the preload commit boundary: disabling future forcing must not mutate already loaded maps/rules.
            typeof(GameTicker).GetMethod("LoadMaps", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(Server.System<GameTicker>(), null);
            Server.CfgMan.SetCVar(KabanCVars.Forcing, false);
        });
        await Pair.RunTicksSync(2);
        await Server.WaitAssertion(() => Assert.That(Server.System<AuRoundSystem>().KabanSelectionMatches(choice), Is.True));
        await Server.WaitPost(() => Server.System<GameTicker>().StartRound(true));
        await Until(() => Server.System<GameTicker>().RunLevel == GameRunLevel.InRound);
        await Server.WaitAssertion(() =>
        {
            Assert.That(Server.System<GameTicker>().CurrentPreset!.ID, Is.EqualTo(choice.Mode));
            Assert.That(Server.System<AuRoundSystem>().KabanSelectionMatches(choice), Is.True);
            Assert.That(_loaded, Does.Contain("KabanTestMap").And.Contain("KabanTestGovShip").And.Contain("KabanTestOpShip"));
            var nextVote = new Dictionary<string, string> { [choice.Mode] = "paid", ["DistressSignal"] = "ordinary" };
            Server.System<AuRoundSystem>().FilterKabanPresets(nextVote);
            Assert.That(nextVote, Has.Count.EqualTo(2), "A fulfilled request must not constrain votes for the following round.");
        });
        KabanState final = applying;
        for (var i = 0; i < 200 && final.Items.Single(v => v.Id == purchase.Operation).Status != "completed"; i++)
        {
            await Pair.RunTicksSync(1);
            final = await db.KabanView(policy);
        }
        Assert.That(final.Items.Single(v => v.Id == purchase.Operation).Status, Is.EqualTo("completed"));
        Assert.That(final.Balance, Is.EqualTo(1200));
        Assert.That(final.Earned, Is.EqualTo(2000));
        Assert.That(final.History.Count(h => h.Description == "purchase:full"), Is.EqualTo(1));
    }

    [TestCase("KabanTestMap"), TestCase("KabanTestGovShip"), TestCase("KabanTestOpShip")]
    public async Task MissingResourceBeforePreloadReturnsEntirePlanAndRefunds(string map)
    {
        var (policy, db, choice, purchase) = await SetupPurchase();
        await Server.WaitPost(() => Server.System<AuRoundSystem>().StartVoteSequence());
        await Until(() => Server.System<AuRoundSystem>().KabanSelectionMatches(choice));
        var changed = new Dictionary<Type, HashSet<string>>();
        Server.ProtoMan.LoadString($"- type: gameMap\n  id: {map}\n  mapName: Missing fixture resource\n  mapPath: /Maps/Test/kaban-file-does-not-exist.yml\n  stations: {{}}\n  minPlayers: 0\n",
            overwrite: true, changed: changed);
        await Server.WaitAssertion(() =>
        {
            Server.ProtoMan.ReloadPrototypes(changed);
            Server.System<AuRoundSystem>().ValidateKabanBeforeLoad();
            Assert.That(Server.System<AuRoundSystem>().GetSelectedPlanet(), Is.Null);
            Assert.That(Server.System<PlatoonSpawnRuleSystem>().SelectedGovforPlatoon, Is.Null);
            Assert.That(Server.System<PlatoonSpawnRuleSystem>().SelectedOpforPlatoon, Is.Null);
            Assert.That(Server.System<AuRoundSystem>().SelectedPreset?.ID, Is.Not.EqualTo(choice.Mode));
            Assert.That(_loaded, Is.Empty, "No partial plan may load before all resources pass validation.");
        });
        KabanState final = await db.KabanView(policy);
        for (var i = 0; i < 200 && final.Items.Single(v => v.Id == purchase.Operation).Status != "refunded"; i++)
        {
            await Pair.RunTicksSync(1);
            final = await db.KabanView(policy);
        }
        Assert.That(final.Items.Single(v => v.Id == purchase.Operation).Status, Is.EqualTo("refunded"));
        Assert.That(final.Balance, Is.EqualTo(2000));
        Assert.That(final.Earned, Is.EqualTo(2000));
    }

    [Test]
    public async Task AdminOverrideRefundsModeAndInvalidatesTheOldPlanetTimer()
    {
        var (policy, db, choice, purchase) = await SetupPurchase("mode");
        await Server.WaitPost(() => Server.System<GameTicker>().RestartRound());
        await Until(() => Server.System<AuRoundSystem>().SelectedPreset?.ID == choice.Mode);
        string adminPlanet = "";
        await Server.WaitPost(() =>
        {
            // Exercise the administrative lock and the same stop/set path used by round commands.
            Server.System<GameTicker>().PauseStart();
            Server.System<CMUPresetVoteSystem>().LockPresets(["DistressSignal"]);
            var round = Server.System<AuRoundSystem>();
            round.StopVoteSequence();
            Server.System<GameTicker>().SetGamePreset("DistressSignal");
            adminPlanet = policy.ForceOptions!.First(o => o.Mode == "DistressSignal").Planet;
            Assert.That(round.SetPlanet(adminPlanet), Is.True);
        });
        await Pair.RunTicksSync(35 * 60);
        await Server.WaitAssertion(() =>
        {
            var round = Server.System<AuRoundSystem>();
            Assert.That(round.SelectedPreset!.ID, Is.EqualTo("DistressSignal"));
            Assert.That(round.GetSelectedPlanetId(), Is.EqualTo(adminPlanet));
            Assert.That(Server.ResolveDependency<IVoteManager>().ActiveVotes, Is.Empty,
                "The cancelled paid sequence's 32-second callback must not create another vote.");
        });
        KabanState final = await db.KabanView(policy);
        for (var i = 0; i < 200 && final.Items.Single(v => v.Id == purchase.Operation).Status != "refunded"; i++)
        {
            await Pair.RunTicksSync(1);
            final = await db.KabanView(policy);
        }
        Assert.That(final.Items.Single(v => v.Id == purchase.Operation).Status, Is.EqualTo("refunded"));
        Assert.That(final.Balance, Is.EqualTo(2000));
    }

    [Test]
    public async Task CancellingFullPlanClearsEverySelectionBeforeManualOverrides()
    {
        var (policy, db, choice, purchase) = await SetupPurchase();
        var previousCamouflage = CamouflageType.Jungle;
        await Server.WaitPost(() => previousCamouflage = Server.System<ItemCamouflageSystem>().CurrentMapCamouflage);
        await Server.WaitPost(() => Server.System<AuRoundSystem>().StartVoteSequence());
        await Until(() => Server.System<AuRoundSystem>().KabanSelectionMatches(choice));
        await Server.WaitAssertion(() =>
        {
            var round = Server.System<AuRoundSystem>();
            Assert.That(Server.System<ItemCamouflageSystem>().CurrentMapCamouflage, Is.EqualTo(CamouflageType.Desert));
            round.KabanAdminMapOverride();
            Assert.That(Server.System<ItemCamouflageSystem>().CurrentMapCamouflage, Is.EqualTo(previousCamouflage));
            Assert.That(round.GetSelectedPlanet(), Is.Null);
            Assert.That(round.GetSelectedGovforShip(), Is.Null);
            Assert.That(round.GetSelectedOpforShip(), Is.Null);
            Assert.That(Server.System<PlatoonSpawnRuleSystem>().SelectedGovforPlatoon, Is.Null);
            Assert.That(Server.System<PlatoonSpawnRuleSystem>().SelectedOpforPlatoon, Is.Null);
            Assert.That(Server.System<GameTicker>().Preset?.ID, Is.Not.EqualTo(choice.Mode));
            // The administrative value is applied after cancellation and must survive it.
            round.SetGovforShip("manual-test-ship");
            Assert.That(round.GetSelectedGovforShip(), Is.EqualTo("manual-test-ship"));
        });
        KabanState final = await db.KabanView(policy);
        for (var i = 0; i < 200 && final.Items.Single(v => v.Id == purchase.Operation).Status != "refunded"; i++)
        {
            await Pair.RunTicksSync(1);
            final = await db.KabanView(policy);
        }
        Assert.That(final.Items.Single(v => v.Id == purchase.Operation).Status, Is.EqualTo("refunded"));
        Assert.That(final.Balance, Is.EqualTo(2000));
    }

    [TestCase("mode"), TestCase("map"), TestCase("govfor"), TestCase("opfor")]
    public async Task SingleParameterKeepsUnpaidVotesAndStartsTheChosenRound(string product)
    {
        var (policy, db, choice, purchase) = await SetupPurchase(product);
        var complete = new KabanForceChoice("KabanTestMode", "KabanTestPlanet", "KabanTestGov", "KabanTestOp");
        var observedUnpaidVote = false;
        var selected = false;
        await Server.WaitPost(() => Server.System<AuRoundSystem>().StartVoteSequence());
        for (var i = 0; i < 180 && !selected; i++)
        {
            await Pair.RunTicksSync(60);
            await Server.WaitPost(() =>
            {
                var votes = Server.ResolveDependency<IVoteManager>();
                var active = votes.ActiveVotes.Where(v => !v.Finished).ToArray();
                observedUnpaidVote |= active.Length > 0;
                foreach (var vote in active)
                {
                    vote.CastVote(ServerSession!, 0);
                    // Votes use RealTime, not simulated ticks. Finish through the actual manager after casting a ballot.
                    var registration = vote.GetType().GetField("_reg", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vote);
                    typeof(VoteManager).GetMethod("EndVote", BindingFlags.Instance | BindingFlags.NonPublic)!
                        .Invoke(votes, new[] { registration });
                }
                votes.Update();
                selected = Server.System<AuRoundSystem>().KabanSelectionMatches(complete);
            });
            await Task.Delay(10);
        }
        Assert.That(observedUnpaidVote, Is.True, "A single parameter must not silently force the unpaid selections.");
        Assert.That(selected, Is.True, "Compatible votes must finish with all fixture selections before starting.");
        Assert.That((await db.KabanView(policy)).Items.Single(i => i.Id == purchase.Operation).Status, Is.EqualTo("applying"));
        await Server.WaitPost(() => Server.System<GameTicker>().StartRound(true));
        await Until(() => Server.System<GameTicker>().RunLevel == GameRunLevel.InRound);
        KabanState final = await db.KabanView(policy);
        for (var i = 0; i < 200 && final.Items.Single(v => v.Id == purchase.Operation).Status != "completed"; i++)
        {
            await Pair.RunTicksSync(1);
            final = await db.KabanView(policy);
        }
        Assert.That(final.Items.Single(v => v.Id == purchase.Operation).Status, Is.EqualTo("completed"));
        Assert.That(final.Balance, Is.EqualTo(2000 - purchase.Amount));
    }

    private const string Prototypes = """
        - type: gamePreset
          id: KabanTestMode
          name: cmu-kaban-force-field-mode
          showInVote: true
          requiresGovforVote: true
          requiresOpforVote: true
          supportedPlanets: [KabanTestPlanet]
          rules: []
        - type: gameMap
          id: KabanTestMap
          mapName: Kaban test planet
          minPlayers: 0
          mapPath: /Maps/Test/empty.yml
          stations:
            Empty:
              stationProto: StandardNanotrasenStation
              components:
              - type: StationJobs
                availableJobs:
                  Passenger: [-1, -1]
                  CMRifleman: [-1, -1]
        - type: gameMap
          id: KabanTestGovShip
          mapName: Kaban test GOVFOR ship
          minPlayers: 0
          mapPath: /Maps/Test/empty.yml
          stations:
            Empty:
              stationProto: StandardNanotrasenStation
              components:
              - type: StationJobs
                availableJobs:
                  Passenger: [-1, -1]
                  CMRifleman: [-1, -1]
        - type: gameMap
          id: KabanTestOpShip
          mapName: Kaban test OPFOR ship
          minPlayers: 0
          mapPath: /Maps/Test/empty.yml
          stations:
            Empty:
              stationProto: StandardNanotrasenStation
              components:
              - type: StationJobs
                availableJobs:
                  Passenger: [-1, -1]
                  CMRifleman: [-1, -1]
        - type: platoon
          id: KabanTestGov
          name: cmu-kaban-force-field-govfor
          possibleships: [KabanTestGovShip]
        - type: platoon
          id: KabanTestOp
          name: cmu-kaban-force-field-opfor
          possibleships: [KabanTestOpShip]
        - type: entity
          id: KabanTestPlanet
          components:
          - type: RMCPlanetMapPrototype
            mapId: KabanTestMap
            votename: Kaban test planet
            camouflage: Desert
            govforinship: true
            opforinship: true
            platoonsGovfor: [KabanTestGov]
            platoonsOpfor: [KabanTestOp]
        """;

    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (var child in parent.Children)
        {
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    private static void Click(BaseButton button)
    {
        var pointer = new ScreenCoordinates(button.GlobalPixelPosition + new Vector2(2, 2), default);
        var down = new GUIBoundKeyEventArgs(EngineKeyFunctions.UIClick, BoundKeyState.Down, pointer, true, new(2, 2), new(2, 2));
        var up = new GUIBoundKeyEventArgs(EngineKeyFunctions.UIClick, BoundKeyState.Up, pointer, true, new(2, 2), new(2, 2));
        typeof(BaseButton).GetMethod("KeyBindDown", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(button, new object[] { down });
        typeof(BaseButton).GetMethod("KeyBindUp", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(button, new object[] { up });
    }

}

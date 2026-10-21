using System;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._RuCM.Qualifications;
using Content.Server.CMU14.Round;
using Content.Server.GameTicking.Events;
using Content.Server.GameTicking.Presets;
using Content.Shared._RuCM.Qualifications;
using Content.Shared.CCVar;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests._RuCM.Qualifications;

public sealed class QualificationInsurgencyTests : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Destructive = true };
    [Test]
    public async Task InsurgencyReplacesOnlyTimersAndOtherModesRestoreOriginalRequirements()
    {
        const string jobId = "AU14JobGOVFORSectionSergeant"; // CMU14: instructors have a mandatory gate in all modes.
        await Server.WaitPost(() =>
        {
            Server.ResolveDependency<IConfigurationManager>().SetCVar(QualificationCVars.Enabled, false);
            var seed = new QualificationStore();
            seed.Definitions["sergeant"] = new() { Id = "sergeant" };
            seed.Roles[jobId] = new() { JobId = jobId, MinimumLevel = MilitaryLevel.Sergeant };
            Server.System<QualificationSystem>().ConfigureRepository(new MemoryQualificationRepository(), seed);
        });
        await Pair.RunTicksSync(5);
        await Server.WaitAssertion(() =>
        {
            var system = Server.System<QualificationSystem>(); var roles = Server.System<SharedRoleSystem>();
            var cfg = Server.ResolveDependency<IConfigurationManager>(); var prototypes = Server.ResolveDependency<IPrototypeManager>();
            var round = Server.System<AuRoundSystem>(); var job = prototypes.Index<JobPrototype>(jobId);
            var original = roles.GetRoleRequirements(job).ToArray();
            Assert.That(original.Any(r => r is OverallPlaytimeRequirement), Is.True);
            Assert.That(original.OfType<OverallPlaytimeRequirement>().Single().Check(Server.EntMan, prototypes,
                HumanoidCharacterProfile.DefaultWithSpecies(), new System.Collections.Generic.Dictionary<string, TimeSpan>(), out _), Is.False);
            Assert.That(prototypes.Index<JobPrototype>("AU14JobGOVFORadvisorRMC").LocalizedName, Is.EqualTo(Server.ResolveDependency<Robust.Shared.Localization.ILocalizationManager>().GetString("rucm-qualifications-drill-instructor-name")));
            Assert.That(prototypes.Index<JobPrototype>("AU14JobGOVFORadvisorUPP").LocalizedName, Is.EqualTo(Server.ResolveDependency<Robust.Shared.Localization.ILocalizationManager>().GetString("rucm-qualifications-drill-instructor-name")));
            cfg.SetCVar(QualificationCVars.Enabled, true); cfg.SetCVar(QualificationCVars.Enforce, true);
            round.SetPreset(prototypes.Index<GamePresetPrototype>("Insurgency")); system.SynchronizeRolePolicy();
            var insurgency = roles.GetRoleRequirements(job);
            Assert.That(insurgency.Any(r => r is OverallPlaytimeRequirement or RoleTimeRequirement or DepartmentTimeRequirement), Is.False);
            foreach (var requirement in original.Where(r => r is not OverallPlaytimeRequirement and not RoleTimeRequirement and not DepartmentTimeRequirement))
                Assert.That(insurgency, Does.Contain(requirement), "Preserve age, traits and other restrictions");
            Assert.That(system.Mode, Is.EqualTo(QualificationMode.Enforce));
            Assert.That(system.CanTakeJob(ServerSession.UserId, jobId), Is.False);
            Assert.That(prototypes.Index<JobPrototype>("AU14JobGOVFORadvisor").LocalizedName,
                Is.EqualTo(Server.ResolveDependency<Robust.Shared.Localization.ILocalizationManager>().GetString("rucm-qualifications-drill-instructor-name")));
            foreach (var preset in new[] { "ForceOnForce", "DistressSignal", "Insurgency" })
            {
                round.SetPreset(prototypes.Index<GamePresetPrototype>(preset)); system.SynchronizeRolePolicy();
                if (preset == "Insurgency") continue;
                Assert.That(system.Mode, Is.EqualTo(QualificationMode.Disabled));
                Assert.That(system.CanTakeJob(ServerSession.UserId, jobId), Is.False,
                    "Instructor certification remains mandatory outside Insurgency");
                Assert.That(roles.GetRoleRequirements(job), Is.EquivalentTo(original));
                var instructorRequirements = roles.GetRoleRequirements(prototypes.Index<JobPrototype>("AU14JobGOVFORadvisor"));
                Assert.That(instructorRequirements.OfType<QualificationAccessRequirement>().Count(), Is.EqualTo(1));
                Assert.That(system.CanTakeJob(ServerSession.UserId, "AU14JobGOVFORadvisor"), Is.False);
            }
            cfg.SetCVar(QualificationCVars.Enabled, false); system.SynchronizeRolePolicy();
            Assert.That(roles.GetRoleRequirements(job), Is.EquivalentTo(original));
        });
    }
    [Test]
    public async Task LobbyAndEscapeButtonsComposePublicPanelsWithoutDuplicatesOrPrivilegeLeak()
    {
        await Client.WaitAssertion(() =>
        {
            using var lobby = new Content.Client.Lobby.UI.LobbyGui();
            var first = Content.Client._RuCM.Qualifications.QualificationEntrySystem.AttachLobby(lobby, () => { });
            var again = Content.Client._RuCM.Qualifications.QualificationEntrySystem.AttachLobby(lobby, () => { });
            Assert.That(again, Is.SameAs(first)); Assert.That(first.Visible, Is.True);
            Assert.That(first.ToolTip, Is.Not.Empty);
            using var escape = new Content.Client.Options.UI.EscapeMenu();
            var button = Content.Client._RuCM.Qualifications.QualificationEntrySystem.AttachEscape(escape, false, () => { });
            Assert.That(button.Visible, Is.False);
            var staff = Content.Client._RuCM.Qualifications.QualificationEntrySystem.AttachEscape(escape, true, () => { });
            Assert.That(staff, Is.SameAs(button)); Assert.That(button.Visible, Is.True); Assert.That(button.ToolTip, Is.Not.Empty);
            Assert.That(button.GetPositionInParent(), Is.EqualTo(escape.AdminRemarksButton.GetPositionInParent() + 1));
            Assert.That(button.Parent, Is.SameAs(escape.RulesButton.Parent));
        });
    }

    [Test]
    public async Task PrivatePolicyWirePreservesNonTimerRequirementsAndCarriesOnlyOwnEligibility()
    {
        byte[] bytes = null;
        await Server.WaitAssertion(() =>
        {
            var policy = new QualificationPolicyState { Active = true, Staff = false };
            const string job = "AU14JobGOVFORadvisor";
            policy.Jobs.Add(job); policy.Eligibility[job] = false;
            Server.System<QualificationSystem>().SynchronizeRolePolicy();
            policy.BaseRequirements[job] = new(Server.System<QualificationRolePolicy>().BaseRequirements[job]);
            using var stream = new System.IO.MemoryStream();
            Server.ResolveDependency<Robust.Shared.Serialization.IRobustSerializer>().Serialize(stream, policy); bytes = stream.ToArray();
        });
        await Client.WaitAssertion(() =>
        {
            using var stream = new System.IO.MemoryStream(bytes);
            var policy = Client.ResolveDependency<Robust.Shared.Serialization.IRobustSerializer>().Deserialize<QualificationPolicyState>(stream);
            Assert.That(policy.Active, Is.True); Assert.That(policy.Staff, Is.False);
            Assert.That(policy.Eligibility.Single().Value, Is.False);
            Assert.That(policy.BaseRequirements.Single().Value.Any(r => r is OverallPlaytimeRequirement), Is.True);
            Assert.That(policy.BaseRequirements.Single().Value.Any(r => r is AgeRequirement), Is.True);
            var clientPolicy = Client.EntMan.System<QualificationRolePolicy>();
            clientPolicy.Eligibility.Clear(); foreach (var (job, allowed) in policy.Eligibility) clientPolicy.Eligibility[job] = allowed;
            clientPolicy.Apply(true, policy.Jobs, true, policy.Baseline, policy.BaseRequirements);
            var requirement = new QualificationAccessRequirement { Job = policy.Jobs.Single() };
            Assert.That(requirement.Check(Client.EntMan, Client.ResolveDependency<IPrototypeManager>(),
                HumanoidCharacterProfile.DefaultWithSpecies(), new System.Collections.Generic.Dictionary<string, TimeSpan>(), out var reason), Is.False);
            Assert.That(reason.IsEmpty, Is.False);
            var owned = Client.ResolveDependency<IPrototypeManager>().Index<JobRequirementOverridePrototype>(QualificationRolePolicy.OverrideId);
            Assert.That(owned.Jobs[policy.Jobs.Single()].Any(r => r is AgeRequirement), Is.True);
            Assert.That(owned.Jobs[policy.Jobs.Single()].Any(r => r is OverallPlaytimeRequirement), Is.False);
        });
    }

    [Test]
    public async Task ActualScreenEventsAddButtonsAndLobbyButtonOpensRecordOverNetwork()
    {
        await Server.WaitPost(() =>
        {
            var seed = new QualificationStore(); seed.Management.Add(ServerSession.UserId);
            Server.System<QualificationSystem>().ConfigureRepository(new MemoryQualificationRepository(), seed);
        });
        await Pair.RunTicksSync(10); await RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            var ui = Client.ResolveDependency<Robust.Client.UserInterface.IUserInterfaceManager>();
            ui.LoadScreen<Content.Client.Lobby.UI.LobbyGui>();
            var lobby = (Content.Client.Lobby.UI.LobbyGui) ui.ActiveScreen;
            var button = Content.Client._RuCM.Qualifications.QualificationEntrySystem.Descendants(lobby)
                .OfType<Robust.Client.UserInterface.Controls.Button>().Single(b => b.Name == "RuCMQualificationLobby");
            var size = new System.Numerics.Vector2(1280, 900);
            foreach (var control in Content.Client._RuCM.Qualifications.QualificationEntrySystem.Descendants(ui.RootControl).Prepend(ui.RootControl))
            { control.InvalidateMeasure(); control.InvalidateArrange(); }
            ui.RootControl.Measure(size); ui.RootControl.Arrange(new Robust.Shared.Maths.UIBox2(System.Numerics.Vector2.Zero, size));
            Assert.That(button.VisibleInTree, Is.True);
            QualificationUiTests.Click(button);
        });
        await RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            var ui = Client.ResolveDependency<Robust.Client.UserInterface.IUserInterfaceManager>();
            var window = Content.Client._RuCM.Qualifications.QualificationEntrySystem.Descendants(ui.WindowRoot)
                .OfType<Content.Client._RuCM.Qualifications.QualificationWindow>().Single();
            window.Close();
            using var escape = new Content.Client.Options.UI.EscapeMenu(); escape.OpenCentered();
            var button = Content.Client._RuCM.Qualifications.QualificationEntrySystem.Descendants(escape)
                .OfType<Robust.Client.UserInterface.Controls.Button>().Single(b => b.Name == "RuCMQualificationEscape");
            Assert.That(button.Text, Is.Not.Empty); Assert.That(button.ToolTip, Is.Not.Empty);
        });
        await RunUntilSynced();
    }
}

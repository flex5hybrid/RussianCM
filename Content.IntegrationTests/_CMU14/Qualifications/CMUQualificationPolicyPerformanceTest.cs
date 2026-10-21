using System.Diagnostics;
using System.Reflection;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server._RuCM.Qualifications;
using Content.Server.Administration.Managers;
using Content.Server.CMU14.Round;
using Content.Server.GameTicking.Presets;
using Content.Shared._RuCM.Qualifications;
using Content.Shared.CCVar;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.CMU14.Qualifications;

[TestFixture]
public sealed class CMUQualificationPolicyPerformanceTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Destructive = true };

    [Test]
    public async Task UnchangedPoliciesAvoidRebuildingEveryJobAndChangedQualificationsReachTheClient()
    {
        const string jobId = "AU14JobGOVFORSquadRifleman";
        await Server.WaitPost(() =>
        {
            var admins = Server.ResolveDependency<IAdminManager>();
            if (admins.GetAdminData(ServerSession) != null) admins.DeAdmin(ServerSession);
            var seed = new QualificationStore();
            seed.Definitions["enlisted"] = new() { Id = "enlisted" };
            foreach (var job in SProtoMan.EnumeratePrototypes<JobPrototype>())
                seed.Roles[job.ID] = new() { JobId = job.ID, MinimumLevel = MilitaryLevel.Enlisted };
            SEntMan.System<QualificationSystem>().ConfigureRepository(new MemoryQualificationRepository(), seed);
        });
        await Pair.RunTicksSync(5);
        await Server.WaitPost(() =>
        {
            Server.ResolveDependency<IConfigurationManager>().SetCVar(QualificationCVars.Enforce, true);
            SEntMan.System<AuRoundSystem>().SetPreset(SProtoMan.Index<GamePresetPrototype>("Insurgency"));
            SEntMan.System<QualificationSystem>().Update(1);
        });
        await RunUntilSynced();
        await Client.WaitAssertion(() => Assert.That(CEntMan.System<QualificationRolePolicy>().Eligibility[jobId], Is.False));

        await Server.WaitAssertion(() =>
        {
            var system = SEntMan.System<QualificationSystem>();
            var send = typeof(QualificationSystem).GetMethod("SendPolicy", BindingFlags.Instance | BindingFlags.NonPublic)!
                .CreateDelegate<Action<ICommonSession, bool>>(system);
            for (var i = 0; i < 5; i++) send(ServerSession, false);
            var timer = new Stopwatch();
            var before = GC.GetAllocatedBytesForCurrentThread();
            timer.Start();
            // Equivalent to 500 connected players whose store, mode and staff access haven't changed.
            for (var i = 0; i < 500; i++) send(ServerSession, false);
            timer.Stop();
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestContext.Progress.WriteLine($"500 unchanged policy checks: {allocated:N0} bytes, {timer.Elapsed.TotalMilliseconds:F3} ms.");
            Assert.That(allocated, Is.LessThan(512 * 1024));
            before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1000; i++) system.SynchronizeRolePolicy();
            allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            TestContext.Progress.WriteLine($"1000 role policy synchronizations: {allocated:N0} bytes.");
            Assert.That(allocated, Is.LessThan(128 * 1024));
        });

        Task mutation = null;
        await Server.WaitPost(() =>
        {
            var system = SEntMan.System<QualificationSystem>();
            var context = new TrainingContext(Guid.NewGuid(), "admin", "", 1, "test", DateTimeOffset.UtcNow);
            mutation = system.Service.Apply(new(context, true, false, false, false, false), QualificationAction.Grant,
                new() { Target = ServerSession.UserId, Qualification = "enlisted", Reason = "test" });
        });
        await mutation;
        await Server.WaitPost(() => SEntMan.System<QualificationSystem>().Update(1));
        await RunUntilSynced();
        await Client.WaitAssertion(() => Assert.That(CEntMan.System<QualificationRolePolicy>().Eligibility[jobId], Is.True));

        await Server.WaitPost(() => Server.ResolveDependency<IConfigurationManager>().SetCVar(QualificationCVars.Enabled, false));
        await Server.WaitPost(() => SEntMan.System<QualificationSystem>().Update(1));
        await RunUntilSynced();
        await Server.WaitAssertion(() =>
        {
            var overrides = SProtoMan.Index<JobRequirementOverridePrototype>(QualificationRolePolicy.OverrideId);
            Assert.That(overrides.Jobs.ContainsKey(jobId), Is.False, "General gates are disabled");
            Assert.That(overrides.Jobs["AU14JobGOVFORadvisor"].OfType<QualificationAccessRequirement>().Count(), Is.EqualTo(1),
                "Instructor accreditation remains mandatory");
        });
    }
}

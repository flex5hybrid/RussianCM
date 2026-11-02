using Content.IntegrationTests.Fixtures;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Marines.Squads.Specialization;
using Content.Shared._RMC14.Vendors;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.CMU14.Squads;

[TestFixture]
public sealed class SquadSpecializationTest : GameTest
{
    private static readonly ProtoId<SquadSpecializationPrototype> SpecVDV = "UPPSpecVDV";
    private static readonly ProtoId<SquadSpecializationPrototype> SpecSOF = "UPPSpecSOF";

    [Test]
    public async Task TestSquadSpecializationFlowAndVendorFiltering()
    {
        var map = await Pair.CreateTestMap();
        EntityUid marine = default;
        EntityUid squad = default;

        await Server.WaitAssertion(() =>
        {
            marine = SEntMan.SpawnEntity("CMMobHuman", map.GridCoords);
            squad = SEntMan.SpawnEntity("SquadOpfor", map.GridCoords);

            Server.System<SquadSystem>().AssignSquad(marine, squad, null);

            var specSys = Server.System<SharedSquadSpecializationSystem>();
            var vendorSys = Server.System<SharedCMAutomatedVendorSystem>();

            // 1. Before specialization is chosen
            Assert.That(specSys.TryGetSquadSpecialization(marine, out _), Is.False);

            var vdvList = new List<ProtoId<SquadSpecializationPrototype>> { SpecVDV };
            var sofList = new List<ProtoId<SquadSpecializationPrototype>> { SpecSOF };
            var emptyList = new List<ProtoId<SquadSpecializationPrototype>>();

            // Standard item (empty requirement) is allowed
            Assert.That(vendorSys.IsSpecializationAllowed(marine, emptyList, emptyList), Is.True);
            // Specialized items are not allowed yet
            Assert.That(vendorSys.IsSpecializationAllowed(marine, vdvList, emptyList), Is.False);
            Assert.That(vendorSys.IsSpecializationAllowed(marine, sofList, emptyList), Is.False);

            // 2. Choose VDV specialization
            var squadSpec = SEntMan.EnsureComponent<SquadSpecializationComponent>(squad);
            squadSpec.Specialization = SpecVDV;
            squadSpec.Locked = true;

            // 3. Verify marine now has VDV specialization
            Assert.That(specSys.TryGetSquadSpecialization(marine, out var activeSpec), Is.True);
            Assert.That(activeSpec, Is.EqualTo(SpecVDV));

            // 4. Vendor filtering with VDV
            Assert.That(vendorSys.IsSpecializationAllowed(marine, vdvList, emptyList), Is.True, "VDV items should be allowed for VDV squad");
            Assert.That(vendorSys.IsSpecializationAllowed(marine, sofList, emptyList), Is.False, "SOF items should not be allowed for VDV squad");
            Assert.That(vendorSys.IsSpecializationAllowed(marine, emptyList, vdvList), Is.False, "Items excluding VDV should be rejected");
            Assert.That(vendorSys.IsSpecializationAllowed(marine, emptyList, sofList), Is.True, "Items excluding SOF should be allowed for VDV squad");
        });
    }
}

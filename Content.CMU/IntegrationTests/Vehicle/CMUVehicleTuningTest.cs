using System.Reflection;
using Content.IntegrationTests.Fixtures;
using Content.Server.CMU14.Electric;
using Content.Server.CMU14.Fluids;
using Content.Server.Power.Components;
using Content.Shared._RMC14.Vehicle;
using Content.Shared.Buckle.Components;
using Content.Shared.CMU14.Electric;
using Content.Shared.CMU14.Vehicle;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using Content.Shared.Vehicle;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.IntegrationTests.CMU14.Vehicle;

// CMU14 class
[TestFixture]
public sealed class CMUVehicleTuningTest : GameTest
{
    private const string ArmorModule = "CMUVehicleArmorModuleCivilian";
    private const string Wheel = "RMCVanWheel";

    [TestCase("AU14VehicleCivTruck")]
    [TestCase("AU14VehicleCivBlueTruck")]
    [TestCase("AU14VehicleCivCar")]
    [TestCase("AU14VehicleCivHippieVan")]
    [TestCase("VehiclePizzaVan")]
    public async Task ArmorModuleAddsHumveeArmorIntegrityAndExamineLine(string vehicleId)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var vehicle = SEntMan.SpawnEntity(vehicleId, map.GridCoords);
            var hardpoints = SEntMan.GetComponent<HardpointSlotsComponent>(vehicle);
            foreach (var slotId in new[] { "cmu-armor", "cmu-front", "cmu-engine", "cmu-turret" })
                Assert.That(hardpoints.Slots.Single(slot => slot.Id == slotId).Required, Is.False, slotId);

            Fit(vehicle, "wheel-1", Wheel);
            var frame = SEntMan.GetComponent<HardpointIntegrityComponent>(vehicle);
            var withoutArmor = frame.MaxIntegrity;
            Fit(vehicle, "cmu-armor", ArmorModule);

            Assert.That(frame.MaxIntegrity, Is.EqualTo(withoutArmor + 500f).Within(0.01f));
            Assert.That(SEntMan.GetComponent<DamageProtectionBuffComponent>(vehicle).Modifiers.ContainsKey("VehicleFrameHumvee"), Is.True);
            Assert.That(Speed(vehicle), Is.EqualTo(0.8f).Within(0.001f));

            var examiner = SEntMan.SpawnEntity("CMMobHuman", map.GridCoords);
            var text = SEntMan.System<ExamineSystemShared>().GetExamineText(vehicle, examiner).ToMarkup();
            Assert.That(text, Does.Contain(Loc.GetString("cmu-vehicle-module-examine-armor")));
        });
    }

    [TestCase("CMUVehicleArmorModuleCivilianLight", 300f, 1f, 1f)]
    [TestCase("CMUVehicleArmorModuleCivilian", 500f, 0.8f, 0.9f)]
    [TestCase("CMUVehicleArmorModuleCivilianHeavy", 1000f, 0.5f, 0.75f)]
    public async Task ArmorTiersSlowAndTheEngineHalvesTheSlowdown(string armor, float integrity, float slowed, float withEngine)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var vehicle = SEntMan.SpawnEntity("AU14VehicleCivCar", map.GridCoords);
            Fit(vehicle, "wheel-1", Wheel);
            var frame = SEntMan.GetComponent<HardpointIntegrityComponent>(vehicle);
            var before = frame.MaxIntegrity;
            Fit(vehicle, "cmu-armor", armor);
            Assert.That(frame.MaxIntegrity - before, Is.EqualTo(integrity).Within(0.01f));
            Assert.That(Speed(vehicle), Is.EqualTo(slowed).Within(0.001f));

            Fit(vehicle, "cmu-engine", "CMUVehicleEngineHeavyFuel");
            Assert.That(Speed(vehicle), Is.EqualTo(withEngine).Within(0.001f));
        });
    }

    [Test]
    public async Task ArmoredWheelAndSpikedBullbar()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var vehicle = SEntMan.SpawnEntity("AU14VehicleCivTruck", map.GridCoords);
            var wheel = Fit(vehicle, "wheel-1", "CMUVehicleWheelArmored");
            Assert.That(SEntMan.GetComponent<HardpointIntegrityComponent>(wheel).MaxIntegrity, Is.EqualTo(350f));
            Assert.That(Speed(vehicle), Is.EqualTo(0.93f).Within(0.001f));

            var damage = typeof(GridVehicleMoverSystem).GetMethod("CMUGetMobCollisionDamage", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var mover = SEntMan.System<GridVehicleMoverSystem>();
            Assert.That(damage.Invoke(mover, new object[] { vehicle }), Is.Null);

            var bullbar = Fit(vehicle, "cmu-front", "CMUVehicleBullbarSpiked");
            Assert.That(SEntMan.GetComponent<HardpointIntegrityComponent>(bullbar).MaxIntegrity, Is.EqualTo(200f));
            var ram = (DamageSpecifier) damage.Invoke(mover, new object[] { vehicle })!;
            Assert.That(ram.GetTotal().Float(), Is.EqualTo(50f).Within(0.01f));
        });
    }

    // Each vehicle draws what is fitted on it: each module's state on the layer of its slot and a plain van wheel nothing.
    // The turret draws its own rotating overlay, put where the vehicle's roof is.
    [TestCase("VehiclePizzaVan")]
    [TestCase("AU14VehicleCivTruck")]
    [TestCase("AU14VehicleCivBlueTruck")]
    [TestCase("AU14VehicleCivCar")]
    [TestCase("AU14VehicleCivHippieVan")]
    public async Task VehicleShowsTheFittedModules(string vehicleId)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var vehicle = SEntMan.SpawnEntity(vehicleId, map.GridCoords);
            var visuals = SEntMan.GetComponent<VehicleHardpointVisualsComponent>(vehicle);
            string Layer(string layer) => visuals.Layers.SingleOrDefault(l => l.Layer == layer).State ?? string.Empty;

            Fit(vehicle, "wheel-1", Wheel);
            Assert.That(Layer("cmu-wheels"), Is.Empty);
            Assert.That(Layer("cmu-armor"), Is.Empty);

            Fit(vehicle, "cmu-armor", "CMUVehicleArmorModuleCivilianHeavy");
            Fit(vehicle, "cmu-front", "CMUVehicleBullbarSpiked");
            var turret = Fit(vehicle, "cmu-turret", "CMUVehicleTurretHomemade");
            Assert.That(Layer("cmu-armor"), Is.EqualTo("armour_heavy"));
            Assert.That(Layer("cmu-front"), Is.EqualTo("bullbar_spiked"));

            var mount = SEntMan.GetComponent<CMUVehicleTurretMountComponent>(vehicle);
            var overlay = SEntMan.GetComponent<VehicleTurretComponent>(turret);
            Assert.That(overlay.ShowOverlay, Is.True);
            Assert.That(overlay.UseDirectionalOffsets, Is.True);
            Assert.That(overlay.PixelOffsetNorth, Is.EqualTo(mount.North));
            Assert.That(overlay.PixelOffsetEast, Is.EqualTo(mount.East));
            Assert.That(overlay.PixelOffsetSouth, Is.EqualTo(mount.South));
            Assert.That(overlay.PixelOffsetWest, Is.EqualTo(mount.West));

            var gun = Fit(turret, "turret-cannon", "CMUVehicleGunPKP");
            Assert.That(SEntMan.GetComponent<VehicleTurretComponent>(gun).ShowOverlay, Is.True);
        });
    }

    [Test]
    public async Task TurretSwapsThePassengerSeatForAGunnerSeatThatLoadsTheGun()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var vehicle = SEntMan.SpawnEntity("AU14VehicleCivCar", map.GridCoords);
            Fit(vehicle, "wheel-1", Wheel);
            var turret = Fit(vehicle, "cmu-turret", "CMUVehicleTurretHomemade");
            var gun = Fit(turret, "turret-cannon", "CMUVehicleGunPKP");

            var state = SEntMan.GetComponent<CMUVehicleGunnerSeatStateComponent>(vehicle);
            var seat = state.GunnerSeat!.Value;
            Assert.That(SEntMan.HasComponent<VehicleWeaponsSeatComponent>(seat), Is.True);
            Assert.That(state.OriginalPrototype?.Id, Is.EqualTo("CMChairComfy"));
            var grid = state.Coordinates.EntityId;
            Assert.That(Seats(grid).Count(uid => !SEntMan.HasComponent<VehicleDriverSeatComponent>(uid)), Is.EqualTo(1),
                "The gunner seat replaces the passenger seat; it is not added beside it.");

            // A PKP-MS box used on the gunner seat goes into the mounted gun.
            var user = SEntMan.SpawnEntity("CMMobHuman", state.Coordinates);
            var magazine = SEntMan.SpawnEntity("RuMCMagazinePKP_MS", state.Coordinates);
            Assert.That(SEntMan.System<SharedHandsSystem>().TryPickup(user, magazine), Is.True);
            SEntMan.EventBus.RaiseLocalEvent(seat, new InteractUsingEvent(user, magazine, seat, state.Coordinates));
            Assert.That(SEntMan.System<ItemSlotsSystem>().GetItemOrNull(gun, "gun_magazine"), Is.EqualTo(magazine));
            var ammo = new GetAmmoCountEvent();
            SEntMan.EventBus.RaiseLocalEvent(gun, ref ammo);
            Assert.That(ammo.Count, Is.GreaterThan(0));

            // Taking the turret off puts the passenger seat back.
            var slots = SEntMan.System<ItemSlotsSystem>();
            var removal = new HardpointRemoveDoAfterEvent("cmu-turret");
            removal.DoAfter = new Content.Shared.DoAfter.DoAfter(0,
                new Content.Shared.DoAfter.DoAfterArgs(SEntMan, user, TimeSpan.Zero, removal, vehicle, vehicle), SGameTiming.CurTime);
            SEntMan.EventBus.RaiseLocalEvent(vehicle, removal);
            Assert.That(slots.GetItemOrNull(vehicle, "cmu-turret"), Is.Null);
            Assert.That(SEntMan.HasComponent<CMUVehicleGunnerSeatStateComponent>(vehicle), Is.False);
            Assert.That(SEntMan.Deleted(seat) || SEntMan.IsQueuedForDeletion(seat), Is.True);
            Assert.That(Seats(grid).Any(uid => SEntMan.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID == "CMChairComfy"), Is.True);
        });
    }

    [TestCase(1, ArmorModule)]
    [TestCase(2, Wheel)]
    [TestCase(3, "CMUVehicleArmorModuleCivilianHeavy")]
    [TestCase(4, "CMUVehicleArmorModuleCivilianLight")]
    [TestCase(5, "CMUVehicleWheelArmored")]
    [TestCase(6, "CMUVehicleBullbarSpiked")]
    [TestCase(7, "CMUVehicleEngineHeavyFuel")]
    [TestCase(8, "CMUVehicleTurretHomemade")]
    [TestCase(9, "CMUVehicleGunPKP")]
    public async Task WorkshopCircuitPicksTheModule(int circuit, string expected)
    {
        var (stacks, items, fluids) = circuit switch
        {
            1 => (new[] { ("RUMCArmourPlatesItem", 5) }, new string[0], new (string, float)[0]),
            2 => (new[] { ("RUMCIronRodItem", 5), ("CMSheetMetal", 10), ("RUMCRubberSheetItem", 8) }, new string[0], new (string, float)[0]),
            3 => (new[] { ("RUMCArmourPlatesItem", 10), ("RUMCLeadSheet", 5), ("CMSheetPlasteel", 20) }, new string[0], new[] { ("WeldingFuel", 20f) }),
            4 => (new[] { ("RUMCTitaniumSheet", 5), ("CMSheetPlasteel", 5), ("CMSheetMetal", 10) }, new string[0], new (string, float)[0]),
            5 => (new (string, int)[0], new[] { ArmorModule, Wheel }, new[] { ("WeldingFuel", 20f) }),
            6 => (new[] { ("RUMCIronRodItem", 20), ("CMSheetPlasteel", 5), ("CMSheetMetal", 20) }, new string[0], new[] { ("WeldingFuel", 20f) }),
            8 => (new[] { ("RUMCArmourPlatesItem", 5), ("CMSheetPlasteel", 10), ("RUMCKitFastenersItem", 10), ("RUMCIronRodItem", 10) }, new string[0], new (string, float)[0]),
            9 => (new[] { ("RUMCIronRodItem", 10), ("RUMCKitFastenersItem", 5) }, new[] { "RuMCMachineGunPKP_MS" }, new (string, float)[0]),
            _ => (new[] { ("CMSheetMetal", 50), ("RUMCRubberSheetItem", 5), ("RUMCIronIngot", 20), ("RUMCIronRodItem", 20) }, new string[0], new[] { ("RUMCHeavyFuel", 100f) }),
        };

        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var slots = SEntMan.System<ItemSlotsSystem>();
            var machine = SEntMan.SpawnEntity("CMUMachineVehicleWorkshop", map.GridCoords);
            SEntMan.GetComponent<ApcPowerReceiverComponent>(machine).Powered = true;
            var recipeMachine = SEntMan.GetComponent<RecipeMachineComponent>(machine);
            recipeMachine.Circuit = circuit;
            recipeMachine.NeedsRecipeCheck = true;

            var input = 1;
            foreach (var (id, count) in stacks)
            {
                var stack = SEntMan.SpawnEntity(id, map.GridCoords);
                SEntMan.System<SharedStackSystem>().SetCount(stack, count);
                Assert.That(slots.TryInsert(machine, $"input{input++}", stack, null), Is.True, id);
            }

            foreach (var id in items)
                Assert.That(slots.TryInsert(machine, $"input{input++}", SEntMan.SpawnEntity(id, map.GridCoords), null), Is.True, id);

            foreach (var (kind, amount) in fluids)
                SEntMan.System<FluidSystem>().Add(machine, kind, amount);

            var recipes = SEntMan.System<RecipeMachineSystem>();
            for (var i = 0; i < 40 && slots.GetItemOrNull(machine, "output1") == null; i++)
                recipes.Update(1f);

            var result = slots.GetItemOrNull(machine, "output1");
            Assert.That(result, Is.Not.Null, "The workshop never finished its recipe.");
            Assert.That(SEntMan.GetComponent<MetaDataComponent>(result!.Value).EntityPrototype!.ID, Is.EqualTo(expected));
            foreach (var (kind, _) in fluids)
                Assert.That(SEntMan.System<FluidSystem>().GetAmount(machine, kind), Is.Zero, kind);
        });
    }

    [Test]
    public async Task WorkshopDoesNothingWithoutPower()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var slots = SEntMan.System<ItemSlotsSystem>();
            var machine = SEntMan.SpawnEntity("CMUMachineVehicleWorkshop", map.GridCoords);
            SEntMan.GetComponent<ApcPowerReceiverComponent>(machine).Powered = false;
            SEntMan.GetComponent<RecipeMachineComponent>(machine).Circuit = 1;
            var plates = SEntMan.SpawnEntity("RUMCArmourPlatesItem", map.GridCoords);
            SEntMan.System<SharedStackSystem>().SetCount(plates, 5);
            Assert.That(slots.TryInsert(machine, "input1", plates, null), Is.True);

            var recipes = SEntMan.System<RecipeMachineSystem>();
            for (var i = 0; i < 40; i++)
                recipes.Update(1f);

            Assert.That(slots.GetItemOrNull(machine, "output1"), Is.Null);
        });
    }

    private EntityUid Fit(EntityUid owner, string slot, string prototype)
    {
        var item = SEntMan.SpawnEntity(prototype, SEntMan.GetComponent<TransformComponent>(owner).Coordinates);
        var hardpoints = SEntMan.GetComponent<HardpointSlotsComponent>(owner);
        var definition = hardpoints.Slots.Single(s => s.Id == slot);
        // A null user skips the install do-after, so check the hardpoint rules the real install path applies.
        var isValid = typeof(HardpointSystem).GetMethod("IsValidHardpoint", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assert.That(isValid.Invoke(SEntMan.System<HardpointSystem>(), new object[] { item, hardpoints, definition }), Is.True,
            $"{prototype} must fit {slot}");
        Assert.That(SEntMan.System<ItemSlotsSystem>().TryInsert(owner, slot, item, null), Is.True, $"{prototype} into {slot}");
        return item;
    }

    private float Speed(EntityUid vehicle)
    {
        return SEntMan.TryGetComponent(vehicle, out VehicleSpeedModifierComponent? speed) ? speed.SpeedMultiplier : 1f;
    }

    private IEnumerable<EntityUid> Seats(EntityUid grid)
    {
        var seats = new List<EntityUid>();
        var children = SEntMan.GetComponent<TransformComponent>(grid).ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            if (SEntMan.HasComponent<StrapComponent>(child) && !SEntMan.IsQueuedForDeletion(child))
                seats.Add(child);
        }

        return seats;
    }
}

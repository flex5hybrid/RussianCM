// RuMC: the recipe buttons of the machine tools (RuMCRecipeSelectionSystem).
using Content.IntegrationTests;
using Content.Server.CMU14.Electric;
using Content.Server.Power.Components;
using Content.Shared.CMU14.Electric;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Stacks;
using NUnit.Framework;

namespace Content.IntegrationTests._RuCM.Industry;

[TestFixture]
public sealed class RuRecipeSelectionTest
{
    // A machine tool makes only the recipe chosen in its window; with the choice cleared it makes whatever its inputs pay for.
    [Test]
    public async Task MachineToolMakesOnlyTheChosenRecipe()
    {
        await using var pair = await PoolManager.GetServerClient();
        var map = await pair.CreateTestMap();
        await pair.Server.WaitAssertion(() =>
        {
            var entities = pair.Server.EntMan;
            var recipes = entities.System<RecipeMachineSystem>();
            var slots = entities.System<ItemSlotsSystem>();
            var machine = entities.SpawnEntity("RUMCMachineWiremill", map.GridCoords);
            var comp = entities.GetComponent<RecipeMachineComponent>(machine);
            Assert.That(comp.RecipeSelection, Is.True);
            entities.GetComponent<ApcPowerReceiverComponent>(machine).Powered = true;

            // A recipe of another machine is not taken.
            entities.EventBus.RaiseLocalEvent(machine, (object) new RecipeMachineSelectMessage("RUMCMRBenderSheetIron"));
            Assert.That(comp.SelectedRecipe, Is.Null);

            entities.EventBus.RaiseLocalEvent(machine, (object) new RecipeMachineSelectMessage("RUMCMRWiremillWireGold"));
            Assert.That(comp.SelectedRecipe, Is.EqualTo("RUMCMRWiremillWireGold"));

            var copper = entities.SpawnEntity("RUMCCopperIngot", map.GridCoords);
            Assert.That(slots.TryInsert(machine, "input1", copper, null), Is.True);
            recipes.Update(5);
            recipes.Update(5);
            Assert.That(slots.GetItemOrNull(machine, "output1"), Is.Null, "Copper must not be drawn while gold wire is chosen.");

            entities.EventBus.RaiseLocalEvent(machine, (object) new RecipeMachineSelectMessage(null));
            Assert.That(comp.SelectedRecipe, Is.Null);
            recipes.Update(5);
            recipes.Update(5);
            var result = slots.GetItemOrNull(machine, "output1");
            Assert.That(result, Is.Not.Null);
            Assert.That(entities.GetComponent<StackComponent>(result!.Value).StackTypeId.Id, Is.EqualTo("RUMCKitWiring"));
        });
        await pair.CleanReturnAsync();
    }
}

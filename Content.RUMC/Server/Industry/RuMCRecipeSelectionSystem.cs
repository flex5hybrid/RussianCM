using Content.Shared.CMU14.Electric;
using Robust.Shared.Prototypes;

namespace Content.Server.CMU14.Electric;

// RuMC: the recipe chosen in the window of a machine tool. It takes only a recipe of the machine's kind, and sets its circuit.
public sealed class RuMCRecipeSelectionSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RecipeMachineComponent, RecipeMachineSelectMessage>(OnSelect);
    }

    private void OnSelect(Entity<RecipeMachineComponent> machine, ref RecipeMachineSelectMessage args)
    {
        if (!machine.Comp.RecipeSelection)
            return;

        if (args.Recipe == null)
        {
            machine.Comp.SelectedRecipe = null;
        }
        else
        {
            if (!_proto.TryIndex<MachineRecipePrototype>(args.Recipe, out var recipe) || recipe.Machine != machine.Comp.Kind)
                return;

            machine.Comp.SelectedRecipe = recipe.ID;
            machine.Comp.Circuit = Math.Clamp(recipe.Circuit ?? 0, 0, machine.Comp.MaxCircuit);
        }

        machine.Comp.NeedsRecipeCheck = true;
        Dirty(machine);
    }
}

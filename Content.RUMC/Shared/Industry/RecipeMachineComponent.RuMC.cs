using Robust.Shared.Serialization;

namespace Content.Shared.CMU14.Electric;

// RuMC: a machine tool (the lathe, the assembler, the sewing machine...) lists its recipes in the window as buttons, and the one chosen
// there is the only one it makes. With none chosen it makes whatever its inputs pay for, like the processors do.
public sealed partial class RecipeMachineComponent
{
    [DataField]
    public bool RecipeSelection;

    // The recipe chosen in the window of a machine tool, or null for any.
    [DataField, AutoNetworkedField]
    public string? SelectedRecipe;
}

// A recipe chosen in the window of a machine tool; null clears the choice.
[Serializable, NetSerializable]
public sealed class RecipeMachineSelectMessage(string? recipe) : BoundUserInterfaceMessage
{
    public string? Recipe = recipe;
}

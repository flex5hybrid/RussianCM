namespace Content.Shared.CMU14.Crafting;

// RuMC: how many come out of a craft that went better than usual.
public sealed partial class CraftingRecipePrototype
{
    // When set, a craft that went better than usual makes this many, and no material is spared; otherwise one more of a product made
    // several at a time, or a material spared for a single thing.
    [DataField]
    public int? GoodOutputCount { get; private set; }
}

using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Materials;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Shared._CE.Trading.Systems;

//TODO: All of this should be removed when PricingSystem in the upstream moves to Shared.
public abstract partial class CESharedEconomySystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly RMCReagentSystem _reagentSystem = default!;

    /// <summary>
    /// Get a rough price for an entityprototype. Does not consider contained entities.
    /// </summary>
    public double GetEstimatedPrice(EntityPrototype prototype)
    {
        var ev = new EstimatedPriceCalculationEvent(prototype);

        RaiseLocalEvent(ref ev);

        if (ev.Handled)
            return ev.Price;

        var price = ev.Price;
        price += GetMaterialsPrice(prototype);
        price += GetSolutionsPrice(prototype);
        // Can't use static price with stackprice
        var oldPrice = price;
        price += GetStackPrice(prototype);

        if (oldPrice.Equals(price))
        {
            price += GetStaticPrice(prototype);
        }

        // TODO: Proper container support.

        return price;
    }

    private double GetMaterialsPrice(EntityPrototype prototype)
    {
        double price = 0;

        //CE We take materials into account when calculating the price in any case.
        if ((prototype.Components.ContainsKey(Factory.GetComponentName<MaterialComponent>()) || prototype.ID.StartsWith("CE")) &&
            prototype.Components.TryGetValue(Factory.GetComponentName<PhysicalCompositionComponent>(), out var composition))
        {
            var compositionComp = (PhysicalCompositionComponent) composition.Component;
            var matPrice = GetMaterialPrice(compositionComp);

            if (prototype.Components.TryGetValue(Factory.GetComponentName<StackComponent>(), out var stackProto))
            {
                matPrice *= ((StackComponent) stackProto.Component).Count;
            }

            price += matPrice;
        }

        return price;
    }

    private double GetMaterialPrice(PhysicalCompositionComponent component)
    {
        double price = 0;
        foreach (var (id, quantity) in component.MaterialComposition)
        {
            price += _prototypeManager.Index<MaterialPrototype>(id).Price * quantity;
        }
        return price;
    }

    private double GetSolutionsPrice(EntityPrototype prototype)
    {
        var price = 0.0;

        if (prototype.Components.TryGetValue(Factory.GetComponentName<SolutionContainerManagerComponent>(), out var solManager))
        {
            var solComp = (SolutionContainerManagerComponent) solManager.Component;
            price += GetSolutionPrice(solComp);
        }

        return price;
    }
private double GetSolutionPrice(SolutionContainerManagerComponent component)
{
    var price = 0.0;

    if (component.Solutions == null)
        return price;

    foreach (var solution in component.Solutions.Values)
    {
        foreach (var (reagent, quantity) in solution.Contents)
        {
            if (!_reagentSystem.TryIndex(reagent.Prototype, out var reagentProto))
                continue;

            price += (double) quantity * reagentProto.PricePerUnit;
        }
    }

    return price;
}
protected virtual double GetStaticPrice(EntityPrototype prototype)
{
    return 0;
}

protected virtual double GetStackPrice(EntityPrototype prototype)
{
    return 0;
}
}

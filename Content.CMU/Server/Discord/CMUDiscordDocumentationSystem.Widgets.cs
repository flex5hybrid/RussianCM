using System.Linq;
using System.Text;
using System.Xml.Linq;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Prototypes;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Kitchen;
using Content.Shared.Metabolism;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.CMU14.Discord;

public sealed partial class CMUDiscordDocumentationSystem
{
    [Dependency] private readonly RMCReagentSystem _reagents = default!;

    private string RenderWidget(XElement element)
    {
        string Required(string name) => (string?) element.Attribute(name)
                                       ?? throw new FormatException($"Missing {name} on {element.Name}.");
        switch (element.Name.LocalName)
        {
            case "CMUFobGuideText":
                return _localization.GetString(Required("Key"));
            case "CMUGuideCraftingRecipes":
                return Text("crafting-recipes");
            case "GuideEntityEmbed":
            {
                var entity = _prototypes.Index<EntityPrototype>(Required("Entity"));
                var caption = (string?) element.Attribute("Caption") ?? entity.Name;
                return $"**{caption}** — {entity.Name}\n{entity.Description}";
            }
            case "CMUGuideImage":
            case "CMUGuideZoomImage":
                return $"[{Text("image")}]({Source(Required("Path"))})";
            case "CMUGuideOutfitEmbed":
            {
                var gear = _prototypes.Index<StartingGearPrototype>(Required("Gear"));
                var items = gear.Equipment.Values.Concat(gear.Inhand).Concat(gear.Storage.Values.SelectMany(p => p))
                    .Select(id => _prototypes.Index<EntityPrototype>(id).Name);
                return $"**{(string?) element.Attribute("Caption") ?? Text("outfit")}**\n" +
                       string.Join(", ", items);
            }
            case "GuideReagentEmbed":
                return RenderReagent(_reagents.Index(Required("Reagent")));
            case "GuideReagentGroupEmbed":
            {
                var group = Required("Group");
                var includeUpstream = (bool?) element.Attribute("IncludeUpstream") ?? false;
                var reagents = includeUpstream
                    ? _prototypes.EnumeratePrototypes<ReagentPrototype>()
                    : _prototypes.EnumerateCM<ReagentPrototype>();
                return string.Join("\n\n", reagents.Where(p => p.Group == group)
                    .OrderBy(p => p.LocalizedName, StringComparer.Ordinal).Select(RenderReagent));
            }
            case "GuideMicrowaveEmbed":
                return RenderRecipe(_prototypes.Index<FoodRecipePrototype>(Required("Recipe")));
            case "GuideMicrowaveGroupEmbed":
            {
                var group = Required("Group");
                return string.Join("\n\n", _prototypes.EnumeratePrototypes<FoodRecipePrototype>()
                    .Where(p => p.Group == group).OrderBy(p => p.Name, StringComparer.Ordinal).Select(RenderRecipe));
            }
            default:
                throw new FormatException($"Unsupported guidebook control {element.Name}.");
        }
    }

    private string RenderRecipe(FoodRecipePrototype recipe)
    {
        var result = _prototypes.Index<EntityPrototype>(recipe.Result);
        var ingredients = recipe.IngredientsSolids.Select(p => $"{_prototypes.Index<EntityPrototype>(p.Key).Name} × {p.Value}")
            .Concat(recipe.IngredientsReagents.Select(p => Text("reagent-quantity", ("name", ReagentName(p.Key)), ("amount", p.Value))));
        return $"### {result.Name}\n{result.Description}\n" +
               Text("recipe", ("ingredients", string.Join(", ", ingredients)), ("time", recipe.CookTime));
    }

    private string ReagentName(ProtoId<ReagentPrototype> id) => _reagents.Index(id).LocalizedName;

    private string RenderReagent(ReagentPrototype reagent)
    {
        var text = new StringBuilder($"### {reagent.LocalizedName}\n{reagent.LocalizedDescription}\n");
        text.AppendLine(Text("appearance", ("description", reagent.LocalizedPhysicalDescription)));
        if (reagent.Overdose != null)
            text.AppendLine(Text("overdose", ("amount", reagent.Overdose.ToString()!)));
        if (reagent.CriticalOverdose != null)
            text.AppendLine(Text("critical-overdose", ("amount", reagent.CriticalOverdose.ToString()!)));

        var registry = EntityManager.System<ChemistryGuideDataSystem>().ReagentGuideRegistry;
        if (registry.TryGetValue(reagent.ID, out var guide))
        {
            if (guide.GuideEntries != null)
            {
                foreach (var (stage, effects) in guide.GuideEntries.OrderBy(p => p.Key.Id, StringComparer.Ordinal))
                {
                    text.AppendLine(Text("metabolism", ("stage", _prototypes.Index<MetabolismStagePrototype>(stage).LocalizedName),
                        ("rate", effects.MetabolismRate)));
                    foreach (var effect in effects.EffectDescriptions)
                        text.AppendLine("• " + effect);
                    if (effects.Metabolites != null)
                        text.AppendLine(Text("metabolites", ("items", string.Join(", ", effects.Metabolites.Select(p => $"{ReagentName(p.Key)} × {p.Value}")))));
                }
            }
            if (guide.PlantMetabolisms is { Count: > 0 })
                text.AppendLine(Text("plant-effects", ("effects", string.Join("\n", guide.PlantMetabolisms))));
        }

        foreach (var reaction in _prototypes.EnumeratePrototypes<ReactionPrototype>()
                     .Where(p => !p.Source && p.Products.ContainsKey(reagent.ID)).OrderBy(p => p.ID, StringComparer.Ordinal))
        {
            var inputs = reaction.Reactants.Select(p => $"{ReagentName(p.Key)} × {p.Value.Amount}" + (p.Value.Catalyst ? Text("catalyst") : ""));
            var outputs = reaction.Products.Select(p => $"{ReagentName(p.Key)} × {p.Value}");
            text.AppendLine(Text("reaction", ("inputs", string.Join(" + ", inputs)), ("outputs", string.Join(" + ", outputs))));
            if (reaction.MinimumTemperature > 0)
                text.AppendLine(Text("minimum-temperature", ("temperature", reaction.MinimumTemperature)));
            if (float.IsFinite(reaction.MaximumTemperature))
                text.AppendLine(Text("maximum-temperature", ("temperature", reaction.MaximumTemperature)));
            if (reaction.MixingCategories != null)
                text.AppendLine(Text("mixing", ("categories", string.Join(", ", reaction.MixingCategories.Select(p =>
                    _localization.GetString(_prototypes.Index<MixingCategoryPrototype>(p).VerbText))))));
        }
        return text.ToString();
    }
}

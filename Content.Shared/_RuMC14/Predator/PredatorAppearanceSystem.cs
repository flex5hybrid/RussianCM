using System.Linq;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Shared.RuMC14.Predator;

public sealed class PredatorAppearanceSystem : EntitySystem
{
    // public override void Initialize()
    // {
    //     SubscribeLocalEvent<PredatorComponent, ComponentInit>(OnHumanoidAppearance);
    // }

    // private void OnHumanoidAppearance(EntityUid uid, PredatorComponent comp, ref ComponentInit args)
    // {
    //     if (!TryComp(uid, out HumanoidAppearanceComponent? humanoid))
    //         return;

    //     var color = comp.Color.ToString().ToLower();

    //     var newLayers = new Dictionary<HumanoidVisualLayers, HumanoidSpeciesSpriteLayer>();

    //     var oldLayers = humanoid.BaseLayers.Keys;

    //     humanoid.BaseLayers[HumanoidVisualLayers.Head].



    //     humanoid.BaseLayers = newLayers;
    //     Dirty(uid, humanoid);
    // }
}

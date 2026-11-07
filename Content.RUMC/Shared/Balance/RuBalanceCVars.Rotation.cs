using Robust.Shared.Configuration;

namespace Content.Shared._RuCM.Balance;

[CVarDefs]
public sealed class RuBalanceCVars
{
    public static readonly CVarDef<float> DistressBaseWeight =
        CVarDef.Create("rucm.rotation.distress_base_weight", 1.5f, CVar.SERVER);

    public static readonly CVarDef<float> DistressWeightGrowth =
        CVarDef.Create("rucm.rotation.distress_weight_growth", 0.2f, CVar.SERVER);

    public static readonly CVarDef<float> DistressMaximumMultiplier =
        CVarDef.Create("rucm.rotation.distress_maximum_multiplier", 2f, CVar.SERVER);

    public static readonly CVarDef<int> DistressGuaranteeAfter =
        CVarDef.Create("rucm.rotation.distress_guarantee_after", 4, CVar.SERVER);
}

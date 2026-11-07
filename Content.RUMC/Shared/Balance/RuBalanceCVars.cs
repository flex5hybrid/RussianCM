using Robust.Shared.Configuration;

namespace Content.Shared._RuCM.Balance;

[CVarDefs]
public sealed partial class RuBalanceCVars
{
    public static readonly CVarDef<float> LimbSeveranceThresholdMultiplier =
        CVarDef.Create("rucm.balance.limb_severance_threshold_multiplier", 1.35f, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> RepeatSeveranceSeconds =
        CVarDef.Create("rucm.balance.repeat_severance_seconds", 120f, CVar.SERVER);

    public static readonly CVarDef<float> RepeatSeveranceThresholdMultiplier =
        CVarDef.Create("rucm.balance.repeat_severance_threshold_multiplier", 2f, CVar.SERVER);
}

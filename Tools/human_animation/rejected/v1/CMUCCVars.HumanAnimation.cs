using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>Opt-in draft until in-game clothing, combat readability and frame-time review.</summary>
    public static readonly CVarDef<bool> CMUHumanAnimations =
        CVarDef.Create("cmu.human_animations", true, CVar.CLIENTONLY | CVar.ARCHIVE);
}

using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    /// Applies Dark Ambient color grading to the main game viewport.
    /// </summary>
    public static readonly CVarDef<bool> CMUDarkAmbientEnabled =
        CVarDef.Create("cmu.lighting.dark_ambient_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Dark Ambient intensity as a percentage, from 0 to 100.
    /// </summary>
    public static readonly CVarDef<int> CMUDarkAmbientIntensity =
        CVarDef.Create("cmu.lighting.dark_ambient_intensity", 65, CVar.CLIENTONLY | CVar.ARCHIVE);
}

using Robust.Shared.Configuration;

namespace Content.Shared._Bluedge.CCVars;

[CVarDefs]
public sealed class CCVars220
{
    /// <summary>
    /// Whether is bloom lighting eanbled or not
    /// </summary>
    public static readonly CVarDef<bool> BloomLightingEnabled =
        CVarDef.Create("bloom_lighting.enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);
}

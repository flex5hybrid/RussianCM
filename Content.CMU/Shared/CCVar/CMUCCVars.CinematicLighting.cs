using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>0 = original rendering, 1 = low, 2 = high, 3 = ultra. Applied live.</summary>
    public static readonly CVarDef<int> CMUCinematicLighting =
        CVarDef.Create("cmu.lighting.cinematic_quality", 0, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> CMUCinematicAOIntensity =
        CVarDef.Create("cmu.lighting.cinematic_ao_intensity", 1.2f, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> CMUCinematicAORadius =
        CVarDef.Create("cmu.lighting.cinematic_ao_radius", 1.25f, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> CMUCinematicAtmosphere =
        CVarDef.Create("cmu.lighting.cinematic_atmosphere", 0.3f, CVar.CLIENTONLY | CVar.ARCHIVE);
}

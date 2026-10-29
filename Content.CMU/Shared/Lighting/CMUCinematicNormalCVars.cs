using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared.CMU14.Lighting;

/// <summary>Client-only feature switch; registered by Robust through CVarDefs.</summary>
[CVarDefs]
public static class CMUCinematicNormalCVars
{
    public static readonly CVarDef<bool> Enabled =
        CVarDef.Create("cmu.lighting.cinematic_normals", false, CVar.CLIENTONLY);

    /// <summary>Client-local mass-test mode, no new replicated components.</summary>
    public static readonly CVarDef<bool> AllSprites =
        CVarDef.Create("cmu.lighting.cinematic_normals_all", false, CVar.CLIENTONLY);

    /// <summary>Multiplier for the extra per-pixel light contribution.</summary>
    public static readonly CVarDef<float> Gain =
        CVarDef.Create("cmu.lighting.cinematic_normals_gain", 1.0f, CVar.CLIENTONLY);

    /// <summary>Approximate light height in world units. Smaller values emphasize side relief.</summary>
    public static readonly CVarDef<float> Height =
        CVarDef.Create("cmu.lighting.cinematic_normals_height", 0.45f, CVar.CLIENTONLY);

    /// <summary>Contrast of directional response relative to a flat sprite.</summary>
    public static readonly CVarDef<float> Contrast =
        CVarDef.Create("cmu.lighting.cinematic_normals_contrast", 3.0f, CVar.CLIENTONLY);

    /// <summary>Flat-surface subtraction: 1 is purely directional, below 1 also lifts flat areas.</summary>
    public static readonly CVarDef<float> Flatness =
        CVarDef.Create("cmu.lighting.cinematic_normals_flatness", 0.7f, CVar.CLIENTONLY);

    /// <summary>Caps normal response before the final per-light gain.</summary>
    public static readonly CVarDef<float> MaxBoost =
        CVarDef.Create("cmu.lighting.cinematic_normals_max_boost", 1.0f, CVar.CLIENTONLY);
}

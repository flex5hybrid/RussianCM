using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared.CMU14.Lighting;

/// <summary>Client-only feature switch; registered by Robust through CVarDefs.</summary>
[CVarDefs]
public static class CMUCinematicNormalCVars
{
    public static readonly CVarDef<bool> Enabled =
        CVarDef.Create("cmu.lighting.cinematic_normals", false, CVar.CLIENTONLY);
}

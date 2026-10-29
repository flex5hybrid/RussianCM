using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>Experimental opt-in normal mapping. Off by default and client-only.</summary>
    public static readonly CVarDef<bool> CMUCinematicNormals =
        CVarDef.Create("cmu.lighting.cinematic_normals", false, CVar.CLIENTONLY);
}

using Content.Client.CMU14.Lighting;
using Content.Shared.CCVar;
using Robust.Client.Graphics;

namespace Content.Client.Viewport;

public sealed partial class ScalingViewport
{
    /// <summary>Explicit opt-in. Small previews and camera feeds retain their original rendering.</summary>
    public bool CinematicLighting { get; set; }

    private CinematicLightingSystem.ViewportScope BeginCinematicLighting(IClydeViewport viewport)
    {
        return CinematicLighting && _cfg.GetCVar(CCVars.CMUCinematicLighting) > 0
            ? _entityManager.System<CinematicLightingSystem>().BeginViewport(viewport)
            : default;
    }
}

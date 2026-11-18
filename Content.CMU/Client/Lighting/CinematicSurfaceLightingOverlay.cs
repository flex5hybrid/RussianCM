using Content.Client.Light;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.CMU14.Lighting;

/// <summary>
/// Adds material reflections before every entity, including small/hidden mobs and corpses. The normal
/// sprite renderer retains sole ownership of actor rendering and visibility. Resources belong to
/// the final overlay; this view never owns or disposes them a second time.
/// </summary>
public sealed class CinematicSurfaceLightingOverlay : Overlay
{
    private readonly CinematicLightingOverlay _owner;
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;
    public override bool RequestScreenTexture => true;

    public CinematicSurfaceLightingOverlay(CinematicLightingOverlay owner)
    {
        _owner = owner;
        // Contact shading must attenuate the reflected field along with the floor beneath it.
        // Drawing above native AO would paint bright specular light back over contact creases.
        ZIndex = AfterLightTargetOverlay.ContentZIndex;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args) => _owner.Prepare(args, gatherSources: false);

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture != null)
            _owner.DrawSurfaces(args, ScreenTexture);
    }
}

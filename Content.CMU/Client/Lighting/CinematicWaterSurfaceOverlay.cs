using Robust.Client.Graphics;
using Robust.Shared.Enums;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client.CMU14.Lighting;

/// <summary>After BelowFloor water, before carpets, furniture, corpses and every native actor layer.</summary>
public sealed class CinematicWaterSurfaceOverlay : Overlay
{
    private readonly CinematicLightingOverlay _owner;
    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    public CinematicWaterSurfaceOverlay(CinematicLightingOverlay owner)
    {
        _owner = owner;
        ZIndex = (int) DrawDepth.FloorTiles;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args) => _owner.Prepare(args, gatherSources: false);
    protected override void Draw(in OverlayDrawArgs args) => _owner.DrawWaterSurfaces(args);
}

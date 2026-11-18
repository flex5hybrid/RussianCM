using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.CMU14.Lighting;

/// <summary>Adds visible, occluded local irradiance to this viewport's native light buffer before world drawing.</summary>
public sealed class CinematicIrradianceOverlay : Overlay
{
    private readonly CinematicLightingOverlay _owner;
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;

    public CinematicIrradianceOverlay(CinematicLightingOverlay owner)
    {
        _owner = owner;
        ZIndex = -100;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args) => _owner.Prepare(args);
    protected override void Draw(in OverlayDrawArgs args) => _owner.DrawIrradiance(args);
}

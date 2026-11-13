using Content.Client._RMC14.Water;
using Content.Client.Graphics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.CMU14.Lighting;

public sealed partial class CinematicLightingSystem
{
    private void InitializeWater() =>
        SubscribeLocalEvent<SpriteComponent, BeforePostShaderRenderEvent>(OnWaterPostShader,
            after: [typeof(RMCWaterVisualsSystem)]);

    private void OnWaterPostShader(Entity<SpriteComponent> ent, ref BeforePostShaderRenderEvent args)
    {
        if (args.Id != ContentPostShaderIds.WaterSubmersion)
            return;
        var enabled = TryComp<RMCWaterVisualsComponent>(ent, out var visual)
            && visual.Water is { } receiver && _overlay?.BindWaterSubmersion(args, receiver) == true;
        // Native post-shaders outlive quality/viewport changes. Reset this even in Off,
        // debug, NV and secondary viewports, without retaining any owned texture there.
        args.Shader.SetParameter("cmu_water_enabled", enabled);
        if (!enabled)
        {
            args.Shader.SetParameter("water_reflection_texture", Texture.White);
            args.Shader.SetParameter("blocker_texture", Texture.White);
        }
    }
}

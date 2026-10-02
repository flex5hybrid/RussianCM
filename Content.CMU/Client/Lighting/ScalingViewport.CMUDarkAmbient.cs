using System;
using System.Numerics;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.Viewport;

public sealed partial class ScalingViewport
{
    private static readonly ProtoId<ShaderPrototype> CMUDarkAmbientShader = "CMUDarkAmbient";
    private static readonly ProtoId<ShaderPrototype> CMUDarkAmbientStencilShader = "CMUDarkAmbientStencil";

    private ShaderInstance? _cmuDarkAmbientShader;
    private ShaderInstance? _cmuDarkAmbientStencilShader;

    /// <summary>
    /// Enables the player's color grading preference for this viewport. Character previews and
    /// secondary viewports retain their original colors unless explicitly opted in.
    /// </summary>
    public bool CMUAllowDarkAmbient { get; set; }

    private bool ApplyCMUDarkAmbientShader(DrawingHandleScreen screen, Texture texture, bool stencil = false)
    {
        if (!CMUAllowDarkAmbient || !_cfg.GetCVar(CCVars.CMUDarkAmbientEnabled))
            return false;

        var intensity = Math.Clamp(_cfg.GetCVar(CCVars.CMUDarkAmbientIntensity), 0, 100) / 100f;
        if (intensity <= 0)
            return false;

        var shader = stencil
            ? (_cmuDarkAmbientStencilShader ??= _prototypeManager.Index(CMUDarkAmbientStencilShader).InstanceUnique())
            : (_cmuDarkAmbientShader ??= _prototypeManager.Index(CMUDarkAmbientShader).InstanceUnique());

        // The main image retains the existing unsharp mask; secondary Z composites were not sharpened.
        shader.SetParameter("Sharpness", ReferenceEquals(texture, _viewport?.RenderTarget.Texture) ? _sharpnessStrength : 0f);
        shader.SetParameter("PixelSize", new Vector2(1f / texture.Size.X, 1f / texture.Size.Y));
        shader.SetParameter("Intensity", intensity);
        screen.UseShader(shader);
        return true;
    }
}

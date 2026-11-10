using System;
using System.Numerics;
using Content.Client.Stylesheets;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.CMU14.Interface;

/// <summary>
///     Scanlines and optional convex-glass shading over live UI, with no render target involved.
/// </summary>
/// <remarks>
///     <para>
///     The cheap half of <see cref="CrtScreenControl"/>, for surfaces that want the raster but must
///     not be captured. Scanlines only darken pixels, so unlike the roll bar they need no copy of the
///     source to sample at an offset - and that is the whole point here. Capturing a live, scrolling,
///     constantly-rebuilt subtree such as the chat means its clipping and scroll offsets have to
///     survive a re-entrant render pass, and a stale or misaligned copy is drawn opaque over the real
///     thing, so it fails by showing the player old text rather than by throwing.
///     </para>
///     <para>
///     The raster is stationary while signal grain animates on both reading and glass surfaces.
///     Glass mode adds curved raster lines, edge shading and reflections. Both modes share the
///     existing CRT intensity, pitch, static and palette preferences.
///     </para>
/// </remarks>
public sealed partial class CrtScanlineOverlay : Control
{
    private static readonly ProtoId<ShaderPrototype> ShaderId = "CMUCrtScanlines";
    private static readonly ProtoId<ShaderPrototype> GlassShaderId = "CMUCrtHudGlass";

    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    private readonly ShaderInstance _shader;
    private ShaderInstance? _glassShader;
    private float _height;
    private float _pitch;
    public bool ChatSurface { get; set; }
    public bool MenuSurface { get; set; }
    public bool Glass { get; set; }
    /// <summary>Local readability adjustment; global CRT preferences still determine the effect.</summary>
    public float IntensityScale { get; set; } = 1f;

    public CrtScanlineOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypes.Index(ShaderId).InstanceUnique();

        MouseFilter = MouseFilterMode.Ignore;
        CanKeyboardFocus = false;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (CmuCrtCapture.Capturing)
            return;
        // Checked here every frame rather than trusted to callers, for the same reason
        // CrtScreenControl checks it: this is a CRT-theme effect and must never reach the base UI.
        if (!StyleNano.CrtUiEnabled || ChatSurface && !_cfg.GetCVar(CCVars.CMUChatCrtHaze))
            return;
        if (MenuSurface && !_cfg.GetCVar(CCVars.CMUCrtMenuEffect))
            return;

        var intensity = Math.Clamp(_cfg.GetCVar(CCVars.CMUCrtEffectIntensity), 0f, 1f)
            * Math.Clamp(IntensityScale, 0f, 1f)
            * (ChatSurface ? Math.Clamp(_cfg.GetCVar(CCVars.CMUChatCrtIntensity), 0, 1) : 1);
        if (intensity <= 0f)
            return;

        var size = PixelSize;
        if (size.X <= 0 || size.Y <= 0)
            return;

        // Below 2 the line and the gap stop resolving separately and the whole thing reads as a flat
        // darkening - the same floor the shader's `pitch` carries.
        var pitch = MathF.Max(_cfg.GetCVar(CCVars.CMUCrtEffectPitch), 2f);
        var phosphor = Color.FromSrgb(CrtTerminalPalette.Accent);
        var grain = Math.Clamp(_cfg.GetCVar(CCVars.CMUCrtEffectStatic), 0f, 1f);

        if (_height != size.Y)
        {
            _height = size.Y;
            _shader.SetParameter("height", _height);
        }

        if (_pitch != pitch)
        {
            _pitch = pitch;
            _shader.SetParameter("pitch", pitch);
        }

        var effect = _shader;
        if (Glass && size.X >= 120 && size.Y >= 96)
        {
            effect = _glassShader ??= _prototypes.Index(GlassShaderId).InstanceUnique();
            effect.SetParameter("screenSize", new Vector2(size.X, size.Y));
            effect.SetParameter("pitch", pitch);
            effect.SetParameter("curvature", Math.Clamp(_cfg.GetCVar(CCVars.CMUCrtEffectCurvature), 0f, 0.15f));
            effect.SetParameter("vignette", Math.Clamp(_cfg.GetCVar(CCVars.CMUCrtEffectVignette), 0f, 1f));
        }

        // Both reading surfaces and glass carry the signal texture. Glass only adds its geometry.
        effect.SetParameter("strength", Math.Clamp(intensity, 0f, 1f));
        effect.SetParameter("grain", grain);
        effect.SetParameter("phosphor", new Vector3(phosphor.R, phosphor.G, phosphor.B));

        var shader = handle.GetShader();
        handle.UseShader(effect);
        handle.DrawRect(new UIBox2(0, 0, size.X, size.Y), Color.White);
        handle.UseShader(shader);
    }
}

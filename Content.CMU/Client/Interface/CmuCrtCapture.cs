using System.Numerics;
using Content.Client.Stylesheets;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.CMU14.Interface;

/// <summary>A live, full-resolution CRT pass over a control and its complete subtree.</summary>
public sealed class CmuCrtCapture
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private IRenderTexture? _target;
    private ShaderInstance? _shader;
    private bool _failed;

    // Nested panels and authored terminals must not process the same pixels twice.
    public static bool Capturing { get; private set; }
    public bool Drawn { get; private set; }

    public CmuCrtCapture()
    {
        IoCManager.InjectDependencies(this);
    }

    public bool Draw(Control source, IRenderHandle render, bool enabled, bool chat, float scale)
    {
        Drawn = false;
        if (Capturing || CrtScreenControl.CapturingSource || _failed)
            return false;

        var strength = Math.Clamp(_cfg.GetCVar(CCVars.CMUCrtEffectIntensity) * scale
            * (chat ? _cfg.GetCVar(CCVars.CMUChatCrtIntensity) : 1f), 0f, 1f);
        if (!enabled || !StyleNano.CrtUiEnabled || strength <= 0
            || !(chat ? _cfg.GetCVar(CCVars.CMUChatCrtHaze) : _cfg.GetCVar(CCVars.CMUCrtMenuEffect)))
        {
            Release();
            return false;
        }

        var size = source.PixelSize;
        if (size.X <= 0 || size.Y <= 0)
            return false;

        var glass = _cfg.GetCVar(CCVars.CMUCrtDisplayEffect);
        var curvature = glass ? Math.Clamp(_cfg.GetCVar(CCVars.CMUCrtEffectCurvature), 0, 0.15f) * strength : 0;
        // SpriteView and custom storage/terminal renderers use global screen coordinates.
        // Capture in that same coordinate system; translating only the UI tree breaks their icons.
        var targetSize = source.Root?.PixelSize ?? size;
        var origin = source.GlobalPixelPosition;
        var handle = render.DrawingHandleScreen;
        var previousShader = handle.GetShader();
        var previousTransform = handle.GetTransform();
        var previousModulate = handle.Modulate;
        UIBox2i? scissor = null;
        var ancestorModulate = Color.White;
        for (var control = source; control != null; control = control.Parent)
        {
            if (control.RectClipContent)
                scissor = scissor == null ? control.GlobalPixelRect : scissor.Value.Intersection(control.GlobalPixelRect);
            if (control != source)
                ancestorModulate *= control.Modulate;
        }
        try
        {
            _shader ??= _prototypes.Index<ShaderPrototype>("CMUCrtDisplay").InstanceUnique();
            if (_target == null || _target.Size != targetSize)
            {
                Release();
                _target = _clyde.CreateRenderTarget(targetSize, RenderTargetColorFormat.Rgba8Srgb, name: "cmu-crt-display");
            }

            // Disable the screen-space scissor before the target is cleared, not just before
            // drawing: OpenGL also applies scissoring to framebuffer clears.
            render.SetScissor(null);
            handle.RenderInRenderTarget(_target, () =>
            {
                Capturing = true;
                try
                {
                    // RenderInRenderTarget changes projection, but retains the caller's UI origin
                    // and scissor. Both must be reset before drawing the source in target pixels.
                    render.SetScissor(null);
                    handle.SetTransform(Matrix3x2.Identity);
                    handle.UseShader(null);
                    _ui.RenderControl(render, source, origin);
                }
                finally
                {
                    Capturing = false;
                }
            }, Color.Black.WithAlpha(0f)); // Empty premultiplied pixels must have zero RGB.

            var hz = Math.Clamp(_cfg.GetCVar(CCVars.CMUCrtDisplayRefreshRate), 12, 60);
            _shader.SetParameter("strength", strength);
            _shader.SetParameter("pitch", MathF.Max(2, _cfg.GetCVar(CCVars.CMUCrtEffectPitch)));
            _shader.SetParameter("curvature", curvature);
            _shader.SetParameter("vignette", glass ? Math.Clamp(_cfg.GetCVar(CCVars.CMUCrtEffectVignette), 0, 1) : 0);
            _shader.SetParameter("grain", Math.Clamp(_cfg.GetCVar(CCVars.CMUCrtEffectStatic), 0, 1));
            _shader.SetParameter("samplePhase", (float) (Math.Floor(_timing.RealTime.TotalSeconds * hz) % 4096));
            _shader.SetParameter("screenSize", (Vector2) size);
            // Clyde converts screen regions to bottom-origin texture UVs in DrawTextureScreen.
            // Using the top-origin UI Y here clips everything except a strip of the panel.
            _shader.SetParameter("regionOrigin", TextureOrigin(origin, size, targetSize));
            _shader.SetParameter("regionSize", (Vector2) size / (Vector2) targetSize);
            handle.SetTransform(previousTransform);
            // The source's own modulation is already in the capture; ancestors apply once here.
            handle.Modulate = ancestorModulate;
            handle.UseShader(_shader);
            render.SetScissor(scissor);
            handle.DrawTextureRectRegion(_target.Texture, UIBox2.FromDimensions(Vector2.Zero, size),
                UIBox2.FromDimensions(origin, size));
            Drawn = true;
            return true;
        }
        catch (Exception e)
        {
            _failed = true;
            Release();
            Logger.ErrorS("cmu.crt", $"Full-interface CRT pass disabled: {e}");
            return false;
        }
        finally
        {
            handle.SetTransform(previousTransform);
            handle.Modulate = previousModulate;
            handle.UseShader(previousShader);
            render.SetScissor(scissor);
        }
    }

    public static Vector2 TextureOrigin(Vector2i origin, Vector2i size, Vector2i targetSize) => new(
        origin.X / (float) targetSize.X,
        1f - (origin.Y + size.Y) / (float) targetSize.Y);

    public void Release()
    {
        _target?.Dispose();
        _target = null;
    }
}

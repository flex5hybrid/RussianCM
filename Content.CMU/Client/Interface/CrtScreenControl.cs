using System.Numerics;
using Content.Client.Stylesheets;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Client.CMU14.Interface;

/// <summary>
///     Draws <see cref="Source"/> through the CRT shader - scanlines, crawling grain, and a roll bar
///     that shears the image sideways as it passes.
/// </summary>
/// <remarks>
///     <para>
///     The shear is why this renders the source into a texture first rather than laying an overlay on
///     top. An overlay can only add pixels over what is beneath it; it can never move them. Once the
///     UI is in a texture the shader can sample it at an offset, which is what produces the tear.
///     </para>
///     <para>
///     Sits after <see cref="Source"/> in a <see cref="CrtScreenContainer"/>. The container skips the
///     source's ordinary draw while this pass is active, retaining layout, input and live updates.
///     Other parents remain supported, but also draw the source normally beneath the shaded copy.
///     </para>
///     <para>
///     The terminal shader applies barrel curvature and vignette to the captured surface.
///     </para>
/// </remarks>
public sealed partial class CrtScreenControl : Control
{
    private const string ShaderId = "CMUCrtTerminal";


    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;

    private readonly ShaderInstance? _shader;


    /// <summary>Authored terminal passes supply their own raster; background materials must not double it.</summary>
    public static bool CapturingSource { get; private set; }

    /// <summary>The subtree to draw through the shader. Must precede this control in the tree.</summary>
    public Control? Source { get; set; }

    public Color Phosphor { get; set; } = Color.FromHex("#46FF8E");

    /// <summary>
    ///     Per-instance overrides; null falls through to the cvar. A menu wants a much quieter
    ///     effect than a prop terminal does - no bulge, no tearing, just the surface texture - while
    ///     still sharing the cvars that tune that texture so both move together.
    /// </summary>
    public float? Curvature { get; set; }

    public float? Vignette { get; set; }

    /// <summary>
    ///     Seconds between roll bars; null falls through to the cvar. Overridable for the same reason
    ///     as the two above: the shipped 19s is right for a surface being used and far too slow for
    ///     one being reviewed side by side with the other artifacts.
    /// </summary>
    public float? RollPeriod { get; set; }

    /// <summary>
    ///     Whether the roll bar runs. Off for anything being read continuously: a band that shears
    ///     the text sideways is characterful on a prop and obstructive on a settings page.
    /// </summary>
    public bool Roll { get; set; } = true;

    /// <summary>
    ///     Whether the animated grain runs. Off for anything being read continuously: per-pixel noise
    ///     redrawn every frame under a paragraph is constant motion in the worst place for it.
    ///     Scanlines are static and stay on either way - they are the part that reads as a tube.
    /// </summary>
    public bool Grain { get; set; } = true;


    /// <summary>
    ///     Which scheduled artifact fires: 0 cycles through them by a hash of the cycle index,
    ///     1 tear, 2 chroma split, 3 dropout. Forcing one is for the control kit, where each has to
    ///     be looked at in isolation rather than waited for.
    /// </summary>
    public enum ArtifactKind
    {
        Cycle = 0,
        Tear = 1,
        Chroma = 2,
        Dropout = 3,
    }

    /// <summary>
    ///     Strength of the scheduled artifacts, 0 for none. Left at 0 the surface behaves exactly as
    ///     it did before artifacts existed - the roll bar and grain are unaffected by this.
    /// </summary>
    public float ArtifactAmount { get; set; }

    /// <summary>Seconds between artifact events.</summary>
    public float ArtifactPeriod { get; set; } = 11f;

    /// <summary>How long one event lasts, in seconds.</summary>
    public float ArtifactSweep { get; set; } = 0.5f;

    public ArtifactKind Artifact { get; set; } = ArtifactKind.Cycle;

    /// <summary>False means the shader prototype did not resolve and nothing will ever draw.</summary>
    public bool ShaderLoaded => _shader != null;

    /// <summary>Set when the render-target path threw, so the console command can report why.</summary>
    public string? LastError { get; private set; }

    /// <summary>
    ///     Whether the pass draws at all right now. A surface that needs its scanlines either way -
    ///     a prop screen still looks like a tube with the theme off - draws its own when this is false.
    /// </summary>
    /// <remarks>
    ///     Checked here, every frame, rather than trusted to callers. This is a CRT-theme effect and
    ///     must never appear on the base UI - but each window gating it for itself means every new
    ///     caller has to remember, and has to subscribe to every cvar that could change the answer.
    ///     OptionsMenu did neither correctly: it watched CrtUiEnabled but only re-applied the
    ///     palette, so turning the theme off left the grain running on the settings window.
    /// </remarks>
    public bool Drawing => _shader != null
        && !CmuCrtCapture.Capturing
        && Source != null
        && Source.Visible
        && Visible
        && PixelSize.X > 0 && PixelSize.Y > 0
        && StyleNano.CrtUiEnabled
        && _cfg.GetCVar(CCVars.CMUCrtEffectIntensity) > 0f
        && _cfg.GetCVar(CCVars.CMUCrtMenuEffect);

    public CrtScreenControl()
    {
        IoCManager.InjectDependencies(this);

        MouseFilter = MouseFilterMode.Ignore;
        CanKeyboardFocus = false;

        if (_proto.TryIndex<ShaderPrototype>(ShaderId, out var proto))
            _shader = proto.InstanceUnique();
    }

    // Close() only removes the control from the tree, it never calls Dispose() - so the render
    // target has to be released here or it outlives every window that ever hosted this control.
    protected override void ExitedTree()
    {
        base.ExitedTree();
        _cmuCapture.Release();
    }

    private readonly CmuCrtCapture _cmuCapture = new();

    protected override void Draw(IRenderHandle renderHandle)
    {
        if (Source == null || !Drawing)
            return;
        LastError = _cmuCapture.Draw(Source, renderHandle, true, false, 1)
            ? null : "CRT capture unavailable";
    }

    /// <summary>
    ///     Converts an sRGB colour to linear for use as a shader uniform.
    /// </summary>
    /// <remarks>
    ///     <see cref="Color"/> components are sRGB. The render target is <c>Rgba8Srgb</c>, so the GPU
    ///     encodes linear to sRGB on write - handing sRGB components straight to the shader means
    ///     they get encoded a second time and publish far brighter than the colour chosen. Near
    ///     blacks suffer worst: 7/255 arriving as roughly 48/255 is the difference between invisible
    ///     and a pale band around the picture.
    /// </remarks>
    private static Vector3 Linear(Color srgb)
    {
        var linear = Color.FromSrgb(srgb);
        return new Vector3(linear.R, linear.G, linear.B);
    }
}

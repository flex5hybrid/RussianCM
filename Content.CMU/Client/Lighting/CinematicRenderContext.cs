using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.CMU14.Lighting;

/// <summary>Reusable pass state and delegates, rather than per-source/per-pass closures.</summary>
internal sealed class CinematicRenderContext
{
    // Color.Transparent is transparent WHITE in Robust. These targets are sampled as RGB data,
    // so invisible pixels must also have zero RGB or they become blockers / additive white light.
    public static readonly Color ClearColor = new(0f, 0f, 0f, 0f);
    // Radiance targets use native HDR RGB, with implicit destination alpha one. Clyde's
    // Add uses DstAlpha; an HDR RGBA target would amplify overlapping sources recursively.
    // Black with alpha one also makes diagnostic screenshots opaque.
    public static readonly Color RadianceClearColor = new(0f, 0f, 0f, 1f);
    private readonly Action _draw;
    private Action _pass = default!;
    private IRenderTexture _target = default!;
    private readonly IPrototypeManager _prototypes;
    private readonly IResourceCache _textures;
    private readonly Dictionary<string, Texture> _masks = new();
    private readonly Dictionary<ShaderInstance, FrameBinding> _frameBindings = new();
    private readonly Dictionary<ShaderInstance, SourceBinding> _sourceBindings = new();

    public IClydeViewport Viewport = default!;
    public DrawingHandleWorld Handle = default!;
    public Texture Scene = default!;
    public Box2Rotated Bounds;
    public MapId Map;
    public EntityUid MapUid;
    public Vector2 WorldOrigin;
    public Vector2 WorldX;
    public Vector2 WorldY;
    public readonly List<CinematicLightSource> Sources = new(65);
    public readonly HashSet<EntityUid> SeenMeshes = new();
    public CinematicLightingControls Controls = new();

    public CinematicRenderContext(IPrototypeManager prototypes, IResourceCache textures)
    {
        _prototypes = prototypes;
        _textures = textures;
        _draw = DrawPass;
    }

    public void Begin(in OverlayDrawArgs args, Texture scene)
    {
        Viewport = args.Viewport;
        Handle = args.WorldHandle;
        Scene = scene;
        Bounds = args.WorldBounds;
        Map = args.MapId;
        MapUid = args.MapUid;
        WorldOrigin = Viewport.LocalToWorld(new Vector2(0, Viewport.Size.Y)).Position;
        WorldX = Viewport.LocalToWorld((Vector2) Viewport.Size).Position - WorldOrigin;
        WorldY = Viewport.LocalToWorld(Vector2.Zero).Position - WorldOrigin;
    }

    public void Render(IRenderTexture target, Action pass, Color? clear = null)
    {
        _target = target;
        _pass = pass;
        Handle.RenderInRenderTarget(target, _draw, clear);
    }

    private void DrawPass()
    {
        var scale = Viewport.RenderScale * (_target.Size / (Vector2) Viewport.Size);
        Handle.SetTransform(_target.GetWorldToLocalMatrix(Viewport.Eye!, scale));
        _pass();
        Handle.UseShader(null);
    }

    public Vector2 TargetSize => _target.Size;

    public void SetWorldTransform(Matrix3x2 transform)
    {
        var scale = Viewport.RenderScale * (_target.Size / (Vector2) Viewport.Size);
        Handle.SetTransform(transform * _target.GetWorldToLocalMatrix(Viewport.Eye!, scale));
    }

    public void BindFrame(ShaderInstance shader)
    {
        var binding = new FrameBinding(_target.Size, WorldOrigin, WorldX, WorldY,
            Viewport.Eye!.Position.Position, Viewport.Eye.DrawFov, Viewport.FovRenderTarget.Texture);
        if (_frameBindings.TryGetValue(shader, out var previous) && previous == binding)
            return;
        _frameBindings[shader] = binding;
        shader.SetParameter("target_size", (Vector2) _target.Size);
        shader.SetParameter("world_origin", WorldOrigin);
        shader.SetParameter("world_x", WorldX);
        shader.SetParameter("world_y", WorldY);
        shader.SetParameter("eye_position", Viewport.Eye!.Position.Position);
        shader.SetParameter("draw_fov", Viewport.Eye.DrawFov);
        shader.SetParameter("fov_map", Viewport.FovRenderTarget.Texture);
    }

    public void BindSource(ShaderInstance shader, in CinematicLightSource source)
    {
        var emission = source.LuminousPosition ?? source.EmissionPosition;
        var binding = new SourceBinding(source.Position, emission, source.Radius,
            source.Color, source.Energy, source.Rotation, source.Mask);
        if (_sourceBindings.TryGetValue(shader, out var previous) && previous == binding)
            return;
        _sourceBindings[shader] = binding;
        shader.SetParameter("source_position", source.Position);
        shader.SetParameter("emission_position", emission);
        shader.SetParameter("source_radius", source.Radius);
        var linear = Color.FromSrgb(source.Color);
        shader.SetParameter("source_color", new Vector3(linear.R, linear.G, linear.B));
        shader.SetParameter("source_energy", source.Energy);
        shader.SetParameter("source_rotation", (float) source.Rotation.Theta);
        shader.SetParameter("has_light_mask", source.Mask != null);
        // An unused sampler must not retain a viewport texture that can be disposed on resize.
        var mask = Texture.White;
        if (source.Mask is { } maskId)
        {
            if (!_masks.TryGetValue(maskId.Id, out mask))
            {
                mask = _textures.GetResource<TextureResource>(_prototypes.Index(maskId).MaskPath).Texture;
                _masks.Add(maskId.Id, mask);
            }
        }
        shader.SetParameter("light_mask", mask);
    }

    private readonly record struct FrameBinding(Vector2i Size, Vector2 Origin, Vector2 X, Vector2 Y,
        Vector2 Eye, bool DrawFov, Texture Fov);
    private readonly record struct SourceBinding(Vector2 Position, Vector2 Emission, float Radius,
        Color Color, float Energy, Angle Rotation, ProtoId<LightMaskPrototype>? Mask);
}

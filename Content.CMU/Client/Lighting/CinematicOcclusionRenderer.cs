using System.Numerics;
using System.Runtime.InteropServices;
using Robust.Client.Graphics;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using static Content.Client.CMU14.Lighting.CinematicLightingGeometry;

namespace Content.Client.CMU14.Lighting;

/// <summary>Occluder mask for the bloom pyramid, and cached, exact source visibility meshes.</summary>
internal sealed class CinematicOcclusionRenderer : IDisposable
{
    private const int MaximumEdges = 8192;
    private readonly CinematicRenderContext _frame;
    private readonly OccluderSystem _occluders;
    private readonly SharedTransformSystem _transform;
    private readonly ShaderInstance _unshaded;
    private readonly Action _drawBlockers;
    private readonly Action _drawReceivers;
    private readonly CinematicShaderPool _receivers;
    private readonly ReceiverBinding?[] _receiverBindings = new ReceiverBinding?[64];
    private Configuration.CinematicLightingQuality _receiverQuality;
    private Texture _receiverEmission = default!;
    private readonly List<(int Start, int Count)> _polygons = new();
    private readonly List<Edge> _edges = new(2048);
    private readonly List<Vector2> _vertices = new(2048);
    private readonly List<ushort> _indices = new(4096);
    private readonly List<Edge> _localEdges = new(320);
    private readonly List<float> _angles = new(2048);
    private readonly Dictionary<EntityUid, Mesh> _meshes = new();
    private readonly List<EntityUid> _staleMeshes = new();
    private ulong _geometryHash;
    private bool _overflow;

    public int MeshRebuilds { get; private set; }
    public int EdgeCount => _edges.Count;

    public CinematicOcclusionRenderer(IEntityManager entities, IPrototypeManager prototypes, CinematicRenderContext frame)
    {
        _frame = frame;
        _occluders = entities.System<OccluderSystem>();
        _transform = entities.System<SharedTransformSystem>();
        _unshaded = prototypes.Index<ShaderPrototype>("unshaded").Instance();
        _drawBlockers = DrawBlockers;
        _drawReceivers = DrawReceivers;
        _receivers = new CinematicShaderPool(prototypes.Index<ShaderPrototype>("CMUCinematicWallReceiver"), 64);
    }

    public bool Update(MapId map, Box2 bounds)
    {
        _edges.Clear();
        _vertices.Clear();
        _indices.Clear();
        _polygons.Clear();
        _geometryHash = 14695981039346656037UL;
        _overflow = false;
        MeshRebuilds = 0;
        var state = this;
        _occluders.QueryAabb(ref state,
            static (ref CinematicOcclusionRenderer renderer, in ComponentTreeEntry<OccluderComponent> entry) =>
                renderer.AddOccluder(entry), map, bounds);
        return !_overflow;
    }

    private bool AddOccluder(in ComponentTreeEntry<OccluderComponent> entry)
    {
        var polygon = entry.Component.Polygon;
        if (!entry.Component.Enabled || polygon.Length < 3)
            return true;
        if (_edges.Count + polygon.Length > MaximumEdges)
        {
            _overflow = true;
            return false;
        }
        var matrix = _transform.GetWorldMatrix(entry.Transform);
        var start = _vertices.Count;
        _polygons.Add((start, polygon.Length));
        for (var i = 0; i < polygon.Length; i++)
        {
            var point = Vector2.Transform(polygon[i], matrix);
            _vertices.Add(point);
            _geometryHash = (_geometryHash ^ BitConverter.SingleToUInt32Bits(point.X)) * 1099511628211UL;
            _geometryHash = (_geometryHash ^ BitConverter.SingleToUInt32Bits(point.Y)) * 1099511628211UL;
        }
        var winding = 0f;
        for (var i = 0; i < polygon.Length; i++)
            winding += Cross(_vertices[start + i], _vertices[start + (i + 1) % polygon.Length]);
        for (var i = 0; i < polygon.Length; i++)
        {
            var a = _vertices[start + i];
            var b = _vertices[start + (i + 1) % polygon.Length];
            _edges.Add(winding >= 0f ? new Edge(a, b) : new Edge(b, a));
        }
        for (var i = 1; i < polygon.Length - 1; i++)
        {
            _indices.Add((ushort) start);
            _indices.Add((ushort) (start + i));
            _indices.Add((ushort) (start + i + 1));
        }
        return true;
    }

    public void RenderBlockers(IRenderTexture target) => _frame.Render(target, _drawBlockers, CinematicRenderContext.ClearColor);

    private void DrawBlockers()
    {
        _frame.Handle.UseShader(_unshaded);
        _frame.Handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList,
            CollectionsMarshal.AsSpan(_indices), CollectionsMarshal.AsSpan(_vertices), new Color(1f, 0f, 0f, 1f));
    }

    public void RenderReceivers(IRenderTexture target, Configuration.CinematicLightingQuality quality, Texture emission)
    {
        if (!_frame.Sources.Exists(static source => source.Profile.WallMounted && source.LuminousPosition != null))
            return;
        _receiverQuality = quality;
        _receiverEmission = emission;
        // Preserve the exact blocker R channel. G records only certified, visible source-facing skin.
        _frame.Render(target, _drawReceivers);
    }

    private void DrawReceivers()
    {
        for (var slot = 0; slot < _frame.Sources.Count; slot++)
        {
            var source = _frame.Sources[slot];
            if (!source.Profile.WallMounted || source.LuminousPosition is not { } point || source.LuminousSpan <= 0f)
                continue;
            var mesh = GetMesh(source, _receiverQuality);
            if (!mesh.Valid)
                continue;
            if (mesh.ReceiverHash != _geometryHash || mesh.ReceiverOrigin != source.Position
                || mesh.ReceiverPoint != point || mesh.ReceiverAxis != source.LuminousAxis
                || mesh.ReceiverSpan != source.LuminousSpan)
            {
                mesh.ReceiverHash = _geometryHash;
                mesh.ReceiverOrigin = source.Position;
                mesh.ReceiverPoint = point;
                mesh.ReceiverAxis = source.LuminousAxis;
                mesh.ReceiverSpan = source.LuminousSpan;
                var into = point - source.Position;
                var guarded = into.LengthSquared() > 0.0001f ? point + Vector2.Normalize(into) * 0.16f : point;
                mesh.ReceiverValid = ReceiverRouteIsSafe(source.Position, guarded, source.LuminousAxis,
                    source.LuminousSpan + 0.16f, CollectionsMarshal.AsSpan(_edges));
                foreach (var polygon in _polygons)
                {
                    if (PointInsidePolygon(source.Position,
                            CollectionsMarshal.AsSpan(_vertices).Slice(polygon.Start, polygon.Count)))
                    {
                        mesh.ReceiverValid = false;
                        break;
                    }
                }
            }
            if (!mesh.ReceiverValid)
                continue;
            var shader = _receivers.Get(slot);
            _frame.BindFrame(shader);
            shader.SetParameter("emission_texture", _receiverEmission);
            var binding = new ReceiverBinding(source.Position, point, source.LuminousAxis, source.LuminousSpan);
            if (_receiverBindings[slot] != binding)
            {
                _receiverBindings[slot] = binding;
                shader.SetParameter("face_origin", binding.Origin);
                shader.SetParameter("face_luminous", binding.Point);
                shader.SetParameter("face_axis", binding.Axis);
                shader.SetParameter("face_span", binding.Span);
            }
            _frame.Handle.UseShader(shader);
            var extent = new Vector2(source.LuminousSpan + 0.2f);
            var min = Vector2.Min(source.Position, point) - extent;
            var max = Vector2.Max(source.Position, point) + extent;
            _frame.Handle.DrawRect(new Box2(min, max), Color.White);
        }
    }

    public void BeginMeshes()
    {
        _frame.SeenMeshes.Clear();
    }

    public void EndMeshes()
    {
        _staleMeshes.Clear();
        foreach (var (uid, _) in _meshes)
        {
            if (!_frame.SeenMeshes.Contains(uid))
                _staleMeshes.Add(uid);
        }
        foreach (var uid in _staleMeshes)
            _meshes.Remove(uid);
    }

    public Mesh GetMesh(in CinematicLightSource source, Configuration.CinematicLightingQuality quality)
    {
        _frame.SeenMeshes.Add(source.Uid);
        if (!_meshes.TryGetValue(source.Uid, out var mesh))
        {
            mesh = new Mesh();
            _meshes.Add(source.Uid, mesh);
        }
        if (mesh.Hash == _geometryHash && mesh.Origin == source.Position && mesh.Radius == source.GeometryRadius)
            return mesh;
        MeshRebuilds++;
        mesh.Hash = _geometryHash;
        mesh.Origin = source.Position;
        mesh.Radius = source.GeometryRadius;
        mesh.Valid = Build(source.Position, source.GeometryRadius, CollectionsMarshal.AsSpan(_edges),
            quality.CircleSegments, quality.LocalEdges, _localEdges, _angles, mesh.Vertices);
        mesh.SurfaceDirection = mesh.Valid ? SurfaceDirection(CollectionsMarshal.AsSpan(mesh.Vertices)) : Vector2.Zero;
        return mesh;
    }

    public void DrawMesh(Mesh mesh)
    {
        if (mesh.Valid)
            _frame.Handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan,
                CollectionsMarshal.AsSpan(mesh.Vertices), Color.White);
    }

    public void Clear() => _meshes.Clear();
    public void Dispose()
    {
        _receivers.Dispose();
        Clear();
    }

    private readonly record struct ReceiverBinding(Vector2 Origin, Vector2 Point, Vector2 Axis, float Span);

    internal sealed class Mesh
    {
        public readonly List<Vector2> Vertices = new(256);
        public ulong Hash;
        public Vector2 Origin;
        public float Radius;
        public bool Valid;
        public Vector2 SurfaceDirection;
        public ulong ReceiverHash;
        public Vector2 ReceiverOrigin;
        public Vector2 ReceiverPoint;
        public Vector2 ReceiverAxis;
        public float ReceiverSpan;
        public bool ReceiverValid;
    }
}

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
internal sealed class CinematicOcclusionRenderer
{
    private const int MaximumEdges = 8192;
    private readonly CinematicRenderContext _frame;
    private readonly OccluderSystem _occluders;
    private readonly SharedTransformSystem _transform;
    private readonly ShaderInstance _unshaded;
    private readonly Action _drawBlockers;
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
    }

    public bool Update(MapId map, Box2 bounds)
    {
        _edges.Clear();
        _vertices.Clear();
        _indices.Clear();
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
        for (var i = 0; i < polygon.Length; i++)
        {
            var point = Vector2.Transform(polygon[i], matrix);
            _vertices.Add(point);
            _geometryHash = (_geometryHash ^ BitConverter.SingleToUInt32Bits(point.X)) * 1099511628211UL;
            _geometryHash = (_geometryHash ^ BitConverter.SingleToUInt32Bits(point.Y)) * 1099511628211UL;
        }
        for (var i = 0; i < polygon.Length; i++)
            _edges.Add(new Edge(_vertices[start + i], _vertices[start + (i + 1) % polygon.Length]));
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
            CollectionsMarshal.AsSpan(_indices), CollectionsMarshal.AsSpan(_vertices), Color.White);
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
        if (mesh.Hash == _geometryHash && mesh.Origin == source.Position && mesh.Radius == source.Radius)
            return mesh;
        MeshRebuilds++;
        mesh.Hash = _geometryHash;
        mesh.Origin = source.Position;
        mesh.Radius = source.Radius;
        mesh.Valid = Build(source.Position, source.Radius, CollectionsMarshal.AsSpan(_edges),
            quality.CircleSegments, quality.LocalEdges, _localEdges, _angles, mesh.Vertices);
        return mesh;
    }

    public void DrawMesh(Mesh mesh)
    {
        if (mesh.Valid)
            _frame.Handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan,
                CollectionsMarshal.AsSpan(mesh.Vertices), Color.White);
    }

    public void Clear() => _meshes.Clear();

    internal sealed class Mesh
    {
        public readonly List<Vector2> Vertices = new(256);
        public ulong Hash;
        public Vector2 Origin;
        public float Radius;
        public bool Valid;
    }
}

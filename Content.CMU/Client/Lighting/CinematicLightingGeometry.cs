using System.Numerics;

namespace Content.Client.CMU14.Lighting;

/// <summary>
/// Visibility polygons from the actual, transformed occluder edges. Every endpoint is sampled on
/// both sides, so thin diagonal obstacles clip the source instead of falling between regular rays.
/// No physics raycasts, shadow-light slots, readbacks, or gameplay visibility changes are involved.
/// </summary>
public static class CinematicLightingGeometry
{
    public readonly record struct Edge(Vector2 A, Vector2 B);

    /// <summary>Match native noRot and cardinal RSI rendering when placing authored sprite-local effects.</summary>
    public static Angle RenderedRotation(Angle world, Angle eye, bool noRotation, bool snapCardinals)
    {
        if (noRotation)
            return -eye;
        var cardinal = snapCardinals ? (world + eye).Reduced().FlipPositive().RoundToCardinalAngle() : Angle.Zero;
        return world - cardinal;
    }

    public static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

    public static float RayDistance(Vector2 origin, Vector2 direction, in Edge edge, float maximum)
    {
        var segment = edge.B - edge.A;
        var determinant = Cross(direction, segment);
        if (MathF.Abs(determinant) < 0.000001f)
            return maximum;

        var offset = edge.A - origin;
        var distance = Cross(offset, segment) / determinant;
        var along = Cross(offset, direction) / determinant;
        return distance >= 0f && distance < maximum && along >= 0f && along <= 1f
            ? distance
            : maximum;
    }

    public static float DistanceSquared(Vector2 point, in Edge edge)
    {
        var delta = edge.B - edge.A;
        var length = delta.LengthSquared();
        var t = length > 0f ? Math.Clamp(Vector2.Dot(point - edge.A, delta) / length, 0f, 1f) : 0f;
        return Vector2.DistanceSquared(point, edge.A + delta * t);
    }

    public static bool PointInsidePolygon(Vector2 point, ReadOnlySpan<Vector2> polygon)
    {
        var inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            var a = polygon[i];
            var b = polygon[j];
            if ((a.Y > point.Y) != (b.Y > point.Y)
                && point.X < (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X)
                inside = !inside;
        }
        return inside;
    }

    /// <summary>A luminous wall face may enter the solid union, never exit its far face. Edges are CCW.</summary>
    public static bool ReceiverRouteIsSafe(Vector2 origin, Vector2 luminous, Vector2 axis, float span,
        ReadOnlySpan<Edge> edges)
    {
        if (!float.IsFinite(span) || span <= 0f || span > 1f
            || Vector2.DistanceSquared(origin, luminous) > 0.75f * 0.75f)
            return false;
        var hits = new List<ReceiverHit>(32);
        for (var sample = -1; sample <= 1; sample++)
        {
            hits.Clear();
            var delta = luminous + axis * (span * sample) - origin;
            var length = delta.Length();
            if (!float.IsFinite(length))
                return false;
            if (length < 0.001f)
                continue;
            var direction = delta / length;
            foreach (var edge in edges)
            {
                var hit = RayDistance(origin, direction, edge, length);
                if (hit < length - 0.001f)
                    hits.Add(new ReceiverHit(hit, Cross(direction, edge.B - edge.A) < 0f ? 1 : -1));
            }
            hits.Sort(static (a, b) => a.Distance.CompareTo(b.Distance));
            var coverage = 0;
            for (var i = 0; i < hits.Count;)
            {
                var end = i + 1;
                var change = hits[i].Change;
                while (end < hits.Count && MathF.Abs(hits[end].Distance - hits[i].Distance) < 0.0001f)
                {
                    change += hits[end].Change;
                    end++;
                }
                var next = coverage + change;
                // Shared seams have zero net change. Overlapping solids raise coverage;
                // duplicate polygons must never cancel each other into an apparent opening.
                if (next < 0 || coverage > 0 && next == 0)
                    return false;
                coverage = next;
                i = end;
            }
        }
        return true;
    }

    private readonly record struct ReceiverHit(float Distance, int Change);

    /// <summary>On overflow, callers must omit the source rather than use an incomplete mask.</summary>
    public static bool Build(
        Vector2 origin,
        float radius,
        ReadOnlySpan<Edge> edges,
        int circleSegments,
        int edgeBudget,
        List<Edge> localEdges,
        List<float> angles,
        List<Vector2> vertices)
    {
        vertices.Clear();
        localEdges.Clear();
        angles.Clear();
        if (!float.IsFinite(radius) || radius <= 0f || circleSegments < 3)
            return false;

        foreach (ref readonly var edge in edges)
        {
            if (DistanceSquared(origin, edge) > radius * radius)
                continue;
            if (localEdges.Count >= edgeBudget)
                return false;
            localEdges.Add(edge);
            AddAngles(edge.A - origin, angles);
            AddAngles(edge.B - origin, angles);
        }

        for (var i = 0; i < circleSegments; i++)
            angles.Add(-MathF.PI + MathF.Tau * i / circleSegments);
        angles.Sort();

        vertices.Add(origin);
        foreach (var angle in angles)
        {
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            var distance = radius;
            foreach (var edge in localEdges)
                distance = RayDistance(origin, direction, edge, distance);
            vertices.Add(origin + direction * MathF.Max(0f, distance - 0.002f));
        }

        vertices.Add(vertices[1]);
        return true;
    }

    private static void AddAngles(Vector2 delta, List<float> angles)
    {
        var angle = MathF.Atan2(delta.Y, delta.X);
        angles.Add(WrapAngle(angle - 0.0001f));
        angles.Add(angle);
        angles.Add(WrapAngle(angle + 0.0001f));
    }

    /// <summary>Direction into the open surface, from the area centroid of the clipped visibility fan.</summary>
    public static Vector2 SurfaceDirection(ReadOnlySpan<Vector2> vertices)
    {
        if (vertices.Length < 4)
            return Vector2.Zero;
        var origin = vertices[0];
        var moment = Vector2.Zero;
        var area = 0f;
        for (var i = 1; i < vertices.Length - 1; i++)
        {
            var a = vertices[i] - origin;
            var b = vertices[i + 1] - origin;
            var weight = MathF.Abs(Cross(a, b));
            moment += (a + b) * weight;
            area += weight;
        }
        if (area < 0.00001f)
            return Vector2.Zero;
        var centroid = moment / (area * 3f);
        return centroid.LengthSquared() > 0.0001f ? Vector2.Normalize(centroid) : Vector2.Zero;
    }

    private static float WrapAngle(float angle) => angle < -MathF.PI ? angle + MathF.Tau
        : angle >= MathF.PI ? angle - MathF.Tau : angle;

    public static float Pulse(float age, float duration)
    {
        if (!float.IsFinite(age) || !float.IsFinite(duration) || duration <= 0f || age < 0f || age >= duration)
            return 0f;
        var remaining = 1f - age / duration;
        return remaining * remaining;
    }
}

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

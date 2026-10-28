using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Client.CMU14.Lighting;
using Content.Client.CMU14.Lighting.Configuration;
using NUnit.Framework;
using static Content.Client.CMU14.Lighting.CinematicLightingGeometry;

namespace Content.Tests.Client.CMU14.Lighting;

[TestFixture]
public sealed class CinematicLightingGeometryTest
{
    [TestCase(0f, 0.8f, 0f)]
    [TestCase(0.5f, 0.8f, 0.4f)]
    [TestCase(1f, 0f, 0f)]
    [TestCase(4f, 0.8f, 0.8f)]
    public void UserAtmosphereControlAlsoAppliesToAuthoredRooms(float user, float room, float expected)
    {
        Assert.That(CinematicAtmosphereRenderer.CombineDensity(user, room), Is.EqualTo(expected).Within(0.0001f));
    }

    [Test]
    public void RayStopsAtTheFirstOpaqueSurface()
    {
        var wall = new Edge(new Vector2(1, -1), new Vector2(1, 1));
        Assert.That(RayDistance(Vector2.Zero, Vector2.UnitX, wall, 5f), Is.EqualTo(1f));
        Assert.That(RayDistance(Vector2.Zero, -Vector2.UnitX, wall, 5f), Is.EqualTo(5f));
        Assert.That(RayDistance(Vector2.Zero, Vector2.UnitY, wall, 5f), Is.EqualTo(5f));
    }

    [Test]
    public void NarrowOccluderCannotFallBetweenRegularRays()
    {
        var wall = new Edge(new Vector2(1, -0.003f), new Vector2(1, 0.003f));
        var vertices = BuildMesh([wall]);
        Assert.That(Contains(vertices, new Vector2(2, 0)), Is.False);
        Assert.That(Contains(vertices, new Vector2(0.5f, 0)), Is.True);
        Assert.That(Contains(vertices, new Vector2(2, 0.1f)), Is.True);
    }

    [Test]
    public void DoorAperturePassesLightWhileDoorFrameStopsIt()
    {
        Edge[] frame =
        [
            new(new Vector2(1, -4), new Vector2(1, -0.25f)),
            new(new Vector2(1, 0.25f), new Vector2(1, 4)),
        ];
        var open = BuildMesh(frame);
        Assert.That(Contains(open, new Vector2(2, 0)), Is.True);
        Assert.That(Contains(open, new Vector2(2, 1)), Is.False);
        var closed = BuildMesh([..frame, new Edge(new Vector2(1, -0.25f), new Vector2(1, 0.25f))]);
        Assert.That(Contains(closed, new Vector2(2, 0)), Is.False);
    }

    [Test]
    public void PolygonClippingWorksAcrossTheAngleSeam()
    {
        var wall = new Edge(new Vector2(-1, -1), new Vector2(-1, 1));
        var vertices = BuildMesh([wall]);
        Assert.That(Contains(vertices, new Vector2(-2, 0)), Is.False);
        Assert.That(Contains(vertices, new Vector2(-0.5f, 0)), Is.True);
        Assert.That(Contains(vertices, new Vector2(2, 0)), Is.True);
    }

    [Test]
    public void RotatedGridGeometryRetainsItsShadow()
    {
        var matrix = Matrix3x2.CreateRotation(0.7f) * Matrix3x2.CreateTranslation(200, -130);
        var origin = Vector2.Transform(Vector2.Zero, matrix);
        var wall = new Edge(Vector2.Transform(new Vector2(1, -1), matrix),
            Vector2.Transform(new Vector2(1, 1), matrix));
        var vertices = BuildMesh([wall], origin);
        Assert.That(Contains(vertices, Vector2.Transform(new Vector2(2, 0), matrix)), Is.False);
        Assert.That(Contains(vertices, Vector2.Transform(new Vector2(0.5f, 0), matrix)), Is.True);
    }

    [Test]
    public void GeometryBudgetOverflowFailsClosed()
    {
        Edge[] walls = [new(new Vector2(1, -1), new Vector2(1, 1)), new(new Vector2(2, -1), new Vector2(2, 1))];
        var vertices = new List<Vector2>();
        Assert.That(Build(Vector2.Zero, 5f, walls, 24, 1, new List<Edge>(), new List<float>(), vertices), Is.False);
        Assert.That(vertices, Is.Empty);
    }

    [TestCase(0f)]
    [TestCase(-1f)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    public void InvalidRadiusHasNoVisibilityMesh(float radius)
    {
        Assert.That(Build(Vector2.Zero, radius, [], 24, 12, new List<Edge>(), new List<float>(),
            new List<Vector2>()), Is.False);
    }

    [Test]
    public void ReactivePulseHasFiniteLifetimeAndSmoothDecay()
    {
        Assert.That(Pulse(-0.1f, 0.5f), Is.Zero);
        Assert.That(Pulse(0f, 0.5f), Is.EqualTo(1f));
        Assert.That(Pulse(0.25f, 0.5f), Is.EqualTo(0.25f));
        Assert.That(Pulse(0.5f, 0.5f), Is.Zero);
        Assert.That(Pulse(1f, 0.5f), Is.Zero);
        Assert.That(Pulse(float.NaN, 0.5f), Is.Zero);
    }

    [Test]
    public void PresetsBoundEveryExpensiveFeatureAndOffHasNoBudget()
    {
        Assert.That(CinematicLightingQuality.For(0), Is.EqualTo(default(CinematicLightingQuality)));
        Assert.That(CinematicLightingQuality.For(-100), Is.EqualTo(default(CinematicLightingQuality)));
        var low = CinematicLightingQuality.For(1);
        var high = CinematicLightingQuality.For(2);
        var ultra = CinematicLightingQuality.For(3);
        Assert.That(low.SurfaceEffects, Is.False);
        Assert.That(low.AtmosphericSources, Is.Zero);
        Assert.That(high.SurfaceEffects, Is.True);
        Assert.That(high.Sources, Is.LessThan(ultra.Sources));
        Assert.That(high.AtmosphericSources, Is.LessThan(ultra.AtmosphericSources));
        Assert.That(high.BloomLevels, Is.LessThan(ultra.BloomLevels));
        Assert.That(high.LocalEdges, Is.LessThan(ultra.LocalEdges));
        Assert.That(CinematicLightingQuality.For(int.MaxValue), Is.EqualTo(ultra));
    }

    private static List<Vector2> BuildMesh(Edge[] edges, Vector2 origin = default)
    {
        var vertices = new List<Vector2>();
        Assert.That(Build(origin, 5f, edges, 24, 320, new List<Edge>(), new List<float>(), vertices), Is.True);
        return vertices;
    }

    private static bool Contains(List<Vector2> vertices, Vector2 point)
    {
        for (var i = 1; i < vertices.Count - 1; i++)
        {
            if (MathF.Abs(Cross(vertices[i] - vertices[0], vertices[i + 1] - vertices[0])) < 0.00000001f)
                continue;
            var a = Cross(vertices[i] - vertices[0], point - vertices[0]);
            var b = Cross(vertices[i + 1] - vertices[i], point - vertices[i]);
            var c = Cross(vertices[0] - vertices[i + 1], point - vertices[i + 1]);
            if (a >= -0.000001f && b >= -0.000001f && c >= -0.000001f)
                return true;
        }
        return false;
    }
}

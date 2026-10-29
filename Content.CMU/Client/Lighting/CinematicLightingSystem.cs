using System.Numerics;
using Content.Client.CMU14.Lighting.Configuration;
using Content.Client.Power.Components;
using Content.Client.Viewport;
using Content.Shared.CCVar;
using Content.Shared.CMU14.Lighting;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Configuration;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client.CMU14.Lighting;

/// <summary>Owns only presentation resources. Never writes to gameplay lights or visibility.</summary>
public sealed partial class CinematicLightingSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IOverlayManager _overlays = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private SharedLightTreeSystem _lightTree = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private CinematicReactiveLightSystem _reactive = default!;

    private readonly HashSet<Entity<SharedPointLightComponent, TransformComponent>> _lights = new();
    private readonly HashSet<Entity<CMUCinematicEmitterComponent>> _equipment = new();
    private readonly HashSet<EntityUid> _seen = new();
    private long? _activeViewport;
    private CinematicLightingOverlay? _overlay;
    private CinematicSurfaceLightingOverlay? _surfaceOverlay;
    private CinematicWaterSurfaceOverlay? _waterOverlay;
    private CinematicIrradianceOverlay? _irradianceOverlay;
    public CinematicLightingQuality Quality { get; private set; }
    internal bool NormalsEnabled { get; private set; }
    public CinematicLightingStatistics LastFrame { get; internal set; }
    internal CinematicLightingControls Controls { get; } = new();
    internal IReadOnlyList<CinematicLightSource> Sources => _overlay?.Sources ?? Array.Empty<CinematicLightSource>();
    internal CinematicRenderContext? CaptureFrame => _overlay?.CaptureFrame;

    internal void CaptureDebug(Action<string, Image<Rgba32>> capture) =>
        _overlay?.CaptureDebug(capture);

    public override void Initialize()
    {
        base.Initialize();
        InitializeWater();
        Subs.CVar(_config, CCVars.CMUCinematicLighting, OnQualityChanged, true);
        Subs.CVar(_config, CCVars.CMUCinematicNormals, OnNormalsChanged, true);
    }

    private void OnNormalsChanged(bool enabled)
    {
        if (NormalsEnabled == enabled)
            return;
        NormalsEnabled = enabled;
        // Invalidate viewport-local render targets and shader references atomically.
        OnQualityChanged(_config.GetCVar(CCVars.CMUCinematicLighting));
    }

    private void OnQualityChanged(int value)
    {
        Quality = CinematicLightingQuality.For(value);
        RemoveOverlay();
        _reactive.Clear();
        if (Quality.Level != 0)
        {
            _overlay = new CinematicLightingOverlay(EntityManager, this);
            _overlays.AddOverlay(_overlay);
            if (Quality.SurfaceEffects)
            {
                _irradianceOverlay = new CinematicIrradianceOverlay(_overlay);
                _overlays.AddOverlay(_irradianceOverlay);
                _surfaceOverlay = new CinematicSurfaceLightingOverlay(_overlay);
                _overlays.AddOverlay(_surfaceOverlay);
                _waterOverlay = new CinematicWaterSurfaceOverlay(_overlay);
                _overlays.AddOverlay(_waterOverlay);
            }
        }
        LastFrame = default;
    }

    public override void Shutdown()
    {
        RemoveOverlay();
        _activeViewport = null;
        base.Shutdown();
    }

    private void RemoveOverlay()
    {
        if (_waterOverlay != null)
        {
            _overlays.RemoveOverlay(_waterOverlay);
            _waterOverlay.Dispose();
            _waterOverlay = null;
        }
        if (_irradianceOverlay != null)
        {
            _overlays.RemoveOverlay(_irradianceOverlay);
            _irradianceOverlay.Dispose();
            _irradianceOverlay = null;
        }
        if (_surfaceOverlay != null)
        {
            _overlays.RemoveOverlay(_surfaceOverlay);
            _surfaceOverlay.Dispose();
            _surfaceOverlay = null;
        }
        if (_overlay == null)
            return;
        // OverlayManager.RemoveOverlay does not call Dispose in this engine revision.
        _overlays.RemoveOverlay(_overlay);
        _overlay.Dispose();
        _overlay = null;
    }

    public ViewportScope BeginViewport(IClydeViewport viewport)
    {
        var previous = _activeViewport;
        _activeViewport = viewport.Id;
        return new ViewportScope(this, previous);
    }

    public bool IsEligible(IClydeViewport viewport, ILightManager lights)
    {
        var eye = viewport.Eye;
        return Quality.Level > 0 && _activeViewport == viewport.Id && eye is { DrawLight: true }
            && lights.Enabled && lights.DrawLighting
            && (eye is not ScalingViewport.ZEye zEye || zEye.Depth == 0);
    }

    internal void CollectSources(MapId map, Box2 bounds, IEye eye, List<CinematicLightSource> sources)
    {
        sources.Clear();
        _seen.Clear();
        _lights.Clear();
        _lightTree.QueryAabb(_lights, map, bounds);
        foreach (var light in _lights)
        {
            if (TryComp<CMUCinematicEmitterComponent>(light.Owner, out var emitter))
                AddSource(light.Owner, emitter, light.Comp2, light.Comp1, map, bounds, eye, sources);
        }

        // Non-light emissive screens use the spatial lookup, never a global entity enumeration.
        _equipment.Clear();
        _lookup.GetEntitiesIntersecting(map, bounds, _equipment);
        foreach (var entity in _equipment)
        {
            if (_seen.Contains(entity.Owner) || !TryComp<TransformComponent>(entity, out var xform))
                continue;
            TryComp<PointLightComponent>(entity, out var light);
            AddSource(entity, entity.Comp, xform, light, map, bounds, eye, sources);
        }
    }

    private void AddSource(
        EntityUid uid,
        CMUCinematicEmitterComponent emitter,
        TransformComponent xform,
        SharedPointLightComponent? light,
        MapId map,
        Box2 bounds,
        IEye eye,
        List<CinematicLightSource> sources)
    {
        if (!_seen.Add(uid) || !emitter.Enabled || emitter.Intensity <= 0f || xform.MapID != map
            || !_prototypes.TryIndex(emitter.Profile, out var profile))
            return;

        if (profile.MountProfiles.TryGetValue(xform.LocalRotation.GetCardinalDir(), out var mountedId)
            && _prototypes.TryIndex(mountedId, out var mounted))
            profile = mounted;

        if (light is null && profile.RequireLight || light is { Enabled: false } or { ContainerOccluded: true }
            || light is { Energy: <= 0f } or { Radius: <= 0f })
            return;

        if (profile.RequirePower && TryComp<ApcPowerReceiverComponent>(uid, out var power)
            && (power.PowerDisabled || power.NeedsPower && !power.Powered))
            return;

        TryComp<SpriteComponent>(uid, out var sprite);
        if (sprite != null)
        {
            if (!sprite.Visible || sprite.Color.A <= 0.01f)
                return;
            if (sprite.ContainerOccluded)
            {
                // Hands may hide the standalone item sprite while explicitly passing its light.
                // Retain that native light, but never draw a second item sprite over the actor.
                if (light == null || light.ContainerOccluded)
                    return;
                sprite = null;
            }
        }

        // Do not let an opt-in item disclose a hidden wearer, ghost, or special visibility layer.
        var current = uid;
        for (var depth = 0; depth < 16; depth++)
        {
            if (TryComp<MetaDataComponent>(current, out var metadata) && metadata.VisibilityMask != 1)
                return;
            if (TryComp<SpriteComponent>(current, out var parentSprite)
                && (!parentSprite.Visible || parentSprite.Color.A <= 0.01f))
                return;
            if (profile.FollowParentLight && current != uid
                && TryComp<PointLightComponent>(current, out var parentLight)
                && (!parentLight.Enabled || parentLight.ContainerOccluded))
                return;
            if (profile.RequirePower && TryComp<ApcPowerReceiverComponent>(current, out var parentPower)
                && (parentPower.PowerDisabled || parentPower.NeedsPower && !parentPower.Powered))
                return;
            if (!TryComp<TransformComponent>(current, out var parent) || parent.ParentUid == EntityUid.Invalid
                || parent.ParentUid == xform.GridUid || parent.ParentUid == xform.MapUid)
                break;
            current = parent.ParentUid;
        }

        var (position, rotation) = _transform.GetWorldPositionRotation(xform);
        var entityPosition = position;
        if (light != null)
        {
            var offset = rotation.RotateVec(light.Offset);
            if (profile.FollowParentLight && TryComp<PointLightComponent>(xform.ParentUid, out var fixture)
                && TryComp<TransformComponent>(xform.ParentUid, out var fixtureXform))
                offset += _transform.GetWorldRotation(fixtureXform).RotateVec(fixture.Offset);
            position += offset;
            // Preserve Clyde's exact point. RMC map fixtures can be just 0.005m outside a
            // wall face; a blind "outward" bias can move them into the adjacent wall.
        }
        // RMC wall fixtures move their sprite independently of the physical light point.
        // Extract bloom at the rendered sprite, while spill still originates at the native light.
        var emissionPosition = position;
        if (sprite != null && !profile.ProceduralEmission)
            emissionPosition = entityPosition + (sprite.NoRotation ? -eye.Rotation : rotation).RotateVec(sprite.Offset);
        emissionPosition += rotation.RotateVec(emitter.EmissionOffset);
        if (!bounds.Enlarged(Math.Clamp(profile.Radius, 0.15f, 7f)).Contains(position))
            return;

        var pulse = _reactive.GetPulse(emitter, profile);
        var energy = Math.Clamp(light?.Energy ?? 1f, 0f, 3f) * Math.Clamp(emitter.Intensity, 0f, 3f) * pulse;
        if (!float.IsFinite(energy) || energy <= 0.001f)
            return;

        var radius = Math.Clamp(MathF.Min(light?.Radius ?? profile.Radius, profile.Radius), 0.15f, 7f);
        var targetColor = light?.Color ?? profile.Color;
        var saturation = MathF.Max(targetColor.R, MathF.Max(targetColor.G, targetColor.B))
            - MathF.Min(targetColor.R, MathF.Min(targetColor.G, targetColor.B));
        var tint = profile.Tint * (1f - Math.Clamp((saturation - 0.08f) / 0.3f, 0f, 1f));
        // A blue lamp or saturated red/blue alert is not turned into a warm industrial fixture.
        if (targetColor.B > targetColor.R + 0.035f && profile.Color.R > profile.Color.B)
            tint = 0f;
        targetColor = new Color(MathHelper.Lerp(targetColor.R, profile.Color.R, tint),
            MathHelper.Lerp(targetColor.G, profile.Color.G, tint),
            MathHelper.Lerp(targetColor.B, profile.Color.B, tint), targetColor.A);
        var color = _reactive.GetColor(uid, targetColor);
        var maskRotation = light?.Rotation ?? Angle.Zero;
        if (light is { MaskAutoRotate: true })
            maskRotation += rotation;
        var distance = Vector2.DistanceSquared(position, eye.Position.Position + eye.Offset);
        var score = profile.Priority * 100f + energy * radius / (1f + distance * 0.1f);
        var source = new CinematicLightSource(uid, position, emissionPosition, radius, color, energy,
            maskRotation, light?.LightMask, profile, score, xform.GridUid, sprite, entityPosition, rotation,
            light?.Color ?? Color.Black, light?.Radius ?? 0f, light?.Energy ?? 0f,
            light?.Falloff ?? 1f, light?.CurveFactor ?? 1f);

        // Insertion into a bounded array gives stable priority and avoids sorting an unbounded light set.
        var index = sources.Count;
        while (index > 0 && (sources[index - 1].Score < score
            || sources[index - 1].Score == score && sources[index - 1].Uid.Id > uid.Id))
            index--;
        if (index >= Quality.Sources)
            return;
        sources.Insert(index, source);
        if (sources.Count > Quality.Sources)
            sources.RemoveAt(sources.Count - 1);
    }

    public readonly struct ViewportScope : IDisposable
    {
        private readonly CinematicLightingSystem? _system;
        private readonly long? _previous;
        internal ViewportScope(CinematicLightingSystem system, long? previous)
        {
            _system = system;
            _previous = previous;
        }
        public void Dispose()
        {
            if (_system != null)
                _system._activeViewport = _previous;
        }
    }
}

internal readonly record struct CinematicLightSource(
    EntityUid Uid,
    Vector2 Position,
    Vector2 EmissionPosition,
    float Radius,
    Color Color,
    float Energy,
    Angle Rotation,
    ProtoId<LightMaskPrototype>? Mask,
    CMUCinematicLightProfilePrototype Profile,
    float Score,
    EntityUid? Grid,
    SpriteComponent? Sprite,
    Vector2 EntityPosition,
    Angle EntityRotation,
    Color NativeColor,
    float NativeRadius,
    float NativeEnergy,
    float NativeFalloff,
    float NativeCurveFactor)
{
    // Native replacement is bounded to Radius too. No pass samples the discarded outer ring.
    public float GeometryRadius => Radius;
    public Vector2? LuminousPosition { get; init; }
    public Vector2 LuminousAxis { get; init; }
    public float LuminousSpan { get; init; }
}

public readonly record struct CinematicLightingStatistics(
    int Quality,
    int Sources,
    int AtmosphereSources,
    int OccluderEdges,
    int MeshRebuilds,
    int Targets,
    long TargetBytes,
    double CpuMilliseconds);

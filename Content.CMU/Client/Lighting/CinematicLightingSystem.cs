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
    public CinematicLightingQuality Quality { get; private set; }
    public CinematicLightingStatistics LastFrame { get; internal set; }

    internal void CaptureDebug(Action<string, Image<Rgba32>> capture) =>
        _overlay?.CaptureDebug(capture);

    public override void Initialize()
    {
        base.Initialize();
        Subs.CVar(_config, CCVars.CMUCinematicLighting, OnQualityChanged, true);
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

        if (light is null && profile.RequireLight || light is { Enabled: false } or { ContainerOccluded: true }
            || light is { Energy: <= 0f } or { Radius: <= 0f })
            return;

        if (profile.RequirePower && TryComp<ApcPowerReceiverComponent>(uid, out var power)
            && (power.PowerDisabled || power.NeedsPower && !power.Powered))
            return;

        if (TryComp<SpriteComponent>(uid, out var sprite)
            && (!sprite.Visible || sprite.ContainerOccluded || sprite.Color.A <= 0.01f))
            return;

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
                && TryComp<PointLightComponent>(current, out var parentLight) && !parentLight.Enabled)
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
            // Native wall fixtures sit 0.005m inside the wall edge. Move the cosmetic origin
            // outward by half a world pixel so exact geometric clipping starts on the room side.
            if (offset.LengthSquared() > 0.01f)
                position += Vector2.Normalize(offset) * 0.016f;
        }
        // RMC wall fixtures move their sprite independently of the physical light point.
        // Extract bloom at the rendered sprite, while spill still originates at the native light.
        var emissionPosition = position;
        if (sprite != null && !profile.ProceduralEmission)
            emissionPosition = entityPosition + (sprite.NoRotation ? -eye.Rotation : rotation).RotateVec(sprite.Offset);
        emissionPosition += rotation.RotateVec(emitter.EmissionOffset);
        if (!bounds.Enlarged(0.5f).Contains(emissionPosition))
            return;

        var pulse = _reactive.GetPulse(emitter, profile);
        var energy = Math.Clamp(light?.Energy ?? 1f, 0f, 3f) * Math.Clamp(emitter.Intensity, 0f, 3f) * pulse;
        if (!float.IsFinite(energy) || energy <= 0.001f)
            return;

        var radius = Math.Clamp(MathF.Min(light?.Radius ?? profile.Radius, profile.Radius), 0.15f, 7f);
        var color = _reactive.GetColor(uid, light?.Color ?? profile.Color);
        var maskRotation = light?.Rotation ?? Angle.Zero;
        if (light is { MaskAutoRotate: true })
            maskRotation += rotation;
        var distance = Vector2.DistanceSquared(position, eye.Position.Position + eye.Offset);
        var score = profile.Priority * 100f + energy * radius / (1f + distance * 0.1f);
        var source = new CinematicLightSource(uid, position, emissionPosition, radius, color, energy,
            maskRotation, light?.LightMask, profile, score, xform.GridUid);

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
    EntityUid? Grid);

public readonly record struct CinematicLightingStatistics(
    int Quality,
    int Sources,
    int AtmosphereSources,
    int OccluderEdges,
    int MeshRebuilds,
    int Targets,
    long TargetBytes,
    double CpuMilliseconds);

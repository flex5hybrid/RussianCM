using System.Numerics;
using Content.Client.Examine;
using Content.Client.Gameplay;
using Content.Shared.Imperial.ICCVar;
using Content.Shared.Interaction;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Imperial.TargetOverlay;


public sealed partial class TargetOverlay : Overlay
{
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    private readonly SpriteSystem _spriteSystem;
    private readonly SharedTransformSystem _transformSystem;
    private readonly EntityLookupSystem _lookupSystem;
    private readonly ExamineSystem _examineSystem;
    private readonly MapSystem _mapSystem;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;


    public const string TargetIconPath = "/Textures/Imperial/Interface/Misc/TargetOverlay/target.png";
    public List<(MapCoordinates CursorPosition, EntityUid? Target, Angle Rotation)> Targets = new();
    public (MapCoordinates CursorPosition, EntityUid? Target, Angle Rotation) Targeting;


    public int MaxTargetCount = 1;
    public Color AimColor;
    public Color CapturedAimColor;
    public HashSet<Type> WhiteListComponents = new();
    public HashSet<Type> BlackListComponents = new();


    public TargetOverlay()
    {
        IoCManager.InjectDependencies(this);

        _spriteSystem = _entitySystem.GetEntitySystem<SpriteSystem>();
        _transformSystem = _entitySystem.GetEntitySystem<SharedTransformSystem>();
        _lookupSystem = _entitySystem.GetEntitySystem<EntityLookupSystem>();
        _examineSystem = _entitySystem.GetEntitySystem<ExamineSystem>();
        _mapSystem = _entitySystem.GetEntitySystem<MapSystem>();

        AimColor = Color.TryFromHex(_configurationManager.GetCVar(ICCVars.TargetOverlayAimColor)) ?? Color.Red;
        CapturedAimColor = Color.TryFromHex(_configurationManager.GetCVar(ICCVars.TargetOverlayCapturedAimColor)) ?? Color.Red;

        _configurationManager.OnValueChanged(ICCVars.TargetOverlayAimColor, newColor => { AimColor = Color.TryFromHex(newColor) ?? Color.Red; });
        _configurationManager.OnValueChanged(ICCVars.TargetOverlayCapturedAimColor, newColor => { CapturedAimColor = Color.TryFromHex(newColor) ?? Color.Red; });
    }


    protected override void Draw(in OverlayDrawArgs args)
    {
        DrawCursorAim(args);
        DrawTargets(args);
    }

    private void DrawCursorAim(OverlayDrawArgs args)
    {
        if (IsMaxTargetCount()) return;
        if (args.Viewport.Eye == null) return;

        var handle = args.WorldHandle;
        var rotation = -Angle.FromDegrees(_timing.CurTime.TotalMilliseconds / 20);

        var texture = _spriteSystem.Frame0(new SpriteSpecifier.Texture(new(TargetIconPath)));
        var mousePosition = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition.Position);

        if (mousePosition.MapId != args.MapId) return;
        if (!_mapSystem.TryGetMap(mousePosition.MapId, out var mapUid)) return;

        var worldMatrix = _transformSystem.GetWorldMatrix(mapUid.Value);
        var invMatrix = _transformSystem.GetInvWorldMatrix(mapUid.Value);

        var localPos = Vector2.Transform(mousePosition.Position, invMatrix);
        var aabb = Box2.UnitCentered.Translated(localPos);
        var box = new Box2Rotated(aabb, rotation, localPos);

        var currentState = _stateManager.CurrentState;

        Targeting.CursorPosition = mousePosition;
        Targeting.Rotation = rotation;
        Targeting.Target = null;

        if (currentState is GameplayStateBase screen)
        {
            var target = GetClickedEntity(screen, mousePosition, args.Viewport.Eye) ?? GetFirstIntersectsTarget(mousePosition.MapId, box);

            if (target != null && _playerManager.LocalEntity != null)
            {
                var originPosition = _transformSystem.GetMapCoordinates(_playerManager.LocalEntity.Value);
                var targetPosition = _transformSystem.GetMapCoordinates(target.Value);

                if (_examineSystem.InRangeUnOccluded(originPosition, targetPosition, SharedInteractionSystem.MaxRaycastRange, null))
                    SetTarget(target.Value, rotation, invMatrix, ref box);
            }
        }

        handle.SetTransform(worldMatrix);
        handle.DrawTextureRect(texture, box, AimColor);
        handle.SetTransform(Matrix3x2.Identity);
    }

    private void DrawTargets(OverlayDrawArgs args)
    {
        if (args.Viewport.Eye == null) return;

        var sortedTargets = GetSortedTargets();

        foreach (var (entity, targetCount) in sortedTargets.EntityTargets)
        {
            var entityCoords = _transformSystem.GetMapCoordinates(entity);

            DrawAimOnCoords(args, entityCoords, targetCount);
        }

        foreach (var (coords, targetCount) in sortedTargets.PositionTargets)
            DrawAimOnCoords(args, coords, targetCount);
    }

    private void DrawAimOnCoords(OverlayDrawArgs args, MapCoordinates coords, List<Angle> rotation)
    {
        var handle = args.WorldHandle;
        var texture = _spriteSystem.Frame0(new SpriteSpecifier.Texture(new(TargetIconPath)));

        if (coords.MapId != args.MapId) return;
        if (!_mapSystem.TryGetMap(coords.MapId, out var mapUid)) return;

        var worldMatrix = _transformSystem.GetWorldMatrix(mapUid.Value);
        var invMatrix = _transformSystem.GetInvWorldMatrix(mapUid.Value);

        var localPos = Vector2.Transform(coords.Position, invMatrix);
        var aabb = Box2.UnitCentered.Translated(localPos);

        for (var i = 0; i < rotation.Count; i++)
        {
            var millisecondsOnTarget = rotation[i].Degrees * -20;
            var angleRatio = (_timing.CurTime.TotalMilliseconds - millisecondsOnTarget) / -35;

            var box = new Box2Rotated(aabb, Angle.FromDegrees(rotation[i].Degrees - angleRatio), localPos);

            handle.SetTransform(worldMatrix);
            handle.DrawTextureRect(texture, box, CapturedAimColor);
            handle.SetTransform(Matrix3x2.Identity);
        }
    }

    #region Helpers

    private EntityUid? GetFirstIntersectsTarget(MapId mapId, Box2Rotated bounds)
    {
        foreach (var ent in _lookupSystem.GetEntitiesIntersecting(mapId, bounds, LookupFlags.All))
        {
            if (!CheckWhiteList(ent)) continue;
            if (!CheckBlackList(ent)) continue;

            return ent;
        }

        return null;
    }

    private EntityUid? GetClickedEntity(GameplayStateBase screen, MapCoordinates mousePosition, IEye eye)
    {
        var ent = screen.GetClickedEntity(mousePosition, eye);

        if (ent == null) return null;
        if (!CheckWhiteList(ent.Value)) return null;
        if (!CheckBlackList(ent.Value)) return null;

        return ent;
    }

    private void SetTarget(EntityUid uid, Angle rotation, Matrix3x2 invMatrix, ref Box2Rotated output)
    {
        var localPos = Vector2.Transform(_transformSystem.GetWorldPosition(uid), invMatrix);
        var aabb = Box2.UnitCentered.Translated(localPos);

        output = new Box2Rotated(aabb, rotation, localPos);
        Targeting.Target = uid;
    }

    private bool CheckWhiteList(EntityUid uid)
    {
        if (WhiteListComponents.Count == 0) return false;

        foreach (var comp in WhiteListComponents)
        {
            if (_entityManager.HasComponent(uid, comp)) continue;

            return false;
        }

        return true;
    }

    private bool CheckBlackList(EntityUid uid)
    {
        if (BlackListComponents.Count == 0) return true;

        foreach (var comp in BlackListComponents)
        {
            if (!_entityManager.HasComponent(uid, comp)) continue;

            return false;
        }

        return true;
    }

    private (Dictionary<EntityUid, List<Angle>> EntityTargets, Dictionary<MapCoordinates, List<Angle>> PositionTargets) GetSortedTargets()
    {
        var entityTargets = new Dictionary<EntityUid, List<Angle>>();
        var positionTargets = new Dictionary<MapCoordinates, List<Angle>>();

        foreach (var (mapCoords, target, rotation) in Targets)
        {
            if (target != null)
            {
                if (entityTargets.TryAdd(target.Value, new List<Angle>() { rotation })) continue;
                entityTargets[target.Value].Add(rotation);

                continue;
            }

            if (positionTargets.TryAdd(mapCoords, new List<Angle>() { rotation })) continue;
            positionTargets[mapCoords].Add(rotation);
        }

        return (entityTargets, positionTargets);
    }

    #endregion

    #region Public API

    public void AddTarget()
    {
        if (IsMaxTargetCount()) return;

        Targets.Add(Targeting);
    }

    public bool IsMaxTargetCount() => Targets.Count + 1 > MaxTargetCount;

    #endregion
}

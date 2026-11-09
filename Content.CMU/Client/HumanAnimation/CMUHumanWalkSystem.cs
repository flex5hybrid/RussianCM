using System.Numerics;
using Content.Client.CMU14.ThreeD.Scene;
using Content.Client.Inventory;
using Content.Shared.Body;
using Content.Shared.Buckle.Components;
using Content.Shared.Camera;
using Content.Shared.CMU14.CrawlState;
using Content.Shared.CMU14.HumanAnimation;
using Content.Shared.CMU14.Pushups;
using Content.Shared.CMU14.Surrender;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Standing;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.CMU14.HumanAnimation;

/// <summary>Opt-in V2 joint poses on live human layers. Never replaces a character with a baked example outfit.</summary>
public sealed partial class CMUHumanWalkSystem : EntitySystem
{
    public const string CVarName = "cmu.human_animations";
    private const string Maps = "CMU14/Effects/human_walk_v2.rsi";
    private const float SprintStepKick = 0.06f;
    private static readonly ResPath HumanParts = new("/Textures/Mobs/Species/Human/parts.rsi");
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private SpriteSystem _sprites = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IEyeManager _eyes = default!;
    [Dependency] private SharedCameraRecoilSystem _recoil = default!;
    private bool _enabled;
    private int _nextAlias;
    private readonly Dictionary<EntityUid, WalkState> _states = new();
    private readonly HashSet<EntityUid> _seen = new();
    private readonly List<EntityUid> _finished = new();

    private sealed class WalkState
    {
        public EntityUid Parent;
        public Vector2 Position;
        public TimeSpan LastMoved;
        public float StridePhase;
        public uint FootstepSequence;
        public Clip? LowerPose;
        public Clip? UpperPose;
        public bool Standing;
        public CMUHumanAction Action;
        public TimeSpan ActionStarted;
        public int LayerCount;
        public readonly List<PosedLayer> Layers = new();
    }

    private sealed record PoseMap(string Key, string Role, SpriteComponent.Layer Layer, bool LowerBody);

    private sealed record PosedLayer(string Alias, SpriteComponent.Layer Layer, List<PoseMap> Maps,
        ShaderInstance? OriginalShader, ProtoId<ShaderPrototype>? OriginalPrototype,
        ShaderInstance Shader);

    public override void Initialize()
    {
        SubscribeNetworkEvent<CMUHumanActionEvent>(OnAction);
        // This option controls the client renderer independently of the server's movement rules.
        if (!_config.IsCVarRegistered(CVarName))
            _config.RegisterCVar(CVarName, false, CVar.CLIENTONLY | CVar.ARCHIVE);
        _config.OnValueChanged<bool>(CVarName, OnEnabled, true);
    }

    private void OnEnabled(bool enabled)
    {
        _enabled = enabled;
        if (!enabled)
            RestoreAll();
    }

    public override void Shutdown()
    {
        _config.UnsubValueChanged<bool>(CVarName, OnEnabled);
        RestoreAll();
        base.Shutdown();
    }

    private void RestoreAll()
    {
        foreach (var (uid, state) in _states)
            Restore(uid, state);
        _states.Clear();
    }

    public override void FrameUpdate(float frameTime)
    {
        if (!_enabled)
            return;
        _seen.Clear();
        var now = _timing.CurTime;
        var localEntity = _player.LocalEntity;
        var query = EntityQueryEnumerator<HumanoidProfileComponent, InputMoverComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var profile, out var input, out var sprite, out var xform))
        {
            if (profile.Species.Id != "Human"
                || !TryComp<MobMoverComponent>(uid, out var mover)
                || !TryComp<StandingStateComponent>(uid, out var standing)
                || !TryComp<MobStateComponent>(uid, out var mob))
                continue;
            _seen.Add(uid);
            if (!_states.TryGetValue(uid, out var state))
            {
                state = new WalkState
                {
                    Parent = xform.ParentUid,
                    Position = xform.LocalPosition,
                    LastMoved = now - TimeSpan.FromSeconds(1),
                    FootstepSequence = mover.FootstepSequence,
                    Standing = standing.Standing,
                };
                _states.Add(uid, state);
            }
            var newStep = state.FootstepSequence != mover.FootstepSequence;
            state.FootstepSequence = mover.FootstepSequence;
            var sameParent = state.Parent == xform.ParentUid;
            var delta = sameParent ? Vector2.Distance(state.Position, xform.LocalPosition) : 0;
            state.Parent = xform.ParentUid;
            state.Position = xform.LocalPosition;
            if (!sameParent || delta >= 1.5f)
            {
                state.StridePhase = 0;
                state.LastMoved = now - TimeSpan.FromSeconds(1);
                delta = 0;
            }
            if (delta > 0.001f && delta < 1.5f)
                state.LastMoved = now;
            var wasStanding = state.Standing;
            state.Standing = standing.Standing;
            if (IsPaused(uid) || !sprite.Visible || sprite.ContainerOccluded
                || HasComp<CMUSurrenderComponent>(uid)
                || TryComp<BuckleComponent>(uid, out var buckle) && buckle.Buckled
                || TryComp<CMUPushupsComponent>(uid, out var exercise) && exercise.Active)
            {
                Restore(uid, state);
                state.StridePhase = 0;
                state.Action = CMUHumanAction.None;
                continue;
            }

            if (wasStanding != state.Standing)
            {
                state.Action = state.Standing ? CMUHumanAction.Rise : CMUHumanAction.Fall;
                state.ActionStarted = now;
            }

            var alive = mob.CurrentState == MobState.Alive;
            var moving = input.CanMove && input.HasDirectionalMovement
                && (now - state.LastMoved).TotalSeconds < .12;
            var crawling = !state.Standing && moving && (alive || HasComp<ActiveCrawlWhileCritComponent>(uid));
            var body = CompOrNull<BodyComponent>(uid);
            var missingLegs = body != null && body.LegEntities.Count < body.RequiredLegs;
            var held = HasHeldItem(uid);
            var action = ActionClip(state, now, alive, state.Standing, held);
            if (action == null && (!crawling && (!alive || !state.Standing) || moving && missingLegs))
            {
                Restore(uid, state);
                state.StridePhase = 0;
                continue;
            }

            Clip? gait = null;
            var stepDistance = input.GetFootstepDistance(mover);
            if (moving && !missingLegs && float.IsFinite(stepDistance) && stepDistance > 0)
            {
                if (crawling)
                {
                    state.StridePhase = (state.StridePhase + delta / stepDistance * .75f) % 1;
                    gait = new Clip(held ? "crawl-held" : "crawl", Math.Clamp((int) (state.StridePhase * 8), 0, 7));
                }
                else if (state.Standing && alive)
                {
                    int frame;
                    if (uid == localEntity)
                    {
                        frame = (int) (mover.FootstepSequence & 1) * 4;
                        // The contact pose and camera impulse follow the step sound request.
                        if (!newStep)
                            frame += Math.Clamp((int) (mover.StepSoundDistance / stepDistance * 4), 0, 3);
                        else if (input.Sprinting && _eyes.MainViewport is not CMU3DSceneControl)
                            _recoil.KickCamera(uid, new Vector2(0, -SprintStepKick));
                    }
                    else
                    {
                        state.StridePhase = (state.StridePhase + delta / stepDistance * .5f) % 1;
                        frame = Math.Clamp((int) (state.StridePhase * 8), 0, 7);
                    }
                    gait = new Clip(WalkingClip(uid, input, body, held), frame);
                }
            }
            else
            {
                state.StridePhase = 0;
            }

            if (state.Action is CMUHumanAction.Fall or CMUHumanAction.Rise)
                gait = null;
            var lower = gait ?? action ?? IdleClip(uid, now, held);
            var upper = action ?? lower;
            if (!Valid((uid, sprite), state))
                Restore(uid, state);
            if (state.Layers.Count == 0)
                Apply((uid, sprite), state);
            if (state.LowerPose == lower && state.UpperPose == upper)
                continue;
            state.LowerPose = lower;
            state.UpperPose = upper;
            foreach (var entry in state.Layers)
            {
                foreach (var map in entry.Maps)
                {
                    var clip = map.LowerBody ? lower : upper;
                    var key = clip.Name + "-" + map.Role;
                    if (map.Layer.State.Name != key)
                        _sprites.LayerSetRsiState((uid, sprite), map.Key, key);
                    _sprites.LayerSetAnimationTime((uid, sprite), map.Key, clip.Frame * .1f + .001f);
                }
            }
        }
        _finished.Clear();
        foreach (var (uid, _) in _states)
        {
            if (!_seen.Contains(uid))
                _finished.Add(uid);
        }
        foreach (var uid in _finished)
        {
            Restore(uid, _states[uid]);
            _states.Remove(uid);
        }
    }

    private bool HasHeldItem(EntityUid uid)
    {
        if (!TryComp<HandsComponent>(uid, out var hands))
            return false;
        foreach (var hand in hands.SortedHands)
        {
            if (_hands.TryGetHeldItem((uid, hands), hand, out _))
                return true;
        }
        return false;
    }

    private bool Valid(Entity<SpriteComponent> ent, WalkState state)
    {
        if (state.Layers.Count > 0 && state.LayerCount != ent.Comp.LayerData.Count)
            return false;
        foreach (var layer in state.Layers)
        {
            if (!_sprites.TryGetLayer(ent.AsNullable(), layer.Alias, out var current, false) || current != layer.Layer ||
                current.Shader != layer.Shader)
                return false;
            foreach (var pose in layer.Maps)
            {
                if (!_sprites.TryGetLayer(ent.AsNullable(), pose.Key, out var map, false) || map != pose.Layer)
                    return false;
            }
        }
        return true;
    }

    private void Apply(Entity<SpriteComponent> ent, WalkState state)
    {
        foreach (var part in Enum.GetValues<HumanoidVisualLayers>())
        {
            if (part is HumanoidVisualLayers.StencilMask or HumanoidVisualLayers.Fire or HumanoidVisualLayers.Ensnare)
                continue;
            Pose(ent, part, Role(part), state);
        }
        foreach (var organ in _body.EnumerateOrgans<VisualOrganMarkingsComponent>(ent.Owner))
        {
            foreach (var marking in organ.Comp2.AppliedMarkings)
            {
                if (!_prototypes.TryIndex<MarkingPrototype>(marking.MarkingId, out var proto))
                    continue;
                foreach (var specifier in proto.Sprites)
                {
                    if (specifier is SpriteSpecifier.Rsi rsi)
                        Pose(ent, $"{proto.ID}-{rsi.RsiState}", Role(proto.BodyPart), state);
                }
            }
        }
        if (TryComp<InventorySlotsComponent>(ent, out var inventory))
        {
            foreach (var (slot, keys) in inventory.VisualLayerKeys)
            {
                var role = slot switch
                {
                    "jumpsuit" or "outerClothing" => "clothes",
                    "gloves" => "gloves",
                    "shoes" => "shoes",
                    _ => "root",
                };
                foreach (var key in keys)
                    Pose(ent, key, role, state, fillTorso: role == "clothes");
            }
        }
        if (TryComp<HandsComponent>(ent, out var hands))
        {
            foreach (var keys in hands.RevealedLayers.Values)
            {
                foreach (var key in keys)
                    Pose(ent, key, "root", state);
            }
        }
        state.LayerCount = ent.Comp.LayerData.Count;
    }

    private static string Role(HumanoidVisualLayers part) => part switch
    {
        HumanoidVisualLayers.LLeg => "leg-left",
        HumanoidVisualLayers.RLeg => "leg-right",
        HumanoidVisualLayers.LFoot => "foot-left",
        HumanoidVisualLayers.RFoot => "foot-right",
        HumanoidVisualLayers.LArm or HumanoidVisualLayers.LHand => "arm-left",
        HumanoidVisualLayers.RArm or HumanoidVisualLayers.RHand => "arm-right",
        HumanoidVisualLayers.UndergarmentTop or HumanoidVisualLayers.UndergarmentBottom or HumanoidVisualLayers.Special => "clothes",
        _ => "root",
    };

    private void Pose(Entity<SpriteComponent> ent, object key, string role, WalkState state, bool fillTorso = false)
    {
        var index = 0;
        var mapped = key is Enum enumKey ? _sprites.LayerMapTryGet(ent.AsNullable(), enumKey, out index, false) :
            key is string stringKey && _sprites.LayerMapTryGet(ent.AsNullable(), stringKey, out index, false);
        if (!mapped || !_sprites.TryGetLayer(ent.AsNullable(), index, out var layer, false) ||
            layer.CopyToShaderParameters != null ||
            _sprites.LayerGetEffectiveRsi(ent.AsNullable(), index) is not { } rsi || rsi.Size != new Vector2i(32, 32))
            return;
        foreach (var existing in state.Layers)
        {
            if (existing.Layer == layer)
                return;
        }
        var original = layer.Shader;
        var prototype = layer.ShaderPrototype;
        var id = prototype?.Id;
        var displaced = id is "DisplacedDraw" or "DisplacedDrawUnshaded" or "DisplacedStencilDraw";
        if (original != null && !displaced && id != "unshaded")
            return; // Preserve other owners' special body/clothing effects.
        var shaderId = id is "unshaded" or "DisplacedDrawUnshaded" ? "CMUHumanWalkV2Unshaded" : "CMUHumanWalkV2";
        var shader = _prototypes.Index<ShaderPrototype>(shaderId).InstanceUnique();
        var slices = role switch
        {
            "clothes" => new[] { "clothes-back-arm", "clothes-back-leg", "clothes-front-leg", "clothes-body", "clothes-front-arm" },
            "gloves" => new[] { "glove-right", "glove-left" },
            "shoes" => new[] { "shoe-right", "shoe-left" },
            _ => new[] { role },
        };
        if (fillTorso)
            slices = [..slices, "clothes-fill"];
        shader.SetParameter("component_count", (float) slices.Length);
        shader.SetParameter("use_displacement", displaced ? 1f : 0f);
        shader.SetParameter("displacementSize", 127f);
        var repairTorsoCuts = key is HumanoidVisualLayers.Chest && rsi.Path == HumanParts &&
            layer.State.Name is "torso_m" or "torso_f";
        shader.SetParameter("repair_torso_cuts", repairTorsoCuts ? 1f : 0f);
        if (original != null)
            shader.Stencil = original.Stencil;
        var alias = $"cmu-walk-v2-{_nextAlias++}";
        _sprites.LayerMapSet(ent.AsNullable(), alias, index);
        ent.Comp.LayerSetShader(index, shader, shaderId);
        var maps = new List<PoseMap>(slices.Length);
        for (var slice = 0; slice < slices.Length; slice++)
        {
            var mapKey = alias + "-pose-" + slice;
            var mapIndex = _sprites.AddLayer(ent.AsNullable(), new PrototypeLayerData
            {
                RsiPath = Maps,
                State = "walk-" + slices[slice],
                CopyToShaderParameters = new PrototypeCopyToShaderParameters
                {
                    LayerKey = alias,
                    // All candidates share the same RSI atlas; only the frame rectangles differ.
                    ParameterTexture = slice == 0 ? "poseMap" : null,
                    ParameterUV = slice == 0 ? "poseUV" : "poseUV" + slice,
                },
            }, index);
            _sprites.LayerMapSet(ent.AsNullable(), mapKey, mapIndex);
            var mapLayer = (SpriteComponent.Layer) ent.Comp[mapIndex];
            _sprites.LayerSetDirOffset(mapLayer, layer.DirOffset);
            _sprites.LayerSetAutoAnimated(ent.AsNullable(), mapKey, false);
            var mapRole = slices[slice];
            var lowerBody = mapRole is "clothes-back-leg" or "clothes-front-leg"
                or "leg-left" or "leg-right" or "foot-left" or "foot-right" or "shoe-left" or "shoe-right";
            maps.Add(new(mapKey, mapRole, mapLayer, lowerBody));
        }
        state.Layers.Add(new(alias, layer, maps, original, prototype, shader));
    }

    private void Restore(EntityUid uid, WalkState state)
    {
        state.LowerPose = null;
        state.UpperPose = null;
        if (state.Layers.Count == 0)
            return;
        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            var indices = new Dictionary<ISpriteLayer, int>();
            var index = 0;
            foreach (var layer in sprite.AllLayers)
                indices[layer] = index++;
            var removals = new List<int>();
            foreach (var entry in state.Layers)
            {
                if (indices.TryGetValue(entry.Layer, out var layerIndex) && entry.Layer.Shader == entry.Shader)
                {
                    if (entry.OriginalShader == null && entry.OriginalPrototype is { } prototype)
                        sprite.LayerSetShader(layerIndex, prototype.Id);
                    else
                        sprite.LayerSetShader(layerIndex, entry.OriginalShader, entry.OriginalPrototype?.Id);
                }
                foreach (var map in entry.Maps)
                {
                    if (indices.TryGetValue(map.Layer, out var mapIndex))
                        removals.Add(mapIndex);
                }
            }
            removals.Sort();
            for (var i = removals.Count - 1; i >= 0; i--)
                _sprites.RemoveLayer((uid, sprite), removals[i], false);
            foreach (var entry in state.Layers)
                _sprites.LayerMapRemove((uid, sprite), entry.Alias);
        }
        foreach (var entry in state.Layers)
            entry.Shader.Dispose();
        state.Layers.Clear();
    }
}

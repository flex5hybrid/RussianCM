using System.Numerics;
using Content.Client.Graphics;
using Content.Shared.Body;
using Content.Shared.Buckle.Components;
using Content.Shared.CCVar;
using Content.Shared.CMU14.HumanAnimation;
using Content.Shared.CMU14.Pushups;
using Content.Shared.CMU14.Surrender;
using Content.Shared.CombatMode;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Standing;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Wieldable.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.CMU14.HumanAnimation;

/// <summary>Local presentation only; no movement, attack, rotation or inventory state is written.</summary>
public sealed partial class CMUHumanAnimationSystem : EntitySystem
{
    private const string ShaderId = "cmu-human-animation";
    private static readonly ProtoId<ShaderPrototype> ShaderPrototype = "CMUHumanAnimation";
    [Dependency] private SpriteSystem _sprites = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IConfigurationManager _config = default!;

    private readonly Dictionary<EntityUid, AnimationState> _states = new();
    private readonly List<EntityUid> _removed = new();
    private TimeSpan _nextUpdate;
    private bool _enabled;

    private sealed class AnimationState
    {
        public ShaderInstance? Shader;
        public EntityUid Parent;
        public Vector2 Position;
        public float Cycle;
        public bool Standing;
        public TimeSpan LastUpdate;
        public TimeSpan ActionStarted;
        public float ActionDuration;
        public CMUHumanAnimationKind Action;
        public CMUHumanAnimationPose Pose;
    }

    public override void Initialize()
    {
        SubscribeLocalEvent<CMUHumanAnimationComponent, BeforePostShaderRenderEvent>(OnRender);
        SubscribeLocalEvent<CMUHumanAnimationComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CMUHumanAnimationComponent, CMUHumanAnimationImpulseEvent>(OnImpulse);
        Subs.CVar(_config, CCVars.CMUHumanAnimations, value => _enabled = value, true);
    }

    public override void Shutdown()
    {
        foreach (var (uid, state) in _states)
            RemoveShader(uid, state);
        _states.Clear();
        base.Shutdown();
    }

    private void OnShutdown(Entity<CMUHumanAnimationComponent> ent, ref ComponentShutdown args)
    {
        if (_states.Remove(ent, out var state))
            RemoveShader(ent, state);
    }

    private void OnImpulse(Entity<CMUHumanAnimationComponent> ent, ref CMUHumanAnimationImpulseEvent args)
    {
        if (!_enabled || !_timing.IsFirstTimePredicted || !_states.TryGetValue(ent, out var state) ||
            !TryComp<StandingStateComponent>(ent, out var standing) || !standing.Standing ||
            !TryComp<MobStateComponent>(ent, out var mob) || mob.CurrentState != MobState.Alive)
            return;

        // Posture transitions have priority over combat impulses.
        if (state.Action is CMUHumanAnimationKind.Fall or CMUHumanAnimationKind.GetUp &&
            (_timing.CurTime - state.ActionStarted).TotalSeconds < state.ActionDuration)
            return;
        state.Action = args.Kind;
        state.ActionStarted = _timing.CurTime;
        state.ActionDuration = args.Kind == CMUHumanAnimationKind.Shoot
            ? CMUHumanAnimation.ShootDuration : CMUHumanAnimation.MeleeDuration;
    }

    public override void FrameUpdate(float frameTime)
    {
        if (!_enabled)
        {
            foreach (var (uid, state) in _states)
                RemoveShader(uid, state);
            _states.Clear();
            return;
        }
        var now = _timing.CurTime;
        if (now < _nextUpdate)
            return;
        _nextUpdate = now + TimeSpan.FromSeconds(1f / 12);

        var query = EntityQueryEnumerator<CMUHumanAnimationComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var sprite, out var xform))
        {
            if (!TryComp<StandingStateComponent>(uid, out var standing) ||
                !TryComp<MobStateComponent>(uid, out var mob))
                continue; // Lobby/mannequin previews remain still.

            if (!_states.TryGetValue(uid, out var state))
            {
                state = new AnimationState
                {
                    Parent = xform.ParentUid,
                    Position = xform.LocalPosition,
                    Standing = standing.Standing,
                    LastUpdate = now,
                };
                _states.Add(uid, state);
            }

            var elapsed = (float) (now - state.LastUpdate).TotalSeconds;
            var distance = state.Parent == xform.ParentUid ? Vector2.Distance(state.Position, xform.LocalPosition) : 0;
            state.Parent = xform.ParentUid;
            state.Position = xform.LocalPosition;
            state.LastUpdate = now;
            // Reparenting, teleports and streaming back in must not produce a stride burst.
            var continuous = elapsed is > 0 and < 0.5f && distance < 1.5f;
            var moving = continuous && distance > 0.005f &&
                TryComp<InputMoverComponent>(uid, out var mover) && mover.CanMove && mover.HasDirectionalMovement;
            if (moving)
                state.Cycle = (state.Cycle + distance / 1.4f) % 1;

            var postureChanged = state.Standing != standing.Standing;
            state.Standing = standing.Standing;
            var suppressed = !_enabled || !sprite.Visible || sprite.ContainerOccluded || IsPaused(uid) ||
                HasComp<CMUSurrenderComponent>(uid) ||
                TryComp<CMUPushupsComponent>(uid, out var exercise) && exercise.Active ||
                TryComp<BuckleComponent>(uid, out var buckle) && buckle.Buckled;
            if (suppressed)
            {
                state.Action = CMUHumanAnimationKind.None;
                RemoveShader(uid, state);
                continue;
            }

            if (postureChanged)
            {
                state.Action = standing.Standing ? CMUHumanAnimationKind.GetUp : CMUHumanAnimationKind.Fall;
                state.ActionStarted = now;
                state.ActionDuration = standing.Standing ? CMUHumanAnimation.GetUpDuration : CMUHumanAnimation.FallDuration;
            }

            var actionTime = (float) (now - state.ActionStarted).TotalSeconds;
            var transitioning = state.Action is CMUHumanAnimationKind.Fall or CMUHumanAnimationKind.GetUp;
            if (state.Action != CMUHumanAnimationKind.None && actionTime < state.ActionDuration &&
                (transitioning || mob.CurrentState == MobState.Alive && standing.Standing))
            {
                state.Pose = CMUHumanAnimation.Sample(state.Action, state.Cycle, (float) now.TotalSeconds,
                    actionTime / state.ActionDuration);
            }
            else
            {
                state.Action = CMUHumanAnimationKind.None;
                var held = _hands.GetActiveItem(uid);
                var wielded = TryComp<WieldableComponent>(held, out var wieldable) && wieldable.Wielded;
                var aiming = TryComp<CombatModeComponent>(uid, out var combat) && combat.IsInCombatMode ||
                    wielded && HasComp<GunComponent>(held);
                var heavy = wielded || TryComp<MovementSpeedModifierComponent>(uid, out var speed) &&
                    speed.CurrentSprintSpeed < speed.BaseSprintSpeed * 0.8f;
                // Only use public missing-limb state, never owner-only pain/diagnosis data.
                var limping = TryComp<BodyComponent>(uid, out var body) && body.LegEntities.Count < body.RequiredLegs;
                var running = TryComp<InputMoverComponent>(uid, out var input) && input.Sprinting;
                var kind = CMUHumanAnimation.Select(mob.CurrentState == MobState.Alive, !standing.Standing,
                    moving, running, aiming, heavy, limping, IsWorking(uid));
                if (kind == CMUHumanAnimationKind.None)
                {
                    RemoveShader(uid, state);
                    continue;
                }
                state.Pose = CMUHumanAnimation.Sample(kind, state.Cycle,
                    (float) now.TotalSeconds + (uid.GetHashCode() & 255) * 0.13f);
            }

            state.Shader ??= _prototypes.Index(ShaderPrototype).InstanceUnique();
            if (!_sprites.HasPostShader((uid, sprite), ShaderId))
            {
                _sprites.SetPostShader((uid, sprite), new SpriteComponent.PostShaderArgs(ShaderId, state.Shader)
                {
                    RaiseShaderEvent = true,
                    Before = [.. ContentPostShaderIds.AfterBaseEffects, "RMCInvisible", .. ContentPostShaderIds.BeforeOutlines],
                });
            }
        }

        _removed.Clear();
        foreach (var (uid, _) in _states)
        {
            if (!Exists(uid) || !HasComp<CMUHumanAnimationComponent>(uid))
                _removed.Add(uid);
        }
        foreach (var uid in _removed)
        {
            RemoveShader(uid, _states[uid]);
            _states.Remove(uid);
        }
    }

    private bool IsWorking(EntityUid uid)
    {
        if (!TryComp<DoAfterComponent>(uid, out var work))
            return false;
        foreach (var action in work.DoAfters.Values)
        {
            if (!action.Completed && !action.Cancelled && action.Args.NeedHand)
                return true;
        }
        return false;
    }

    private void RemoveShader(EntityUid uid, AnimationState state)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
            _sprites.RemovePostShader((uid, sprite), ShaderId);
        state.Shader?.Dispose();
        state.Shader = null;
    }

    private void OnRender(Entity<CMUHumanAnimationComponent> ent, ref BeforePostShaderRenderEvent args)
    {
        if (args.Id != ShaderId || !_states.TryGetValue(ent, out var state))
            return;
        var sprite = args.Sprite;
        var viewport = args.Viewport;
        var eyeRotation = viewport.Eye?.Rotation ?? Angle.Zero;
        var rotation = sprite.NoRotation ? -eyeRotation : _transform.GetWorldRotation(ent);
        var origin = _transform.GetWorldPosition(ent) + rotation.RotateVec(sprite.Offset);
        rotation += sprite.Rotation;
        var center = ScreenUv(viewport, origin);
        var x = ScreenUv(viewport, origin + rotation.RotateVec(new Vector2(sprite.Scale.X / 32, 0))) - center;
        var y = ScreenUv(viewport, origin + rotation.RotateVec(new Vector2(0, -sprite.Scale.Y / 32))) - center;
        var determinant = x.X * y.Y - y.X * x.Y;
        if (MathF.Abs(determinant) < 1e-12f)
            return;
        args.Shader.SetParameter("body_origin", center);
        args.Shader.SetParameter("body_x", new Vector2(y.Y, -y.X) / determinant);
        args.Shader.SetParameter("body_y", new Vector2(-x.Y, x.X) / determinant);
        args.Shader.SetParameter("pixel_x", x * (Vector2) viewport.Size);
        args.Shader.SetParameter("pixel_y", y * (Vector2) viewport.Size);
        var direction = sprite.EnableDirectionOverride ? sprite.DirectionOverride :
            (_transform.GetWorldRotation(ent) + eyeRotation).GetCardinalDir();
        var facing = direction switch
        {
            Direction.East => new Vector2(1, 0),
            Direction.West => new Vector2(-1, 0),
            Direction.North => new Vector2(0, -1),
            _ => new Vector2(0, 1),
        };
        args.Shader.SetParameter("facing", facing);
        args.Shader.SetParameter("gait", state.Pose.Gait);
        args.Shader.SetParameter("action", state.Pose.Action);
    }

    private static Vector2 ScreenUv(IClydeViewport viewport, Vector2 world)
    {
        var point = viewport.WorldToLocal(world) / viewport.Size;
        return new Vector2(point.X, 1 - point.Y);
    }
}

[ByRefEvent]
public readonly record struct CMUHumanAnimationImpulseEvent(CMUHumanAnimationKind Kind);

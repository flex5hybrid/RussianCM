using Content.Shared.CCVar;
using Content.Shared.CMU14.Lighting;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.CMU14.Lighting;

/// <summary>Reuses native muzzle/explosion entities and the replicated light state; sends no messages.</summary>
public sealed partial class CinematicReactiveLightSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IConfigurationManager _config = default!;
    private readonly Dictionary<EntityUid, ColorState> _colors = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CMUCinematicEmitterComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CMUCinematicEmitterComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<CMUCinematicEmitterComponent> entity, ref ComponentStartup args)
    {
        entity.Comp.PulseStarted = _timing.CurTime;
    }

    private void OnShutdown(Entity<CMUCinematicEmitterComponent> entity, ref ComponentShutdown args)
    {
        _colors.Remove(entity.Owner);
    }

    /// <summary>Called where the existing client muzzle light has actually been configured.</summary>
    public void MuzzleFlash(EntityUid uid)
    {
        if (_config.GetCVar(CCVars.CMUCinematicLighting) == 0)
            return;
        var emitter = EnsureComp<CMUCinematicEmitterComponent>(uid);
        emitter.NetSyncEnabled = false;
        emitter.Profile = "CMUMuzzleFlash";
        emitter.PulseStarted = _timing.CurTime;
    }

    public float GetPulse(CMUCinematicEmitterComponent emitter, CMUCinematicLightProfilePrototype profile)
    {
        if (profile.PulseDuration <= 0f)
            return 1f;
        return emitter.PulseStarted is { } started
            ? CinematicLightingGeometry.Pulse((float) (_timing.CurTime - started).TotalSeconds, profile.PulseDuration)
            : 0f;
    }

    public Color GetColor(EntityUid uid, Color target)
    {
        var now = _timing.CurTime;
        if (!_colors.TryGetValue(uid, out var state))
        {
            if (_colors.Count >= 256)
                _colors.Clear();
            _colors[uid] = new ColorState(target, now);
            return target;
        }

        var factor = 1f - MathF.Exp(-12f * Math.Clamp((float) (now - state.Time).TotalSeconds, 0f, 1f));
        var color = new Color(
            MathHelper.Lerp(state.Color.R, target.R, factor),
            MathHelper.Lerp(state.Color.G, target.G, factor),
            MathHelper.Lerp(state.Color.B, target.B, factor), target.A);
        _colors[uid] = new ColorState(color, now);
        return color;
    }

    public void Clear() => _colors.Clear();
    private readonly record struct ColorState(Color Color, TimeSpan Time);
}

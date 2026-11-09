using System.Numerics;

namespace Content.Shared.CMU14.HumanAnimation;

public enum CMUHumanAnimationKind : byte
{
    None,
    Idle,
    Walk,
    Run,
    HeavyWalk,
    CombatWalk,
    Limp,
    Crawl,
    Melee,
    Shoot,
    Interact,
    Fall,
    GetUp,
}

/// <summary>Pixel-space rig parameters shared by the client and the animation review exporter.</summary>
public readonly record struct CMUHumanAnimationPose(Vector4 Gait, Vector4 Action);

public static class CMUHumanAnimation
{
    public const float FallDuration = 0.3f;
    public const float GetUpDuration = 0.4f;
    public const float MeleeDuration = 0.28f;
    public const float ShootDuration = 0.16f;

    /// <summary>Priority prevents dead/unconscious people working or walking, and prone runners standing up.</summary>
    public static CMUHumanAnimationKind Select(bool alive, bool down, bool moving, bool running,
        bool aiming, bool heavy, bool limping, bool working)
    {
        if (!alive)
            return CMUHumanAnimationKind.None;
        if (down)
            return moving ? CMUHumanAnimationKind.Crawl : CMUHumanAnimationKind.None;
        if (moving)
        {
            if (limping)
                return CMUHumanAnimationKind.Limp;
            if (aiming)
                return CMUHumanAnimationKind.CombatWalk;
            if (heavy)
                return CMUHumanAnimationKind.HeavyWalk;
            return running ? CMUHumanAnimationKind.Run : CMUHumanAnimationKind.Walk;
        }
        return working ? CMUHumanAnimationKind.Interact : CMUHumanAnimationKind.Idle;
    }

    /// <param name="cycle">A complete left/right stride, in cycles. Distance-driven during movement.</param>
    /// <param name="seconds">Active entity time, for breathing and working loops.</param>
    /// <param name="progress">Clamped 0..1 progress of a one-shot action.</param>
    public static CMUHumanAnimationPose Sample(CMUHumanAnimationKind kind, float cycle, float seconds, float progress = 0)
    {
        var step = MathF.Sin(cycle * MathF.Tau);
        var lift = MathF.Abs(step);
        var breath = (1 - MathF.Cos(seconds * MathF.Tau / 3.2f)) * 0.32f;
        var t = Math.Clamp(progress, 0, 1);
        var pulse = MathF.Sin(t * MathF.PI);
        // Gait: signed stride, foot lift, upper-body bob, forward lean.
        // Action: arm swing/reach, recoil, crouch/compression, breathing.
        return kind switch
        {
            CMUHumanAnimationKind.Idle => new(Vector4.Zero, new(0, 0, 0, breath)),
            CMUHumanAnimationKind.Walk => new(new(step * 1.35f, lift * 0.9f, lift * 0.6f, 0), new(-step * 0.7f, 0, 0, 0)),
            CMUHumanAnimationKind.Run => new(new(step * 2.25f, lift * 1.4f, lift, 0.8f), new(-step, 0, 0, 0)),
            CMUHumanAnimationKind.HeavyWalk => new(new(step, lift * 0.65f, lift * 0.45f, 0.45f), new(-step * 0.35f, 0, 0, 0)),
            CMUHumanAnimationKind.CombatWalk => new(new(step * 0.9f, lift * 0.6f, lift * 0.35f, 0.35f), Vector4.Zero),
            CMUHumanAnimationKind.Limp => new(new(step * (step < 0 ? 0.45f : 1.2f), lift * 0.5f, MathF.Max(0, step) * 0.9f, 0.4f), new(-step * 0.25f, 0, 0, 0)),
            CMUHumanAnimationKind.Crawl => new(new(step * 0.8f, lift * 0.4f, lift * 0.3f, 0), new(-step * 0.6f, 0, 0, 0)),
            CMUHumanAnimationKind.Melee => new(new(0, 0, 0, pulse * 0.8f), new(pulse * 2.4f, 0, pulse * 0.5f, 0)),
            CMUHumanAnimationKind.Shoot => new(Vector4.Zero, new(0, pulse * 1.3f, 0, 0)),
            CMUHumanAnimationKind.Interact => new(Vector4.Zero, new(MathF.Sin(seconds * MathF.Tau * 1.6f) * 0.7f, 0, 0.4f, breath * 0.3f)),
            CMUHumanAnimationKind.Fall => new(Vector4.Zero, new(0, 0, pulse * 2, 0)),
            CMUHumanAnimationKind.GetUp => new(Vector4.Zero, new(0, 0, (1 - t) * 2, 0)),
            _ => new(Vector4.Zero, Vector4.Zero),
        };
    }
}

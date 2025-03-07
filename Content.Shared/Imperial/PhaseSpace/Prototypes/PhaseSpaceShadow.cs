using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.PhaseSpace;


[DataDefinition, Serializable, NetSerializable]
public sealed partial class PhaseSpaceShadow
{
    /// <summary>
    /// Current position of the shadow in the world
    /// </summary>
    [DataField]
    public Vector2 WorldPosition = Vector2.Zero;

    /// <summary>
    /// Shadow direction. Oxyet.
    /// </summary>
    [DataField]
    public Direction? ShadowDirection;

    /// <summary>
    /// Cached sprite rotation
    /// </summary>
    [DataField]
    public Angle Rotation = Angle.Zero;

    /// <summary>
    /// The time after which the shadow will disappear. Set only by the parent
    /// </summary>
    [ViewVariables]
    public TimeSpan DestroyTime = TimeSpan.FromSeconds(0);

    /// <summary>
    /// A convenient variable that is calculated using the formula (<see cref="DestroyTime" /> - CurTime) / ShadowLifeTime
    /// </summary>
    [ViewVariables]
    public float Opacity = 1.0f;
}

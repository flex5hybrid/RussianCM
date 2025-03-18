using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.PhaseSpace;


[DataDefinition, Serializable, NetSerializable]
public sealed partial class PhaseSpaceDistortion
{
    /// <summary>
    /// The current relative position of the shadow to the parent
    /// </summary>
    [DataField]
    public Vector2 RelativePosition = Vector2.Zero;

    /// <summary>
    /// The position to which the shadow will return
    /// </summary>
    [DataField]
    public Vector2 FadePosition = Vector2.Zero;

    /// <summary>
    /// The position to which the shadow will strive
    /// </summary>
    [DataField]
    public Vector2 AppearancePosition = Vector2.Zero;

    /// <summary>
    /// The time it takes for the shadow to disappear and appear
    /// </summary>
    [DataField]
    public TimeSpan DistortionPathTime = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Whether to animate the exit. If not, the shadow will spawn on the current target without animation.
    /// </summary>
    [DataField]
    public bool AnimateFadeOnMove = true;

    /// <summary>
    /// Whether to animate the appearance. If not, the shadow will spawn on the current target without animation.
    /// </summary>
    [DataField]
    public bool AnimateAppearanceOnMove = true;

    /// <summary>
    /// Override shadow rotation. If null, then parent's direction is used.
    /// </summary>
    [DataField]
    public Direction? ShadowDirection;


    /// <summary>
    /// The position to which the shadow will strive. Taken from <see cref="FadePosition"/> and <see cref="AppearancePosition"/>.
    /// </summary>
    [ViewVariables]
    public Vector2 TargetPosition = new Vector2(float.PositiveInfinity, float.PositiveInfinity);

    /// <summary>
    /// A convenient variable that is calculated using the formula (<see cref="DestroyTime" /> - CurTime) / ShadowLifeTime
    /// </summary>
    [ViewVariables]
    public float Opacity = 1.0f;

    /// <summary>
    /// A min opacity of shadow
    /// </summary>
    [DataField]
    public float MinOpacity = 0f;

    /// <summary>
    /// The step that the shadow should take for a smooth animation.
    /// </summary>
    public Vector2 DistortionStep => (TargetPosition - RelativePosition) / new Vector2((float)DistortionPathTime.TotalMilliseconds);
}

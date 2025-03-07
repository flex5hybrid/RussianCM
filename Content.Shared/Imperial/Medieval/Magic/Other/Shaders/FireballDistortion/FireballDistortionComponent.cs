using System.Numerics;

namespace Content.Shared.Imperial.Medieval.Magic.Shaders;


/// <summary>
/// When this component is added, it turns the sprite into a fireball.
/// </summary>
[RegisterComponent]
public sealed partial class FireballDistortionComponent : Component
{
    /// <summary>
    /// Start point in shader space. Change this field if you scale fireball
    /// </summary>
    [DataField]
    public Vector2 StartPoint = new Vector2(0.0f, 0.0f);

    /// <summary>
    /// The color of fireball fire
    /// </summary>
    [DataField]
    public Color FireColor = Color.FromHex("#FF5400");

    /// <summary>
    /// The color of the edges of fire
    /// </summary>
    [DataField]
    public Color FireEdgeColor = Color.FromHex("#800000");

    /// <summary>
    /// Fireball size scale
    /// </summary>
    [DataField]
    public float FireballScale = 1.0f;

    /// <summary>
    /// A scale of fireball fire intensity
    /// </summary>
    [DataField]
    public float FireIntensity = 1.0f;

    /// <summary>
    /// Fireball flames power
    /// </summary>
    [DataField]
    public float FirePowerFactor = 1.0f;
}

using Robust.Shared.GameStates;

namespace Content.Shared.Imperial.ShockWave;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShockWaveDistortionComponent : Component
{
    /// <summary>
    /// Speed of wave
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Speed = 1.0f;

    /// <summary>
    /// A force of distortion
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Distortion = 0.05f;

    /// <summary>
    /// Width of border
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Thikness = 0.1f;

    /// <summary>
    /// The maximum distance to which the distortion will decrease
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxDistance = 1.0f;

    /// <summary>
    /// Percentage of total distance after which distortion will begin to decrease
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DisappearDistancePercentage = 0.0f;

    /// <summary>
    /// Color of shock wave. Dosent change field if you need transparent shock wave
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color Color = Color.Black;


    [ViewVariables]
    public TimeSpan SpawnTime = TimeSpan.FromSeconds(0);
}

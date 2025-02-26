using System.Numerics;

namespace Content.Server.Imperial.ImperialLightning;


[RegisterComponent]
public sealed partial class ImperialLightningSenderComponent : Component
{
    /// <summary>
    /// Entities with <see cref="ImperialLightningReciverComponent"/> and the same <see cref="ImperialLightningReciverComponent.ReceiptFrequency" /> field will be connected by lightning
    /// </summary>
    [DataField(required: true)]
    public string SendingFrequency = "";

    /// <summary>
    /// Speed of lightning
    /// </summary>
    [DataField]
    public float Speed = 1.0f;

    /// <summary>
    /// Lightning width
    /// </summary>
    [DataField]
    public float Intensity = 2.0f;

    /// <summary>
    /// Needed for procedural generation of lightning.
    /// Lightning with the same seed will look the same.
    /// Leave this value to null if you want different seeds every time.
    /// </summary>
    [DataField]
    public float Seed = 0.0f;

    /// <summary>
    /// Lightning scatter at start and end.
    /// Increase this value to make the lightning more chaotic.
    /// </summary>
    [DataField]
    public float Amplitude = 0.2f;

    /// <summary>
    /// Lightning frequency.
    /// Responsible for the quality and randomness of lightning generation.
    /// </summary>
    [DataField]
    public float Frequency = 3.0f;

    /// <summary>
    /// Lightning color
    /// </summary>
    [DataField]
    public Color LightningColor = Color.FromHex("#1A40F0");

    /// <summary>
    /// Lightning offset relative to itself
    /// </summary>
    [DataField]
    public Vector2 Offset = Vector2.Zero;

    /// <summary>
    /// Try to guess
    /// </summary>
    [DataField]
    public TimeSpan LifeTime = TimeSpan.FromDays(1);
}

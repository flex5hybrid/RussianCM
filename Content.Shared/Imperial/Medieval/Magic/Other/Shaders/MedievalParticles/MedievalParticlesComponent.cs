using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Medieval.Magic.Shaders;


/// <summary>
/// When this component is added, it turns the sprite into a fireball.
/// </summary>
[RegisterComponent, Serializable]
public sealed partial class MedievalParticlesComponent : Component
{
    [DataField]
    public int ParticlesCount = 32;

    [DataField]
    public float Gravity = 0.72f;

    [DataField]
    public float Speed = 1.0f;

    [DataField]
    public Color Color = Color.Red;

    [DataField]
    public bool Inverted = false;

    [DataField]
    public bool CollapseOnMinDistance = false;

    [DataField]
    public bool DisappearOnMinDistance = false;


    [ViewVariables]
    public TimeSpan SpawnTime = TimeSpan.FromSeconds(0);

    [ViewVariables]
    public float Seed = 0.32f;
}

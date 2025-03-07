namespace Content.Server.Imperial.Medieval.Magic.MedievalBlackHoleExplosion;


/// <summary>
/// A hardcode black hole spell explosion on trigger
/// </summary>
[RegisterComponent]
public sealed partial class MedievalBlackHoleExplosionComponent : Component
{
    [DataField]
    public float BlackHoleSizePerTick = 0.01f;

    [DataField]
    public float BlackHoleLightEnergyPerTick = 0.03f;

    [DataField]
    public float BlackHoleLightRangePerTick = 0.03f;


    [DataField]
    public Color TargetColor = Color.FromHex("#007ACC");


    [DataField]
    public TimeSpan ExplodeTime = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan ExplodeUpdateRate = TimeSpan.FromSeconds(0.1);


    [ViewVariables]
    public TimeSpan NextExplodeUpdate = TimeSpan.Zero;

    [ViewVariables]
    public Color? InitialColor;

    [ViewVariables]
    public TimeSpan WhenExplode = TimeSpan.Zero;

    [ViewVariables]
    public bool Active = false;
}

using System.Numerics;
using Robust.Shared.Map;

namespace Content.Server.Imperial.Medieval.Magic.MedievalHomingProjectile;


/// <summary>
///
/// </summary>
[RegisterComponent]
public sealed partial class MedievalHomingProjectileComponent : Component
{
    [DataField]
    public float LinearVelocityIntensy = 1.0f;

    [DataField]
    public Angle RelativeAngle = Angle.Zero;

    [DataField]
    public bool RotateToTarget = true;

    [DataField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(0.01);

    [ViewVariables]
    public Vector2 TargetCoords = Vector2.Zero;

    [ViewVariables]
    public MapCoordinates? MapTarget;

    [ViewVariables]
    public EntityUid? EntityTarget;

    [ViewVariables]
    public Vector2i Signs;

    [ViewVariables]
    public TimeSpan NextUpdate = TimeSpan.Zero;
}

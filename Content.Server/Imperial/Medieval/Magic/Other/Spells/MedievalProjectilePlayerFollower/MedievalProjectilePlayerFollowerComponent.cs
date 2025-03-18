namespace Content.Server.Imperial.Medieval.Magic.MedievalProjectilePlayerFollower;


/// <summary>
///
/// </summary>
[RegisterComponent]
public sealed partial class MedievalProjectilePlayerFollowerComponent : Component
{
    [DataField]
    public TimeSpan ActivateTime = TimeSpan.Zero;

    [ViewVariables]
    public TimeSpan NextTargetSelect = TimeSpan.Zero;

    [ViewVariables]
    public EntityUid Target;

    [ViewVariables]
    public bool Binded = false;
}

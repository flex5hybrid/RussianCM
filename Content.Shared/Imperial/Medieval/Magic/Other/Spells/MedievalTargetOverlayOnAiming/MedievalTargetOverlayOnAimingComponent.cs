using Robust.Shared.Prototypes;

namespace Content.Shared.Imperial.Medieval.Magic.MedievalTargetOverlayOnAiming;


/// <summary>
///
/// </summary>
[RegisterComponent]
public sealed partial class MedievalTargetOverlayOnAimingComponent : Component
{
    [DataField]
    public int MaxTargetCount = 1;


    [DataField]
    public ComponentRegistry WhiteListComponents = new();

    [DataField]
    public ComponentRegistry BlackListComponents = new();
}

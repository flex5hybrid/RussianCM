using Content.Shared.EntityEffects;

namespace Content.Server.Imperial.Medieval.Magic.MagicTraining.KillingTraining;


/// <summary>
/// Allows get essence from projectile hit
/// </summary>
[RegisterComponent]
public sealed partial class KillingTrainingComponent : Component
{
    [DataField]
    public List<EntityEffect> TrainingResults = new();
}

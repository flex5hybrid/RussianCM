using Content.Shared.EntityEffects;

namespace Content.Server.Imperial.Medieval.Magic.MagicTraining.DamageTraining;


/// <summary>
/// Allows get essence from projectile hit
/// </summary>
[RegisterComponent]
public sealed partial class DamageTrainingComponent : Component
{
    [DataField]
    public List<EntityEffect> TrainingResults = new();
}

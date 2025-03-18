using Content.Shared.EntityEffects;

namespace Content.Server.Imperial.Medieval.Magic.MagicTraining.CastTraining;


/// <summary>
/// Allows get essence from succes spell cast
/// </summary>
[RegisterComponent]
public sealed partial class CastTrainingComponent : Component
{
    [DataField]
    public List<EntityEffect> TrainingResults = new();
}

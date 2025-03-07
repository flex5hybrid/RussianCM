using Content.Shared.EntityEffects;

namespace Content.Server.Imperial.Medieval.Magic.MagicTraining.DamageTraining;


/// <summary>
/// Add essence to caster after damage
/// </summary>
[RegisterComponent]
public sealed partial class MedievalAddEssenceOnProjectileHitComponent : Component
{
    /// <summary>
    /// Training results
    /// </summary>
    [DataField]
    public List<EntityEffect> TrainingResults = new();

    /// <summary>
    /// The parent action that fired this projectile
    /// </summary>
    [ViewVariables]
    public EntityUid Action;

    /// <summary>
    /// A SUPER SEXY MAGICAN
    /// </summary>
    [ViewVariables]
    public EntityUid Performer;
}

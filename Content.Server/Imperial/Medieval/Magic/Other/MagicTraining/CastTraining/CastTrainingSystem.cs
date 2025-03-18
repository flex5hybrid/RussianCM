using Content.Shared.Imperial.Medieval.Magic;
using Robust.Shared.Random;
using Content.Shared.EntityEffects;
using System.Linq;


namespace Content.Server.Imperial.Medieval.Magic.MagicTraining.CastTraining;


/// <summary>
/// This system add currency after succes spell cast
/// </summary>
public sealed partial class CastTrainingSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CastTrainingComponent, MedievalAfterCastSpellEvent>(OnSpellCast);
    }

    private void OnSpellCast(EntityUid uid, CastTrainingComponent component, MedievalAfterCastSpellEvent args)
    {
        foreach (var trainingResult in component.TrainingResults)
        {
            var magicEntityEffectsArgs = new MagicEntityEffectsArgs(args.Performer, args.Performer, uid, EntityManager);
            var canApplyTrainingResult = trainingResult.Conditions?.Aggregate(true, (acc, cond) => acc && cond.Condition(magicEntityEffectsArgs)) ?? true;

            if (
                canApplyTrainingResult &&
                _random.Prob(trainingResult.Probability)
            )
                trainingResult.Effect(magicEntityEffectsArgs);
        }
    }
}

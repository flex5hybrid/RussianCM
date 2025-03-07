namespace Content.Shared.EntityEffects;


public record class MagicEntityEffectsArgs : EntityEffectBaseArgs
{
    public EntityUid Performer;

    public EntityUid Action;


    public MagicEntityEffectsArgs(
        EntityUid targetUid,
        EntityUid performerUid,
        EntityUid actionUid,
        EntityManager entityManager
    ) : base(targetUid, entityManager)
    {
        Performer = performerUid;
        Action = actionUid;
    }
}

using Content.Server.Flash;
using Content.Shared.EntityEffects;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReactionEffects;


[DataDefinition]
public sealed partial class ImperialFlashReactionEffect : EntityEffect
{
    [DataField("maxRange", required: true)]
    public float MaxRange = 10;

    [DataField("maxDuration")]
    public float MaxDuration = 3.0f;

    [DataField("slowTo")]
    public float SlowTo = 0.8f;

    [DataField("powerPerUnit")]
    public float PowerPerUnit = 0.25f;

    [DataField]
    public bool SlowOnlyTarget = false;

    /// <summary>
    ///     The prototype ID used for the visual effect.
    /// </summary>
    [DataField]
    public EntProtoId? FlashEffectPrototype = "ReactionFlash";


    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-flash",
            ("chance", Probability)
        );

    public override void Effect(EntityEffectBaseArgs args)
    {
        var flasSystem = args.EntityManager.EntitySysManager.GetEntitySystem<FlashSystem>();
        var transformSystem = args.EntityManager.System<SharedTransformSystem>();

        var transform = args.EntityManager.GetComponent<TransformComponent>(args.TargetEntity);
        var uid = args.EntityManager.SpawnEntity(FlashEffectPrototype, transformSystem.GetMapCoordinates(transform));

        var range = 1f;

        transformSystem.AttachToGridOrMap(uid);

        if (args.EntityManager.TryGetComponent<PointLightComponent>(uid, out var pointLightComp))
        {
            var pointLightSystem = args.EntityManager.System<SharedPointLightSystem>();
            // PointLights with a radius lower than 1.1 are too small to be visible, so this is hardcoded
            pointLightSystem.SetRadius(uid, MathF.Max(1.1f, range), pointLightComp);
        }

        if (args is not EntityEffectReagentArgs reagentArgs)
        {
            if (SlowOnlyTarget)
            {
                flasSystem.Flash(
                    args.TargetEntity,
                    null,
                    null,
                    MaxDuration * 1000f,
                    1.0f
                );

                return;
            }

            flasSystem.FlashArea(
                args.TargetEntity,
                null,
                MaxRange,
                MaxDuration * 1000f,
                SlowTo
            );

            return;
        }

        range = MathF.Min((float)(reagentArgs.Quantity * PowerPerUnit), MaxRange);
        var duration = MathF.Min((float)(reagentArgs.Quantity * PowerPerUnit), MaxDuration) * 1000f;

        if (SlowOnlyTarget)
        {
            flasSystem.Flash(
                args.TargetEntity,
                null,
                null,
                duration * 1000f,
                SlowTo
            );

            return;
        }

        flasSystem.FlashArea(
            args.TargetEntity,
            null,
            range,
            duration,
            SlowTo
        );
    }
}

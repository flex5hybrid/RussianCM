using System.Numerics;
using Content.Shared.Imperial.ImperialLightning;
using Content.Shared.Imperial.ImperialLightning.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.Imperial.ImperialLightning;


public sealed partial class ImperialLightningSystem : SharedImperialLightningSystem
{
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImperialLightningReciverComponent, ComponentStartup>(OnStartup);

        InitializeCommand();
    }

    private void OnStartup(EntityUid uid, ImperialLightningReciverComponent component, ComponentStartup args)
    {
        var enumerator = EntityQueryEnumerator<ImperialLightningSenderComponent>();

        while (enumerator.MoveNext(out var sender, out var lightningSenderComponent))
        {
            if (lightningSenderComponent.SendingFrequency != component.ReceiptFrequency) return;

            SpawnLightningBetween(
                sender,
                uid,
                lightningSenderComponent.LightningColor,
                lightningSenderComponent.Offset,
                lightningSenderComponent.LifeTime,
                lightningSenderComponent.Speed,
                lightningSenderComponent.Intensity,
                lightningSenderComponent.Seed,
                lightningSenderComponent.Amplitude,
                lightningSenderComponent.Frequency
            );
        }
    }

    #region Public API

    #region SetNonCancelableLightningTarget

    public void SpawnLightningBetween(
        Vector2 startCoords,
        EntityUid target,
        Color? lightningColor = null,
        Vector2? offset = null,
        TimeSpan? lifeTime = null,
        float? speed = 0.0f,
        float? intensity = 2.0f,
        float? seed = 0.0f,
        float? amplitude = 0.2f,
        float? frequency = 3.0f
    )
    {
        RaiseNetworkEvent(new SpawnLightningEffectsMessage()
        {
            StartPoint = (startCoords, null),
            TargetPoint = (_transformSystem.GetWorldPosition(target), GetNetEntity(target)),
            LightningColor = lightningColor ?? Color.FromHex("#1A40F0"),
            Offset = offset ?? Vector2.Zero,
            Speed = speed ?? 0.0f,
            Intensity = intensity ?? 2.0f,
            Seed = seed ?? (float)_timing.CurTime.TotalSeconds,
            Amplitude = amplitude ?? 0.2f,
            Frequency = frequency ?? 3.0f,

            LifeTime = lifeTime ?? TimeSpan.FromSeconds(1)
        });
    }

    public void SpawnLightningBetween(
        EntityUid fromEntity,
        EntityUid target,
        Color? lightningColor = null,
        Vector2? offset = null,
        TimeSpan? lifeTime = null,
        float? speed = 0.0f,
        float? intensity = 2.0f,
        float? seed = 0.0f,
        float? amplitude = 0.2f,
        float? frequency = 3.0f
    )
    {
        RaiseNetworkEvent(new SpawnLightningEffectsMessage()
        {
            StartPoint = (_transformSystem.GetWorldPosition(fromEntity), GetNetEntity(fromEntity)),
            TargetPoint = (_transformSystem.GetWorldPosition(target), GetNetEntity(target)),
            LightningColor = lightningColor ?? Color.FromHex("#1A40F0"),
            Offset = offset ?? Vector2.Zero,
            Speed = speed ?? 0.0f,
            Intensity = intensity ?? 2.0f,
            Seed = seed ?? (float)_timing.CurTime.TotalSeconds,
            Amplitude = amplitude ?? 0.2f,
            Frequency = frequency ?? 3.0f,

            LifeTime = lifeTime ?? TimeSpan.FromSeconds(1)
        });
    }

    public void SpawnLightningBetween(
        EntityUid fromEntity,
        Vector2 target,
        Color? lightningColor = null,
        Vector2? offset = null,
        TimeSpan? lifeTime = null,
        float? speed = 0.0f,
        float? intensity = 2.0f,
        float? seed = 0.0f,
        float? amplitude = 0.2f,
        float? frequency = 3.0f
    )
    {
        RaiseNetworkEvent(new SpawnLightningEffectsMessage()
        {
            StartPoint = (_transformSystem.GetWorldPosition(fromEntity), GetNetEntity(fromEntity)),
            TargetPoint = (target, null),
            LightningColor = lightningColor ?? Color.FromHex("#1A40F0"),
            Offset = offset ?? Vector2.Zero,
            Speed = speed ?? 0.0f,
            Intensity = intensity ?? 2.0f,
            Seed = seed ?? (float)_timing.CurTime.TotalSeconds,
            Amplitude = amplitude ?? 0.2f,
            Frequency = frequency ?? 3.0f,

            LifeTime = lifeTime ?? TimeSpan.FromSeconds(1)
        });
    }

    public void SpawnLightningBetween(
        Vector2 startCoords,
        Vector2 target,
        Color? lightningColor = null,
        Vector2? offset = null,
        TimeSpan? lifeTime = null,
        float? speed = 0.0f,
        float? intensity = 2.0f,
        float? seed = 0.0f,
        float? amplitude = 0.2f,
        float? frequency = 3.0f
    )
    {
        RaiseNetworkEvent(new SpawnLightningEffectsMessage()
        {
            StartPoint = (startCoords, null),
            TargetPoint = (target, null),
            LightningColor = lightningColor ?? Color.FromHex("#1A40F0"),
            Offset = offset ?? Vector2.Zero,
            Speed = speed ?? 0.0f,
            Intensity = intensity ?? 2.0f,
            Seed = seed ?? (float)_timing.CurTime.TotalSeconds,
            Amplitude = amplitude ?? 0.2f,
            Frequency = frequency ?? 3.0f,

            LifeTime = lifeTime ?? TimeSpan.FromSeconds(1)
        });
    }


    #endregion

    #endregion
}

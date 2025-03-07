using Content.Shared.Imperial.ColorHelper;
using Content.Shared.Imperial.ImperialLightning;
using Content.Shared.Imperial.ImperialLightning.Events;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Timing;

namespace Content.Client.Imperial.ImperialLightning;


public sealed partial class ImperialLightningSystem : SharedImperialLightningSystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;


    private ImperialLightningOverlay _overlay = default!;


    public override void Initialize()
    {
        base.Initialize();

        _overlay = new();

        SubscribeNetworkEvent<SpawnLightningEffectsMessage>(OnSpawnLightning);
    }

    private void OnSpawnLightning(SpawnLightningEffectsMessage args)
    {
        if (!_overlayManager.HasOverlay(_overlay.GetType()))
        {
            if (_playerManager.LocalEntity == null) return;

            _overlayManager.AddOverlay(_overlay);
        }

        _overlay.AddLightning(
            (args.StartPoint.StartCoords, GetEntity(args.StartPoint.StartEntityPoint)),
            (args.TargetPoint.TargetCoords, GetEntity(args.TargetPoint.TargetEntityPoint)),
            ColorHelper.ToVector3(args.LightningColor),
            args.Offset,
            args.Speed,
            args.Intensity,
            args.Seed,
            args.Amplitude,
            args.Frequency,
            args.LifeTime + _timing.CurTime
        );
    }

    #region Helpers


    #endregion
}

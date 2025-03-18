using Content.Shared.Imperial.ShockWave;
using Robust.Shared.Timing;

namespace Content.Client.Imperial.ShockWave;

public sealed partial class ShockWaveSystem : SharedShockWaveSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShockWaveDistortionComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, ShockWaveDistortionComponent component, ComponentInit args)
    {
        component.SpawnTime = _timing.CurTime;
    }
}

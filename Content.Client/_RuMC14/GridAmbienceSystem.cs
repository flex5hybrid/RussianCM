using Content.Shared.Audio;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Content.Shared._RuMC14.Audio;

namespace Content.Client._RuMC14.Audio;

public sealed class GridAmbienceSystem : SharedGridAmbienceSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityUid? _stream;
    private EntityUid? _activeGrid;
    private SoundSpecifier? _activeSound;
    private float _activeVolume;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;

        SubscribeLocalEvent<GridAmbienceComponent, ComponentShutdown>(OnComponentShutdown);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        StopCurrent();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        if (_player.LocalEntity is not { } player)
        {
            StopCurrent();
            return;
        }

        if (!TryComp<TransformComponent>(player, out var xform))
        {
            StopCurrent();
            return;
        }

        if (xform.GridUid is not { } gridUid)
        {
            StopCurrent();
            return;
        }

        if (!TryComp<GridAmbienceComponent>(gridUid, out var ambience))
        {
            StopCurrent();
            return;
        }

        if (!ambience.Enabled)
        {
            StopCurrent();
            return;
        }

        if (_activeGrid == gridUid &&
            _stream != null &&
            _activeSound == ambience.Sound &&
            MathHelper.CloseToPercent(_activeVolume, ambience.Volume))
        {
            return;
        }

        StopCurrent();

        var sound = ambience.Sound;
        if (sound == null)
        {
            return;
        }

        var audioParams = sound.Params
            .WithLoop(true)
            .WithVolume(ambience.Volume);

        var result = _audio.PlayGlobal(ambience.Sound, Filter.Local(), false, audioParams);

        if (result == null)
        {
            return;
        }

        _stream = result.Value.Entity;
        _activeGrid = gridUid;
        _activeSound = ambience.Sound;
        _activeVolume = ambience.Volume;
    }

    private void OnComponentShutdown(EntityUid uid, GridAmbienceComponent component, ComponentShutdown args)
    {
        if (_activeGrid == uid)
            StopCurrent();
    }

    private void StopCurrent()
    {
        _audio.Stop(_stream);

        _stream = null;
        _activeGrid = null;
        _activeSound = null;
        _activeVolume = 0f;
    }
}

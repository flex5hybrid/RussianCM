using System.IO;
using System.Threading.Tasks;
using Content.Client.CMU14.Cassette;
using Content.Shared.CMU14.Cassette;
using Content.Shared._RMC14.Cassette;
using Content.Shared._RMC14.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Client.Audio;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
// using Robust.Shared.Prototypes; // CMU14: cassette songs load on demand.
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Cassette;

public sealed partial class CassetteSystem : SharedCassetteSystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private IAudioManager _audioManager = default!;
    // [Dependency] private IComponentFactory _compFactory = default!; // CMU14
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IFileDialogManager _dialogs = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private IPlayerManager _player = default!;
    // [Dependency] private IPrototypeManager _prototype = default!; // CMU14
    [Dependency] private IResourceCache _resourceCache = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private CMUDynamicCassetteSystem _dynamicCassettes = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private float _gain;
    private readonly Dictionary<AudioStream, string> _names = new();
    private bool _trackDialogOpen;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<CassettePlayerComponent, ComponentRemove>(OnPlayerRemoved); // CMU14

        Subs.CVar(_config, RMCCVars.VolumeGainCassettes, SetGain, true);
        _dynamicCassettes.UploadFinished += OnDynamicUploadFinished;

        // CMU14 Begin: AudioSystem loads songs when played. Preloading every tape retains
        // the entire decoded music library in native audio buffers for the client's lifetime.
        /*
        try
        {
            foreach (var entity in _prototype.EnumeratePrototypes<EntityPrototype>())
            {
                if (!entity.TryComp(out CassetteTapeComponent? tape, _compFactory))
                    continue;

                foreach (var sound in tape.Songs)
                {
                    var path = _audio.GetAudioPath(_audio.ResolveSound(sound));
                    _resourceCache.TryGetResource(new ResPath(path), out AudioResource? _);
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error preloading cassette songs:\n{e}");
        }
        */
        // CMU14 End
    }

    public override void Shutdown()
    {
        _dynamicCassettes.UploadFinished -= OnDynamicUploadFinished;
        base.Shutdown();
    }

    // CMU14: reconcile local audio after state application and prediction replay have finished.
    // Applying an intermediate rollback state directly would stop a freshly predicted track.
    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var players = EntityQueryEnumerator<CassettePlayerComponent>();
        while (players.MoveNext(out var uid, out var player))
        {
            if (player.CustomAudioStream == null)
                continue;

            if (player.State == AudioState.Stopped)
                StopCustomTrack((uid, player));
            else
                _audio.SetState(player.CustomAudioStream, player.State);
        }
    }

    private void OnPlayerRemoved(Entity<CassettePlayerComponent> ent, ref ComponentRemove args)
    {
        StopCustomTrack(ent);
    }

    private void StopCustomTrack(Entity<CassettePlayerComponent> player)
    {
        // This entity is client-owned. Authoritative removal must also clean it up during state application.
        if (player.Comp.CustomAudioStream is { } stream && IsClientSide(stream))
            QueueDel(stream);
        player.Comp.CustomAudioStream = null;
    }
    // CMU14 end

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _names.Clear();
    }

    private void SetGain(float gain)
    {
        _gain = gain;
        if (_player.LocalEntity is not { } ent)
            return;

        var slots = _inventory.GetSlotEnumerator(ent);
        while (slots.MoveNext(out var slot))
        {
            if (slot.ContainedEntity is not { } contained ||
                !TryComp(contained, out CassettePlayerComponent? player))
            {
                continue;
            }

            SetAudioGain(player.AudioStream);
            SetAudioGain(player.CustomAudioStream);
        }
    }

    protected override EntityUid? PlayCustomTrack(Entity<CassettePlayerComponent> player, Entity<CassetteTapeComponent> tape)
    {
        base.PlayCustomTrack(player, tape);
        if (tape.Comp.CustomTrack is not AudioStream stream)
            return null;

        if (!_timing.IsFirstTimePredicted)
            return null;

        if (!_names.TryGetValue(stream, out var name))
            return null;

        var audioParams = player.Comp.AudioParams.WithVolume(SharedAudioSystem.GainToVolume(_gain));
        return _audio.PlayEntity(stream, player, new ResolvedPathSpecifier(name), audioParams)?.Entity;
    }

    protected override async void ChooseCustomTrack(Entity<CassetteTapeComponent> tape)
    {
        try
        {
            if (!_timing.IsFirstTimePredicted || _trackDialogOpen)
                return;

            _trackDialogOpen = true;
            var filters = new FileDialogFilters(new FileDialogFilters.Group("ogg"));
            var (file, fileName) = await _dialogs.GetFileAndName(filters, FileAccess.Read);
            if (file == null)
                return;

            await using (file)
            {
                if (TryComp(tape, out CMUDynamicCassetteComponent? dynamicCassette))
                {
                    await ChooseDynamicTrack(tape, dynamicCassette, file, fileName);
                    return;
                }

                var audio = _audioManager.LoadAudioOggVorbis(file);
                tape.Comp.CustomTrack = audio;

                var name = $"/Audio/_RMC14/_CustomCassetteUploads/upload_{_names.Count}.ogg";
                _resourceCache.CacheResource(name, new AudioResource(audio));
                _names[audio] = name;
            }
        }
        catch (Exception e)
        {
            ShowUploadPopup("cmu-cassette-upload-invalid-file");
            Log.Error($"Ошибка выбора пользовательского трека / Error choosing custom cassette track:\n{e}");
        }
        finally
        {
            _trackDialogOpen = false;
        }
    }

    private async Task ChooseDynamicTrack(Entity<CassetteTapeComponent> tape,
        CMUDynamicCassetteComponent dynamicCassette,
        Stream file,
        string? fileName)
    {
        if (dynamicCassette.Tracks.Count >= dynamicCassette.MaxTracks)
        {
            ShowUploadPopup("cmu-cassette-upload-cassette-full");
            return;
        }

        var trackData = await ReadBoundedTrack(file);
        if (trackData == null || !CMUDynamicCassetteLimits.IsValidOggVorbis(trackData))
        {
            ShowUploadPopup("cmu-cassette-upload-invalid-file");
            return;
        }

        using (var validationStream = new MemoryStream(trackData, writable: false))
        using (var decodedTrack = _audioManager.LoadAudioOggVorbis(validationStream, fileName))
        {
            if (decodedTrack.Length > TimeSpan.FromSeconds(CMUDynamicCassetteLimits.MaxDurationSeconds))
            {
                ShowUploadPopup("cmu-cassette-upload-invalid-file");
                return;
            }
        }

        var title = GetTrackTitle(fileName);
        if (!CMUDynamicCassetteLimits.IsTitleValid(title))
            title = $"Трек / Track {dynamicCassette.Tracks.Count + 1}";

        if (!_dynamicCassettes.TryQueueUpload(tape, title, trackData))
        {
            ShowUploadPopup("cmu-cassette-upload-busy");
            return;
        }

        ShowUploadPopup("cmu-cassette-upload-started");
    }

    private static string GetTrackTitle(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;

        var separatorIndex = Math.Max(fileName.LastIndexOf('/'), fileName.LastIndexOf('\\'));
        var titleStart = separatorIndex + 1;
        var extensionIndex = fileName.LastIndexOf('.');
        var titleLength = extensionIndex > titleStart
            ? extensionIndex - titleStart
            : fileName.Length - titleStart;
        return fileName.Substring(titleStart, titleLength).Trim();
    }

    private static async Task<byte[]?> ReadBoundedTrack(Stream file)
    {
        if (file.CanSeek && file.Length > CMUDynamicCassetteLimits.MaxTrackBytes)
            return null;

        var buffer = new byte[CMUDynamicCassetteLimits.MaxTrackBytes + 1];
        var bytesRead = 0;
        while (bytesRead < buffer.Length)
        {
            var read = await file.ReadAsync(buffer.AsMemory(bytesRead, buffer.Length - bytesRead));
            if (read == 0)
                break;
            bytesRead += read;
        }

        if (bytesRead == 0 || bytesRead > CMUDynamicCassetteLimits.MaxTrackBytes)
            return null;

        Array.Resize(ref buffer, bytesRead);
        return buffer;
    }

    private void OnDynamicUploadFinished(CMUCassetteUploadStatus status, Guid _)
    {
        var localization = status switch
        {
            CMUCassetteUploadStatus.Success => "cmu-cassette-upload-success",
            CMUCassetteUploadStatus.CassetteFull => "cmu-cassette-upload-cassette-full",
            CMUCassetteUploadStatus.RoundStorageFull => "cmu-cassette-upload-round-full",
            CMUCassetteUploadStatus.UserStorageFull => "cmu-cassette-upload-user-full",
            CMUCassetteUploadStatus.RateLimited => "cmu-cassette-upload-rate-limited",
            CMUCassetteUploadStatus.InvalidPermit => "cmu-cassette-upload-permit-expired",
            _ => "cmu-cassette-upload-failed",
        };
        ShowUploadPopup(localization);
    }

    private void ShowUploadPopup(string localization)
    {
        if (_player.LocalEntity is { } player)
            _popup.PopupEntity(Loc.GetString(localization), player, player);
    }

    private void SetAudioGain(EntityUid? audio)
    {
        if (!TryComp(audio, out AudioComponent? audioComp))
            return;

#pragma warning disable RA0002
        audioComp.Params = audioComp.Params with { Volume = SharedAudioSystem.GainToVolume(_gain) };
#pragma warning restore RA0002
    }
}

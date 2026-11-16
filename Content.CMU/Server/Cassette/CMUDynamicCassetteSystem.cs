using Content.Shared.CMU14.Cassette;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared._RMC14.Cassette;
using Content.Server._RMC14.Cassette;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.CMU14.Cassette;

/// <summary>
/// Хранит загруженное кассетное аудио в течение раунда и раздаёт проверенные треки клиентам. /
/// Owns uploaded cassette audio for one round and distributes validated tracks to clients.
/// Системы предметов и интерфейса используют API загрузки, но не управляют хранилищем аудио. /
/// Item and UI systems use the upload API but do not own audio storage.
/// </summary>
public sealed class CMUDynamicCassetteSystem : EntitySystem
{
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly IServerNetManager _net = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IResourceManager _resources = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly CassetteSystem _cassette = default!;

    private readonly MemoryContentRoot _content = new();
    private readonly Dictionary<Guid, byte[]> _tracks = new();
    private readonly Dictionary<Guid, UploadPermit> _permits = new();
    private readonly Dictionary<NetUserId, Guid> _activePermits = new();
    private readonly Dictionary<NetUserId, TimeSpan> _lastPermits = new();
    private readonly Dictionary<NetUserId, int> _userStoredBytes = new();
    private int _storedBytes;

    public override void Initialize()
    {
        base.Initialize();

        _resources.AddRoot(CMUDynamicCassetteLimits.ContentPrefix, _content);
        _net.RegisterNetMessage<CMUCassetteUploadMsg>(OnUpload);
        _net.RegisterNetMessage<CMUCassetteUploadPermitMsg>();
        _net.RegisterNetMessage<CMUCassetteTrackDataMsg>();
        _net.RegisterNetMessage<CMUCassetteUploadResultMsg>();
        _net.Connected += OnConnected;

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    public override void Shutdown()
    {
        _net.Connected -= OnConnected;
        ClearTracks();
        base.Shutdown();
    }

    private void OnUpload(CMUCassetteUploadMsg message)
    {
        if (!_players.TryGetSessionByChannel(message.MsgChannel, out var session) ||
            session.AttachedEntity is not { } user)
        {
            Reply(message, CMUCassetteUploadStatus.InvalidEntity);
            return;
        }

        // Разрешение создаётся только доверенным серверным взаимодействием и используется один раз. /
        // A permit is created only by a trusted server-side interaction and is consumed once.
        if (!_permits.TryGetValue(message.Permit, out var uploadPermit) ||
            uploadPermit.UserId != session.UserId ||
            uploadPermit.ExpiresAt < _timing.CurTime)
        {
            Reply(message, CMUCassetteUploadStatus.InvalidPermit);
            return;
        }

        _permits.Remove(message.Permit);
        _activePermits.Remove(session.UserId);

        var cassette = uploadPermit.Cassette;
        if (!TryComp(cassette, out CMUDynamicCassetteComponent? dynamicCassette) ||
            !TryComp(cassette, out CassetteTapeComponent? tape))
        {
            Reply(message, CMUCassetteUploadStatus.InvalidEntity);
            return;
        }

        if (!_interaction.InRangeUnobstructed(user, cassette))
        {
            Reply(message, CMUCassetteUploadStatus.OutOfRange);
            return;
        }

        if (dynamicCassette.Tracks.Count >= dynamicCassette.MaxTracks)
        {
            Reply(message, CMUCassetteUploadStatus.CassetteFull);
            return;
        }

        if (message.Data.Length == 0 ||
            message.Data.Length > CMUDynamicCassetteLimits.MaxTrackBytes ||
            !CMUDynamicCassetteLimits.IsTitleValid(message.Title) ||
            !CMUDynamicCassetteLimits.IsValidOggVorbis(message.Data))
        {
            Reply(message, CMUCassetteUploadStatus.InvalidFile);
            return;
        }

        if (_tracks.Count >= CMUDynamicCassetteLimits.MaxTracksPerRound ||
            _storedBytes > CMUDynamicCassetteLimits.MaxRoundBytes - message.Data.Length)
        {
            Reply(message, CMUCassetteUploadStatus.RoundStorageFull);
            return;
        }

        var userStored = _userStoredBytes.GetValueOrDefault(session.UserId);
        if (userStored > CMUDynamicCassetteLimits.MaxUserBytesPerRound - message.Data.Length)
        {
            Reply(message, CMUCassetteUploadStatus.UserStorageFull);
            return;
        }

        var trackId = Guid.NewGuid();
        var relativePath = CMUDynamicCassetteLimits.RelativePath(trackId);
        var storedTrackData = (byte[]) message.Data.Clone();
        _content.AddOrUpdateFile(relativePath, storedTrackData);
        _tracks.Add(trackId, storedTrackData);
        _storedBytes += storedTrackData.Length;
        _userStoredBytes[session.UserId] = userStored + storedTrackData.Length;

        dynamicCassette.Tracks.Add(trackId);
        dynamicCassette.Titles.Add(message.Title);
        _cassette.AppendSong((cassette, tape), new SoundPathSpecifier(CMUDynamicCassetteLimits.FullPath(trackId)));
        Dirty(cassette, dynamicCassette);

        _net.ServerSendToAll(new CMUCassetteTrackDataMsg { TrackId = trackId, Data = storedTrackData });
        Reply(message, CMUCassetteUploadStatus.Success, trackId);
    }

    private void OnConnected(object? sender, NetChannelArgs args)
    {
        foreach (var (trackId, trackData) in _tracks)
            args.Channel.SendMessage(new CMUCassetteTrackDataMsg { TrackId = trackId, Data = trackData });
    }

    /// <summary>
    /// Вызывается будущей системой записывающей машинки после проверки слота и состояния взаимодействия. /
    /// Called by the future recorder system after it validates its slot and interaction state.
    /// Клиент не может создавать или выбирать такие разрешения. /
    /// The client cannot mint or choose these permits.
    /// </summary>
    public bool IssueUploadPermit(ICommonSession session, EntityUid cassette)
    {
        if (session.AttachedEntity is not { } user ||
            !HasComp<CMUDynamicCassetteComponent>(cassette) ||
            !HasComp<CassetteTapeComponent>(cassette) ||
            !_interaction.InRangeUnobstructed(user, cassette))
        {
            return false;
        }

        if (_lastPermits.TryGetValue(session.UserId, out var last) &&
            _timing.CurTime - last < TimeSpan.FromSeconds(5))
        {
            return false;
        }

        if (_activePermits.Remove(session.UserId, out var oldPermit))
            _permits.Remove(oldPermit);

        var permitId = Guid.NewGuid();
        _permits[permitId] = new UploadPermit(session.UserId,
            cassette,
            _timing.CurTime + CMUDynamicCassetteLimits.UploadPermitLifetime);
        _activePermits[session.UserId] = permitId;
        _lastPermits[session.UserId] = _timing.CurTime;
        _net.ServerSendMessage(new CMUCassetteUploadPermitMsg
        {
            Permit = permitId,
            Cassette = GetNetEntity(cassette),
        }, session.Channel);
        return true;
    }

    private void Reply(CMUCassetteUploadMsg request, CMUCassetteUploadStatus status, Guid trackId = default)
    {
        _net.ServerSendMessage(new CMUCassetteUploadResultMsg { Status = status, TrackId = trackId }, request.MsgChannel);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args) => ClearTracks();

    private void ClearTracks()
    {
        _tracks.Clear();
        _permits.Clear();
        _activePermits.Clear();
        _lastPermits.Clear();
        _userStoredBytes.Clear();
        _content.Clear();
        _storedBytes = 0;
    }

    private readonly record struct UploadPermit(NetUserId UserId, EntityUid Cassette, TimeSpan ExpiresAt);

}

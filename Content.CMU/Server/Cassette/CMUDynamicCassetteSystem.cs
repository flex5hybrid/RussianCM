using Content.Shared.CMU14.Cassette;
using Content.Shared.GameTicking;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared._RMC14.Cassette;
using Content.Server._RMC14.Cassette;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Containers;
using Robust.Shared.ContentPack;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.CMU14.Cassette;

public sealed class CMUDynamicCassetteSystem : EntitySystem
{
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IServerNetManager _net = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IResourceManager _resources = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly CassetteSystem _cassette = default!;

    private readonly MemoryContentRoot _content = new();
    private readonly Dictionary<Guid, byte[]> _tracks = new();
    private readonly Dictionary<Guid, UploadPermit> _permits = new();
    private readonly Dictionary<Guid, PendingUpload> _uploads = new();
    private readonly Dictionary<NetUserId, Guid> _activePermits = new();
    private readonly Dictionary<NetUserId, TimeSpan> _lastPermits = new();
    private readonly Dictionary<NetUserId, int> _userStoredBytes = new();
    private readonly Dictionary<NetUserId, HashSet<Guid>> _clientTracks = new();
    private readonly HashSet<NetUserId> _clientTrackCachesReceived = new();
    private readonly Dictionary<NetUserId, CacheRequest> _cacheRequests = new();
    private readonly Queue<TrackDelivery> _trackDeliveries = new();
    private readonly HashSet<TrackDelivery> _pendingTrackDeliveries = new();
    private int _storedBytes;
    private int _pendingUploadBytes;
    private TimeSpan _nextTrackDelivery;
    private TimeSpan _nextTrackScan;

    private const int MaxPendingTrackDeliveries = 256;

    public override void Initialize()
    {
        base.Initialize();

        _resources.AddRoot(CMUDynamicCassetteLimits.ContentPrefix, _content);
        _players.PlayerStatusChanged += OnPlayerStatusChanged;

        SubscribeLocalEvent<CMUDynamicCassetteComponent, GotEquippedHandEvent>(OnCassettePickedUp);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    public override void Shutdown()
    {
        _players.PlayerStatusChanged -= OnPlayerStatusChanged;
        ClearTracks();
        base.Shutdown();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (_timing.CurTime >= _nextTrackDelivery)
        {
            _nextTrackDelivery = _timing.CurTime + TimeSpan.FromMilliseconds(100);
            DeliverNextTrack();
        }

        if (_timing.CurTime >= _nextTrackScan)
        {
            _nextTrackScan = _timing.CurTime + TimeSpan.FromMilliseconds(500);
            RemoveExpiredTransfers();
            QueueTracksInRange();
        }
    }

    private void OnCassettePickedUp(Entity<CMUDynamicCassetteComponent> cassette, ref GotEquippedHandEvent args)
    {
        if (cassette.Comp.OwnerName != null)
            return;

        cassette.Comp.OwnerName = Name(args.User);
        Dirty(cassette);
        _metaData.SetEntityName(cassette,
            Loc.GetString("cmu-cassette-tape-owner-name", ("owner", cassette.Comp.OwnerName)));
    }

    internal void OnUploadStart(CMUCassetteUploadStartMsg message)
    {
        if (!_players.TryGetSessionByChannel(message.MsgChannel, out var session) ||
            session.AttachedEntity is not { } user)
        {
            Reply(message.MsgChannel, CMUCassetteUploadStatus.InvalidEntity);
            return;
        }

        if (!_permits.TryGetValue(message.Permit, out var uploadPermit) ||
            uploadPermit.UserId != session.UserId ||
            uploadPermit.ExpiresAt < _timing.CurTime)
        {
            Reply(message.MsgChannel, CMUCassetteUploadStatus.InvalidPermit);
            return;
        }

        _permits.Remove(message.Permit);
        _activePermits.Remove(session.UserId);

        var cassette = uploadPermit.Cassette;
        if (!TryComp(cassette, out CMUDynamicCassetteComponent? dynamicCassette) ||
            !TryComp(cassette, out CassetteTapeComponent? tape))
        {
            Reply(message.MsgChannel, CMUCassetteUploadStatus.InvalidEntity);
            return;
        }

        if (!_interaction.InRangeUnobstructed(user, cassette) || !_hands.IsHolding(user, cassette))
        {
            Reply(message.MsgChannel, CMUCassetteUploadStatus.OutOfRange);
            return;
        }

        if (dynamicCassette.Tracks.Count >= dynamicCassette.MaxTracks)
        {
            Reply(message.MsgChannel, CMUCassetteUploadStatus.CassetteFull);
            return;
        }

        if (message.TotalBytes <= 0 ||
            message.TotalBytes > CMUDynamicCassetteLimits.MaxTrackBytes ||
            !CMUDynamicCassetteLimits.IsTitleValid(message.Title))
        {
            Reply(message.MsgChannel, CMUCassetteUploadStatus.InvalidFile);
            return;
        }

        if (_tracks.Count >= CMUDynamicCassetteLimits.MaxTracksPerRound ||
            _storedBytes + _pendingUploadBytes > CMUDynamicCassetteLimits.MaxRoundBytes - message.TotalBytes)
        {
            Reply(message.MsgChannel, CMUCassetteUploadStatus.RoundStorageFull);
            return;
        }

        var userStored = _userStoredBytes.GetValueOrDefault(session.UserId);
        if (userStored > CMUDynamicCassetteLimits.MaxUserBytesPerRound - message.TotalBytes)
        {
            Reply(message.MsgChannel, CMUCassetteUploadStatus.UserStorageFull);
            return;
        }

        _uploads[message.Permit] = new PendingUpload(session.UserId,
            cassette,
            message.Title,
            new byte[message.TotalBytes],
            _timing.CurTime + CMUDynamicCassetteLimits.UploadPermitLifetime);
        _pendingUploadBytes += message.TotalBytes;
    }

    internal void OnUploadChunk(CMUCassetteUploadChunkMsg message)
    {
        if (!_players.TryGetSessionByChannel(message.MsgChannel, out var session) ||
            !_uploads.TryGetValue(message.Permit, out var upload) ||
            upload.UserId != session.UserId)
        {
            Reply(message.MsgChannel, CMUCassetteUploadStatus.InvalidPermit);
            return;
        }

        if (upload.ExpiresAt < _timing.CurTime ||
            message.Offset != upload.ReceivedBytes ||
            message.Data.Length == 0 ||
            message.Data.Length > upload.Data.Length - upload.ReceivedBytes)
        {
            RemoveUpload(message.Permit);
            Reply(message.MsgChannel, CMUCassetteUploadStatus.InvalidPermit);
            return;
        }

        message.Data.CopyTo(upload.Data, upload.ReceivedBytes);
        upload.ReceivedBytes += message.Data.Length;
        if (upload.ReceivedBytes != upload.Data.Length)
            return;

        RemoveUpload(message.Permit);
        CompleteUpload(message.MsgChannel, session, upload);
    }

    private void CompleteUpload(INetChannel channel, ICommonSession session, PendingUpload upload)
    {
        if (session.AttachedEntity is not { } user ||
            !TryComp(upload.Cassette, out CMUDynamicCassetteComponent? dynamicCassette) ||
            !TryComp(upload.Cassette, out CassetteTapeComponent? tape))
        {
            Reply(channel, CMUCassetteUploadStatus.InvalidEntity);
            return;
        }

        if (!_interaction.InRangeUnobstructed(user, upload.Cassette) || !_hands.IsHolding(user, upload.Cassette))
        {
            Reply(channel, CMUCassetteUploadStatus.OutOfRange);
            return;
        }

        if (dynamicCassette.Tracks.Count >= dynamicCassette.MaxTracks)
        {
            Reply(channel, CMUCassetteUploadStatus.CassetteFull);
            return;
        }

        if (!CMUDynamicCassetteLimits.IsValidOggVorbis(upload.Data))
        {
            Reply(channel, CMUCassetteUploadStatus.InvalidFile);
            return;
        }

        if (_tracks.Count >= CMUDynamicCassetteLimits.MaxTracksPerRound ||
            _storedBytes > CMUDynamicCassetteLimits.MaxRoundBytes - upload.Data.Length)
        {
            Reply(channel, CMUCassetteUploadStatus.RoundStorageFull);
            return;
        }

        var userStored = _userStoredBytes.GetValueOrDefault(session.UserId);
        if (userStored > CMUDynamicCassetteLimits.MaxUserBytesPerRound - upload.Data.Length)
        {
            Reply(channel, CMUCassetteUploadStatus.UserStorageFull);
            return;
        }

        var trackId = Guid.NewGuid();
        var relativePath = CMUDynamicCassetteLimits.RelativePath(trackId);
        _content.AddOrUpdateFile(relativePath, upload.Data);
        _tracks.Add(trackId, upload.Data);
        _storedBytes += upload.Data.Length;
        _userStoredBytes[session.UserId] = userStored + upload.Data.Length;

        dynamicCassette.Tracks.Add(trackId);
        dynamicCassette.Titles.Add(upload.Title);
        _cassette.AppendSong((upload.Cassette, tape), new SoundPathSpecifier(CMUDynamicCassetteLimits.FullPath(trackId)));
        Dirty(upload.Cassette, dynamicCassette);

        GetClientTracks(session.UserId).Add(trackId);
        Reply(channel, CMUCassetteUploadStatus.Success, trackId);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus is SessionStatus.Disconnected or SessionStatus.Zombie)
        {
            _clientTracks.Remove(args.Session.UserId);
            _clientTrackCachesReceived.Remove(args.Session.UserId);
            _cacheRequests.Remove(args.Session.UserId);
            RemoveUploads(args.Session.UserId);
            if (_activePermits.Remove(args.Session.UserId, out var permit))
                _permits.Remove(permit);
            return;
        }

        if (args.NewStatus == SessionStatus.Connected)
        {
            _clientTracks[args.Session.UserId] = new HashSet<Guid>();
            _clientTrackCachesReceived.Remove(args.Session.UserId);
            _cacheRequests.Remove(args.Session.UserId);
            return;
        }

        if (args.NewStatus != SessionStatus.InGame)
            return;

        var request = new CacheRequest(Guid.NewGuid(), _timing.CurTime + TimeSpan.FromSeconds(30));
        _cacheRequests[args.Session.UserId] = request;
        _net.ServerSendMessage(new CMUCassetteTrackCacheRequestMsg { RequestId = request.Id }, args.Session.Channel);
    }

    internal void OnTrackCache(CMUCassetteTrackCacheMsg message)
    {
        if (!_players.TryGetSessionByChannel(message.MsgChannel, out var session) ||
            !_cacheRequests.Remove(session.UserId, out var request) ||
            request.Id != message.RequestId ||
            request.ExpiresAt < _timing.CurTime)
            return;

        var clientTracks = GetClientTracks(session.UserId);
        clientTracks.Clear();
        foreach (var trackId in message.TrackIds)
        {
            if (_tracks.ContainsKey(trackId))
                clientTracks.Add(trackId);
        }

        _clientTrackCachesReceived.Add(session.UserId);
    }

    internal void OnPermitRequest(CMUCassetteUploadPermitRequestMsg message)
    {
        if (!_players.TryGetSessionByChannel(message.MsgChannel, out var session) ||
            !TryGetEntity(message.Cassette, out var cassette) ||
            cassette is not { } cassetteUid)
        {
            Reply(message.MsgChannel, CMUCassetteUploadStatus.InvalidEntity);
            return;
        }

        var status = IssueUploadPermit(session, cassetteUid);
        if (status != CMUCassetteUploadStatus.Success)
            Reply(message.MsgChannel, status);
    }

    private CMUCassetteUploadStatus IssueUploadPermit(ICommonSession session, EntityUid cassette)
    {
        if (session.AttachedEntity is not { } user ||
            !TryComp(cassette, out CMUDynamicCassetteComponent? dynamicCassette) ||
            !HasComp<CassetteTapeComponent>(cassette) ||
            !_interaction.InRangeUnobstructed(user, cassette) ||
            !_hands.IsHolding(user, cassette))
        {
            return CMUCassetteUploadStatus.InvalidEntity;
        }

        if (dynamicCassette.Tracks.Count >= dynamicCassette.MaxTracks)
            return CMUCassetteUploadStatus.CassetteFull;

        if (_lastPermits.TryGetValue(session.UserId, out var last) &&
            _timing.CurTime - last < TimeSpan.FromSeconds(5))
        {
            return CMUCassetteUploadStatus.RateLimited;
        }

        if (_activePermits.Remove(session.UserId, out var oldPermit))
            _permits.Remove(oldPermit);
        RemoveUploads(session.UserId);

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
        return CMUCassetteUploadStatus.Success;
    }

    private void Reply(INetChannel channel, CMUCassetteUploadStatus status, Guid trackId = default) =>
        _net.ServerSendMessage(new CMUCassetteUploadResultMsg { Status = status, TrackId = trackId }, channel);

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        ClearTracks();
        foreach (var session in _players.Sessions)
        {
            if (session.Status is SessionStatus.Disconnected or SessionStatus.Zombie)
                continue;

            _clientTracks[session.UserId] = new HashSet<Guid>();
            _clientTrackCachesReceived.Add(session.UserId);
        }
    }

    private void ClearTracks()
    {
        _tracks.Clear();
        _permits.Clear();
        _uploads.Clear();
        _activePermits.Clear();
        _lastPermits.Clear();
        _userStoredBytes.Clear();
        _clientTracks.Clear();
        _clientTrackCachesReceived.Clear();
        _cacheRequests.Clear();
        _trackDeliveries.Clear();
        _pendingTrackDeliveries.Clear();
        _content.Clear();
        _storedBytes = 0;
        _pendingUploadBytes = 0;
        _nextTrackDelivery = default;
        _nextTrackScan = default;
    }

    private void QueueTracksInRange()
    {
        if (_tracks.Count == 0 || _clientTrackCachesReceived.Count == 0)
            return;

        var speakers = EntityQueryEnumerator<CassettePlayerComponent, CMUPublicCassettePlayerComponent>();
        while (speakers.MoveNext(out var speaker, out var player, out _))
        {
            if (player.State != AudioState.Playing)
                continue;

            if (!_containers.TryGetContainer(speaker, player.ContainerId, out var container) ||
                !container.ContainedEntities.TryFirstOrNull(out var cassette) ||
                !TryComp(cassette, out CMUDynamicCassetteComponent? dynamicCassette) ||
                player.Tape < 0 ||
                player.Tape >= dynamicCassette.Tracks.Count)
            {
                continue;
            }

            var trackId = dynamicCassette.Tracks[player.Tape];
            var deliveryRange = player.AudioParams.MaxDistance + 2f;
            foreach (var userId in _clientTrackCachesReceived)
            {
                if (!_players.TryGetSessionById(userId, out var session) ||
                    session.Status is SessionStatus.Disconnected or SessionStatus.Zombie ||
                    session.AttachedEntity is not { } user ||
                    !_transform.InRange(user, speaker, deliveryRange))
                {
                    continue;
                }

                var clientTracks = GetClientTracks(userId);
                var delivery = new TrackDelivery(userId, trackId);
                if (clientTracks.Contains(trackId) ||
                    !_tracks.ContainsKey(trackId) ||
                    _trackDeliveries.Count >= MaxPendingTrackDeliveries ||
                    !_pendingTrackDeliveries.Add(delivery))
                    continue;

                _trackDeliveries.Enqueue(delivery);
            }
        }
    }

    private void DeliverNextTrack()
    {
        while (_trackDeliveries.TryDequeue(out var delivery))
        {
            _pendingTrackDeliveries.Remove(delivery);
            if (!_clientTrackCachesReceived.Contains(delivery.UserId) ||
                !_players.TryGetSessionById(delivery.UserId, out var session) ||
                session.AttachedEntity is not { } user ||
                !_tracks.TryGetValue(delivery.TrackId, out var trackData) ||
                GetClientTracks(delivery.UserId).Contains(delivery.TrackId) ||
                !IsTrackAudibleTo(user, delivery.TrackId))
            {
                continue;
            }

            _net.ServerSendMessage(new CMUCassetteTrackDataMsg
            {
                TrackId = delivery.TrackId,
                Data = trackData,
            }, session.Channel);
            GetClientTracks(delivery.UserId).Add(delivery.TrackId);
            return;
        }
    }

    private bool IsTrackAudibleTo(EntityUid user, Guid trackId)
    {
        var speakers = EntityQueryEnumerator<CassettePlayerComponent, CMUPublicCassettePlayerComponent>();
        while (speakers.MoveNext(out var speaker, out var player, out _))
        {
            if (player.State != AudioState.Playing ||
                !_containers.TryGetContainer(speaker, player.ContainerId, out var container) ||
                !container.ContainedEntities.TryFirstOrNull(out var cassette) ||
                !TryComp(cassette, out CMUDynamicCassetteComponent? dynamicCassette) ||
                player.Tape < 0 ||
                player.Tape >= dynamicCassette.Tracks.Count ||
                dynamicCassette.Tracks[player.Tape] != trackId)
            {
                continue;
            }

            if (_transform.InRange(user, speaker, player.AudioParams.MaxDistance + 2f))
                return true;
        }

        return false;
    }

    private void RemoveUploads(NetUserId userId)
    {
        foreach (var (permit, upload) in _uploads.ToArray())
        {
            if (upload.UserId == userId)
                RemoveUpload(permit);
        }
    }

    private void RemoveUpload(Guid permit)
    {
        if (_uploads.Remove(permit, out var upload))
            _pendingUploadBytes -= upload.Data.Length;
    }

    private void RemoveExpiredTransfers()
    {
        foreach (var (permit, upload) in _uploads.ToArray())
        {
            if (upload.ExpiresAt < _timing.CurTime)
                RemoveUpload(permit);
        }

        foreach (var (permit, uploadPermit) in _permits.ToArray())
        {
            if (uploadPermit.ExpiresAt >= _timing.CurTime)
                continue;

            _permits.Remove(permit);
            if (_activePermits.GetValueOrDefault(uploadPermit.UserId) == permit)
                _activePermits.Remove(uploadPermit.UserId);
        }

        foreach (var (userId, request) in _cacheRequests.ToArray())
        {
            if (request.ExpiresAt < _timing.CurTime)
                _cacheRequests.Remove(userId);
        }
    }

    private HashSet<Guid> GetClientTracks(NetUserId userId)
    {
        if (_clientTracks.TryGetValue(userId, out var tracks))
            return tracks;

        tracks = new HashSet<Guid>();
        _clientTracks[userId] = tracks;
        return tracks;
    }

    private readonly record struct UploadPermit(NetUserId UserId, EntityUid Cassette, TimeSpan ExpiresAt);
    private sealed record PendingUpload(NetUserId UserId,
        EntityUid Cassette,
        string Title,
        byte[] Data,
        TimeSpan ExpiresAt)
    {
        public int ReceivedBytes;
    }
    private readonly record struct CacheRequest(Guid Id, TimeSpan ExpiresAt);
    private readonly record struct TrackDelivery(NetUserId UserId, Guid TrackId);

}

using Content.Shared.CMU14.Cassette;
using Content.Shared.GameTicking;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared._RMC14.Cassette;
using Content.Server._RMC14.Cassette;
using Robust.Server.Player;
using Robust.Shared.Audio;
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
    private readonly Dictionary<NetUserId, Guid> _activePermits = new();
    private readonly Dictionary<NetUserId, TimeSpan> _lastPermits = new();
    private readonly Dictionary<NetUserId, int> _userStoredBytes = new();
    private readonly Dictionary<NetUserId, HashSet<Guid>> _clientTracks = new();
    private readonly HashSet<NetUserId> _clientTrackCachesReceived = new();
    private int _storedBytes;
    private TimeSpan _nextTrackDelivery;

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
        if (_timing.CurTime < _nextTrackDelivery)
            return;

        _nextTrackDelivery = _timing.CurTime + TimeSpan.FromMilliseconds(500);
        DeliverTracksInRange();
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

    internal void OnUpload(CMUCassetteUploadMsg message)
    {
        if (!_players.TryGetSessionByChannel(message.MsgChannel, out var session) ||
            session.AttachedEntity is not { } user)
        {
            Reply(message, CMUCassetteUploadStatus.InvalidEntity);
            return;
        }

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

        if (!_interaction.InRangeUnobstructed(user, cassette) || !_hands.IsHolding(user, cassette))
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

        GetClientTracks(session.UserId).Add(trackId);
        Reply(message, CMUCassetteUploadStatus.Success, trackId);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus is SessionStatus.Disconnected or SessionStatus.Zombie)
        {
            _clientTracks.Remove(args.Session.UserId);
            _clientTrackCachesReceived.Remove(args.Session.UserId);
            return;
        }

        if (args.NewStatus != SessionStatus.Connected)
            return;

        _clientTracks[args.Session.UserId] = new HashSet<Guid>();
        _clientTrackCachesReceived.Remove(args.Session.UserId);
        _net.ServerSendMessage(new CMUCassetteTrackCacheRequestMsg(), args.Session.Channel);
    }

    internal void OnTrackCache(CMUCassetteTrackCacheMsg message)
    {
        if (!_players.TryGetSessionByChannel(message.MsgChannel, out var session))
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

    private void Reply(CMUCassetteUploadMsg request, CMUCassetteUploadStatus status, Guid trackId = default)
    {
        Reply(request.MsgChannel, status, trackId);
    }

    private void Reply(INetChannel channel, CMUCassetteUploadStatus status, Guid trackId = default) =>
        _net.ServerSendMessage(new CMUCassetteUploadResultMsg { Status = status, TrackId = trackId }, channel);

    private void OnRoundRestart(RoundRestartCleanupEvent args) => ClearTracks();

    private void ClearTracks()
    {
        _tracks.Clear();
        _permits.Clear();
        _activePermits.Clear();
        _lastPermits.Clear();
        _userStoredBytes.Clear();
        _clientTracks.Clear();
        _clientTrackCachesReceived.Clear();
        _content.Clear();
        _storedBytes = 0;
        _nextTrackDelivery = default;
    }

    private void DeliverTracksInRange()
    {
        if (_tracks.Count == 0 || _clientTrackCachesReceived.Count == 0)
            return;

        var players = _players.Sessions;
        var speakers = EntityQueryEnumerator<CassettePlayerComponent, CMUPublicCassettePlayerComponent>();
        while (speakers.MoveNext(out var speaker, out var player, out _))
        {
            if (!_containers.TryGetContainer(speaker, player.ContainerId, out var container) ||
                !container.ContainedEntities.TryFirstOrNull(out var cassette) ||
                !TryComp(cassette, out CMUDynamicCassetteComponent? dynamicCassette))
            {
                continue;
            }

            var deliveryRange = player.AudioParams.MaxDistance + 2f;
            foreach (var session in players)
            {
                if (!_clientTrackCachesReceived.Contains(session.UserId) ||
                    session.Status is SessionStatus.Disconnected or SessionStatus.Zombie ||
                    session.AttachedEntity is not { } user ||
                    !_transform.InRange(user, speaker, deliveryRange))
                {
                    continue;
                }

                var clientTracks = GetClientTracks(session.UserId);
                foreach (var trackId in dynamicCassette.Tracks)
                {
                    if (clientTracks.Contains(trackId) || !_tracks.TryGetValue(trackId, out var trackData))
                        continue;

                    _net.ServerSendMessage(new CMUCassetteTrackDataMsg { TrackId = trackId, Data = trackData },
                        session.Channel);
                    clientTracks.Add(trackId);
                }
            }
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

}

using Content.Shared.CMU14.Cassette;
using Content.Shared.GameTicking;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Client.CMU14.Cassette;

public sealed class CMUDynamicCassetteSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IResourceManager _resources = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly MemoryContentRoot _content = new();
    private readonly HashSet<Guid> _knownTracks = new();
    private PendingUpload? _pendingUpload;
    private PendingUpload? _uploadInFlight;

    public event Action<CMUCassetteUploadStatus, Guid>? UploadFinished;

    public override void Initialize()
    {
        base.Initialize();

        _resources.AddRoot(CMUDynamicCassetteLimits.ContentPrefix, _content);

        SubscribeNetworkEvent<RoundRestartCleanupEvent>(_ => ClearRoundData());
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        if ((_pendingUpload is { } pending && pending.ExpiresAt < _timing.CurTime) ||
            (_uploadInFlight is { } inFlight && inFlight.ExpiresAt < _timing.CurTime))
        {
            _pendingUpload = null;
            _uploadInFlight = null;
            UploadFinished?.Invoke(CMUCassetteUploadStatus.InvalidPermit, default);
        }
    }

    public bool TryQueueUpload(EntityUid cassette, string title, byte[] trackData)
    {
        if (_pendingUpload != null ||
            _uploadInFlight != null ||
            !CMUDynamicCassetteLimits.IsTitleValid(title) ||
            !CMUDynamicCassetteLimits.IsValidOggVorbis(trackData))
        {
            return false;
        }

        var networkCassette = GetNetEntity(cassette);
        _pendingUpload = new PendingUpload(networkCassette,
            title,
            trackData,
            _timing.CurTime + CMUDynamicCassetteLimits.UploadPermitLifetime);
        _net.ClientSendMessage(new CMUCassetteUploadPermitRequestMsg
        {
            Cassette = networkCassette,
        });
        return true;
    }

    internal void OnUploadPermit(CMUCassetteUploadPermitMsg message)
    {
        if (_pendingUpload is not { } pending || pending.Cassette != message.Cassette)
            return;

        _pendingUpload = null;
        _uploadInFlight = pending with
        {
            ExpiresAt = _timing.CurTime + CMUDynamicCassetteLimits.UploadPermitLifetime,
        };
        _net.ClientSendMessage(new CMUCassetteUploadStartMsg
        {
            Permit = message.Permit,
            Title = pending.Title,
            TotalBytes = pending.TrackData.Length,
        });

        for (var offset = 0; offset < pending.TrackData.Length; offset += CMUDynamicCassetteLimits.MaxUploadChunkBytes)
        {
            var length = Math.Min(CMUDynamicCassetteLimits.MaxUploadChunkBytes, pending.TrackData.Length - offset);
            _net.ClientSendMessage(new CMUCassetteUploadChunkMsg
            {
                Permit = message.Permit,
                Offset = offset,
                Data = pending.TrackData.AsSpan(offset, length).ToArray(),
            });
        }
    }

    internal void OnTrackData(CMUCassetteTrackDataMsg message)
    {
        if (!CMUDynamicCassetteLimits.IsValidOggVorbis(message.Data))
            return;

        _content.AddOrUpdateFile(CMUDynamicCassetteLimits.RelativePath(message.TrackId), message.Data);
        RememberTrack(message.TrackId);
    }

    internal void OnTrackCacheRequest(CMUCassetteTrackCacheRequestMsg message)
    {
        var trackIds = new Guid[_knownTracks.Count];
        _knownTracks.CopyTo(trackIds);
        _net.ClientSendMessage(new CMUCassetteTrackCacheMsg
        {
            RequestId = message.RequestId,
            TrackIds = trackIds,
        });
    }

    internal void OnUploadResult(CMUCassetteUploadResultMsg message)
    {
        if (message.Status == CMUCassetteUploadStatus.Success &&
            message.TrackId != Guid.Empty &&
            _uploadInFlight is { } upload)
        {
            _content.AddOrUpdateFile(CMUDynamicCassetteLimits.RelativePath(message.TrackId), upload.TrackData);
            RememberTrack(message.TrackId);
        }

        _pendingUpload = null;
        _uploadInFlight = null;
        UploadFinished?.Invoke(message.Status, message.TrackId);
    }

    private void ClearRoundData()
    {
        _content.Clear();
        _knownTracks.Clear();
        _pendingUpload = null;
        _uploadInFlight = null;
    }

    private void RememberTrack(Guid trackId)
    {
        if (_knownTracks.Contains(trackId))
            return;

        if (_knownTracks.Count >= CMUDynamicCassetteLimits.MaxTracksPerRound)
        {
            var forgottenTrack = Guid.Empty;
            foreach (var knownTrack in _knownTracks)
            {
                forgottenTrack = knownTrack;
                break;
            }

            _knownTracks.Remove(forgottenTrack);
        }

        _knownTracks.Add(trackId);
    }

    private sealed record PendingUpload(NetEntity Cassette, string Title, byte[] TrackData, TimeSpan ExpiresAt);
}

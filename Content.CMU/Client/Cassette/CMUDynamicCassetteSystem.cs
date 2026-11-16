using Content.Shared.CMU14.Cassette;
using Content.Shared.GameTicking;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Client.CMU14.Cassette;

/// <summary>
/// Получает кассетное аудио текущего раунда и предоставляет API будущему интерфейсу записи. /
/// Receives round-scoped cassette audio and exposes an upload API for the future recorder UI.
/// </summary>
public sealed class CMUDynamicCassetteSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IResourceManager _resources = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly MemoryContentRoot _content = new();
    private PendingUpload? _pendingUpload;

    public event Action<CMUCassetteUploadStatus, Guid>? UploadFinished;

    public override void Initialize()
    {
        base.Initialize();

        _resources.AddRoot(CMUDynamicCassetteLimits.ContentPrefix, _content);
        _net.RegisterNetMessage<CMUCassetteUploadPermitRequestMsg>();
        _net.RegisterNetMessage<CMUCassetteUploadMsg>();
        _net.RegisterNetMessage<CMUCassetteUploadPermitMsg>(OnUploadPermit);
        _net.RegisterNetMessage<CMUCassetteTrackDataMsg>(OnTrackData);
        _net.RegisterNetMessage<CMUCassetteUploadResultMsg>(OnUploadResult);

        SubscribeNetworkEvent<RoundRestartCleanupEvent>(_ => ClearRoundData());
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        if (_pendingUpload is { } pending && pending.ExpiresAt < _timing.CurTime)
        {
            _pendingUpload = null;
            UploadFinished?.Invoke(CMUCassetteUploadStatus.InvalidPermit, default);
        }
    }

    public bool TryQueueUpload(EntityUid cassette, string title, byte[] trackData)
    {
        if (_pendingUpload != null ||
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

    private void OnUploadPermit(CMUCassetteUploadPermitMsg message)
    {
        if (_pendingUpload is not { } pending || pending.Cassette != message.Cassette)
            return;

        _pendingUpload = null;
        _net.ClientSendMessage(new CMUCassetteUploadMsg
        {
            Permit = message.Permit,
            Title = pending.Title,
            Data = pending.TrackData,
        });
    }

    private void OnTrackData(CMUCassetteTrackDataMsg message)
    {
        if (!CMUDynamicCassetteLimits.IsValidOggVorbis(message.Data))
            return;

        _content.AddOrUpdateFile(CMUDynamicCassetteLimits.RelativePath(message.TrackId), message.Data);
    }

    private void OnUploadResult(CMUCassetteUploadResultMsg message)
    {
        _pendingUpload = null;
        UploadFinished?.Invoke(message.Status, message.TrackId);
    }

    private void ClearRoundData()
    {
        _content.Clear();
        _pendingUpload = null;
    }

    private sealed record PendingUpload(NetEntity Cassette, string Title, byte[] TrackData, TimeSpan ExpiresAt);
}

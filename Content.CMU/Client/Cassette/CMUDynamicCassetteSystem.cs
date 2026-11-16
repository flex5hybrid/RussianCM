using Content.Shared.CMU14.Cassette;
using Content.Shared.GameTicking;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;

namespace Content.Client.CMU14.Cassette;

/// <summary>
/// Получает кассетное аудио текущего раунда и предоставляет API будущему интерфейсу записи. /
/// Receives round-scoped cassette audio and exposes an upload API for the future recorder UI.
/// </summary>
public sealed class CMUDynamicCassetteSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IResourceManager _resources = default!;

    private readonly MemoryContentRoot _content = new();
    private readonly Dictionary<NetEntity, Guid> _permits = new();

    public event Action<CMUCassetteUploadStatus, Guid>? UploadFinished;

    public override void Initialize()
    {
        base.Initialize();

        _resources.AddRoot(CMUDynamicCassetteLimits.ContentPrefix, _content);
        _net.RegisterNetMessage<CMUCassetteUploadMsg>();
        _net.RegisterNetMessage<CMUCassetteUploadPermitMsg>(OnUploadPermit);
        _net.RegisterNetMessage<CMUCassetteTrackDataMsg>(OnTrackData);
        _net.RegisterNetMessage<CMUCassetteUploadResultMsg>(OnUploadResult);

        SubscribeNetworkEvent<RoundRestartCleanupEvent>(_ => ClearRoundData());
    }

    public bool TryUpload(EntityUid cassette, string title, byte[] trackData)
    {
        var networkCassette = GetNetEntity(cassette);
        if (!_permits.Remove(networkCassette, out var uploadPermit) ||
            !CMUDynamicCassetteLimits.IsTitleValid(title) ||
            !CMUDynamicCassetteLimits.IsValidOggVorbis(trackData))
        {
            return false;
        }

        _net.ClientSendMessage(new CMUCassetteUploadMsg
        {
            Permit = uploadPermit,
            Title = title,
            Data = trackData,
        });
        return true;
    }

    private void OnUploadPermit(CMUCassetteUploadPermitMsg message)
    {
        _permits[message.Cassette] = message.Permit;
    }

    private void OnTrackData(CMUCassetteTrackDataMsg message)
    {
        if (!CMUDynamicCassetteLimits.IsValidOggVorbis(message.Data))
            return;

        _content.AddOrUpdateFile(CMUDynamicCassetteLimits.RelativePath(message.TrackId), message.Data);
    }

    private void OnUploadResult(CMUCassetteUploadResultMsg message)
    {
        UploadFinished?.Invoke(message.Status, message.TrackId);
    }

    private void ClearRoundData()
    {
        _content.Clear();
        _permits.Clear();
    }
}

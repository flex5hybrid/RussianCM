using Content.Server.CMU14.Cassette;
using Content.Shared.CMU14.Cassette;
using Robust.Shared.Network;

namespace Content.Server.Entry;

public sealed partial class EntryPoint
{
    [Dependency] private readonly IServerNetManager _cmuCassetteNet = default!;

    private void InitializeCMUDynamicCassetteNetwork()
    {
        _cmuCassetteNet.RegisterNetMessage<CMUCassetteUploadPermitRequestMsg>(message =>
            _entSys.GetEntitySystem<CMUDynamicCassetteSystem>().OnPermitRequest(message));
        _cmuCassetteNet.RegisterNetMessage<CMUCassetteUploadStartMsg>(message =>
            _entSys.GetEntitySystem<CMUDynamicCassetteSystem>().OnUploadStart(message));
        _cmuCassetteNet.RegisterNetMessage<CMUCassetteUploadChunkMsg>(message =>
            _entSys.GetEntitySystem<CMUDynamicCassetteSystem>().OnUploadChunk(message));
        _cmuCassetteNet.RegisterNetMessage<CMUCassetteUploadPermitMsg>();
        _cmuCassetteNet.RegisterNetMessage<CMUCassetteTrackDataMsg>();
        _cmuCassetteNet.RegisterNetMessage<CMUCassetteTrackCacheRequestMsg>();
        _cmuCassetteNet.RegisterNetMessage<CMUCassetteTrackCacheMsg>(message =>
            _entSys.GetEntitySystem<CMUDynamicCassetteSystem>().OnTrackCache(message));
        _cmuCassetteNet.RegisterNetMessage<CMUCassetteUploadResultMsg>();
    }
}

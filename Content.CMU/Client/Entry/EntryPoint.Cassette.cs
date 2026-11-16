using Content.Client.CMU14.Cassette;
using Content.Shared.CMU14.Cassette;
using Robust.Shared.Network;

namespace Content.Client.Entry;

public sealed partial class EntryPoint
{
    [Dependency] private readonly INetManager _cmuCassetteNet = default!;

    private void InitializeCMUDynamicCassetteNetwork()
    {
        _cmuCassetteNet.RegisterNetMessage<CMUCassetteUploadPermitRequestMsg>();
        _cmuCassetteNet.RegisterNetMessage<CMUCassetteUploadMsg>();
        _cmuCassetteNet.RegisterNetMessage<CMUCassetteUploadPermitMsg>(message =>
            _entitySystemManager.GetEntitySystem<CMUDynamicCassetteSystem>().OnUploadPermit(message));
        _cmuCassetteNet.RegisterNetMessage<CMUCassetteTrackDataMsg>(message =>
            _entitySystemManager.GetEntitySystem<CMUDynamicCassetteSystem>().OnTrackData(message));
        _cmuCassetteNet.RegisterNetMessage<CMUCassetteUploadResultMsg>(message =>
            _entitySystemManager.GetEntitySystem<CMUDynamicCassetteSystem>().OnUploadResult(message));
    }
}

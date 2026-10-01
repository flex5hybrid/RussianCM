using Content.Client._RMC14.LinkAccount;
using Content.Shared.CMU14.Sponsors;
using Robust.Client.UserInterface;

namespace Content.Client.CMU14.Sponsors;

public sealed class CMUSponsorClientSystem : EntitySystem
{
    [Dependency] private IUserInterfaceManager _ui = default!;

    public override void Initialize()
    {
        SubscribeNetworkEvent<CMUSponsorSaveResultEvent>(OnSaved);
    }

    public void Save(CMUSponsorSettings settings) => RaiseNetworkEvent(new CMUSponsorSaveSettingsEvent(settings));

    private void OnSaved(CMUSponsorSaveResultEvent ev) =>
        _ui.GetUIController<LinkAccountUIController>().SponsorSettingsSaved(ev.Success);
}

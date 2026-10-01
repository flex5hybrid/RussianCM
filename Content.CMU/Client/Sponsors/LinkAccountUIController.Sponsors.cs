using Content.Client.CMU14.Sponsors;
using Content.Shared.CMU14.Sponsors;
using Robust.Client.UserInterface.Controls;

// ReSharper disable CheckNamespace
namespace Content.Client._RMC14.LinkAccount;

public sealed partial class LinkAccountUIController
{
    private CMUSponsorTab? _sponsorTab;

    private void AddSponsorTab()
    {
        if (_patronPerksWindow == null)
            return;
        _sponsorTab = new CMUSponsorTab(_linkAccount, IoCManager.Resolve<IEntityManager>());
        _sponsorTab.SaveRequested += settings => EntityManager.System<CMUSponsorClientSystem>().Save(settings);
        _patronPerksWindow.Tabs.AddChild(_sponsorTab);
        TabContainer.SetTabTitle(_sponsorTab, Loc.GetString("cmu-sponsor-customization"));
        _patronPerksWindow.LobbyMessageTab.AddChild(new Label { Text = Loc.GetString("cmu-sponsor-lobby-review") });
        _patronPerksWindow.OnClose += () => _sponsorTab = null;
    }

    public void SponsorSettingsSaved(bool success) => _sponsorTab?.Saved(success);
}

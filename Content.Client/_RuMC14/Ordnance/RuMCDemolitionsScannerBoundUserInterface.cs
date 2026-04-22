using Content.Shared._RuMC14.Ordnance;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RuMC14.Ordnance;

[UsedImplicitly]
public sealed class RuMCDemolitionsScannerBui : BoundUserInterface
{
    private RuMCDemolitionsScannerWindow? _window;

    public RuMCDemolitionsScannerBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RuMCDemolitionsScannerWindow>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not RMCDemolitionsScannerBuiState scan)
            return;

        _window.UpdateState(scan);
    }
}

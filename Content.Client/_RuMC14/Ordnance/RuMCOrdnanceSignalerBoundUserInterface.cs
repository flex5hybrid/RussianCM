using Content.Shared._RuMC14.Ordnance.Signaler;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client._RuMC14.Ordnance;


[UsedImplicitly]
public sealed class RuMCOrdnanceSignalerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private RuMCOrdnanceSignalerWindow? _window;

    public RuMCOrdnanceSignalerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    protected override void Open()
    {
        base.Open();

        _window = new();

        _window.OnFrequencyChanged += frequency =>
        {
            if (uint.TryParse(frequency.Trim(), out var intFreq) && intFreq > 0)
                SendMessage(new SelectOrdnanceSignalerFrequencyMessage(intFreq));
            else
                SendMessage(new SelectOrdnanceSignalerFrequencyMessage(0)); // Query the current frequency
        };

        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _window?.Close();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not OrdnanceSignalerBoundUIState msg)
            return;

        _window?.Update(msg);
    }
}



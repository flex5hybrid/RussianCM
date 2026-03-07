using Content.Shared._RMC14.CrewTracker;
using JetBrains.Annotations;

namespace Content.Client._RMC14.CrewTracker;

[UsedImplicitly]
public sealed class RMCCrewPinpointerBui : BoundUserInterface
{
    private RMCCrewPinpointerWindow? _window;

    public RMCCrewPinpointerBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        EnsureWindow().OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not RMCCrewPinpointerBuiState crewState)
            return;

        EnsureWindow().Populate(crewState, target => SendPredictedMessage(new RMCCrewPinpointerSelectMsg(target)));
    }

    private RMCCrewPinpointerWindow EnsureWindow()
    {
        if (_window != null)
            return _window;

        _window = new RMCCrewPinpointerWindow();
        _window.OnClose += Close;
        _window.RefreshPressed += () => SendPredictedMessage(new RMCCrewPinpointerRefreshMsg());
        _window.ClearPressed += () => SendPredictedMessage(new RMCCrewPinpointerClearMsg());
        _window.TogglePressed += () => SendPredictedMessage(new RMCCrewPinpointerToggleMsg());
        _window.FavoritePressed += name => SendPredictedMessage(new RMCCrewPinpointerFavoriteMsg(name));
        return _window;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _window?.Close();
        _window = null;
    }
}

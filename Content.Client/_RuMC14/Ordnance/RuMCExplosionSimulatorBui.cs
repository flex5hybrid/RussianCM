using Content.Shared._RuMC14.Ordnance;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client._RuMC14.Ordnance;

[UsedImplicitly]
public sealed class RuMCExplosionSimulatorBui : BoundUserInterface
{
    private RuMCExplosionSimulatorWindow? _window;
    private RMCExplosionSimulatorBuiState? _lastState;
    private float _processingSecondsLeft;

    public RuMCExplosionSimulatorBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RuMCExplosionSimulatorWindow>();

        _window.MarinesButton.OnPressed += _ =>
            SendPredictedMessage(new RMCExplosionSimulatorTargetMsg(RMCExplosionSimulatorTarget.Marines));
        _window.SpecialForcesButton.OnPressed += _ =>
            SendPredictedMessage(new RMCExplosionSimulatorTargetMsg(RMCExplosionSimulatorTarget.SpecialForces));
        _window.XenomorphsButton.OnPressed += _ =>
            SendPredictedMessage(new RMCExplosionSimulatorTargetMsg(RMCExplosionSimulatorTarget.Xenomorphs));

        _window.SimulateButton.OnPressed += _ =>
            SendPredictedMessage(new RMCExplosionSimulatorSimulateMsg());
        _window.ReplayButton.OnPressed += _ =>
            SendPredictedMessage(new RMCExplosionSimulatorReplayMsg());

        _window.OnFrameUpdate += OnFrameUpdate;

        if (_lastState != null)
            _window.UpdateState(_lastState, _processingSecondsLeft);
    }

    private void OnFrameUpdate(FrameEventArgs args)
    {
        if (_window == null || _lastState == null || !_lastState.IsProcessing)
            return;

        _processingSecondsLeft = MathF.Max(0f, _processingSecondsLeft - args.DeltaSeconds);
        _window.UpdateState(_lastState, _processingSecondsLeft);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not RMCExplosionSimulatorBuiState simulatorState)
            return;

        _lastState = simulatorState;
        _processingSecondsLeft = simulatorState.ProcessingSecsLeft;

        if (_window == null)
            return;

        _window.UpdateState(simulatorState, _processingSecondsLeft);
    }
}

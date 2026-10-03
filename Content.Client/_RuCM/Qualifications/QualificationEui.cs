using Content.Client.Eui;
using Content.Shared._RuCM.Qualifications;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client._RuCM.Qualifications;

[UsedImplicitly]
public sealed class QualificationEui : BaseEui
{
    private readonly QualificationWindow _window;
    public QualificationEui()
    {
        _window = new((action, request) => SendMessage(new QualificationEuiRequest(action, request)));
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
    }
    public override void Opened() => _window.OpenCentered();
    public override void Closed() => _window.Dispose();
    public override void HandleState(EuiStateBase state)
    {
        if (state is QualificationEuiState training)
            _window.Update(training.View);
    }
}

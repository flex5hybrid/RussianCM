using System;
using Content.Shared._RuCM.Qualifications;

namespace Content.Client._RuCM.Qualifications;

public sealed class QualificationBoundUserInterface : BoundUserInterface
{
    private QualificationWindow? _window;
    public QualificationBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }
    protected override void Open()
    {
        base.Open();
        _window = new((action, request) => SendMessage(new QualificationBoundRequest(action, request)));
        _window.OnClose += Close; _window.OpenCentered();
        SendMessage(new QualificationBoundRequest(QualificationAction.View, new()));
    }
    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);
        if (message is QualificationBoundView training)
            _window?.Update(training.View);
    }
    protected override void Dispose(bool disposing)
    { base.Dispose(disposing); if (disposing) _window?.Dispose(); }
}

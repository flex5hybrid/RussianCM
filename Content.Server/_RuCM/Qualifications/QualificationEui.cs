using System;
using Content.Server.EUI;
using Content.Shared._RuCM.Qualifications;
using Content.Shared.Eui;

namespace Content.Server._RuCM.Qualifications;

public sealed class QualificationEui : BaseEui
{
    private readonly QualificationSystem _system;
    private Guid _target;
    private string _error = "";
    public QualificationEui(QualificationSystem system) { _system = system; }
    public override void Opened() { _target = Player.UserId; StateDirty(); }
    public override void Closed() { _system.Remove(this); }
    public override EuiStateBase GetNewState() => new QualificationEuiState(_system.View(Player, _target, _error));
    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);
        if (msg is not QualificationEuiRequest request) return;
        if (request.Request is { } parsed && parsed.Target != Guid.Empty) _target = parsed.Target;
        _system.Submit(Player, request.Action, request.Request, error => { if (IsShutDown) return; _error = error; StateDirty(); });
    }
}

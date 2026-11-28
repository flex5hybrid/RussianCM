using Content.Shared.CMU14.Photocopier.Paperwork;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Client.CMU14.Photocopier.Paperwork.UI;

[UsedImplicitly]
public sealed class CMUPaperworkCopierBoundUi : BoundUserInterface
{
    [ViewVariables]
    private CMUPaperworkCopierWindow? _window;

    public CMUPaperworkCopierBoundUi(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new CMUPaperworkCopierWindow(IoCManager.Resolve<IPrototypeManager>());
        _window.OpenCentered();

        _window.OnClose += Close;
        _window.FormSelected += form => SendMessage(new CMUPaperworkCopierChoseFormMessage(form));
        _window.PrintRequested += mode => SendMessage(new CMUPaperworkCopierPrintMessage(mode));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not CMUPaperworkCopierUiState cast)
            return;

        _window.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _window?.Dispose();
    }
}

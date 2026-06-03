using Content.Shared._AU14.WorkingJoe;
using Content.Shared._RuMC14.WorkingJoe;
using Content.Shared.Chat.Prototypes;
using Robust.Client.UserInterface;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Client._RuMC14.WorkingJoe;

public sealed partial class WorkingJoeUPPVoiceBui : BoundUserInterface
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private ILocalizationManager _loc = default!;

    private WorkingJoeVoiceWindow? _window;

    public WorkingJoeUPPVoiceBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();
        _window = new WorkingJoeVoiceWindow();
        _window.OnClose += Close;
        _window.OnLineSelected += OnLineSelected;

        var lines = new List<WorkingJoeVoiceLine>();
        foreach (var emote in _proto.EnumeratePrototypes<EmotePrototype>())
        {
            if (emote.Whitelist?.Tags == null)
                continue;
            if (!emote.Whitelist.Tags.Contains("WorkingJoeUPP"))
                continue;

            lines.Add(new WorkingJoeVoiceLine
            {
                EmoteId = emote.ID,
                DisplayName = _loc.GetString(emote.Name),
                Category = emote.Category.ToString(),
            });
        }

        _window.SetLines(lines);
        _window.OpenCentered();
    }

    private void OnLineSelected(string emoteId)
    {
        SendMessage(new WorkingJoePlayLineMessage(emoteId));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        if (_window != null)
        {
            _window.OnClose -= Close;
            _window.OnLineSelected -= OnLineSelected;
        }

        _window?.Close();
        _window = null;
    }
}

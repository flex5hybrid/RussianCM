using Content.Client.CMU14.Interface.Hud;
using Content.Client.Stylesheets;
using Content.Shared.Chat;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client.UserInterface.Systems.Chat.Widgets;

/// <summary>Terminal styling and independent drafts without tool panels over chat history.</summary>
public partial class ChatBox
{
    private readonly Dictionary<ChatSelectChannel, string> _hudChannelDrafts = new();
    private ChatSelectChannel _hudDraftChannel;
    private bool _hudEnabled;
    private PanelContainer? _cmuCaption;
    public bool HudWorkspaceEnabled => _hudEnabled;

    public void EnableHudWorkspace()
    {
        if (_hudEnabled)
            return;
        _hudEnabled = true;
        _hudDraftChannel = SelectedChannel;
        ChatWindowPanel.RasterEnabled = true;
        ChatWindowPanel.AddStyleClass(CmuHudStyle.Surface);
        ChatUIController.SetChatWindowOpacity(ChatWindowPanel, _config.GetCVar(Content.Shared.CCVar.CCVars.ChatWindowOpacity));

        // A real screen caption establishes the terminal hierarchy; native tabs and input stay owned in place.
        var caption = _cmuCaption = new PanelContainer
        {
            Name = "CmuChatCaption",
            MinHeight = 24,
            PanelOverride = CmuHudStyle.Box(CmuHudTone.Header, padding: 4),
            MouseFilter = MouseFilterMode.Ignore,
        };
        caption.AddChild(new Label
        {
            Text = Loc.GetString("cmu-hud-comms"),
            StyleClasses = { CmuHudStyle.Heading },
            MouseFilter = MouseFilterMode.Ignore,
        });
        var rows = (BoxContainer) TabHeaderPanel.Parent!;
        rows.AddChild(caption);
        caption.SetPositionInParent(0);
        CMURefreshHudStyle();
        CMURefreshChatOpacity();
    }

    public void CMURefreshHudStyle()
    {
        if (!_hudEnabled)
            return;
        var enabled = StyleNano.CrtUiEnabled;
        _cmuCaption!.Visible = enabled;
        ChatWindowPanel.RasterEnabled = enabled;
        if (enabled)
            ChatWindowPanel.AddStyleClass(CmuHudStyle.Surface);
        else
            ChatWindowPanel.RemoveStyleClass(CmuHudStyle.Surface);
    }

    public void CMURefreshChatOpacity()
    {
        ApplyTabHeaderBackground(StyleNano.CrtUiEnabled);
        if (_cmuCaption?.PanelOverride is CmuHudStyleBox box)
            box.Opacity = _config.GetCVar(Content.Shared.CCVar.CCVars.ChatWindowOpacity);
    }

    public void CMUOpenSearch()
    {
        ChatInput.SearchButton.SetSearchActive(true);
        ChatInput.SearchInput.Visible = true;
        ChatInput.SearchInput.GrabKeyboardFocus();
    }

    private void CMUOnSearchKeyBindDown(GUIBoundKeyEventArgs args)
    {
        if (args.Function == EngineKeyFunctions.TextReleaseFocus)
        {
            CloseSearch();
            ChatInput.Input.GrabKeyboardFocus();
            args.Handle();
        }
    }

    private void SaveHudChannelDraft(ChatSelectChannel channel)
    {
        if (!_hudEnabled || channel == _hudDraftChannel)
            return;
        _hudChannelDrafts[_hudDraftChannel] = ChatInput.Input.Text;
        _hudDraftChannel = channel;
        ChatInput.Input.Text = _hudChannelDrafts.GetValueOrDefault(channel, string.Empty);
    }
}

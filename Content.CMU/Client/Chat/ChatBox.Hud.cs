using System.Linq;
using System.Numerics;
using Content.Client.CMU14.Interface.Hud;
using Content.Client.UserInterface.Systems.Emotes;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Chat.Widgets;

/// <summary>Live player tools above the existing communication history and native input.</summary>
public partial class ChatBox
{
    private readonly Dictionary<ChatSelectChannel, string> _hudChannelDrafts = new();
    private ChatSelectChannel _hudDraftChannel;
    private bool _hudEnabled;
    private bool _hudCompact;
    private PanelContainer? _hudEmoteDrawer;
    private GridContainer? _hudEmoteGrid;
    private CmuHudActionsPanel? _hudActionsPanel;
    private Button? _hudActionsButton;
    private Button? _hudEmoteButton;
    private Button? _hudCompactButton;

    public event Action<bool>? HudCompactModeChanged;
    public bool HudWorkspaceEnabled => _hudEnabled;
    public bool HudCompactMode => _hudCompact;

    public void EnableHudWorkspace()
    {
        if (_hudEnabled)
            return;
        _hudEnabled = true;
        ChatWindowPanel.RasterEnabled = true;
        _hudDraftChannel = SelectedChannel;
        ChatWindowPanel.AddStyleClass(CmuHudStyle.Surface);
        ChatWindowPanel.PanelOverride = CmuHudStyle.Box(CmuHudTone.Inset);
        var rows = (BoxContainer) ChatInput.Parent!;
        var header = new BoxContainer
        {
            Name = "CmuHudChatHeader",
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 5,
            Margin = new Thickness(8, 5, 8, 4),
        };
        header.AddChild(new Label
        {
            Text = Loc.GetString("cmu-hud-comms"),
            StyleClasses = { CmuHudStyle.Heading },
            HorizontalExpand = true,
        });
        ChatInput.SearchButton.Orphan();
        ChatInput.FilterButton.Orphan();
        header.AddChild(ChatInput.SearchButton);
        header.AddChild(ChatInput.FilterButton);
        var latest = HudButton("↓", "CmuHudLatest");
        latest.HorizontalExpand = false;
        latest.ToolTip = Loc.GetString("cmu-hud-latest");
        latest.OnPressed += _ =>
        {
            Contents.ScrollToBottom();
            SecondaryContents.ScrollToBottom();
            LegacyContents.ScrollToBottom();
        };
        header.AddChild(latest);
        _hudCompactButton = HudButton("↙", "CmuHudCompactToggle");
        _hudCompactButton.HorizontalExpand = false;
        _hudCompactButton.ToolTip = Loc.GetString("cmu-hud-compact");
        _hudCompactButton.OnPressed += _ => SetHudCompact(!_hudCompact);
        header.AddChild(_hudCompactButton);
        rows.AddChild(header);
        header.SetPositionInParent(0);
        ChatInput.SearchInput.Orphan();
        ChatInput.SearchInput.Margin = new Thickness(8, 3, 8, 5);
        rows.AddChild(ChatInput.SearchInput);
        ChatInput.SearchInput.SetPositionInParent(2);

        _hudActionsPanel = new CmuHudActionsPanel { Name = "CmuHudPlayerActions" };
        _hudActionsPanel.CountChanged += count =>
        {
            if (_hudActionsButton != null)
                _hudActionsButton.Text = Loc.GetString("cmu-hud-actions-count", ("count", count));
        };
        rows.AddChild(_hudActionsPanel);
        _hudActionsPanel.SetPositionInParent(3);

        _hudEmoteGrid = new GridContainer { Columns = 2, HSeparationOverride = 4, VSeparationOverride = 3 };
        _hudEmoteDrawer = new PanelContainer
        {
            Name = "CmuHudEmotionDrawer",
            PanelOverride = new StyleBoxEmpty(),
            Visible = false,
        };
        var scroll = new ScrollContainer
        {
            HScrollEnabled = false,
            ReturnMeasure = true,
            MaxHeight = 160,
            Margin = new Thickness(6, 4),
        };
        scroll.AddChild(_hudEmoteGrid);
        _hudEmoteDrawer.AddChild(scroll);
        rows.AddChild(_hudEmoteDrawer);
        _hudEmoteDrawer.SetPositionInParent(4);

        var tools = new BoxContainer
        {
            Name = "CmuHudChatTools",
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 4,
            Margin = new Thickness(6, 4),
        };
        _hudActionsButton = HudButton(Loc.GetString("cmu-hud-actions"), "CmuHudActionsToggle");
        _hudActionsButton.ToggleMode = true;
        _hudActionsButton.Pressed = true;
        _hudActionsButton.OnToggled += args =>
        {
            SetHudToolSection(args.Pressed, false);
        };
        _hudEmoteButton = HudButton(Loc.GetString("cmu-hud-emotes"), "CmuHudQuickEmotes");
        _hudEmoteButton.ToggleMode = true;
        _hudEmoteButton.OnToggled += args =>
        {
            SetHudToolSection(false, args.Pressed);
        };
        tools.AddChild(_hudActionsButton);
        tools.AddChild(_hudEmoteButton);
        rows.AddChild(tools);
        tools.SetPositionInParent(3);
        _controller.SelectableChannelsChanged += OnHudChannelsChanged;
        OnHudChannelsChanged(_controller.SelectableChannels);
        UpdateHudCommandWidth();
    }

    private static Button HudButton(string text, string name) => new()
    {
        Name = name,
        Text = text,
        StyleClasses = { CmuHudStyle.Command },
        MinHeight = 26,
        HorizontalExpand = true,
    };

    private void SetHudToolSection(bool actions, bool emotes)
    {
        // Programmatic Pressed changes do not raise OnToggled. Keep both visual state
        // and content visibility in one place, including compact/channel changes.
        if (_hudActionsButton != null)
            _hudActionsButton.Pressed = actions;
        if (_hudEmoteButton != null)
            _hudEmoteButton.Pressed = emotes;
        if (_hudActionsPanel != null)
            _hudActionsPanel.Visible = actions;
        if (_hudEmoteDrawer != null)
            _hudEmoteDrawer.Visible = emotes;
        if (emotes)
            RebuildHudEmotes();
    }

    public void SetHudCompact(bool compact)
    {
        if (_hudCompact == compact)
            return;
        _hudCompact = compact;
        if (_hudCompactButton != null)
        {
            _hudCompactButton.Text = compact ? "↗" : "↙";
            _hudCompactButton.ToolTip = Loc.GetString(compact ? "cmu-hud-expanded" : "cmu-hud-compact");
        }
        if (compact)
            SetHudToolSection(false, false);
        HudCompactModeChanged?.Invoke(compact);
    }

    private void OnHudChannelsChanged(ChatSelectChannel channels)
    {
        if (_hudEmoteButton == null)
            return;
        _hudEmoteButton.Disabled = (channels & ChatSelectChannel.Emotes) == 0;
        if (_hudEmoteButton.Disabled)
            SetHudToolSection(_hudActionsButton?.Pressed == true, false);
        else if (_hudEmoteDrawer?.Visible == true)
            RebuildHudEmotes();
    }

    private void SaveHudChannelDraft(ChatSelectChannel channel)
    {
        if (!_hudEnabled || channel == _hudDraftChannel)
            return;
        if (_hudEmoteDrawer?.Visible == true)
            SetHudToolSection(true, false);
        _hudChannelDrafts[_hudDraftChannel] = ChatInput.Input.Text;
        _hudDraftChannel = channel;
        ChatInput.Input.Text = _hudChannelDrafts.GetValueOrDefault(channel, string.Empty);
    }

    private void RebuildHudEmotes()
    {
        if (_hudEmoteGrid == null)
            return;
        foreach (var child in _hudEmoteGrid.Children.ToArray())
        {
            child.Orphan();
            child.Release();
        }
        var emotes = UserInterfaceManager.GetUIController<EmotesUIController>();
        var sprites = _entManager.System<SpriteSystem>();
        foreach (var prototype in _prototype.EnumeratePrototypes<EmotePrototype>()
                     .Where(emotes.CanUseEmote).OrderBy(emote => Loc.GetString(emote.Name), StringComparer.CurrentCulture))
        {
            var button = HudButton(string.Empty, "CmuHudEmote" + prototype.ID);
            button.ToolTip = Loc.GetString(prototype.Name);
            var contents = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, SeparationOverride = 5 };
            contents.AddChild(new TextureRect
            {
                Texture = sprites.Frame0(prototype.Icon),
                SetSize = new Vector2(24),
                Stretch = TextureRect.StretchMode.KeepAspectCentered,
                MouseFilter = MouseFilterMode.Ignore,
            });
            contents.AddChild(new Label
            {
                Text = Loc.GetString(prototype.Name),
                ClipText = true,
                HorizontalExpand = true,
                MaxWidth = 120,
                StyleClasses = { CmuHudStyle.Body },
                MouseFilter = MouseFilterMode.Ignore,
            });
            button.AddChild(contents);
            button.OnPressed += _ =>
            {
                if ((_controller.SelectableChannels & ChatSelectChannel.Emotes) != 0)
                    emotes.TryPlayEmote(prototype);
            };
            _hudEmoteGrid.AddChild(button);
        }
        if (_hudEmoteGrid.ChildCount == 0)
            _hudEmoteGrid.AddChild(new Label { Text = Loc.GetString("cmu-hud-no-emotes"), MaxWidth = 220, ClipText = true });
        UpdateHudCommandWidth();
    }

    private void UpdateHudCommandWidth()
    {
        _hudActionsPanel?.SetAvailableWidth(Width);
        if (_hudEmoteGrid != null)
            _hudEmoteGrid.Columns = Width < 340 ? 1 : 2;
    }
}

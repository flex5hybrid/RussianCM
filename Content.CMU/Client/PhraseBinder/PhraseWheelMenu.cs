using System.Numerics;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.CMU14.PhraseBinder;

public sealed class PhraseWheelMenu : RadialMenu
{
    private readonly IReadOnlyList<BoundPhrase> _slots;
    private readonly Action<BoundPhrase> _send;
    private readonly Action _edit;
    private readonly RadialContainer _ring;
    private readonly Label _pageLabel;
    private int _page;

    public PhraseWheelMenu(IReadOnlyList<BoundPhrase> slots, Action<BoundPhrase> send, Action edit)
    {
        _slots = slots;
        _send = send;
        _edit = edit;
        MinSize = new Vector2(500, 500);
        ContextualButton.Visible = false;
        _ring = new RadialContainer { InitialRadius = 115, HorizontalExpand = true, VerticalExpand = true };
        AddChild(_ring);

        // A separate center control keeps navigation clicks away from the radial close button.
        var center = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            MouseFilter = MouseFilterMode.Stop,
        };
        _pageLabel = new Label { Align = Label.AlignMode.Center };
        center.AddChild(_pageLabel);
        var navigation = new BoxContainer { SeparationOverride = 6 };
        center.AddChild(navigation);
        var previous = new Button
        {
            Text = "‹", MinWidth = 28, Disabled = PhraseBinderData.PageCount(slots.Count) == 1,
            ToolTip = Loc.GetString("cmu-phrase-binder-previous"),
        };
        var next = new Button
        {
            Text = "›", MinWidth = 28, Disabled = previous.Disabled,
            ToolTip = Loc.GetString("cmu-phrase-binder-next"),
        };
        var close = new Button { Text = "×", MinWidth = 28, ToolTip = Loc.GetString("cmu-phrase-binder-close") };
        previous.OnPressed += _ => ChangePage(-1);
        next.OnPressed += _ => ChangePage(1);
        close.OnPressed += _ => Close();
        navigation.AddChild(previous);
        navigation.AddChild(close);
        navigation.AddChild(next);
        AddChild(center);
        center.Visible = true;
        FillPage();
    }

    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);
        if (args.Delta.Y == 0)
            return;
        ChangePage(args.Delta.Y > 0 ? -1 : 1);
        args.Handle();
    }

    private void ChangePage(int direction)
    {
        _page = PhraseBinderData.WrapPage(_page + direction, _slots.Count);
        FillPage();
    }

    private void FillPage()
    {
        _ring.RemoveAllChildren();
        _pageLabel.Text = Loc.GetString("cmu-phrase-binder-page",
            ("page", _page + 1), ("pages", PhraseBinderData.PageCount(_slots.Count)));
        for (var offset = 0; offset < PhraseBinderData.PageSize; offset++)
        {
            var index = _page * PhraseBinderData.PageSize + offset;
            if (index >= _slots.Count)
            {
                // Keep sector positions stable on the final, partially filled page.
                AddButton("", "", "").Disabled = true;
                continue;
            }

            var phrase = _slots[index];
            var empty = string.IsNullOrWhiteSpace(phrase.Text);
            var title = string.IsNullOrWhiteSpace(phrase.Title) ? phrase.Text : phrase.Title;
            if (title.Length > 16)
                title = title[..16] + "…";
            var button = AddButton(
                Loc.GetString("cmu-phrase-binder-slot", ("number", index + 1)),
                empty ? Loc.GetString("cmu-phrase-binder-empty") : title,
                empty ? Loc.GetString("cmu-phrase-binder-empty-tooltip") :
                    Loc.GetString(PhraseBinderWindow.ChannelLoc(phrase.Channel)) + ": " + phrase.Text);
            button.OnPressed += _ =>
            {
                Close();
                if (empty)
                    _edit();
                else
                    _send(phrase);
            };
        }

        AddButton("", Loc.GetString("cmu-phrase-binder-edit"), Loc.GetString("cmu-phrase-binder-title"))
            .OnPressed += _ =>
            {
                Close();
                _edit();
            };
    }

    private RadialMenuButtonWithSector AddButton(string number, string title, string tooltip)
    {
        var button = new RadialMenuButtonWithSector
        {
            SetSize = new Vector2(98, 44), ToolTip = tooltip, MouseFilter = MouseFilterMode.Pass, DrawBorder = true,
        };
        var labels = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            VerticalAlignment = VAlignment.Center,
            MouseFilter = MouseFilterMode.Ignore,
        };
        labels.AddChild(new Label { Text = number, Align = Label.AlignMode.Center, MouseFilter = MouseFilterMode.Ignore });
        labels.AddChild(new Label
        {
            Text = title, Align = Label.AlignMode.Center, ClipText = true, MouseFilter = MouseFilterMode.Ignore,
        });
        button.AddChild(labels);
        _ring.AddChild(button);
        return button;
    }
}

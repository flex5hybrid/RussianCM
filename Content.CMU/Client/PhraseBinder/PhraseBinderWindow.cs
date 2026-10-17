using System.Numerics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.CMU14.PhraseBinder;

public sealed class PhraseBinderWindow : DefaultWindow
{
    private readonly List<(Label Number, LineEdit Title, LineEdit Text, OptionButton Channel)> _fields = new();
    private readonly BoxContainer _rows;
    private readonly Label _status;
    private readonly Label _count;
    private readonly int _maxLength;

    public event Func<BoundPhrase[], bool>? SaveRequested;

    public PhraseBinderWindow(IReadOnlyList<BoundPhrase> slots, int maxLength)
    {
        _maxLength = maxLength;
        Title = Loc.GetString("cmu-phrase-binder-title");
        MinSize = new Vector2(620, 360);
        SetSize = new Vector2(720, 560);
        var root = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 8 };
        Contents.AddChild(root);
        var help = new RichTextLabel();
        help.SetMessage(Loc.GetString("cmu-phrase-binder-help"));
        root.AddChild(help);
        var scroll = new ScrollContainer { VerticalExpand = true, HorizontalExpand = true };
        root.AddChild(scroll);
        _rows = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 10 };
        scroll.AddChild(_rows);
        _count = new Label();
        foreach (var phrase in slots)
            AddRow(phrase);

        var addRow = new BoxContainer { SeparationOverride = 8 };
        root.AddChild(addRow);
        var add = new Button { Text = Loc.GetString("cmu-phrase-binder-add"), HorizontalExpand = true };
        add.OnPressed += _ => AddRow(new BoundPhrase());
        addRow.AddChild(add);
        addRow.AddChild(_count);
        _status = new Label { FontColorOverride = Color.Orange, ClipText = true };
        root.AddChild(_status);
        var footer = new BoxContainer { SeparationOverride = 8 };
        root.AddChild(footer);
        var cancel = new Button { Text = Loc.GetString("cmu-phrase-binder-cancel"), HorizontalExpand = true };
        cancel.OnPressed += _ => Close();
        footer.AddChild(cancel);
        var save = new Button { Text = Loc.GetString("cmu-phrase-binder-save"), HorizontalExpand = true };
        save.OnPressed += _ => Save();
        footer.AddChild(save);
    }

    private void AddRow(BoundPhrase phrase)
    {
        var row = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 3 };
        _rows.AddChild(row);
        var header = new BoxContainer { SeparationOverride = 6 };
        row.AddChild(header);
        var number = new Label();
        header.AddChild(number);
        var title = new LineEdit
        {
            Text = phrase.Title,
            PlaceHolder = Loc.GetString("cmu-phrase-binder-name"),
            HorizontalExpand = true,
        };
        header.AddChild(title);
        var channel = new OptionButton { MinWidth = 110 };
        foreach (var value in Enum.GetValues<PhraseChannel>())
            channel.AddItem(Loc.GetString(ChannelLoc(value)), (int) value);
        channel.SelectId((int) phrase.Channel);
        channel.OnItemSelected += args => channel.SelectId(args.Id);
        header.AddChild(channel);
        var text = new LineEdit
        {
            Text = phrase.Text,
            PlaceHolder = Loc.GetString("cmu-phrase-binder-text"),
            HorizontalExpand = true,
        };
        var fields = (number, title, text, channel);
        _fields.Add(fields);
        var remove = new Button { Text = Loc.GetString("cmu-phrase-binder-remove") };
        remove.OnPressed += _ =>
        {
            if (_fields.Count == 1)
            {
                title.Text = string.Empty;
                text.Text = string.Empty;
                channel.SelectId((int) PhraseChannel.Local);
                return;
            }

            _fields.Remove(fields);
            _rows.RemoveChild(row);
            UpdateNumbers();
        };
        header.AddChild(remove);
        row.AddChild(text);
        UpdateNumbers();
    }

    private void UpdateNumbers()
    {
        for (var i = 0; i < _fields.Count; i++)
            _fields[i].Number.Text = Loc.GetString("cmu-phrase-binder-slot", ("number", i + 1));
        _count.Text = Loc.GetString("cmu-phrase-binder-count", ("count", _fields.Count));
    }

    public static string ChannelLoc(PhraseChannel channel) => channel switch
    {
        PhraseChannel.Whisper => "cmu-phrase-binder-channel-whisper",
        PhraseChannel.Radio => "cmu-phrase-binder-channel-radio",
        PhraseChannel.Emote => "cmu-phrase-binder-channel-emote",
        _ => "cmu-phrase-binder-channel-local",
    };

    private void Save()
    {
        var slots = new BoundPhrase[_fields.Count];
        for (var i = 0; i < slots.Length; i++)
        {
            var title = PhraseBinderData.CleanLine(_fields[i].Title.Text);
            var text = PhraseBinderData.CleanLine(_fields[i].Text.Text);
            var phrase = new BoundPhrase(title, text, (PhraseChannel) _fields[i].Channel.SelectedId);
            if (title.Length > PhraseBinderData.MaxTitleLength || phrase.MessageText.Length > _maxLength)
            {
                _status.Text = Loc.GetString("cmu-phrase-binder-too-long",
                    ("number", i + 1), ("titleMax", PhraseBinderData.MaxTitleLength), ("textMax", _maxLength));
                return;
            }
            slots[i] = phrase;
        }

        if (SaveRequested?.Invoke(slots) == true)
            Close();
        else
            _status.Text = Loc.GetString("cmu-phrase-binder-save-error");
    }
}

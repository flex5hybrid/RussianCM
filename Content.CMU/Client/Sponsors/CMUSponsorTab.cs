using System.Linq;
using System.Numerics;
using Content.Client._RMC14.LinkAccount;
using Content.Shared.CMU14.Sponsors;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.CMU14.Sponsors;

/// <summary>Editor for account-wide cosmetic preferences, embedded in the existing sponsor window.</summary>
public sealed class CMUSponsorTab : BoxContainer
{
    private readonly LinkAccountManager _link;
    private readonly IEntityManager _entities;
    private readonly List<(OptionButton Button, List<string> Values)> _choices = [];
    private readonly OptionButton _ghost;
    private readonly OptionButton _patch;
    private readonly OptionButton _cape;
    private readonly OptionButton _trim;
    private readonly OptionButton _emblem;
    private readonly LineEdit _inscription;
    private readonly LineEdit _description;
    private readonly CheckBox _recognition;
    private readonly EntityPrototypeView _ghostPreview;
    private readonly EntityPrototypeView _patchPreview;
    private readonly EntityPrototypeView _capePreview;
    private readonly EntityPrototypeView _figurinePreview;
    private readonly Label _status;
    private readonly Label _approved;
    private readonly Button _save;
    private bool _editing;
    private bool _pending;

    public event Action<CMUSponsorSettings>? SaveRequested;

    public CMUSponsorTab(LinkAccountManager link, IEntityManager entities)
    {
        _link = link;
        _entities = entities;
        Orientation = LayoutOrientation.Vertical;
        SeparationOverride = 6;
        var scroll = new ScrollContainer { VerticalExpand = true, HorizontalExpand = true };
        var fields = new BoxContainer { Orientation = LayoutOrientation.Vertical, SeparationOverride = 6 };
        scroll.AddChild(fields);
        AddChild(scroll);
        fields.AddChild(new Label { Text = Loc.GetString("cmu-sponsor-saved-info") });

        _ghost = Choice(fields, "ghost", CMUSponsorCatalog.Ghosts.Keys, "cmu-sponsor-ghost-");
        _ghostPreview = Preview(fields);
        _ghostPreview.SetPrototype("CMUSponsorGhostPreview");
        _inscription = Text(fields, "inscription", CMUSponsorCatalog.InscriptionLimit);
        _patch = Choice(fields, "patch", CMUSponsorCatalog.Patches.Keys, "cmu-sponsor-patch-");
        _patchPreview = Preview(fields);
        _cape = Choice(fields, "cape", Enumerable.Range(1, 16).Select(i => i.ToString("D2")), "cmu-sponsor-cape-");
        _trim = Choice(fields, "trim", CMUSponsorCatalog.Trims, "cmu-sponsor-trim-");
        _emblem = Choice(fields, "emblem", CMUSponsorCatalog.Patches.Keys, "cmu-sponsor-patch-");
        _capePreview = Preview(fields);
        _description = Text(fields, "figurine-description", CMUSponsorCatalog.DescriptionLimit);
        _approved = new Label();
        fields.AddChild(_approved);
        _figurinePreview = Preview(fields);
        _recognition = new CheckBox { Text = Loc.GetString("cmu-sponsor-public-recognition") };
        _recognition.OnToggled += _ => _editing = true;
        fields.AddChild(_recognition);

        _status = new Label();
        AddChild(_status);
        _save = new Button { Text = Loc.GetString("cmu-sponsor-save") };
        _save.OnPressed += _ =>
        {
            _pending = true;
            _save.Disabled = true;
            Refresh();
            _status.Text = Loc.GetString("cmu-sponsor-saving");
            SaveRequested?.Invoke(ReadSettings());
        };
        AddChild(_save);
        Refresh(force: true);
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        UpdatePreview();
    }

    public void Refresh(bool force = false)
    {
        var tier = _link.Tier;
        _ghost.Disabled = _pending || tier?.GhostColor != true;
        _inscription.Editable = !_pending && tier?.NamedItems == true;
        _patch.Disabled = _pending || tier?.NamedItems != true;
        _cape.Disabled = _pending || !CMUSponsorCatalog.CanUseCapes(tier);
        _trim.Disabled = _emblem.Disabled = _pending || !CMUSponsorCatalog.CanCustomizeCape(tier);
        _description.Editable = !_pending && tier?.Figurines == true;
        _recognition.Disabled = _pending;
        for (var i = 1; i <= 16; i++)
            _cape.SetItemDisabled(i, !CMUSponsorCatalog.CanUseCape(tier, i.ToString("D2")));

        _approved.Text = Loc.GetString(_link.ApprovedFigurineDescription.Length > 0
            ? "cmu-sponsor-description-approved" : "cmu-sponsor-description-review");
        _figurinePreview.SetPrototype(_link.FigurinePrototype is { } figurine ? new EntProtoId(figurine) : (EntProtoId?)null);
        if (!_editing || force)
        {
            var s = _link.SponsorSettings;
            Select(_ghost, s.Ghost);
            Select(_patch, s.Patch);
            Select(_cape, s.Cape);
            Select(_trim, s.Trim);
            Select(_emblem, s.Emblem);
            _inscription.Text = s.Inscription;
            _description.Text = s.FigurineDescription;
            _recognition.Pressed = s.PublicRecognition;
            _editing = false;
        }
        UpdatePreview();
    }

    public void Saved(bool success)
    {
        _pending = false;
        _save.Disabled = false;
        _status.Text = Loc.GetString(success ? "cmu-sponsor-save-success" : "cmu-sponsor-save-failed");
        Refresh(force: success);
    }

    private CMUSponsorSettings ReadSettings() => _link.SponsorSettings with
    {
        Ghost = Selected(_ghost), Inscription = _inscription.Text, Patch = Selected(_patch),
        Cape = Selected(_cape), Trim = Selected(_trim), Emblem = Selected(_emblem),
        FigurineDescription = _description.Text, PublicRecognition = _recognition.Pressed,
    };

    private OptionButton Choice(BoxContainer parent, string name, IEnumerable<string> options, string prefix)
    {
        parent.AddChild(new Label { Text = Loc.GetString($"cmu-sponsor-{name}") });
        var button = new OptionButton { HorizontalExpand = true };
        var values = new List<string> { "" };
        button.AddItem(Loc.GetString("cmu-sponsor-none"), 0);
        foreach (var option in options)
        {
            button.AddItem(Loc.GetString(prefix + option), values.Count);
            values.Add(option);
        }
        _choices.Add((button, values));
        button.OnItemSelected += args =>
        {
            button.SelectId(args.Id);
            _editing = true;
            UpdatePreview();
        };
        parent.AddChild(button);
        return button;
    }

    private LineEdit Text(BoxContainer parent, string name, int limit)
    {
        parent.AddChild(new Label { Text = Loc.GetString($"cmu-sponsor-{name}", ("limit", limit)) });
        var field = new LineEdit { HorizontalExpand = true };
        field.OnTextChanged += args =>
        {
            if (args.Text.Length > limit)
                field.SetText(args.Text[..limit], false);
            _editing = true;
        };
        parent.AddChild(field);
        return field;
    }

    private EntityPrototypeView Preview(BoxContainer parent)
    {
        var view = new EntityPrototypeView(null, _entities)
        {
            MinSize = new Vector2(96, 64), MaxSize = new Vector2(160, 96),
            Scale = new Vector2(2f), OverrideDirection = Direction.South,
        };
        parent.AddChild(view);
        return view;
    }

    private string Selected(OptionButton button) => _choices.First(c => c.Button == button).Values[button.SelectedId];
    private void Select(OptionButton button, string value)
    {
        var index = _choices.First(c => c.Button == button).Values.IndexOf(value);
        button.SelectId(Math.Max(index, 0));
    }

    private void UpdatePreview()
    {
        if (_ghostPreview == null)
            return;
        var visuals = _entities.System<CMUSponsorVisualsSystem>();
        if (_ghostPreview.Entity is { } ghost)
        {
            var comp = _entities.GetComponent<CMUSponsorGhostComponent>(ghost.Owner);
            comp.State = CMUSponsorCatalog.Ghosts.GetValueOrDefault(Selected(_ghost), "animated");
            visuals.UpdateGhost((ghost.Owner, comp));
            _entities.System<SpriteSystem>().SetColor((ghost.Owner, ghost.Comp1), _link.GhostColor ?? Color.FromHex("#FFFFFF88"));
        }
        var patch = Selected(_patch);
        _patchPreview.SetPrototype(patch.Length == 0 ? (EntProtoId?)null : new EntProtoId(CMUSponsorCatalog.PatchPrototype(patch)));
        var cape = Selected(_cape);
        _capePreview.SetPrototype(cape.Length == 0 ? (EntProtoId?)null : new EntProtoId($"CMUSponsorCape{cape}"));
        if (_capePreview.Entity is { } capeEnt)
        {
            var comp = _entities.GetComponent<CMUSponsorCapeComponent>(capeEnt.Owner);
            comp.Trim = Selected(_trim);
            comp.Emblem = Selected(_emblem);
            visuals.UpdateCape((capeEnt.Owner, comp));
            // Show the animated worn cape, so the constructor previews the actual equipment sprite.
            _entities.System<SpriteSystem>().LayerSetRsiState((capeEnt.Owner, capeEnt.Comp1), 0, "equipped-NECK");
        }
        _save.Disabled = _pending;
    }
}

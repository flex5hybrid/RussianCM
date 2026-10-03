// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 wray-git
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Client.Stylesheets;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI.Loadouts;

// Window where the player picks clothing, a name and a color for each slot.
public sealed class ColonistClothingWindow : DefaultWindow
{
    public event Action<ProtoId<LoadoutGroupPrototype>, Loadout>? OnApply;
    public event Action<ProtoId<LoadoutGroupPrototype>, ProtoId<LoadoutPrototype>>? OnClear;

    private const int MaxListed = 80;
    private const int ListIconSize = 32;
    private const int PreviewIconSize = 64;

    private static readonly Color MutedColor = Color.FromHex("#8c8c8c");

    private sealed record SlotEntry(
        string Slot,
        ProtoId<LoadoutGroupPrototype> Group,
        ProtoId<LoadoutPrototype> Loadout,
        Button Button);

    private readonly IPrototypeManager _protoMan;
    private readonly IComponentFactory _factory;
    private readonly SpriteSystem _sprite;

    private readonly List<SlotEntry> _slots = new();
    private readonly Dictionary<string, List<EntityPrototype>> _eligible = new();

    private readonly Label _slotTitle;
    private readonly LineEdit _searchBox;
    private readonly BoxContainer _listBox;
    private readonly Label _moreLabel;
    private readonly TextureRect _preview;
    private readonly Label _pickedLabel;
    private readonly LineEdit _nameEdit;
    private readonly ColorSelectorSliders _colorSelector;
    private readonly Button _applyButton;
    private readonly Button _clearButton;

    private RoleLoadout _special;
    private SlotEntry? _current;
    private EntityPrototype? _picked;
    private Color _color = Color.White;

    public IReadOnlyList<string> SlotNames => _slots.Select(entry => entry.Slot).ToList();

    public int ListedCount => _listBox.ChildCount;

    public string? PickedPrototype => _picked?.ID;

    public ColonistClothingWindow(RoleLoadout special, RoleLoadoutPrototype specialProto, IDependencyCollection collection)
    {
        _protoMan = collection.Resolve<IPrototypeManager>();
        _factory = collection.Resolve<IComponentFactory>();
        _sprite = collection.Resolve<IEntityManager>().System<SpriteSystem>();
        _special = special;

        Title = Loc.GetString("colonist-clothing-title");
        MinSize = new Vector2(760, 560);

        var root = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, SeparationOverride = 8 };

        var slotColumn = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
            MinWidth = 210,
        };
        root.AddChild(slotColumn);

        var buttonGroup = new ButtonGroup();
        foreach (var groupId in specialProto.Groups)
        {
            if (!_protoMan.TryIndex(groupId, out var groupProto))
                continue;

            foreach (var loadoutId in groupProto.Loadouts)
            {
                if (!_protoMan.TryIndex(loadoutId, out var loadoutProto) ||
                    !CustomClothingRules.TryGetEffect(loadoutProto, out var effect))
                {
                    continue;
                }

                var button = new Button
                {
                    ToggleMode = true,
                    Group = buttonGroup,
                    HorizontalExpand = true,
                    ClipText = true,
                };

                var entry = new SlotEntry(effect.Slot, groupId, loadoutId, button);
                button.OnPressed += _ => SelectSlot(entry);
                slotColumn.AddChild(button);
                _slots.Add(entry);
            }
        }

        var right = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 6,
            HorizontalExpand = true,
        };
        root.AddChild(right);

        _slotTitle = new Label { StyleClasses = { "LabelHeadingBigger" } };
        right.AddChild(_slotTitle);

        _searchBox = new LineEdit
        {
            PlaceHolder = Loc.GetString("colonist-clothing-search"),
            HorizontalExpand = true,
        };
        _searchBox.OnTextChanged += _ => PopulateList();
        right.AddChild(_searchBox);

        var scroll = new ScrollContainer
        {
            VerticalExpand = true,
            HScrollEnabled = false,
            MinHeight = 240,
        };
        _listBox = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 2 };
        scroll.AddChild(_listBox);
        right.AddChild(scroll);

        _moreLabel = new Label
        {
            Text = Loc.GetString("colonist-clothing-refine-search", ("count", MaxListed)),
            FontColorOverride = MutedColor,
            Visible = false,
        };
        right.AddChild(_moreLabel);

        var pickedRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 8,
        };
        _preview = new TextureRect
        {
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            MinSize = new Vector2(PreviewIconSize, PreviewIconSize),
        };
        pickedRow.AddChild(_preview);

        var pickedColumn = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
            HorizontalExpand = true,
        };
        _pickedLabel = new Label();
        pickedColumn.AddChild(_pickedLabel);
        _nameEdit = new LineEdit
        {
            PlaceHolder = Loc.GetString("colonist-clothing-name-placeholder"),
            HorizontalExpand = true,
        };
        pickedColumn.AddChild(_nameEdit);
        pickedRow.AddChild(pickedColumn);
        right.AddChild(pickedRow);

        right.AddChild(new Label { Text = Loc.GetString("colonist-clothing-color") });
        _colorSelector = new ColorSelectorSliders { SelectorType = ColorSelectorSliders.ColorSelectorType.Hsv };
        _colorSelector.Color = Color.White;
        _colorSelector.OnColorChanged += color =>
        {
            _color = color;
            RefreshPreview();
        };
        right.AddChild(_colorSelector);

        var buttons = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 8,
            HorizontalAlignment = HAlignment.Right,
        };
        _clearButton = new Button { Text = Loc.GetString("colonist-clothing-clear") };
        _clearButton.OnPressed += _ => ClearSlot();
        buttons.AddChild(_clearButton);
        _applyButton = new Button { Text = Loc.GetString("colonist-clothing-apply") };
        _applyButton.OnPressed += _ => ApplySlot();
        buttons.AddChild(_applyButton);
        right.AddChild(buttons);

        Contents.AddChild(root);

        RefreshSlotButtons();
        if (_slots.Count > 0)
        {
            _slots[0].Button.Pressed = true;
            SelectSlot(_slots[0]);
        }
    }

    public void Refresh(RoleLoadout special)
    {
        _special = special;
        RefreshSlotButtons();
        _clearButton.Disabled = _current == null || FindSelected(_current) == null;
    }

    public void SelectSlot(string slot)
    {
        var entry = _slots.FirstOrDefault(candidate => candidate.Slot == slot);
        if (entry == null)
            return;

        entry.Button.Pressed = true;
        SelectSlot(entry);
    }

    public void SetSearch(string text)
    {
        _searchBox.Text = text;
        PopulateList();
    }

    public void Pick(string prototype)
    {
        if (_protoMan.TryIndex<EntityPrototype>(prototype, out var proto))
            Pick(proto);
    }

    public void SetColor(Color color)
    {
        _color = color;
        _colorSelector.Color = color;
        RefreshPreview();
    }

    public void SetName(string name)
    {
        _nameEdit.Text = name;
    }

    public void ApplyCurrent()
    {
        ApplySlot();
    }

    public void ClearCurrent()
    {
        ClearSlot();
    }

    private Loadout? FindSelected(SlotEntry entry)
    {
        return _special.SelectedLoadouts.TryGetValue(entry.Group, out var picks)
            ? picks.FirstOrDefault(pick => pick.Prototype == entry.Loadout)
            : null;
    }

    private void RefreshSlotButtons()
    {
        foreach (var entry in _slots)
        {
            var slotName = Loc.GetString($"colonist-clothing-slot-{entry.Slot}");
            var selected = FindSelected(entry);

            if (selected?.CustomEntity != null && _protoMan.TryIndex<EntityPrototype>(selected.CustomEntity, out var proto))
            {
                var itemName = CustomClothingRules.SanitizeName(selected.CustomName) ?? DisplayName(proto);
                entry.Button.Text = $"{slotName}: {itemName}";
            }
            else
            {
                entry.Button.Text = slotName;
            }
        }
    }

    private void SelectSlot(SlotEntry entry)
    {
        _current = entry;
        _slotTitle.Text = Loc.GetString($"colonist-clothing-slot-{entry.Slot}");

        var selected = FindSelected(entry);
        _picked = selected?.CustomEntity != null && _protoMan.TryIndex<EntityPrototype>(selected.CustomEntity, out var proto)
            ? proto
            : null;
        _color = selected?.CustomColor ?? Color.White;
        _colorSelector.Color = _color;
        _nameEdit.Text = selected?.CustomName ?? string.Empty;
        _searchBox.Text = string.Empty;

        _clearButton.Disabled = selected == null;
        PopulateList();
        RefreshPreview();
    }

    private List<EntityPrototype> GetEligible(string slot)
    {
        if (_eligible.TryGetValue(slot, out var cached))
            return cached;

        var list = _protoMan.EnumeratePrototypes<EntityPrototype>()
            .Where(proto => CustomClothingRules.IsEligible(proto, slot, _factory))
            .OrderBy(proto => DisplayName(proto), StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        _eligible[slot] = list;
        return list;
    }

    private static string DisplayName(EntityPrototype proto)
    {
        return string.IsNullOrWhiteSpace(proto.Name) ? proto.ID : proto.Name;
    }

    // Fills the item list for the current slot, filtered by the search text.
    private void PopulateList()
    {
        _listBox.RemoveAllChildren();
        _moreLabel.Visible = false;

        if (_current == null)
            return;

        var filter = _searchBox.Text.Trim();
        var matches = GetEligible(_current.Slot)
            .Where(proto => filter.Length == 0 ||
                            DisplayName(proto).Contains(filter, StringComparison.CurrentCultureIgnoreCase) ||
                            proto.ID.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var rowGroup = new ButtonGroup();
        foreach (var proto in matches.Take(MaxListed))
        {
            var row = new ContainerButton
            {
                ToggleMode = true,
                Group = rowGroup,
                StyleClasses = { StyleClass.ButtonOpenBoth },
                Pressed = _picked?.ID == proto.ID,
            };

            var content = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                SeparationOverride = 8,
                Margin = new Thickness(4, 2),
            };
            content.AddChild(new TextureRect
            {
                Texture = IconOf(proto),
                Stretch = TextureRect.StretchMode.KeepAspectCentered,
                MinSize = new Vector2(ListIconSize, ListIconSize),
            });
            content.AddChild(new Label
            {
                Text = DisplayName(proto),
                VerticalAlignment = VAlignment.Center,
                ClipText = true,
                HorizontalExpand = true,
            });
            row.AddChild(content);
            row.ToolTip = proto.ID;

            var captured = proto;
            row.OnPressed += _ => Pick(captured);
            _listBox.AddChild(row);
        }

        _moreLabel.Visible = matches.Count > MaxListed;
    }

    private Robust.Client.Graphics.Texture? IconOf(EntityPrototype proto)
    {
        return _sprite.GetPrototypeIcon(proto).GetFrame(RsiDirection.South, 0);
    }

    private void Pick(EntityPrototype proto)
    {
        _picked = proto;
        _nameEdit.PlaceHolder = DisplayName(proto);
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        if (_picked == null)
        {
            _preview.Texture = null;
            _pickedLabel.Text = Loc.GetString("colonist-clothing-nothing-picked");
            _pickedLabel.FontColorOverride = MutedColor;
            _applyButton.Disabled = true;
            return;
        }

        _preview.Texture = IconOf(_picked);
        _preview.Modulate = _color;
        _pickedLabel.Text = DisplayName(_picked);
        _pickedLabel.FontColorOverride = null;
        _applyButton.Disabled = false;
    }

    // Saves the current pick for this slot.
    private void ApplySlot()
    {
        if (_current == null || _picked == null)
            return;

        var loadout = new Loadout
        {
            Prototype = _current.Loadout,
            CustomEntity = _picked.ID,
            CustomName = CustomClothingRules.SanitizeName(_nameEdit.Text),
            CustomColor = _color.WithAlpha(1f) == Color.White ? null : _color.WithAlpha(1f),
        };

        OnApply?.Invoke(_current.Group, loadout);
        _clearButton.Disabled = false;
    }

    // Removes the custom clothing for this slot.
    private void ClearSlot()
    {
        if (_current == null)
            return;

        OnClear?.Invoke(_current.Group, _current.Loadout);

        _picked = null;
        _color = Color.White;
        _colorSelector.Color = Color.White;
        _nameEdit.Text = string.Empty;
        _clearButton.Disabled = true;
        PopulateList();
        RefreshPreview();
    }
}

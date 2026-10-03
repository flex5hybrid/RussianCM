// SPDX-License-Identifier: AGPL-3.0-only
// Copyright (c) 2026 wray-git
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Preferences.Loadouts.Effects;
using Content.Shared.Roles;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Lobby.UI.Loadouts;

// Lobby window for colonist skills: one slider per skill plus the special loadout list.
public sealed class ColonistSkillEditorWindow : DefaultWindow
{
    public event Action<ProtoId<LoadoutGroupPrototype>, ProtoId<LoadoutPrototype>>? OnLoadoutPressed;
    public event Action<ProtoId<LoadoutGroupPrototype>, ProtoId<LoadoutPrototype>>? OnLoadoutUnpressed;

    public event Action<ProtoId<LoadoutGroupPrototype>, ProtoId<LoadoutPrototype>>? OnSpecialLoadoutPressed;
    public event Action<ProtoId<LoadoutGroupPrototype>, ProtoId<LoadoutPrototype>>? OnSpecialLoadoutUnpressed;

    public event Action? OnClothingEditorRequested;

    // Skills every colonist starts with by default.
    private static readonly (ProtoId<LoadoutGroupPrototype> Group, ProtoId<LoadoutPrototype> Loadout)[] VanillaDefaults =
    {
        ("AU14ColonistSkillGroupFireman", "AU14ColonistSkillFireman1"),
        ("AU14ColonistSkillGroupVehicles", "AU14ColonistSkillVehicles1"),
        ("AU14ColonistSkillGroupDomestics", "AU14ColonistSkillDomestics1"),
        ("AU14ColonistSkillGroupFirearms", "AU14ColonistSkillFirearms2"),
    };

    private const string OtherCategory = "other";
    private const int ItemIconSize = 40;
    private const int MaxItemIcons = 4;

    private static readonly string[] CategoryOrder =
    {
        "combat", "medical", "engineering", "command", "transport", "survival", OtherCategory,
    };

    // Which category each skill is shown under.
    private static readonly Dictionary<string, string> CategoryByGroup = new()
    {
        ["AU14ColonistSkillGroupFirearms"] = "combat",
        ["AU14ColonistSkillGroupMeleeWeapons"] = "combat",
        ["AU14ColonistSkillGroupCqc"] = "combat",
        ["AU14ColonistSkillGroupPolice"] = "combat",
        ["AU14ColonistSkillGroupMedical"] = "medical",
        ["AU14ColonistSkillGroupSurgery"] = "medical",
        ["AU14ColonistSkillGroupEngineer"] = "engineering",
        ["AU14ColonistSkillGroupConstruction"] = "engineering",
        ["AU14ColonistSkillGroupFireman"] = "engineering",
        ["AU14ColonistSkillGroupPowerLoader"] = "engineering",
        ["AU14ColonistSkillGroupResearch"] = "engineering",
        ["AU14ColonistSkillGroupLeadership"] = "command",
        ["AU14ColonistSkillGroupIntel"] = "command",
        ["AU14ColonistSkillGroupJtac"] = "command",
        ["AU14ColonistSkillGroupOverwatch"] = "command",
        ["AU14ColonistSkillGroupNavigations"] = "command",
        ["AU14ColonistSkillGroupPilot"] = "transport",
        ["AU14ColonistSkillGroupVehicles"] = "transport",
        ["AU14ColonistSkillGroupEndurance"] = "survival",
        ["AU14ColonistSkillGroupDomestics"] = "survival",
    };

    private static readonly Color UntrainedColor = Color.FromHex("#8c8c8c");
    private static readonly Color TrainedColor = Color.FromHex("#e6e6e6");
    private static readonly Color ValueTrainedColor = Color.FromHex("#6fcf97");
    private static readonly Color ValueMaxedColor = Color.FromHex("#f2c94c");
    private static readonly Color WarningColor = Color.FromHex("#eb5757");
    private static readonly Color HeaderPanelColor = Color.FromHex("#25252a");

    private readonly IPrototypeManager _protoMan;
    private readonly SpriteSystem _sprite;

    private readonly Label _pointsLabel;
    private readonly Label _trainedLabel;
    private readonly ProgressBar _pointsBar;
    private readonly LineEdit _searchBox;
    private readonly Label _noResultsLabel;
    private readonly BoxContainer _skillsBox;
    private readonly BoxContainer _extrasBox;
    private readonly Label _specialLoadoutPointsLabel;
    private readonly BoxContainer _specialLoadoutBox;

    private sealed record SliderSpec(
        ProtoId<LoadoutGroupPrototype> Group,
        string Category,
        string DisplayName,
        List<LoadoutPrototype> Levels);

    private sealed record SliderRow(
        ProtoId<LoadoutGroupPrototype> Group,
        Slider Slider,
        Label NameLabel,
        Label ValueLabel,
        List<LoadoutPrototype> Levels,
        BoxContainer Container,
        string DisplayName,
        string BaseTooltip);

    private sealed record CategorySection(BoxContainer Box, List<SliderRow> Rows);
    private sealed record CheckRow(ProtoId<LoadoutGroupPrototype> Group, ProtoId<LoadoutPrototype> Loadout, CheckBox Box, List<Control> Icons);

    private readonly List<SliderRow> _sliders = new();
    private readonly List<CategorySection> _sections = new();
    private readonly List<CheckRow> _checks = new();
    private readonly List<CheckRow> _specialLoadoutChecks = new();

    private readonly Dictionary<string, string> _skillNames = new();

    public int SkillRowCount => _sliders.Count;

    public int VisibleSkillRowCount => _sliders.Count(row => row.Container.Visible);

    public IReadOnlyList<TextureRect> SpecialLoadoutIcons =>
        _specialLoadoutChecks.SelectMany(row => row.Icons).OfType<TextureRect>().ToList();

    public int TrainedSkillCount { get; private set; }

    public ColonistSkillEditorWindow(
        HumanoidCharacterProfile profile,
        RoleLoadout loadout,
        RoleLoadoutPrototype roleProto,
        RoleLoadout specialLoadout,
        RoleLoadoutPrototype specialLoadoutProto,
        ICommonSession session,
        IDependencyCollection collection)
    {
        _protoMan = collection.Resolve<IPrototypeManager>();
        _sprite = collection.Resolve<IEntityManager>().System<SpriteSystem>();

        Title = Loc.GetString("colonist-skill-editor-title");
        MinSize = new Vector2(520, 640);

        var root = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 6 };

        var headerPanel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat { BackgroundColor = HeaderPanelColor },
        };
        var header = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 4,
            Margin = new Thickness(6),
        };
        headerPanel.AddChild(header);

        var topRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 8,
            HorizontalExpand = true,
        };
        _pointsLabel = new Label { HorizontalExpand = true };
        topRow.AddChild(_pointsLabel);
        _trainedLabel = new Label { FontColorOverride = UntrainedColor };
        topRow.AddChild(_trainedLabel);
        var resetButton = new Button { Text = Loc.GetString("colonist-skill-editor-reset") };
        resetButton.OnPressed += _ => OnResetPressed();
        topRow.AddChild(resetButton);
        header.AddChild(topRow);

        _pointsBar = new ProgressBar { MinValue = 0, MaxValue = 1, MinSize = new Vector2(0, 14), HorizontalExpand = true };
        header.AddChild(_pointsBar);

        _searchBox = new LineEdit
        {
            PlaceHolder = Loc.GetString("colonist-skill-editor-search"),
            HorizontalExpand = true,
        };
        _searchBox.OnTextChanged += args => SetSearchFilter(args.Text);
        header.AddChild(_searchBox);

        root.AddChild(headerPanel);

        var scroll = new ScrollContainer { VerticalExpand = true, HScrollEnabled = false };
        var inner = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 6,
            Margin = new Thickness(0, 0, 8, 0),
        };
        scroll.AddChild(inner);
        root.AddChild(scroll);

        inner.AddChild(new Label
        {
            Text = Loc.GetString("colonist-skill-editor-skills-header"),
            StyleClasses = { "LabelHeadingBigger" },
        });
        _skillsBox = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 4 };
        inner.AddChild(_skillsBox);
        _noResultsLabel = new Label
        {
            Text = Loc.GetString("colonist-skill-editor-no-results"),
            FontColorOverride = UntrainedColor,
            Visible = false,
        };
        inner.AddChild(_noResultsLabel);

        inner.AddChild(new Label
        {
            Text = Loc.GetString("colonist-skill-editor-extras-header"),
            StyleClasses = { "LabelHeadingBigger" },
            Margin = new Thickness(0, 8, 0, 0),
        });
        _extrasBox = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 4 };
        inner.AddChild(_extrasBox);

        inner.AddChild(new Label
        {
            Text = Loc.GetString("colonist-skill-editor-special-loadout-header"),
            StyleClasses = { "LabelHeadingBigger" },
            Margin = new Thickness(0, 8, 0, 0),
        });
        _specialLoadoutPointsLabel = new Label();
        inner.AddChild(_specialLoadoutPointsLabel);
        var clothingButton = new Button
        {
            Text = Loc.GetString("colonist-skill-editor-clothing-button"),
            HorizontalAlignment = HAlignment.Left,
            MinWidth = 160,
        };
        clothingButton.OnPressed += _ => RequestClothingEditor();
        inner.AddChild(clothingButton);
        _specialLoadoutBox = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 4 };
        inner.AddChild(_specialLoadoutBox);

        Contents.AddChild(root);

        BuildRows(roleProto, specialLoadoutProto);
        RefreshLoadouts(profile, loadout, specialLoadout, session, collection);
    }

    public void RequestClothingEditor()
    {
        OnClothingEditorRequested?.Invoke();
    }

    // Selects the default skills the first time the window is opened.
    public void ApplyVanillaDefaultsIfUntouched(RoleLoadout loadout)
    {
        var alreadyTouched = VanillaDefaults.Any(entry =>
            loadout.SelectedLoadouts.TryGetValue(entry.Group, out var picks) && picks.Count > 0);

        if (alreadyTouched)
            return;

        foreach (var (group, loadoutId) in VanillaDefaults)
            OnLoadoutPressed?.Invoke(group, loadoutId);
    }

    // Shows only skills whose name contains the text.
    public void SetSearchFilter(string text)
    {
        var filter = text.Trim();

        foreach (var section in _sections)
        {
            var anyVisible = false;
            foreach (var row in section.Rows)
            {
                var visible = filter.Length == 0 ||
                              row.DisplayName.Contains(filter, StringComparison.CurrentCultureIgnoreCase);
                row.Container.Visible = visible;
                anyVisible |= visible;
            }

            section.Box.Visible = anyVisible;
        }

        _noResultsLabel.Visible = filter.Length > 0 && _sections.All(section => !section.Box.Visible);
    }

    private void OnResetPressed()
    {
        foreach (var row in _sliders)
        {
            foreach (var level in row.Levels)
                OnLoadoutUnpressed?.Invoke(row.Group, level.ID);
        }

        foreach (var row in _checks)
            OnLoadoutUnpressed?.Invoke(row.Group, row.Loadout);

        foreach (var row in _specialLoadoutChecks)
            OnSpecialLoadoutUnpressed?.Invoke(row.Group, row.Loadout);

        foreach (var (group, loadoutId) in VanillaDefaults)
            OnLoadoutPressed?.Invoke(group, loadoutId);
    }

    // Builds the skill sliders and the special loadout checkboxes from the prototypes.
    private void BuildRows(RoleLoadoutPrototype roleProto, RoleLoadoutPrototype specialLoadoutProto)
    {
        _skillsBox.RemoveAllChildren();
        _extrasBox.RemoveAllChildren();
        _specialLoadoutBox.RemoveAllChildren();
        _sliders.Clear();
        _sections.Clear();
        _checks.Clear();
        _specialLoadoutChecks.Clear();
        _skillNames.Clear();

        var sliderSpecs = new List<SliderSpec>();
        var extraEntries = new List<(ProtoId<LoadoutGroupPrototype> Group, LoadoutPrototype Loadout)>();

        foreach (var groupId in roleProto.Groups)
        {
            if (!_protoMan.TryIndex(groupId, out var groupProto) || !groupProto.Hidden)
                continue;

            var levels = ResolveLevels(groupProto);
            if (levels.Count == 0)
                continue;

            var skillEffects = levels
                .Select(level => level.Effects.OfType<SetSkillLoadoutEffect>().FirstOrDefault())
                .ToList();

            if (skillEffects.All(effect => effect != null) &&
                skillEffects.Select(effect => effect!.Skill).Distinct().Count() == 1)
            {
                var skillId = skillEffects[0]!.Skill;
                var fallbackName = _protoMan.TryIndex(skillId, out var skillProto) ? skillProto.Name : skillId.Id;
                var displayName = Loc.TryGetString(groupProto.Name, out var groupName) ? groupName : fallbackName;
                var category = CategoryByGroup.GetValueOrDefault(groupId.Id, OtherCategory);

                _skillNames[skillId.Id] = displayName;
                sliderSpecs.Add(new SliderSpec(groupId, category, displayName, levels));
                continue;
            }

            foreach (var loadoutProto in levels)
                extraEntries.Add((groupId, loadoutProto));
        }

        foreach (var category in CategoryOrder)
        {
            var specs = sliderSpecs
                .Where(spec => spec.Category == category)
                .OrderBy(spec => spec.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            if (specs.Count == 0)
                continue;

            var box = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 4 };
            box.AddChild(new Label
            {
                Text = Loc.GetString($"colonist-skill-category-{category}"),
                StyleClasses = { "LabelHeading" },
                Margin = new Thickness(0, 6, 0, 2),
            });

            var section = new CategorySection(box, new List<SliderRow>());
            foreach (var spec in specs)
                section.Rows.Add(AddSkillSlider(box, spec));

            _skillsBox.AddChild(box);
            _sections.Add(section);
        }

        foreach (var (groupId, loadoutProto) in extraEntries)
            AddCheckbox(_extrasBox, _checks, groupId, loadoutProto, isSpecialLoadout: false);

        foreach (var groupId in specialLoadoutProto.Groups)
        {
            if (!_protoMan.TryIndex(groupId, out var groupProto))
                continue;

            var entries = ResolveLevels(groupProto);
            if (entries.Count == 0 || entries.Any(entry => CustomClothingRules.TryGetEffect(entry, out _)))
                continue;

            if (Loc.TryGetString(groupProto.Name, out var groupName))
            {
                _specialLoadoutBox.AddChild(new Label
                {
                    Text = groupName,
                    StyleClasses = { "LabelHeading" },
                    Margin = new Thickness(0, 6, 0, 2),
                });
            }

            foreach (var loadoutProto in entries)
                AddCheckbox(_specialLoadoutBox, _specialLoadoutChecks, groupId, loadoutProto, isSpecialLoadout: true);
        }
    }

    private List<LoadoutPrototype> ResolveLevels(LoadoutGroupPrototype groupProto)
    {
        var levels = new List<LoadoutPrototype>();
        foreach (var id in groupProto.Loadouts)
        {
            if (_protoMan.TryIndex(id, out var loadoutProto))
                levels.Add(loadoutProto);
        }

        return levels;
    }

    private static string BuildSkillTooltip(string groupId, List<LoadoutPrototype> levels)
    {
        var lines = new List<string>();

        if (Loc.TryGetString($"colonist-skill-editor-desc-{groupId}", out var description))
        {
            lines.Add(description);
            lines.Add(string.Empty);
        }

        for (var i = 0; i < levels.Count; i++)
        {
            lines.Add(Loc.GetString("colonist-skill-editor-tooltip-level",
                ("level", i + 1),
                ("cost", levels[i].Cost ?? 0)));
        }

        return string.Join("\n", lines);
    }

    private SliderRow AddSkillSlider(BoxContainer container, SliderSpec spec)
    {
        var row = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 8,
            HorizontalExpand = true,
        };

        var tooltip = BuildSkillTooltip(spec.Group.Id, spec.Levels);

        var nameLabel = new Label
        {
            Text = spec.DisplayName,
            MinWidth = 170,
            MouseFilter = Control.MouseFilterMode.Pass,
            ToolTip = tooltip,
        };
        row.AddChild(nameLabel);

        var slider = new Slider
        {
            MinValue = 0,
            MaxValue = spec.Levels.Count,
            Rounded = true,
            HorizontalExpand = true,
            ToolTip = tooltip,
        };
        row.AddChild(slider);

        var valueLabel = new Label
        {
            MinWidth = 130,
            HorizontalAlignment = HAlignment.Right,
            MouseFilter = Control.MouseFilterMode.Pass,
            ToolTip = tooltip,
        };
        row.AddChild(valueLabel);

        container.AddChild(row);
        var sliderRow = new SliderRow(spec.Group, slider, nameLabel, valueLabel, spec.Levels, row, spec.DisplayName, tooltip);
        _sliders.Add(sliderRow);

        slider.OnValueChanged += _ => OnSliderChanged(sliderRow);
        return sliderRow;
    }

    // Moving a slider picks the matching skill level and drops the others.
    private void OnSliderChanged(SliderRow row)
    {
        var target = (int)MathF.Round(row.Slider.Value);

        for (var i = 0; i < row.Levels.Count; i++)
        {
            if (i != target - 1)
                OnLoadoutUnpressed?.Invoke(row.Group, row.Levels[i].ID);
        }

        if (target is > 0 && target <= row.Levels.Count)
            OnLoadoutPressed?.Invoke(row.Group, row.Levels[target - 1].ID);
    }

    private void AddCheckbox(
        BoxContainer container,
        List<CheckRow> rows,
        ProtoId<LoadoutGroupPrototype> groupId,
        LoadoutPrototype loadoutProto,
        bool isSpecialLoadout)
    {
        var nameKey = $"colonist-skill-editor-loadout-{loadoutProto.ID}";
        var name = Loc.TryGetString(nameKey, out var localized) ? localized : loadoutProto.ID;

        var box = new CheckBox
        {
            Text = loadoutProto.Cost is { } cost
                ? Loc.GetString("colonist-skill-editor-extra-cost", ("name", name), ("cost", cost))
                : name,
        };
        box.VerticalAlignment = VAlignment.Center;

        var rowBox = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, SeparationOverride = 6 };
        var icons = BuildItemIcons(loadoutProto);
        foreach (var icon in icons)
            rowBox.AddChild(icon);

        var textColumn = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            VerticalAlignment = VAlignment.Center,
        };
        textColumn.AddChild(box);
        if (FormatRequirements(loadoutProto) is { } requirements)
            textColumn.AddChild(new Label { Text = requirements, FontColorOverride = UntrainedColor });

        rowBox.AddChild(textColumn);
        container.AddChild(rowBox);
        rows.Add(new CheckRow(groupId, loadoutProto.ID, box, icons));

        box.OnToggled += args =>
        {
            if (isSpecialLoadout)
            {
                if (args.Pressed)
                    OnSpecialLoadoutPressed?.Invoke(groupId, loadoutProto.ID);
                else
                    OnSpecialLoadoutUnpressed?.Invoke(groupId, loadoutProto.ID);
            }
            else if (args.Pressed)
            {
                OnLoadoutPressed?.Invoke(groupId, loadoutProto.ID);
            }
            else
            {
                OnLoadoutUnpressed?.Invoke(groupId, loadoutProto.ID);
            }
        };
    }

    private string? FormatRequirements(LoadoutPrototype loadoutProto)
    {
        var parts = loadoutProto.Effects
            .OfType<SkillRequirementLoadoutEffect>()
            .Select(requirement => Loc.GetString("colonist-skill-editor-requirement-part",
                ("skill", _skillNames.GetValueOrDefault(requirement.Skill.Id, requirement.Skill.Id)),
                ("level", requirement.MinLevel)))
            .ToList();

        return parts.Count == 0
            ? null
            : Loc.GetString("colonist-skill-editor-requires", ("skills", string.Join(", ", parts)));
    }

    private IEnumerable<EntProtoId> CollectItems(LoadoutPrototype loadoutProto)
    {
        if (loadoutProto.DummyEntity is { } dummy)
            return new[] { dummy };

        var sources = new List<IEquipmentLoadout> { loadoutProto };
        if (_protoMan.Resolve(loadoutProto.StartingGear, out var gear))
            sources.Add(gear);

        return sources.SelectMany(source => source.Equipment.Values
            .Concat(source.Inhand)
            .Concat(source.Storage.Values.SelectMany(list => list)));
    }

    // Makes small icons of the items a loadout gives.
    private List<Control> BuildItemIcons(LoadoutPrototype loadoutProto)
    {
        var icons = new List<Control>();
        var items = CollectItems(loadoutProto).Distinct().ToList();

        foreach (var itemId in items.Take(MaxItemIcons))
        {
            if (!_protoMan.TryIndex(itemId, out var itemProto))
                continue;

            icons.Add(new TextureRect
            {
                Texture = _sprite.GetPrototypeIcon(itemProto).GetFrame(RsiDirection.South, 0),
                Stretch = TextureRect.StretchMode.KeepAspectCentered,
                MinSize = new Vector2(ItemIconSize, ItemIconSize),
                MouseFilter = Control.MouseFilterMode.Pass,
                ToolTip = itemProto.Name + "\n" + itemProto.Description,
            });
        }

        if (items.Count > MaxItemIcons)
        {
            icons.Add(new Label
            {
                Text = $"+{items.Count - MaxItemIcons}",
                FontColorOverride = UntrainedColor,
                VerticalAlignment = VAlignment.Center,
            });
        }

        return icons;
    }

    // Updates points, slider values and which entries are available.
    public void RefreshLoadouts(
        HumanoidCharacterProfile profile,
        RoleLoadout loadout,
        RoleLoadout specialLoadout,
        ICommonSession session,
        IDependencyCollection collection)
    {
        if (_protoMan.Resolve(loadout.Role, out var roleProto) && roleProto.Points != null && loadout.Points != null)
        {
            var max = roleProto.Points.Value;
            var remaining = loadout.Points.Value;

            _pointsLabel.Text = Loc.GetString("loadouts-points-limit", ("count", remaining), ("max", max));
            _pointsLabel.FontColorOverride = remaining <= 0 ? WarningColor : null;
            _pointsBar.MaxValue = Math.Max(max, 1);
            _pointsBar.Value = Math.Clamp(max - remaining, 0, Math.Max(max, 1));
        }

        if (_protoMan.Resolve(specialLoadout.Role, out var specialRoleProto) &&
            specialRoleProto.Points != null && specialLoadout.Points != null)
        {
            _specialLoadoutPointsLabel.Text = Loc.GetString("loadouts-points-limit",
                ("count", specialLoadout.Points.Value),
                ("max", specialRoleProto.Points.Value));
            _specialLoadoutPointsLabel.FontColorOverride = specialLoadout.Points.Value <= 0 ? WarningColor : null;
        }

        var trained = 0;
        foreach (var row in _sliders)
        {
            var selected = loadout.SelectedLoadouts.TryGetValue(row.Group, out var picks) ? picks : new List<Loadout>();
            var currentLevel = 0;
            for (var i = 0; i < row.Levels.Count; i++)
            {
                if (selected.Any(pick => pick.Prototype.Id == row.Levels[i].ID))
                    currentLevel = i + 1;
            }

            if (currentLevel > 0)
                trained++;

            row.Slider.SetValueWithoutEvent(currentLevel);

            row.ValueLabel.Text = currentLevel == 0
                ? Loc.GetString("colonist-skill-editor-level-none")
                : Loc.GetString("colonist-skill-editor-level",
                    ("level", currentLevel),
                    ("max", row.Levels.Count),
                    ("cost", row.Levels[currentLevel - 1].Cost ?? 0));

            row.ValueLabel.FontColorOverride = currentLevel == 0
                ? UntrainedColor
                : currentLevel == row.Levels.Count ? ValueMaxedColor : ValueTrainedColor;
            row.NameLabel.FontColorOverride = currentLevel == 0 ? UntrainedColor : TrainedColor;

            FormattedMessage? firstReason = null;
            var anyAvailable = false;
            foreach (var level in row.Levels)
            {
                if (loadout.IsValid(profile, session, level.ID, collection, out var reason))
                {
                    anyAvailable = true;
                    break;
                }

                firstReason ??= reason;
            }

            row.Slider.Disabled = currentLevel == 0 && !anyAvailable;

            var tooltip = row.BaseTooltip;
            if (row.Slider.Disabled && firstReason != null)
                tooltip += "\n\n" + firstReason;

            row.Slider.ToolTip = tooltip;
            row.NameLabel.ToolTip = tooltip;
            row.ValueLabel.ToolTip = tooltip;
        }

        TrainedSkillCount = trained;
        _trainedLabel.Text = Loc.GetString("colonist-skill-editor-trained", ("count", trained), ("total", _sliders.Count));

        RefreshChecks(_checks, loadout, profile, session, collection);
        RefreshChecks(_specialLoadoutChecks, specialLoadout, profile, session, collection);
    }

    private static void RefreshChecks(
        List<CheckRow> rows,
        RoleLoadout loadout,
        HumanoidCharacterProfile profile,
        ICommonSession session,
        IDependencyCollection collection)
    {
        foreach (var row in rows)
        {
            var selected = loadout.SelectedLoadouts.TryGetValue(row.Group, out var picks) &&
                           picks.Any(pick => pick.Prototype.Id == row.Loadout.Id);

            row.Box.Pressed = selected;

            FormattedMessage? reason = null;
            var valid = selected || loadout.IsValid(profile, session, row.Loadout, collection, out reason);

            row.Box.Disabled = !valid;

            foreach (var icon in row.Icons)
                icon.Modulate = valid ? Color.White : new Color(1f, 1f, 1f, 0.4f);
            row.Box.ToolTip = !valid && reason != null ? reason.ToString() : null;
        }
    }
}

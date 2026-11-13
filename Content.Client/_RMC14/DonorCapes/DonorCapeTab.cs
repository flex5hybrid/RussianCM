using System.Linq;
using System.Numerics;
using Content.Client._RMC14.LinkAccount;
using Content.Client.Stylesheets;
using Content.Shared._RMC14.DonorCapes;
using Content.Shared.Preferences;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.DonorCapes;

public sealed class DonorCapeTab : BoxContainer
{
    private readonly IPrototypeManager _prototypeManager;
    private readonly LinkAccountManager _linkAccount;
    private readonly SpriteSystem _spriteSystem;
    private readonly ButtonGroup _buttonGroup = new();
    private readonly BoxContainer _capeSections = new()
    {
        Orientation = LayoutOrientation.Vertical,
        HorizontalExpand = true,
        SeparationOverride = 10,
    };
    private readonly Label _tierLabel = new();
    private Button _noneButton = default!;
    private readonly List<(RMCDonorCapePrototype Cape, Button Button)> _capeButtons = new();

    private HumanoidCharacterProfile? _profile;

    public event Action<ProtoId<RMCDonorCapePrototype>?>? OnCapeSelected;

    public DonorCapeTab(IPrototypeManager prototypeManager, LinkAccountManager linkAccount, SpriteSystem spriteSystem)
    {
        _prototypeManager = prototypeManager;
        _linkAccount = linkAccount;
        _spriteSystem = spriteSystem;

        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
        VerticalExpand = true;
        SeparationOverride = 8;

        AddChild(_tierLabel);

        var scroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        scroll.AddChild(_capeSections);
        AddChild(scroll);

        BuildCapeButtons();
        _linkAccount.Updated += RefreshAccess;
    }

    public void SetProfile(HumanoidCharacterProfile? profile)
    {
        _profile = profile;
        RefreshAccess();
    }

    [System.Obsolete]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _linkAccount.Updated -= RefreshAccess;

        base.Dispose(disposing);
    }

    private void BuildCapeButtons()
    {
        _noneButton = BuildButton(
            Loc.GetString("rmc-donor-capes-none"),
            selected: false,
            enabled: true,
            icon: null);
        _noneButton.OnPressed += _ => OnCapeSelected?.Invoke(null);
        var noneGrid = new GridContainer { Columns = 4, HorizontalExpand = true };
        noneGrid.AddChild(_noneButton);
        _capeSections.AddChild(noneGrid);

        var capes = _prototypeManager.EnumeratePrototypes<RMCDonorCapePrototype>().ToArray();
        foreach (var section in DonorCapeLayout.BuildSections(capes, cape => cape.RequiredPriority, cape => cape.Number))
        {
            var sectionContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                HorizontalExpand = true,
                SeparationOverride = 4,
            };
            sectionContainer.AddChild(new Label
            {
                Text = GetSectionTitle(section.RequiredPriority),
                HorizontalExpand = true,
                MinHeight = 24,
            });

            var grid = new GridContainer { Columns = 4, HorizontalExpand = true };
            foreach (var cape in section.Capes)
            {
                var button = BuildButton(
                    Loc.GetString(cape.Name),
                    selected: false,
                    enabled: false,
                    icon: DonorCapePreview.GetPreview(cape.Preview, cape.Icon));
                button.OnPressed += _ => OnCapeSelected?.Invoke(cape.ID);
                _capeButtons.Add((cape, button));
                grid.AddChild(button);
            }

            sectionContainer.AddChild(grid);
            _capeSections.AddChild(sectionContainer);
        }
    }

    private Button BuildButton(string label, bool selected, bool enabled, SpriteSpecifier? icon)
    {
        var button = new Button
        {
            MinSize = new Vector2(160, 150),
            MaxSize = new Vector2(160, 150),
            ToggleMode = true,
            Pressed = selected,
            Group = _buttonGroup,
            Disabled = !enabled,
            ToolTip = label,
            StyleClasses = { StyleClass.ButtonSquare },
        };

        var children = new List<Control>();

        if (icon is { } sprite)
        {
            var iconView = new DonorCapePreviewControl(_spriteSystem)
            {
                MinSize = new Vector2(112, 96),
                MaxSize = new Vector2(112, 96),
            };
            iconView.DisplayRect.MinSize = new Vector2(112, 96);
            iconView.DisplayRect.Stretch = TextureRect.StretchMode.Scale;
            iconView.SetFromSpriteSpecifier(sprite);
            children.Add(iconView);
        }

        children.Add(new Label
        {
            Text = label,
            MinSize = new Vector2(144, 36),
            MaxSize = new Vector2(144, 36),
            Align = Label.AlignMode.Center,
            ClipText = false,
        });

        var container = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            SeparationOverride = 2,
        };
        foreach (var child in children)
            container.AddChild(child);

        button.AddChild(container);

        return button;
    }

    private string GetSectionTitle(int requiredPriority)
    {
        return requiredPriority switch
        {
            4 => Loc.GetString("rmc-donor-capes-section-assault"),
            3 => Loc.GetString("rmc-donor-capes-section-scout"),
            2 => Loc.GetString("rmc-donor-capes-section-cell-commander"),
            1 => Loc.GetString("rmc-donor-capes-section-leader"),
            _ => Loc.GetString("rmc-donor-capes-tier", ("tier", GetRequiredTier(requiredPriority))),
        };
    }

    private void RefreshAccess()
    {
        var tier = _linkAccount.Tier;
        _tierLabel.Text = tier is { } currentTier
            ? Loc.GetString("rmc-donor-capes-tier", ("tier", currentTier.Tier))
            : Loc.GetString("rmc-donor-capes-no-access");

        var selected = _profile?.SelectedDonorCape;
        _noneButton.Pressed = selected is null;
        foreach (var (cape, button) in _capeButtons)
        {
            var access = DonorCapeAccess.HasAccess(tier, cape.RequiredPriority);
            button.Disabled = !access;
            button.Pressed = selected is { } selectedCape && selectedCape == cape.ID;
            button.ToolTip = access
                ? Loc.GetString(cape.Name)
                : Loc.GetString("rmc-donor-capes-locked", ("tier", GetRequiredTier(cape.RequiredPriority)));
        }
    }

    private string GetRequiredTier(int requiredPriority)
    {
        return requiredPriority switch
        {
            1 => Loc.GetString("rmc-donor-capes-tier-leader"),
            2 => Loc.GetString("rmc-donor-capes-tier-cell-commander"),
            3 => Loc.GetString("rmc-donor-capes-tier-scout"),
            4 => Loc.GetString("rmc-donor-capes-tier-assault"),
            _ => requiredPriority.ToString(),
        };
    }
}

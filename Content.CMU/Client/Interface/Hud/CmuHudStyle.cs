using System.Linq;
using Content.Client.Administration.UI.CustomControls;
using Content.Client.MainMenu.UI;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Actions.Controls;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Client.CMU14.Interface.Hud;

/// <summary>Tokens and styles shared by the game HUD, its drawers, and communication workspace.</summary>
public static partial class CmuHudStyle
{
    public const string Surface = "CmuHudSurface";
    public const string Inset = "CmuHudInset";
    public const string ButtonClass = "CmuHudButton";
    public const string MenuButtonClass = "CmuHudMenuButton";
    public const string Heading = "CmuHudHeading";
    public const string Muted = "CmuHudMuted";
    public const string Danger = "CmuHudDanger";
    public const string Body = "CmuHudBody";
    public const string MainMenuPanel = "CmuHudMainMenuPanel";
    public const string MainMenuCaption = "CmuHudMainMenuCaption";
    public const string Command = "CmuHudCommand";
    public static float ChatOpacity { get; internal set; } = 0.85f;

    // Read the existing UI setup palette at use time, including its unsaved preview.
    // Static slot styleboxes must not retain the colour selected when they were constructed.
    public static Color SurfaceColor => StyleNano.CrtUiEnabled
        ? Color.InterpolateBetween(Color.FromHex("#050C10"), CrtTerminalPalette.Surface0, 0.35f)
        : CrtTerminalPalette.Surface0;
    public static Color InsetColor => StyleNano.CrtUiEnabled
        ? Color.InterpolateBetween(Color.FromHex("#03070A"), CrtTerminalPalette.Void, 0.25f)
        : CrtTerminalPalette.Void;
    public static Color HeaderColor => StyleNano.CrtUiEnabled
        ? Color.InterpolateBetween(SurfaceColor, CrtTerminalPalette.Accent, 0.075f)
        : CrtTerminalPalette.Surface3;
    public static Color HoverColor => StyleNano.CrtUiEnabled
        ? Color.InterpolateBetween(SurfaceColor, CrtTerminalPalette.Accent, 0.14f)
        : CrtTerminalPalette.Surface2;
    public static Color EdgeColor => StyleNano.CrtUiEnabled
        ? Color.InterpolateBetween(SurfaceColor, CrtTerminalPalette.Text, 0.42f)
        : CrtTerminalPalette.Line;
    public static Color TextColor => CrtTerminalPalette.TextBright;
    public static Color MutedColor => CrtTerminalPalette.TextDim;
    public static Color SelectionColor => CrtTerminalPalette.Accent;
    public static Color InverseTextColor => StyleNano.CrtUiEnabled ? CrtTerminalPalette.Void : TextColor;
    public static Color DangerColor => CrtTerminalPalette.Alert;

    public static CmuHudStyleBox Box(CmuHudTone tone = CmuHudTone.Surface, bool selected = false, float padding = 0) => new()
    {
        Tone = tone,
        Selected = selected,
        Inverse = selected,
        ContentMarginLeftOverride = padding,
        ContentMarginRightOverride = padding,
        ContentMarginTopOverride = padding,
        ContentMarginBottomOverride = padding,
    };

    public static StyleRule[] BuildRules(IResourceCache cache)
    {
        // Disabling the new interface must restore native rules, not a grey version of this skin.
        if (!StyleNano.CrtUiEnabled)
            return Array.Empty<StyleRule>();
        var normal = Box(padding: 7);
        var hover = Box(CmuHudTone.Hover, padding: 7);
        var selected = Box(CmuHudTone.Selected, selected: true, padding: 7);
        var disabled = Box(CmuHudTone.Inset, padding: 7);
        var menu = Box(padding: 3);
        menu.OutlineOnly = true;
        var menuHover = Box(CmuHudTone.Hover, padding: 3);
        menuHover.Opacity = 0.15f;
        var menuSelected = Box(CmuHudTone.Selected, selected: true, padding: 3);
        var selection = new CmuHudStyleBox { Selected = true, OutlineOnly = true };
        var headingFont = StyleNano.CrtUiEnabled ? StyleNano.GetCrtFont(cache, 8) : cache.NotoStack("Bold", 12);
        var bodyFont = StyleNano.CrtUiEnabled ? StyleNano.GetCrtFont(cache, 8) : cache.NotoStack(size: 12);
        var command = Box(CmuHudTone.Inset, padding: 7);
        var commandHover = Box(CmuHudTone.Hover, padding: 7);
        var commandSelected = Box(CmuHudTone.Selected, selected: true, padding: 7);
        return BuildWindowRules(cache).Concat(BuildContextMenuRules(cache)).Concat(BuildExamineRules(cache))
            .Concat(new StyleRule[]
        {
            Element<PanelContainer>().Class(Surface).Prop(PanelContainer.StylePropertyPanel, Box()),
            Element<PanelContainer>().Class(Inset).Prop(PanelContainer.StylePropertyPanel, Box(CmuHudTone.Inset)),
            Element<Label>().Class(Heading).Prop(Label.StylePropertyFont, headingFont).Prop(Label.StylePropertyFontColor, TextColor),
            Element<Label>().Class(Body).Prop(Label.StylePropertyFont, bodyFont).Prop(Label.StylePropertyFontColor, CrtTerminalPalette.Text),
            Element<Label>().Class(Muted).Prop(Label.StylePropertyFontColor, MutedColor),
            Element<Button>().Class(ButtonClass).Prop(ContainerButton.StylePropertyStyleBox, normal).Prop(Control.StylePropertyModulateSelf, Color.White),
            Element<Button>().Class(ButtonClass).Pseudo(ContainerButton.StylePseudoClassHover).Prop(ContainerButton.StylePropertyStyleBox, hover),
            Element<Button>().Class(ButtonClass).Pseudo(ContainerButton.StylePseudoClassPressed).Prop(ContainerButton.StylePropertyStyleBox, selected),
            Element<Button>().Class(ButtonClass).Pseudo(ContainerButton.StylePseudoClassDisabled).Prop(ContainerButton.StylePropertyStyleBox, disabled),
            new StyleRule(new SelectorChild(new SelectorElement(null, [ButtonClass], null, null), SelectorElement.Type(typeof(Label))),
                [new StyleProperty(Label.StylePropertyFontColor, TextColor), new StyleProperty(Label.StylePropertyFont, bodyFont)]),
            Element<MenuButton>().Class(MenuButtonClass).Prop(ContainerButton.StylePropertyStyleBox, menu).Prop(Control.StylePropertyModulateSelf, Color.White),
            Element<MenuButton>().Class(MenuButtonClass).Pseudo(ContainerButton.StylePseudoClassHover).Prop(ContainerButton.StylePropertyStyleBox, menuHover).Prop(Control.StylePropertyModulateSelf, Color.White),
            Element<MenuButton>().Class(MenuButtonClass).Pseudo(ContainerButton.StylePseudoClassPressed).Prop(ContainerButton.StylePropertyStyleBox, menuSelected).Prop(Control.StylePropertyModulateSelf, Color.White),
            Element<PanelContainer>().Class(ActionButton.StyleClassActionHighlightRect).Prop(PanelContainer.StylePropertyPanel, selection),
            Element<Button>().Class(Command).Prop(ContainerButton.StylePropertyStyleBox, command).Prop(Control.StylePropertyModulateSelf, Color.White),
            Element<Button>().Class(Command).Pseudo(ContainerButton.StylePseudoClassHover).Prop(ContainerButton.StylePropertyStyleBox, commandHover),
            Element<Button>().Class(Command).Pseudo(ContainerButton.StylePseudoClassPressed).Prop(ContainerButton.StylePropertyStyleBox, commandSelected),
            Element<Button>().Class(Command).Pseudo(ContainerButton.StylePseudoClassDisabled).Prop(ContainerButton.StylePropertyStyleBox, command).Prop(Control.StylePropertyModulateSelf, Color.White.WithAlpha(0.45f)),
            Child().Parent(Element<Button>().Class(Command)).Child(Element<Label>())
                .Prop(Label.StylePropertyFont, bodyFont).Prop(Label.StylePropertyFontColor, TextColor),
            Child().Parent(Element<Button>().Class(Command).Pseudo(ContainerButton.StylePseudoClassPressed))
                .Child(Element<Label>()).Prop(Label.StylePropertyFontColor, InverseTextColor),
            Child().Parent(Element<Button>().Class(ButtonClass).Pseudo(ContainerButton.StylePseudoClassPressed))
                .Child(Element<Label>()).Prop(Label.StylePropertyFontColor, InverseTextColor),
        }).ToArray();
    }

    // Native menus must participate in UI setup too. Class-based rules also cover controls
    // created after a window opens, without walking or polling every window's control tree.
    private static IEnumerable<StyleRule> BuildWindowRules(IResourceCache cache)
    {
        if (!StyleNano.CrtUiEnabled)
            yield break;

        var font = StyleNano.GetCrtFont(cache, 8);
        // FancyWindow, changelog and many embedded menus use these native backing classes.
        // Their opaque fills must carry the material themselves, rather than hide a parent pass.
        yield return Element<PanelContainer>().Class(StyleClass.BackgroundPanel)
            .Prop(PanelContainer.StylePropertyPanel, new CmuCrtFillStyleBox { BackgroundColor = SurfaceColor })
            .Prop(Control.StylePropertyModulateSelf, Color.White);
        yield return Element<PanelContainer>().Class(StyleNano.StyleClassBorderedWindowPanel)
            .Prop(PanelContainer.StylePropertyPanel, new CmuCrtFillStyleBox
            {
                BackgroundColor = SurfaceColor, BorderColor = EdgeColor, BorderThickness = new Thickness(2),
            }).Prop(Control.StylePropertyModulateSelf, Color.White);
        foreach (var heading in new[] { "WindowHeadingBackground", "WindowHeadingBackgroundLight" })
        {
            yield return Element<PanelContainer>().Class(heading)
                .Prop(PanelContainer.StylePropertyPanel, new CmuCrtFillStyleBox { BackgroundColor = HeaderColor })
                .Prop(Control.StylePropertyModulateSelf, Color.White);
        }
        yield return Element<Label>().Class("FancyWindowTitle")
            .Prop(Label.StylePropertyFont, font).Prop(Label.StylePropertyFontColor, TextColor);
        yield return Element<PanelContainer>().Class(StyleClass.BackgroundPanel).Class(MainMenuPanel)
            .Prop(PanelContainer.StylePropertyPanel, Box(padding: 12))
            .Prop(Control.StylePropertyModulateSelf, Color.White);
        yield return Element<Label>().Class(MainMenuCaption)
            .Prop(Label.StylePropertyFont, font)
            .Prop(Label.StylePropertyFontColor, CrtTerminalPalette.Text);
        yield return Child()
            .Parent(Element<Button>().Identifier(MainMenuControl.StyleIdentifierMainMenu).Class(ContainerButton.StyleClassButton))
            .Child(Element<Label>())
            .Prop(Label.StylePropertyFont, StyleNano.GetCrtFont(cache, 10));
        yield return Element<TabContainer>()
            .Prop(TabContainer.stylePropertyTabFontColor, TextColor)
            .Prop(TabContainer.StylePropertyTabFontColorInactive, MutedColor)
            .Prop("font", font);

        yield return Element<HSeparator>().Prop(HSeparator.StylePropertyColor, EdgeColor);
        yield return Element<LineEdit>()
            .Prop(LineEdit.StylePropertyStyleBox, Box(CmuHudTone.Inset, padding: 4))
            .Prop("font", font).Prop("font-color", TextColor);
        yield return Element<LineEdit>().Pseudo(LineEdit.StylePseudoClassPlaceholder)
            .Prop("font-color", MutedColor);
        yield return Element<ScrollBar>()
            .Prop(ScrollBar.StylePropertyGrabber, new StyleBoxFlat { BackgroundColor = EdgeColor });
        yield return Element<ScrollBar>().Pseudo(ScrollBar.StylePseudoClassHover)
            .Prop(ScrollBar.StylePropertyGrabber, new StyleBoxFlat { BackgroundColor = MutedColor });
        yield return Element<ScrollBar>().Pseudo(ScrollBar.StylePseudoClassGrabbed)
            .Prop(ScrollBar.StylePropertyGrabber, new StyleBoxFlat { BackgroundColor = SelectionColor });
        yield return Element<PanelContainer>().Class("BackgroundOpenLeft")
            .Prop(PanelContainer.StylePropertyPanel, Box(CmuHudTone.Inset))
            .Prop(Control.StylePropertyModulateSelf, Color.White);
        yield return Element<PanelContainer>().Class(DefaultWindow.StyleClassWindowPanel)
            .Prop(PanelContainer.StylePropertyPanel, Box(padding: 1));
        yield return Element<PanelContainer>().Class(DefaultWindow.StyleClassWindowHeader)
            .Prop(PanelContainer.StylePropertyPanel, Box(CmuHudTone.Header, padding: 4));
        yield return Element<Label>().Class(DefaultWindow.StyleClassWindowTitle)
            .Prop(Label.StylePropertyFont, font).Prop(Label.StylePropertyFontColor, TextColor);
        yield return Element<TextureButton>().Class(DefaultWindow.StyleClassWindowCloseButton)
            .Prop(Control.StylePropertyModulateSelf, MutedColor);
        yield return Child().Parent(Element<Button>()).Child(Element<Label>())
            .Prop(Label.StylePropertyFont, font).Prop(Label.StylePropertyFontColor, TextColor);

        foreach (var shape in new[] { "", StyleNano.ButtonOpenLeft, StyleNano.ButtonOpenRight, StyleNano.ButtonOpenBoth })
        foreach (var semantic in new[] { "", StyleNano.ButtonCaution, "positive" })
        {
            var normal = Element<ContainerButton>().Class(ContainerButton.StyleClassButton);
            var hover = Element<ContainerButton>().Class(ContainerButton.StyleClassButton);
            var pressed = Element<ContainerButton>().Class(ContainerButton.StyleClassButton);
            var disabled = Element<ContainerButton>().Class(ContainerButton.StyleClassButton);
            if (semantic.Length != 0)
            {
                normal = normal.Class(semantic);
                hover = hover.Class(semantic);
                pressed = pressed.Class(semantic);
                disabled = disabled.Class(semantic);
            }
            if (shape.Length != 0)
            {
                normal = normal.Class(shape);
                hover = hover.Class(shape);
                pressed = pressed.Class(shape);
                disabled = disabled.Class(shape);
            }
            yield return normal.Prop(ContainerButton.StylePropertyStyleBox, Box(padding: 7))
                .Prop(Control.StylePropertyModulateSelf, Color.White);
            yield return hover.Pseudo(ContainerButton.StylePseudoClassHover)
                .Prop(ContainerButton.StylePropertyStyleBox, Box(CmuHudTone.Hover, padding: 7))
                .Prop(Control.StylePropertyModulateSelf, Color.White);
            yield return pressed.Pseudo(ContainerButton.StylePseudoClassPressed)
                .Prop(ContainerButton.StylePropertyStyleBox, Box(CmuHudTone.Selected, selected: true, padding: 7))
                .Prop(Control.StylePropertyModulateSelf, Color.White);
            yield return disabled.Pseudo(ContainerButton.StylePseudoClassDisabled)
                .Prop(ContainerButton.StylePropertyStyleBox, Box(CmuHudTone.Inset, padding: 7))
                .Prop(Control.StylePropertyModulateSelf, Color.White.WithAlpha(0.45f));
            yield return Child().Parent(pressed.Pseudo(ContainerButton.StylePseudoClassPressed))
                .Child(Element<Label>()).Prop(Label.StylePropertyFontColor, InverseTextColor);
        }
        yield return Child().Parent(Element<Button>().Class(StyleNano.ButtonCaution)).Child(Element<Label>())
            .Prop(Label.StylePropertyFontColor, DangerColor);
        yield return Child().Parent(Element<Button>().Class(StyleNano.ButtonCaution)
                .Pseudo(ContainerButton.StylePseudoClassPressed)).Child(Element<Label>())
            .Prop(Label.StylePropertyFontColor, InverseTextColor);
    }
}

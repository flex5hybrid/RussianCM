using Content.Client.ContextMenu.UI;
using Content.Client.Stylesheets;
using Content.Client.Verbs.UI;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Client.CMU14.Interface.Hud;

public static partial class CmuHudStyle
{
    private static IEnumerable<StyleRule> BuildContextMenuRules(IResourceCache cache)
    {
        if (!StyleNano.CrtUiEnabled)
            yield break;

        yield return Element<PanelContainer>().Class(ContextMenuPopup.StyleClassContextMenuPopup)
            .Prop(PanelContainer.StylePropertyPanel, Box(padding: ContextMenuElement.ElementMargin))
            .Prop(Control.StylePropertyModulateSelf, Color.White);

        // Context menus use their own row classes, rather than the standard button class.
        // Override each pseudo state's legacy tint so the panel fill is not tinted a second time.
        foreach (var confirmation in new[] { false, true })
        {
            var styleClass = confirmation
                ? ConfirmationMenuElement.StyleClassConfirmationContextMenuButton
                : ContextMenuElement.StyleClassContextMenuButton;
            foreach (var state in new[]
                     {
                         "", ContainerButton.StylePseudoClassNormal, ContainerButton.StylePseudoClassHover,
                         ContainerButton.StylePseudoClassPressed, ContainerButton.StylePseudoClassDisabled,
                     })
            {
                var highlighted = state == ContainerButton.StylePseudoClassHover
                    || state == ContainerButton.StylePseudoClassPressed;
                var disabled = state == ContainerButton.StylePseudoClassDisabled;
                var surface = new CmuCrtFillStyleBox
                {
                    BackgroundColor = confirmation
                        ? DangerColor.WithAlpha(highlighted ? 0.22f : 0.1f)
                        : highlighted ? SelectionColor : InsetColor,
                    BorderColor = confirmation ? DangerColor : SelectionColor,
                    BorderThickness = new Thickness(highlighted || confirmation ? 2 : 0, 0, 0, 0),
                    // Fixed margins keep icons and text still while the active rail appears.
                    ContentMarginLeftOverride = 4,
                    ContentMarginRightOverride = 4,
                    ContentMarginTopOverride = 2,
                    ContentMarginBottomOverride = 2,
                };
                var selector = Element<ContextMenuElement>().Class(styleClass);
                if (state.Length != 0)
                    selector = selector.Pseudo(state);
                yield return selector.Prop(ContainerButton.StylePropertyStyleBox, surface)
                    .Prop(Control.StylePropertyModulateSelf, Color.White.WithAlpha(disabled ? 0.4f : 1f));
            }

            // The text is inside the row's BoxContainer, not a direct child of the button.
            yield return Child()
                .Parent(Child().Parent(Element<ContextMenuElement>().Class(styleClass)).Child(Element<BoxContainer>()))
                .Child(Element<RichTextLabel>())
                .Prop(Label.StylePropertyFont, StyleNano.GetCrtFont(cache, 8))
                .Prop(Label.StylePropertyFontColor, confirmation ? DangerColor : TextColor);
            if (!confirmation)
            {
                foreach (var state in new[] { ContainerButton.StylePseudoClassHover, ContainerButton.StylePseudoClassPressed })
                {
                    yield return Child()
                        .Parent(Child().Parent(Element<ContextMenuElement>().Class(styleClass).Pseudo(state))
                            .Child(Element<BoxContainer>()))
                        .Child(Element<RichTextLabel>())
                        .Prop(Label.StylePropertyFontColor, InverseTextColor);
                }
            }
            yield return Child()
                .Parent(Child().Parent(Element<ContextMenuElement>().Class(styleClass)
                    .Pseudo(ContainerButton.StylePseudoClassDisabled)).Child(Element<BoxContainer>()))
                .Child(Element<RichTextLabel>())
                .Prop(Label.StylePropertyFontColor, MutedColor);
        }

        foreach (var styleClass in new[]
                 {
                     ContextMenuElement.StyleClassContextMenuExpansionTexture,
                     VerbMenuElement.StyleClassVerbMenuConfirmationTexture,
                 })
        {
            yield return Element<TextureRect>().Class(styleClass)
                .Prop(Control.StylePropertyModulateSelf, TextColor);
        }
    }
}

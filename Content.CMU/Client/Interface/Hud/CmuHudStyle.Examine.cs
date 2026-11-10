using Content.Client.Examine;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Client.CMU14.Interface.Hud;

public static partial class CmuHudStyle
{
    public const string ExamineHeading = "CmuHudExamineHeading";
    public const string ExamineBody = "CmuHudExamineBody";

    private static IEnumerable<StyleRule> BuildExamineRules(IResourceCache cache)
    {
        // The legacy tint belongs in the stylesheet so an open popup can follow CRT preview/reset.
        yield return Element<PanelContainer>().Class(ExamineSystem.StyleClassEntityTooltip)
            .Prop(Control.StylePropertyModulateSelf,
                StyleNano.CrtUiEnabled ? Color.White : Color.LightGray.WithAlpha(0.9f));

        // Descriptions are prose. Preserve case and give wrapped lines room to breathe.
        yield return Element<RichTextLabel>().Class(ExamineHeading)
            .Prop(Label.StylePropertyFont, cache.NotoStack("Bold", StyleNano.CrtUiEnabled ? 13 : 12))
            .Prop(nameof(RichTextLabel.LineHeightScale), 1.15f);
        yield return Element<RichTextLabel>().Class(ExamineBody)
            .Prop(Label.StylePropertyFont, cache.NotoStack(size: 12))
            .Prop(nameof(RichTextLabel.LineHeightScale), 1.25f);

        if (!StyleNano.CrtUiEnabled)
            yield break;

        yield return Element<PanelContainer>().Class(ExamineSystem.StyleClassEntityTooltip)
            .Prop(PanelContainer.StylePropertyPanel, new CmuHudStyleBox
            {
                ExamineSurface = true, ContentMarginLeftOverride = 10, ContentMarginRightOverride = 10,
                ContentMarginTopOverride = 10, ContentMarginBottomOverride = 10,
            });
        yield return Element<RichTextLabel>().Class(ExamineHeading)
            .Prop(Label.StylePropertyFontColor, TextColor);
        yield return Element<RichTextLabel>().Class(ExamineBody)
            .Prop(Label.StylePropertyFontColor, CrtTerminalPalette.Text);

        foreach (var state in new[]
                 {
                     "", ContainerButton.StylePseudoClassNormal, ContainerButton.StylePseudoClassHover,
                     ContainerButton.StylePseudoClassPressed, ContainerButton.StylePseudoClassDisabled,
                 })
        {
            var pressed = state == ContainerButton.StylePseudoClassPressed;
            var hovered = state == ContainerButton.StylePseudoClassHover;
            var disabled = state == ContainerButton.StylePseudoClassDisabled;
            var selector = Element<ExamineButton>().Class(ExamineButton.StyleClassExamineButton);
            if (state.Length != 0)
                selector = selector.Pseudo(state);
            yield return selector
                .Prop(ContainerButton.StylePropertyStyleBox,
                    Box(pressed ? CmuHudTone.Selected : hovered ? CmuHudTone.Hover : CmuHudTone.Inset,
                        selected: pressed))
                .Prop(Control.StylePropertyModulateSelf, Color.White.WithAlpha(disabled ? 0.4f : 1f));
        }

        yield return Child().Parent(Element<ExamineButton>().Class(ExamineButton.StyleClassExamineButton))
            .Child(Element<TextureRect>())
            .Prop(Control.StylePropertyModulateSelf, TextColor);
        yield return Child().Parent(Element<ExamineButton>().Class(ExamineButton.StyleClassExamineButton)
                .Pseudo(ContainerButton.StylePseudoClassPressed))
            .Child(Element<TextureRect>())
            .Prop(Control.StylePropertyModulateSelf, InverseTextColor);
        yield return Child().Parent(Element<ExamineButton>().Class(ExamineButton.StyleClassExamineButton)
                .Pseudo(ContainerButton.StylePseudoClassDisabled))
            .Child(Element<TextureRect>())
            .Prop(Control.StylePropertyModulateSelf, MutedColor);
    }
}

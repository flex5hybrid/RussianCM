using System.Numerics;
using Content.Client.CMU14.Interface;
using Content.Client.CMU14.Interface.Hud;
using Content.Shared.CCVar;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Client.Examine;

public sealed partial class ExamineSystem
{
    private EntityUid _cmuExaminer;
    private EntityUid _cmuVisibilityPlayer;
    private SharedInteractionSystem.Ignored? _cmuExaminePredicate;
    private BoxContainer? _cmuHeader;
    private RichTextLabel? _cmuNameLabel;
    private RichTextLabel? _cmuBodyLabel;
    private SpriteView? _cmuPortrait;
    private BoxContainer? _cmuHoverButtons;
    private BoxContainer? _cmuClickButtons;
    private string? _cmuName;
    private bool _cmuKnownTarget;

    private bool CMUCanStillExamine(EntityUid player)
    {
        if (IsClientSide(_examinedEntity))
            return true;
        if (Deleted(_examinedEntity))
            return false;
        if (_cmuExaminePredicate == null || _cmuVisibilityPlayer != player)
        {
            _cmuVisibilityPlayer = player;
            _cmuExaminePredicate = CMUCreatePredicate(player, _examinedEntity);
        }
        return CanExamine(player, EntityManager.System<SharedTransformSystem>().GetMapCoordinates(_examinedEntity),
            _cmuExaminePredicate, _examinedEntity);
    }

    private static SharedInteractionSystem.Ignored CMUCreatePredicate(EntityUid player, EntityUid target) =>
        entity => entity == player || entity == target;

    private void CMUOpenTooltip(EntityUid player, EntityUid target, bool centeredOnCursor, bool openAtOldTooltip,
        bool knowTarget)
    {
        ScreenCoordinates? oldPosition = _examineTooltipOpen != null ? _popupPos : null;
        if (_config.GetCVar(CCVars.HideExamineTooltip))
        {
            CloseTooltip();
            return;
        }

        var reuse = _examineTooltipOpen is { Visible: true } && _examinedEntity == target && _cmuExaminer == player;
        if (!reuse)
            CloseTooltip();

        _examinedEntity = target;
        _cmuExaminer = player;
        _popupPos = openAtOldTooltip && oldPosition != null ? oldPosition.Value : centeredOnCursor
            ? _userInterfaceManager.MousePositionScaled
            : _userInterfaceManager.ScreenToUIPosition(_eyeManager.CoordinatesToScreen(Transform(target).Coordinates));

        if (!reuse)
        {
            _examineTooltipOpen = new Popup { MaxWidth = 400 };
            _userInterfaceManager.ModalRoot.AddChild(_examineTooltipOpen);
            var panel = new CmuCrtDisplayPanel
            {
                Name = "ExaminePopupPanel", MenuSurface = true, IntensityScale = 0.45f,
            };
            panel.AddStyleClass(StyleClassEntityTooltip);
            _examineTooltipOpen.AddChild(panel);
            var body = new BoxContainer
            {
                Name = "ExaminePopupVbox", Orientation = BoxContainer.LayoutOrientation.Vertical, MaxWidth = 400,
                SeparationOverride = 8,
            };
            panel.AddChild(body);
            _cmuHeader = new BoxContainer { SeparationOverride = 10 };
            body.AddChild(_cmuHeader);
            _cmuNameLabel = new RichTextLabel
            {
                StyleClasses = { CmuHudStyle.ExamineHeading },
                HorizontalExpand = true,
            };
            _cmuHeader.AddChild(_cmuNameLabel);
            _cmuBodyLabel = new RichTextLabel
            {
                Margin = new Thickness(0, 0, 0, 2),
                Visible = false,
                StyleClasses = { CmuHudStyle.ExamineBody },
            };
            body.AddChild(_cmuBodyLabel);
            var buttons = new BoxContainer { Name = "ExamineButtonsHBox", VerticalAlignment = Control.VAlignment.Bottom };
            _cmuHoverButtons = new BoxContainer { Name = "HoverExamineHBox", HorizontalExpand = true,
                HorizontalAlignment = Control.HAlignment.Left, VerticalAlignment = Control.VAlignment.Center };
            _cmuClickButtons = new BoxContainer { Name = "ClickExamineHBox", HorizontalExpand = true,
                HorizontalAlignment = Control.HAlignment.Right, VerticalAlignment = Control.VAlignment.Center };
            buttons.AddChild(_cmuHoverButtons);
            buttons.AddChild(_cmuClickButtons);
            body.AddChild(buttons);
            // The display panel retains the body as the first child for UpdateTooltipInfo.
        }

        if (HasComp<SpriteComponent>(target))
        {
            if (_cmuPortrait == null)
            {
                _cmuPortrait = new SpriteView { OverrideDirection = Direction.South, SetSize = new Vector2(32, 32) };
                _cmuPortrait.SetEntity(target);
                _cmuHeader!.AddChild(_cmuPortrait);
                _cmuPortrait.SetPositionFirst();
            }
            _cmuPortrait.Visible = true;
        }
        else if (_cmuPortrait != null)
            _cmuPortrait.Visible = false;

        string name = knowTarget ? Identity.Name(target, EntityManager, player) : "???";
        if (_cmuName != name || _cmuKnownTarget != knowTarget)
        {
            _cmuName = name;
            _cmuKnownTarget = knowTarget;
            // The heading font owns its weight and metrics; inline bold tags select another font.
            _cmuNameLabel!.SetMessage(name);
        }

        _cmuBodyLabel!.Visible = false;
        CMUClearButtons();
        if (reuse)
        {
            // Reopening a visible Popup removes its modal first. Move it directly to retain its lifecycle.
            PopupContainer.SetPopupOrigin(_examineTooltipOpen!, _popupPos.Position);
        }
        else
        {
            // Open stores a minimum size. Let the current children determine the rest so reused headers can shrink.
            _examineTooltipOpen!.Open(UIBox2.FromDimensions(_popupPos.Position, new Vector2(300, 0)));
        }
    }

    private void CMUUpdateBody(FormattedMessage message)
    {
        if (_cmuBodyLabel == null)
            return;
        _cmuBodyLabel.Visible = false;
        foreach (var node in message.Nodes)
        {
            if (node.Name != null || string.IsNullOrWhiteSpace(node.Value.StringValue))
                continue;
            _cmuBodyLabel.SetMessage(message);
            _cmuBodyLabel.Visible = true;
            break;
        }
    }

    private void CMUClearButtons()
    {
        if (_cmuClickButtons != null)
        {
            foreach (var child in _cmuClickButtons.Children)
            {
                if (child is ExamineButton button)
                    button.OnPressed -= VerbButtonPressed;
            }
            _cmuClickButtons.RemoveAllChildren();
        }
        _cmuHoverButtons?.RemoveAllChildren();
    }

    private void CMUResetTooltip()
    {
        CMUClearButtons();
        _cmuHeader = null;
        _cmuNameLabel = null;
        _cmuBodyLabel = null;
        _cmuPortrait = null;
        _cmuHoverButtons = null;
        _cmuClickButtons = null;
        _cmuName = null;
        _cmuExaminePredicate = null;
    }
}

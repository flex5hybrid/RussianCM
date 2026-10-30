using Content.Client.RUMC.Marines.Squads.Specialization;
using Content.Shared._RMC14.Marines.Squads.Specialization;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Marines.Squads.Specialization;

[UsedImplicitly]
public sealed class SquadSpecializationTabletBui : BoundUserInterface
{
    [Dependency] private IPrototypeManager _prototypes = default!;

    private SquadSpecializationTabletWindow? _window;

    public SquadSpecializationTabletBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<SquadSpecializationTabletWindow>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not SquadSpecializationBuiState buiState)
            return;

        _window.SquadNameLabel.Text = buiState.SquadName;
        _window.SquadNameLabel.FontColorOverride = buiState.SquadColor;

        if (buiState.IsLocked && buiState.CurrentSpecialization is { } activeSpec &&
            _prototypes.TryIndex(activeSpec, out var activeProto))
        {
            _window.SquadStatusLabel.Text = Loc.GetString("rmc-squad-specialization-status-locked", ("name", Loc.GetString(activeProto.Name)));
            _window.SquadStatusLabel.FontColorOverride = Color.FromHex("#3A9B52");
        }
        else
        {
            _window.SquadStatusLabel.Text = Loc.GetString("rmc-squad-specialization-status-unselected");
            _window.SquadStatusLabel.FontColorOverride = Color.FromHex("#E5A93C");
        }

        _window.SpecializationsContainer.DisposeAllChildren();

        foreach (var specId in buiState.AvailableSpecializations)
        {
            if (!_prototypes.TryIndex(specId, out var proto))
                continue;

            var isCurrent = buiState.CurrentSpecialization == specId;

            var card = new PanelContainer
            {
                Margin = new Thickness(0, 2),
            };

            var bgColor = isCurrent ? Color.FromHex("#1c2b20") : Color.FromHex("#1a2128");
            var borderColor = isCurrent ? Color.FromHex("#3A9B52") : Color.FromHex("#2d3b47");

            card.PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = bgColor,
                BorderColor = borderColor,
                BorderThickness = new Thickness(isCurrent ? 2 : 1),
            };

            var row = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                Margin = new Thickness(10, 8),
                SeparationOverride = 12,
            };

            if (proto.Icon != null)
            {
                var icon = new TextureRect
                {
                    Texture = EntMan.System<SpriteSystem>().Frame0(proto.Icon),
                    SetSize = new System.Numerics.Vector2(48, 48),
                    Stretch = TextureRect.StretchMode.KeepAspectCentered,
                    VerticalAlignment = Control.VAlignment.Center,
                };
                row.AddChild(icon);
            }

            var textColumn = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                HorizontalExpand = true,
                SeparationOverride = 4,
            };

            var nameLabel = new Label
            {
                Text = Loc.GetString(proto.Name),
                FontColorOverride = isCurrent ? Color.FromHex("#70DB88") : Color.FromHex("#E6ECF2"),
                StyleClasses = { "LabelHeading" },
            };
            textColumn.AddChild(nameLabel);

            var descLabel = new RichTextLabel();
            descLabel.SetMessage(FormattedMessage.FromMarkupPermissive($"[color=#A4B1BC]{Loc.GetString(proto.Description)}[/color]"));
            textColumn.AddChild(descLabel);

            if (proto.Summary != null)
            {
                var summaryLabel = new RichTextLabel
                {
                    Margin = new Thickness(0, 4, 0, 0),
                };
                summaryLabel.SetMessage(FormattedMessage.FromMarkupPermissive($"[color=#C9B277]{Loc.GetString(proto.Summary.Value)}[/color]"));
                textColumn.AddChild(summaryLabel);
            }

            row.AddChild(textColumn);

            var actionButton = new Button
            {
                MinWidth = 160,
                VerticalAlignment = Control.VAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0),
            };

            if (buiState.IsLocked)
            {
                if (isCurrent)
                {
                    actionButton.Text = Loc.GetString("rmc-squad-specialization-btn-active");
                    actionButton.Disabled = true;
                }
                else
                {
                    actionButton.Text = Loc.GetString("rmc-squad-specialization-btn-locked");
                    actionButton.Disabled = true;
                }
            }
            else if (buiState.TakenSpecializations.TryGetValue(specId, out var takenBySquad))
            {
                actionButton.Text = Loc.GetString("rmc-squad-specialization-btn-taken", ("squad", takenBySquad));
                actionButton.Disabled = true;
            }
            else
            {
                actionButton.Text = Loc.GetString("rmc-squad-specialization-btn-select");
                var selectedSpecId = specId;
                actionButton.OnPressed += _ =>
                {
                    SendPredictedMessage(new SquadSpecializationSelectBuiMsg(selectedSpecId));
                };
            }

            row.AddChild(actionButton);
            card.AddChild(row);
            _window.SpecializationsContainer.AddChild(card);
        }
    }
}

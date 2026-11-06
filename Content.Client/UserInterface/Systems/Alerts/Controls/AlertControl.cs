using System.Numerics;
using Content.Client.Actions.UI;
using Content.Client.Cooldown;
using Content.Client.CMU14.Interface.Hud;
using Content.Client.Stylesheets;
using Robust.Client.ResourceManagement;
using Content.Shared.Alert;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Alerts.Controls
{
    public sealed partial class AlertControl : BaseButton
    {
        [Dependency] private IEntityManager _entityManager = default!;

        private readonly SpriteSystem _sprite;

        public AlertPrototype Alert { get; }

        /// <summary>
        /// Current cooldown displayed in this slot. Set to null to show no cooldown.
        /// </summary>
        public (TimeSpan Start, TimeSpan End)? Cooldown
        {
            get => _cooldown;
            set
            {
                _cooldown = value;
                if (SuppliedTooltip is ActionAlertTooltip actionAlertTooltip)
                {
                    actionAlertTooltip.Cooldown = value;
                }
            }
        }

        public string? DynamicMessage
        {
            get => _dynamicMessage;
            set
            {
                _dynamicMessage = value;
                if (SuppliedTooltip is ActionAlertTooltip actionAlertTooltip)
                {
                    actionAlertTooltip.DynamicMessage = value;
                }
            }
        }

        private (TimeSpan Start, TimeSpan End)? _cooldown;
        private string? _dynamicMessage;

        private short? _severity;

        private readonly SpriteView _icon;
        private readonly CooldownGraphic _cooldownGraphic;
        private readonly Label _hudTitle;
        // CMU14: side indicators have no panel fill or frame.
        // private static readonly CmuHudStyleBox HudSurface = CmuHudStyle.Box(CmuHudTone.Inset);

        private EntityUid _spriteViewEntity;

        /// <summary>
        /// Creates an alert control reflecting the indicated alert + state
        /// </summary>
        /// <param name="alert">alert to display</param>
        /// <param name="severity">severity of alert, null if alert doesn't have severity levels</param>
        public AlertControl(AlertPrototype alert, short? severity)
        {
            // Alerts will handle this.
            MuteSounds = true;

            IoCManager.InjectDependencies(this);
            _sprite = _entityManager.System<SpriteSystem>();
            TooltipSupplier = SupplyTooltip;
            Alert = alert;

            HorizontalAlignment = HAlignment.Left;
            _severity = severity;
            _icon = new SpriteView
            {
                Scale = new Vector2(2, 2),
                MaxSize = new Vector2(64, 64),
                Stretch = SpriteView.StretchMode.None,
                HorizontalAlignment = HAlignment.Left
            };

            // CMU14: EnteredTree creates the icon with the latest severity when it becomes visible.
            // SetupIcon();

            Children.Add(_icon);
            _cooldownGraphic = new CooldownGraphic
            {
                MaxSize = new Vector2(64, 64)
            };
            Children.Add(_cooldownGraphic);
            _hudTitle = new Label
            {
                Text = FormattedMessage.RemoveMarkupPermissive(Loc.GetString(alert.Name)),
                ClipText = true,
                Margin = new Thickness(69, 0, 6, 0),
                VerticalAlignment = VAlignment.Center,
                StyleClasses = { CmuHudStyle.Body },

                MouseFilter = MouseFilterMode.Ignore,
            };
            AddChild(_hudTitle);
            SetHudCompact(false);
        }

        public void SetHudCompact(bool compact)
        {
            MinSize = new Vector2(compact ? 64 : 176, 64);
            _icon.Scale = new Vector2(2f);
            _icon.MaxSize = new Vector2(64);
            _icon.SetSize = new Vector2(64);
            _cooldownGraphic.MaxSize = new Vector2(64);
            _hudTitle.Visible = !compact;
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            // CMU14: leave the world visible behind alert icons and cooldowns.
            // HudSurface.Draw(handle, UIBox2.FromDimensions(Vector2.Zero, PixelSize), UIScale);
            base.Draw(handle);
        }

        private Control SupplyTooltip(Control? sender)
        {
            var msg = FormattedMessage.FromMarkupOrThrow(Loc.GetString(Alert.Name));
            var desc = FormattedMessage.FromMarkupOrThrow(Loc.GetString(Alert.Description));
            return new ActionAlertTooltip(msg, desc) { Cooldown = Cooldown, DynamicMessage = DynamicMessage };
        }

        /// <summary>
        /// Change the alert severity, changing the displayed icon
        /// </summary>
        public void SetSeverity(short? severity)
        {
            if (_severity == severity)
                return;
            _severity = severity;

            if (!_entityManager.TryGetComponent<SpriteComponent>(_spriteViewEntity, out var sprite))
                return;
            var icon = Alert.GetIcon(_severity);
            if (_sprite.LayerMapTryGet((_spriteViewEntity, sprite), AlertVisualLayers.Base, out var layer, false))
                _sprite.LayerSetSprite((_spriteViewEntity, sprite), layer, icon);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            CMUUpdateTrainingText(); // CMU14: name/distance of the owner-only teaching target.
            UserInterfaceManager.GetUIController<AlertsUIController>().UpdateAlertSpriteEntity(_spriteViewEntity, Alert);

            if (!Cooldown.HasValue)
            {
                _cooldownGraphic.Visible = false;
                _cooldownGraphic.Progress = 0;
                return;
            }

            _cooldownGraphic.FromTime(Cooldown.Value.Start, Cooldown.Value.End);
        }

        private void SetupIcon()
        {
            if (!_entityManager.Deleted(_spriteViewEntity))
                _entityManager.QueueDeleteEntity(_spriteViewEntity);

            _spriteViewEntity = _entityManager.Spawn(Alert.AlertViewEntity);
            if (_entityManager.TryGetComponent<SpriteComponent>(_spriteViewEntity, out var sprite))
            {
                var icon = Alert.GetIcon(_severity);
                if (_sprite.LayerMapTryGet((_spriteViewEntity, sprite), AlertVisualLayers.Base, out var layer, false))
                    _sprite.LayerSetSprite((_spriteViewEntity, sprite), layer, icon);
            }

            _icon.SetEntity(_spriteViewEntity);
        }

        protected override void EnteredTree()
        {
            base.EnteredTree();
            SetupIcon();
        }

        protected override void ExitedTree()
        {
            base.ExitedTree();

            if (!_entityManager.Deleted(_spriteViewEntity))
                _entityManager.QueueDeleteEntity(_spriteViewEntity);
        }

        [System.Obsolete]
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!_entityManager.Deleted(_spriteViewEntity))
                _entityManager.QueueDeleteEntity(_spriteViewEntity);
        }
    }

    public enum AlertVisualLayers : byte
    {
        Base
    }
}

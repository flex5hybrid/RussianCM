using System.Globalization;
using System.Linq;
using System.Numerics;
using Content.Client.CMU14.Interface.Hud;
using Content.Client.CMU14.UserInterface;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Screens;
using Content.Client.UserInterface.Systems.Actions.Controls;
using Content.Client.UserInterface.Systems.Actions.Widgets;
using Content.Client.UserInterface.Systems.Alerts.Widgets;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Content.Client.UserInterface.Systems.Ghost.Widgets;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Client.UserInterface.Systems.Hands.Controls;
using Content.Client.UserInterface.Systems.Hotbar.Widgets;
using Content.Client.UserInterface.Systems.Inventory.Controls;
using Content.Client.UserInterface.Systems.Inventory.Widgets;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Alert;
using Content.Shared.Chat;
using Content.Shared.CMU14.CCVar;
using Content.Shared.Hands.Components;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Input;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.CMU14.Hud;

[TestFixture, NonParallelizable]
public sealed class CmuHudWorkspaceTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };

    [TestCase(false)]
    [TestCase(true)]
    public async Task WheelInputReachesResizableAndSeparatedChat(bool separated)
    {
        await Client.WaitAssertion(() =>
        {
            var ui = Client.ResolveDependency<IUserInterfaceManager>();
            var update = ui.GetType().GetMethod("FrameUpdate")!.CreateDelegate<Action<FrameEventArgs>>(ui);
            var wheelInput = ui.GetType().GetMethod("MouseWheel")!.CreateDelegate<Action<MouseWheelEventArgs>>(ui);
            Client.ResolveDependency<IConfigurationManager>().SetCVar(Content.Shared.CCVar.CCVars.ChatLegacyMode, false);
            using var host = new LayoutContainer { SetSize = new Vector2(400, 420) };
            ChatBox chat = separated ? new ChatBox() : new ResizableChatBox();
            chat.MinSize = new Vector2(320, 260);
            chat.EnableHudWorkspace();
            host.AddChild(chat);
            LayoutContainer.SetAnchorPreset(chat, LayoutContainer.LayoutPreset.Wide);
            ui.WindowRoot.AddChild(host);
            try
            {
                var log = chat.Contents;
                for (var i = 0; i < 100; i++)
                {
                    var message = $"Message {i}: a wrapping transmission for wheel input.";
                    log.AddMessage(new ChatMessage(ChatChannel.Radio, message, "", default, null),
                        FormattedMessage.FromUnformatted(message), Color.White);
                }

                void Settle()
                {
                    for (var frame = 0; frame < 16; frame++)
                    {
                        update(new FrameEventArgs(1f / 60));
                        host.Measure(host.SetSize);
                        host.Arrange(UIBox2.FromDimensions(Vector2.Zero, host.SetSize));
                    }
                }

                Settle();
                var bar = Descendants(log).OfType<VScrollBar>().Single();
                var viewport = bar.Parent!.GetChild(0);
                var point = new ScreenCoordinates(viewport.GlobalPixelPosition + viewport.PixelSize / 2,
                    viewport.Root!.Window!.Id);
                var hit = ui.MouseGetControl(point);
                var hitsLog = false;
                for (var ancestor = hit; ancestor != null; ancestor = ancestor.Parent)
                    hitsLog |= ancestor == log;
                Assert.That(hitsLog, Is.True, "The wheel must be routed from the rendered chat, not invoked directly on its handler.");
                var before = bar.Value;
                wheelInput(new MouseWheelEventArgs(new Vector2(0, 1), point));
                Settle();
                Assert.That(bar.Value, Is.LessThan(before - 40));
                var after = bar.Value;
                Settle();
                Assert.That(bar.Value, Is.EqualTo(after).Within(0.1f), "Idle layout must not cancel wheel input.");
            }
            finally
            {
                host.Orphan();
            }
        });
    }

    [Test]
    public async Task DefaultChatMeasurementDoesNotCancelWheelOrScrollbarInput()
    {
        await Client.WaitAssertion(() =>
        {
            var ui = Client.ResolveDependency<IUserInterfaceManager>();
            var update = ui.GetType().GetMethod("FrameUpdate")!.CreateDelegate<Action<FrameEventArgs>>(ui);
            var wheelInput = ui.GetType().GetMethod("MouseWheel")!.CreateDelegate<Action<MouseWheelEventArgs>>(ui);
            Client.ResolveDependency<IConfigurationManager>().SetCVar(Content.Shared.CCVar.CCVars.ChatLegacyMode, false);
            using var host = new LayoutContainer { SetSize = new Vector2(1280, 900) };
            var screen = new DefaultGameScreen();
            host.AddChild(screen);
            LayoutContainer.SetAnchorPreset(screen, LayoutContainer.LayoutPreset.Wide);
            ui.WindowRoot.AddChild(host);
            try
            {
                var log = screen.ChatBox.Contents;
                for (var i = 0; i < 24; i++)
                {
                    var text = $"Radio {i}: A wrapping transmission that stays readable when the floating chat is narrow.";
                    log.AddMessage(new ChatMessage(ChatChannel.Radio, text, "", default, null),
                        FormattedMessage.FromUnformatted(text), Color.White);
                }

                void Settle()
                {
                    for (var frame = 0; frame < 16; frame++)
                    {
                        update(new FrameEventArgs(1f / 60));
                        host.Measure(host.SetSize);
                        host.Arrange(UIBox2.FromDimensions(Vector2.Zero, host.SetSize));
                    }
                }

                Settle();
                var bar = Descendants(log).OfType<VScrollBar>().Single();
                Assert.That(bar.MaxValue - bar.Page, Is.GreaterThan(100));
                bar.Value = (bar.MaxValue - bar.Page) / 2;
                Settle();
                var reader = bar.Value;
                var page = bar.Page;
                var maximum = bar.MaxValue;
                // A floating widget is measured with the screen's available space before it is
                // arranged into its anchored rectangle. Measurement cannot consume scroll input.
                log.InvalidateMeasure();
                log.Measure(new Vector2(1400, 1200));
                Assert.That(bar.Value, Is.EqualTo(reader).Within(0.1f));
                Assert.That(bar.Page, Is.EqualTo(page));
                Assert.That(bar.MaxValue, Is.EqualTo(maximum));
                Settle();
                Assert.That(bar.Value, Is.EqualTo(reader).Within(0.1f));

                var viewport = bar.Parent!.GetChild(0);
                var point = new ScreenCoordinates(viewport.GlobalPixelPosition + viewport.PixelSize / 2,
                    viewport.Root!.Window!.Id);
                var before = bar.Value;
                wheelInput(new MouseWheelEventArgs(new Vector2(0, 1), point));
                Settle();
                Assert.That(bar.Value, Is.LessThan(before - 40));
            }
            finally
            {
                host.Orphan();
                host.Release();
            }
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task SplitViewportCanShrinkBeforeItsResizeCallbacksRun(bool centered)
    {
        await Client.WaitAssertion(() =>
        {
            var cfg = Client.ResolveDependency<IConfigurationManager>();
            cfg.SetCVar(AU14CCVars.CenterSeparatedViewport, centered);
            using var screen = new SeparatedChatGameScreen();
            var size = new Vector2(1000, 700);
            screen.Measure(size);
            screen.Arrange(UIBox2.FromDimensions(Vector2.Zero, size));
            var viewport = screen.FindControl<LayoutContainer>("ViewportContainer");
            var hotbar = screen.GetWidget<HotbarGui>()!;
            var ghost = screen.GetWidget<GhostGui>()!;
            // SplitContainer arranges children at the new width before their OnResized events.
            // In particular, do not remeasure or run the coordinator before the narrower arrange.
            foreach (var width in new[] { 120f, 12f, 1f, 0f, 850f, 160f, 700f })
            {
                viewport.Arrange(UIBox2.FromDimensions(Vector2.Zero, new Vector2(width, 700)));
                Assert.That(hotbar.Width, Is.GreaterThanOrEqualTo(0));
                Assert.That(ghost.Width, Is.GreaterThanOrEqualTo(0));
            }
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task BothScreensKeepNativeControlsInsideTheViewport(bool separated)
    {
        await Client.WaitAssertion(() =>
        {
            var ui = Client.ResolveDependency<IUserInterfaceManager>();
            var update = ui.GetType().GetMethod("FrameUpdate")!.CreateDelegate<Action<FrameEventArgs>>(ui);
            var cfg = Client.ResolveDependency<IConfigurationManager>();
            Client.ResolveDependency<ILocalizationManager>().SetCulture(CultureInfo.GetCultureInfo("ru-RU"));
            cfg.SetCVar(AU14CCVars.SeparatedHudStatusSide, "left");
            var host = new LayoutContainer();
            InGameScreen screen = separated ? new SeparatedChatGameScreen() : new DefaultGameScreen();
            host.AddChild(screen);
            LayoutContainer.SetAnchorPreset(screen, LayoutContainer.LayoutPreset.Wide);
            ui.WindowRoot.AddChild(host);
            var inventory = screen.GetWidget<InventoryGui>()!;
            var hotbar = screen.GetWidget<HotbarGui>()!;
            var actions = screen.GetWidget<ActionsBar>()!;
            screen.GetWidget<GhostGui>()!.Visible = false;
            foreach (var name in new[] { "glasses", "head", "ears", "neck", "mask", "uniform", "suit", "gloves", "shoes", "ears2" })
            {
                var texture = name == "ears2" ? "ears" : name;
                inventory.InventoryHotbar.TryAddButton(new SlotButton { SlotName = name, ButtonTexturePath = "Slots/" + texture });
            }
            foreach (var name in new[] { "id", "belt", "back" })
                hotbar.FindControl<ItemSlotButtonContainer>("SecondHotbar").TryAddButton(new SlotButton { SlotName = name, ButtonTexturePath = "Slots/" + name });
            foreach (var name in new[] { "pocket1", "pocket2", "suit_storage" })
                hotbar.FindControl<ItemSlotButtonContainer>("MainHotbar").TryAddButton(new SlotButton { SlotName = name, ButtonTexturePath = "Slots/" + (name.StartsWith("pocket") ? "pocket" : name) });
            hotbar.HandContainer.TryAddButton(new HandButton("right", HandLocation.Right));
            hotbar.HandContainer.TryAddButton(new HandButton("left", HandLocation.Left));
            for (var index = 0; index < 4; index++)
                actions.ActionsContainer.AddChild(new ActionButton(CEntMan) { MinSize = new Vector2(64) });
            inventory.InventoryButton.Visible = true; // Simulate a controlled entity with equipment slots.
            inventory.CMUSetEquipmentVisible(true);
            var alerts = screen.GetWidget<AlertsUI>()!;
            var states = new Dictionary<AlertKey, AlertState>();
            foreach (var id in new[] { "HumanHealth", "Stamina", "LowOxygen", "Fire", "Bleed", "AreaInfo" })
            {
                var prototype = CProtoMan.Index<AlertPrototype>(id);
                states[prototype.AlertKey] = new AlertState { Type = id, Severity = prototype.MinSeverity };
            }
            alerts.SyncControls(Client.System<AlertsSystem>(), null, states);
            Assert.That(alerts.AlertContainer.Children.OfType<Content.Client.UserInterface.Systems.Alerts.Controls.AlertControl>()
                .Single(control => control.Alert.ID == "AreaInfo").Visible, Is.True, "Location is essential HUD information.");
            Assert.That(inventory.FindControl<CmuHudPanel>("EquipmentPanel").RasterEnabled, Is.False,
                "Equipment must retain native sprites and hit regions without a CRT capture.");

            try
            {
                foreach (var (pixels, scale) in new[]
                {
                    (new Vector2(1280,720),1f), (new Vector2(1280,720),1.25f),
                    (new Vector2(1920,1080),1f), (new Vector2(1920,1080),1.5f),
                    (new Vector2(2560,1440),1.5f), (new Vector2(2560,1440),2f),
                })
                {
                    cfg.SetCVar(CVars.DisplayUIScale, scale);
                    var logical = pixels / scale;
                    host.SetSize = logical;
                    screen.SetSize = logical;
                    for (var frame = 0; frame < 16; frame++)
                    {
                        update(new FrameEventArgs(1f/60));
                        host.Measure(logical);
                        host.Arrange(UIBox2.FromDimensions(Vector2.Zero, logical));
                    }
                    var viewport = screen.FindControl<LayoutContainer>("ViewportContainer");
                    var bounds = viewport.GlobalRect;
                    var dock = hotbar.FindControl<CmuHudDock>("Hotbar");
                    AssertInside(dock, bounds, pixels, scale);
                    AssertInside(inventory.FindControl<PanelContainer>("EquipmentPanel"), bounds, pixels, scale);
                    AssertInside(alerts.FindControl<Control>("StatusPanel"), bounds, pixels, scale);
                    if (!separated)
                        Assert.That(alerts.GlobalRect.Right, Is.LessThan(bounds.Left + bounds.Width / 2),
                            "Default HUD keeps health and status indicators on the left.");
                    Assert.That(inventory.FindControl<PanelContainer>("EquipmentPanel").GlobalRect.Bottom,
                        Is.LessThanOrEqualTo(inventory.InventoryButton.GlobalRect.Top),
                        "The equipment drawer stays above its fixed lower-left toggle.");
                    foreach (var button in hotbar.HandContainer.GetButtons())
                        AssertInside(button, bounds, pixels, scale);
                    Assert.That(inventory.InventoryHotbar.ChildCount, Is.EqualTo(10));
                    Assert.That(screen.ChatBox.HudWorkspaceEnabled, Is.True);
                    Assert.That(screen.ChatBox.ChatInput.Input.VisibleInTree, Is.True);
                }
            }
            finally
            {
                host.Orphan();
                host.Release();
            }
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task GameplayLoadKeepsDynamicallyAddedLanguageButtonInMenuGrid(bool separated)
    {
        await Client.WaitAssertion(() =>
        {
            var ui = Client.ResolveDependency<IUserInterfaceManager>();
            if (separated)
                ui.LoadScreen<SeparatedChatGameScreen>();
            else
                ui.LoadScreen<DefaultGameScreen>();
            var loader = ui.GetUIController<GameplayStateLoadController>();
            try
            {
                loader.LoadScreen();
                var menu = ui.GetActiveUIWidget<GameTopMenuBar>();
                var grid = menu.FindControl<GridContainer>("MenuGrid");
                Assert.That(grid.ChildCount, Is.EqualTo(10), "The runtime language button must join all nine stock menu buttons.");
                Assert.That(grid.Children.OfType<MenuButton>().Count(), Is.EqualTo(10));
                Assert.That(menu.ChildCount, Is.EqualTo(1), "Dynamic menu controls must use the adaptive grid.");
            }
            finally
            {
                loader.UnloadScreen();
                ui.UnloadScreen();
            }
        });
    }

    [Test]
    public async Task ChannelChangesKeepDraftsAndNativeSearchControls()
    {
        await Client.WaitAssertion(() =>
        {
            var chat = new ChatBox();
            try
            {
                chat.EnableHudWorkspace();
                chat.ChatInput.ChannelSelector.Select(ChatSelectChannel.Local);
                chat.ChatInput.Input.Text = "Длинное сообщение для ролевого взаимодействия";
                chat.ChatInput.ChannelSelector.Select(ChatSelectChannel.Emotes);
                Assert.That(chat.ChatInput.Input.Text, Is.Empty);
                chat.ChatInput.Input.Text = "salute";
                chat.ChatInput.ChannelSelector.Select(ChatSelectChannel.Local);
                Assert.That(chat.ChatInput.Input.Text, Is.EqualTo("Длинное сообщение для ролевого взаимодействия"));
                chat.ChatInput.ChannelSelector.Select(ChatSelectChannel.Emotes);
                Assert.That(chat.ChatInput.Input.Text, Is.EqualTo("salute"));
                Assert.That(chat.ChatInput.SearchButton.Parent!.Parent!.Parent, Is.SameAs(chat.ChatInput));
                Assert.That(chat.ChatInput.SearchInput.Parent!.Parent, Is.SameAs(chat.ChatInput));
            }
            finally{chat.Release();}
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task SavedHudPlacementMovesItsHitRegionAndCanBeReset(bool separated)
    {
        await Client.WaitAssertion(() =>
        {
            var ui = Client.ResolveDependency<IUserInterfaceManager>();
            var cfg = Client.ResolveDependency<IConfigurationManager>();
            var previous = cfg.GetCVar(Content.Shared.CCVar.CCVars.CMUHudLayout);
            cfg.SetCVar(Content.Shared.CCVar.CCVars.CMUHudLayout, "");
            if (separated) ui.LoadScreen<SeparatedChatGameScreen>();
            else ui.LoadScreen<DefaultGameScreen>();
            try
            {
                var screen = (InGameScreen) ui.ActiveScreen!;
                var controller = ui.GetUIController<CmuHudLayoutController>();
                var size = new Vector2(1280, 900);
                void Arrange()
                {
                    screen.InvalidateMeasure();
                    screen.Measure(size);
                    screen.Arrange(UIBox2.FromDimensions(Vector2.Zero, size));
                }
                Arrange();
                var entry = controller.GetEntries().Single(e => e.Id == "menu");
                var parent = entry.Control.Parent;
                var natural = entry.Control.GlobalPosition;
                controller.Place(entry, new Vector2(180, 210), entry.Control.Size);
                Arrange();
                Assert.That(entry.Control.Parent, Is.SameAs(parent), "Editing must preserve native ownership.");
                Assert.That(entry.Control.GlobalPosition.X, Is.EqualTo(180).Within(2));
                Assert.That(entry.Control.GlobalPosition.Y, Is.EqualTo(210).Within(2));
                var button = Descendants(entry.Control).OfType<MenuButton>().First(b => b.VisibleInTree);
                var point = new ScreenCoordinates(button.GlobalPixelRect.Center, button.Root!.Window!.Id);
                var hit = ui.MouseGetControl(point);
                Assert.That(hit?.GetSelfAndLogicalAncestors().Contains(button), Is.True,
                    "The moved button must remain clickable at its visible position.");
                controller.Reset(entry.Id);
                Arrange();
                Assert.That(entry.Control.GlobalPosition.X, Is.EqualTo(natural.X).Within(2));
                Assert.That(entry.Control.GlobalPosition.Y, Is.EqualTo(natural.Y).Within(2));
            }
            finally
            {
                cfg.SetCVar(Content.Shared.CCVar.CCVars.CMUHudLayout, previous);
                ui.UnloadScreen();
            }
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ResizingGhostAreaReflowsButtonsAndKeepsThemInsideTheFrame(bool separated)
    {
        await Client.WaitAssertion(() =>
        {
            var ui = Client.ResolveDependency<IUserInterfaceManager>();
            var cfg = Client.ResolveDependency<IConfigurationManager>();
            var previous = cfg.GetCVar(Content.Shared.CCVar.CCVars.CMUHudLayout);
            var update = ui.GetType().GetMethod("FrameUpdate")!.CreateDelegate<Action<FrameEventArgs>>(ui);
            cfg.SetCVar(Content.Shared.CCVar.CCVars.CMUHudLayout, "");
            if (separated) ui.LoadScreen<SeparatedChatGameScreen>();
            else ui.LoadScreen<DefaultGameScreen>();
            void Settle()
            {
                for (var frame = 0; frame < 8; frame++)
                    update(new FrameEventArgs(1f / 60));
            }
            try
            {
                var screen = (InGameScreen) ui.ActiveScreen!;
                var controller = ui.GetUIController<CmuHudLayoutController>();
                var entry = controller.GetEntries().Single(e => e.Id == "ghost");
                var ghost = (GhostGui) entry.Control;
                ghost.Visible = true;
                ghost.Update(12, true);
                var parent = ghost.Parent;
                var buttons = Descendants(ghost).OfType<Button>().ToArray();
                var labels = new[] { "Вернуться", "Телепорт", "Роли (12)", "Возродиться" };
                for (var i = 0; i < buttons.Length; i++)
                    buttons[i].Text = labels[i];
                Settle();
                var nativeWidth = ghost.Width;
                float WideWidth() => buttons.Sum(b => b.DesiredSize.X) + 4 * (buttons.Length - 1) + 8;
                void Resize(float width)
                {
                    controller.Place(entry, new Vector2(240, 160), new Vector2(width, 24), resize: true);
                    Settle();
                    Assert.That(ghost.Parent, Is.SameAs(parent));
                    foreach (var button in buttons)
                    {
                        var bounds = button.GlobalRect;
                        Assert.That(bounds.Left, Is.GreaterThanOrEqualTo(ghost.GlobalRect.Left - 1));
                        Assert.That(bounds.Right, Is.LessThanOrEqualTo(ghost.GlobalRect.Right + 1));
                        Assert.That(bounds.Top, Is.GreaterThanOrEqualTo(ghost.GlobalRect.Top - 1));
                        Assert.That(bounds.Bottom, Is.LessThanOrEqualTo(ghost.GlobalRect.Bottom + 1));
                    }
                }
                Resize(WideWidth());
                Assert.That(buttons.Select(b => b.GlobalPosition.Y).Distinct().Count(), Is.EqualTo(1));
                Resize(buttons.Max(b => b.DesiredSize.X) + 4);
                Assert.That(buttons.Select(b => b.GlobalPosition.Y).Distinct().Count(), Is.GreaterThan(1));
                Resize(16); // A frame narrower than a full label must grow to keep that button inside.
                Assert.That(ghost.Width, Is.GreaterThanOrEqualTo(ghost.GetMinimumLayoutWidth() - 1));
                buttons[2].Text = "Роли призраков (12345)";
                Settle();
                Resize(16);
                Resize(WideWidth());
                Assert.That(buttons.Select(b => b.GlobalPosition.Y).Distinct().Count(), Is.EqualTo(1));
                var pixel = new ScreenCoordinates(buttons[0].GlobalPixelRect.Center, buttons[0].Root!.Window!.Id);
                Assert.That(ui.MouseGetControl(pixel)?.GetSelfAndLogicalAncestors().Contains(buttons[0]), Is.True);
                controller.Reset("ghost");
                Settle();
                Assert.That(ghost.Width, Is.EqualTo(nativeWidth).Within(2),
                    "Reset must clear the edited wrapping width.");
            }
            finally
            {
                cfg.SetCVar(Content.Shared.CCVar.CCVars.CMUHudLayout, previous);
                ui.UnloadScreen();
            }
        });
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task HudPlacementSurvivesActionChangesAndEquipmentToggles(bool separated, bool saved)
    {
        await Client.WaitAssertion(() =>
        {
            var ui = Client.ResolveDependency<IUserInterfaceManager>();
            var cfg = Client.ResolveDependency<IConfigurationManager>();
            var previous = cfg.GetCVar(Content.Shared.CCVar.CCVars.CMUHudLayout);
            var update = ui.GetType().GetMethod("FrameUpdate")!.CreateDelegate<Action<FrameEventArgs>>(ui);
            cfg.SetCVar(Content.Shared.CCVar.CCVars.CMUHudLayout, "");
            if (separated) ui.LoadScreen<SeparatedChatGameScreen>();
            else ui.LoadScreen<DefaultGameScreen>();
            CmuHudLayoutEditor? editor = null;
            void Settle()
            {
                for (var frame = 0; frame < 8; frame++)
                    update(new FrameEventArgs(1f / 60));
            }
            try
            {
                var screen = (InGameScreen) ui.ActiveScreen!;
                var controller = ui.GetUIController<CmuHudLayoutController>();
                var actions = Descendants(screen).OfType<ActionsBar>().Single();
                var inventory = Descendants(screen).OfType<InventoryGui>().Single();
                inventory.InventoryButton.Visible = true;
                for (var i = 0; i < 7; i++)
                    actions.ActionsContainer.AddChild(new ActionButton(Client.ResolveDependency<IEntityManager>()));
                var equipment = Descendants(inventory).Single(c => c.Name == "EquipmentPanel");
                equipment.AddChild(new Control { MinSize = new Vector2(190, 260) });
                inventory.CMUSetEquipmentVisible(false);
                Settle();
                var nativeMenuPosition = controller.GetEntries().Single(e => e.Id == "menu").Control.GlobalPosition;
                var settings = new CmuHudLayoutSettings();
                var positions = new Dictionary<string, Vector2>
                {
                    { "menu", new Vector2(0.25f, 0.2f) },
                    { "actions", new Vector2(0.15f, 0.4f) },
                    { "InventoryButton", new Vector2(0.08f, 0.75f) },
                    { "action/0", new Vector2(0.35f, 0.55f) },
                };
                if (!saved)
                {
                    controller.OpenEditor();
                    editor = ui.WindowRoot.Children.OfType<CmuHudLayoutEditor>().Single();
                }
                foreach (var (id, fraction) in positions)
                {
                    var entry = controller.GetEntries().Single(e => e.Id == id);
                    settings.Items[controller.Profile + id] = new CmuHudLayoutSettings.Placement(fraction.X, fraction.Y, 0, 0);
                    if (!saved)
                        controller.Place(entry, Vector2.Max(Vector2.Zero, screen.Size - entry.Control.Size) * fraction,
                            entry.Control.Size);
                }
                if (saved)
                    cfg.SetCVar(Content.Shared.CCVar.CCVars.CMUHudLayout, settings.Serialize());
                void AssertPlacements()
                {
                    foreach (var id in positions.Keys)
                    {
                        var control = controller.GetEntries().Single(e => e.Id == id).Control;
                        var expected = CmuHudLayoutSettings.Resolve(settings.Items[controller.Profile + id], screen.Size, control.Size);
                        var actual = control.GlobalPosition - screen.GlobalPosition;
                        Assert.That(actual.X, Is.EqualTo(expected.Left).Within(2), id + " X after a native subtree layout");
                        Assert.That(actual.Y, Is.EqualTo(expected.Top).Within(2), id + " Y after a native subtree layout");
                    }
                }
                Settle();
                AssertPlacements();
                var added = new ActionButton(Client.ResolveDependency<IEntityManager>());
                actions.ActionsContainer.AddChild(added);
                Settle();
                AssertPlacements();
                Assert.That(controller.GetEntries().Any(e => e.Control == added), Is.True);
                inventory.CMUSetEquipmentVisible(true);
                Settle();
                AssertPlacements();
                added.Orphan();
                inventory.CMUSetEquipmentVisible(false);
                Settle();
                AssertPlacements();
                Assert.That(controller.GetEntries().Any(e => e.Control == added), Is.False);
                Settle();
                AssertPlacements(); // Idle frames must not overwrite the native bounds used by reset/cancel.
                if (saved)
                    controller.Reset();
                else
                    editor!.Close();
                Settle();
                var menuPosition = controller.GetEntries().Single(e => e.Id == "menu").Control.GlobalPosition;
                Assert.That(menuPosition.X, Is.EqualTo(nativeMenuPosition.X).Within(2));
                Assert.That(menuPosition.Y, Is.EqualTo(nativeMenuPosition.Y).Within(2));
            }
            finally
            {
                editor?.Close();
                cfg.SetCVar(Content.Shared.CCVar.CCVars.CMUHudLayout, previous);
                ui.UnloadScreen();
            }
        });
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task EditorDragsThroughMouseRoutingRegardlessOfUseBindingOrder(bool separated, bool useFirst)
    {
        await Client.WaitAssertion(() =>
        {
            var ui = Client.ResolveDependency<IUserInterfaceManager>();
            var cfg = Client.ResolveDependency<IConfigurationManager>();
            var previous = cfg.GetCVar(Content.Shared.CCVar.CCVars.CMUHudLayout);
            var update = ui.GetType().GetMethod("FrameUpdate")!.CreateDelegate<Action<FrameEventArgs>>(ui);
            var down = ui.GetType().GetMethod("KeyBindDown")!.CreateDelegate<Action<BoundKeyEventArgs>>(ui);
            var up = ui.GetType().GetMethod("KeyBindUp")!.CreateDelegate<Action<BoundKeyEventArgs>>(ui);
            var move = ui.GetType().GetMethod("MouseMove")!.CreateDelegate<Action<MouseMoveEventArgs>>(ui);
            cfg.SetCVar(Content.Shared.CCVar.CCVars.CMUHudLayout, "");
            if (separated) ui.LoadScreen<SeparatedChatGameScreen>();
            else ui.LoadScreen<DefaultGameScreen>();
            CmuHudLayoutEditor? editor = null;
            void Settle()
            {
                for (var frame = 0; frame < 8; frame++)
                    update(new FrameEventArgs(1f / 60));
            }
            try
            {
                Settle();
                var controller = ui.GetUIController<CmuHudLayoutController>();
                var entry = controller.GetEntries().Single(e => e.Id == "menu");
                var natural = entry.Control.GlobalPosition;
                controller.OpenEditor();
                Settle();
                editor = ui.WindowRoot.Children.OfType<CmuHudLayoutEditor>().Single();
                var overlay = ui.WindowRoot.Children.Single(c => c.Name == "CmuHudEditOverlay");
                var start = new ScreenCoordinates((Vector2) entry.Control.GlobalPixelPosition
                    + new Vector2(4) * entry.Control.UIScale, entry.Control.Root!.Window!.Id);
                Assert.That(ui.MouseGetControl(start), Is.SameAs(overlay), "The visible edit layer must own the hit region.");
                ui.ControlFocused = overlay;
                var use = new BoundKeyEventArgs(EngineKeyFunctions.Use, BoundKeyState.Down, start, true);
                var click = new BoundKeyEventArgs(EngineKeyFunctions.UIClick, BoundKeyState.Down, start, true);
                if (useFirst)
                {
                    down(use);
                    Assert.That(use.Handled, Is.False, "Use must not suppress the following UIClick binding.");
                }
                down(click);
                Assert.That(click.Handled, Is.True);
                var delta = new Vector2(176, 160) * entry.Control.UIScale;
                var end = new ScreenCoordinates(start.Position + delta, start.Window);
                move(new MouseMoveEventArgs(delta, end));
                Settle(); // Do not force screen layout: the editor must invalidate it itself.
                Assert.That(entry.Control.GlobalPosition.X, Is.GreaterThan(natural.X + 100));
                Assert.That(entry.Control.GlobalPosition.Y, Is.GreaterThan(natural.Y + 100));
                Assert.That(ui.ControlFocused, Is.SameAs(overlay), "Dragging must continue over other windows.");
                up(new BoundKeyEventArgs(EngineKeyFunctions.UIClick, BoundKeyState.Up, end, true));
                Assert.That(ui.ControlFocused, Is.Null);
                editor.Close();
                Settle();
                Assert.That(entry.Control.GlobalPosition.X, Is.EqualTo(natural.X).Within(2));
                Assert.That(entry.Control.GlobalPosition.Y, Is.EqualTo(natural.Y).Within(2));
                Assert.That(overlay.Parent, Is.Null);
            }
            finally
            {
                editor?.Close();
                ui.ControlFocused = null;
                cfg.SetCVar(Content.Shared.CCVar.CCVars.CMUHudLayout, previous);
                ui.UnloadScreen();
            }
        });
    }

    private static void AssertInside(Control control, UIBox2 bounds, Vector2 resolution, float scale)
    {
        Assert.That(control.Width, Is.GreaterThan(0), $"{control.Name} has no width at {resolution}/{scale}");
        Assert.That(control.GlobalRect.Left, Is.GreaterThanOrEqualTo(bounds.Left-1), $"{control.Name} left at {resolution}/{scale}");
        Assert.That(control.GlobalRect.Right, Is.LessThanOrEqualTo(bounds.Right+1), $"{control.Name} right at {resolution}/{scale}");
        Assert.That(control.GlobalRect.Top, Is.GreaterThanOrEqualTo(bounds.Top-1), $"{control.Name} top at {resolution}/{scale}");
        Assert.That(control.GlobalRect.Bottom, Is.LessThanOrEqualTo(bounds.Bottom+1), $"{control.Name} bottom at {resolution}/{scale}");
    }

    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (var child in parent.Children)
        {
            yield return child;
            foreach (var descendant in Descendants(child))
                yield return descendant;
        }
    }
}

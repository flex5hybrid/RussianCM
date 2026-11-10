using System.Linq;
using System.Numerics;
using Content.Client.CMU14.Interface.Hud;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Screens;
using Content.Client.UserInterface.Systems.Actions.Controls;
using Content.Client.UserInterface.Systems.Actions.Widgets;
using Content.Client.UserInterface.Systems.Alerts.Widgets;
using Content.Client.UserInterface.Systems.Ghost.Widgets;
using Content.Client.UserInterface.Systems.Hands.Controls;
using Content.Client.UserInterface.Systems.Hotbar.Widgets;
using Content.Client.UserInterface.Systems.Inventory.Controls;
using Content.Client.UserInterface.Systems.Inventory.Widgets;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Alert;
using Content.Shared.Hands.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.CMU14.Hud;

[TestFixture, NonParallelizable]
public sealed class CmuHudFamilyReflowTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task EveryButtonFamilyReflowsAtItsEditedWidthAndRestoresNativeConstraints(bool separated, bool crt)
    {
        await Client.WaitAssertion(() =>
        {
            var ui = Client.ResolveDependency<IUserInterfaceManager>();
            var cfg = Client.ResolveDependency<IConfigurationManager>();
            var previous = cfg.GetCVar(Content.Shared.CCVar.CCVars.CMUHudLayout);
            var previousCrt = cfg.GetCVar(Content.Shared.CCVar.CCVars.CrtUiEnabled);
            cfg.SetCVar(Content.Shared.CCVar.CCVars.CMUHudLayout, "");
            cfg.SetCVar(Content.Shared.CCVar.CCVars.CrtUiEnabled, crt);
            if (separated) ui.LoadScreen<SeparatedChatGameScreen>();
            else ui.LoadScreen<DefaultGameScreen>();
            var update = ui.GetType().GetMethod("FrameUpdate")!.CreateDelegate<Action<FrameEventArgs>>(ui);
            void Settle()
            {
                for (var frame = 0; frame < 8; frame++)
                    update(new FrameEventArgs(1f / 60));
            }
            try
            {
                var screen = (InGameScreen) ui.ActiveScreen!;
                var controller = ui.GetUIController<CmuHudLayoutController>();
                var actions = screen.GetWidget<ActionsBar>()!;
                var hotbar = screen.GetWidget<HotbarGui>()!;
                var inventory = screen.GetWidget<InventoryGui>()!;
                for (var i = 0; i < 12; i++)
                    actions.ActionsContainer.AddChild(new ActionButton(CEntMan) { MinSize = new Vector2(64) });
                foreach (var name in new[] { "id", "belt", "back" })
                    hotbar.FindControl<ItemSlotButtonContainer>("SecondHotbar").TryAddButton(
                        new SlotButton { SlotName = name, ButtonTexturePath = "Slots/" + name });
                foreach (var name in new[] { "pocket1", "pocket2", "suit_storage" })
                    hotbar.FindControl<ItemSlotButtonContainer>("MainHotbar").TryAddButton(
                        new SlotButton { SlotName = name, ButtonTexturePath = "Slots/" + (name.StartsWith("pocket") ? "pocket" : name) });
                hotbar.HandContainer.TryAddButton(new HandButton("right", HandLocation.Right));
                hotbar.HandContainer.TryAddButton(new HandButton("left", HandLocation.Left));
                foreach (var name in new[] { "glasses", "head", "ears", "neck", "mask", "uniform", "suit", "gloves", "shoes" })
                    inventory.InventoryHotbar.TryAddButton(new SlotButton { SlotName = name, ButtonTexturePath = "Slots/" + name });
                inventory.InventoryButton.Visible = true;
                inventory.CMUSetEquipmentVisible(true);
                var alerts = screen.GetWidget<AlertsUI>()!;
                var states = new Dictionary<AlertKey, AlertState>();
                foreach (var id in new[] { "HumanHealth", "Stamina", "LowOxygen", "Fire", "Bleed", "AreaInfo" })
                {
                    var prototype = CProtoMan.Index<AlertPrototype>(id);
                    states[prototype.AlertKey] = new AlertState { Type = id, Severity = prototype.MinSeverity };
                }
                alerts.SyncControls(Client.System<AlertsSystem>(), null, states);
                var ghost = screen.GetWidget<GhostGui>()!;
                ghost.Visible = true;
                ghost.Update(12, true);
                for (var i = 0; i < 3; i++)
                    hotbar.SingleStorageContainer.AddChild(new PanelContainer { MinSize = new Vector2(96, 64) });
                var left = hotbar.FindControl<BoxContainer>("LeftStorageContainer");
                var right = hotbar.FindControl<BoxContainer>("RightStorageContainer");
                for (var i = 0; i < 2; i++)
                {
                    left.AddChild(new PanelContainer { MinSize = new Vector2(96, 64) });
                    right.AddChild(new PanelContainer { MinSize = new Vector2(96, 64) });
                }
                var vote = controller.GetEntries().Single(e => e.Id == "VoteMenu").Control;
                for (var i = 0; i < 3; i++)
                    vote.AddChild(new Button { Text = "Vote " + i, MinSize = new Vector2(96, 32) });
                Settle();
                var families = new List<(string Id, Control[] Items)>
                {
                    ("actions", actions.ActionsContainer.Children.ToArray()),
                    ("menu", Descendants(controller.GetEntries().Single(e => e.Id == "menu").Control).OfType<MenuButton>().Where(b => b.VisibleInTree).Cast<Control>().ToArray()),
                    ("status", alerts.AlertContainer.Children.ToArray()),
                    ("ghost", Descendants(ghost).OfType<Button>().Cast<Control>().ToArray()),
                    ("MainHotbar", hotbar.FindControl<ItemSlotButtonContainer>("MainHotbar").Children.ToArray()),
                    ("SecondHotbar", hotbar.FindControl<ItemSlotButtonContainer>("SecondHotbar").Children.ToArray()),
                    ("HandContainer", hotbar.HandContainer.GetButtons().Cast<Control>().ToArray()),
                    ("EquipmentPanel", inventory.InventoryHotbar.Children.ToArray()),
                    ("SingleStorageContainer", hotbar.SingleStorageContainer.Children.ToArray()),
                    ("DoubleStorageContainer", left.Children.Concat(right.Children).ToArray()),
                    ("VoteMenu", vote.Children.ToArray()),
                };
                foreach (var (id, items) in families)
                {
                    var entry = controller.GetEntries().Single(e => e.Id == id);
                    var root = entry.Control;
                    var parents = items.Select(c => c.Parent).ToArray();
                    var oldSetSize = root.SetSize;
                    var oldMaxSize = root.MaxSize;
                    var oldHorizontal = root.HorizontalAlignment;
                    foreach (var item in items)
                        item.Measure(new Vector2(float.PositiveInfinity));
                    var wide = MathF.Min(screen.Width - 24, items.Sum(c => c.DesiredSize.X) + 4 * items.Length + 64);
                    var narrow = items.Max(c => c.DesiredSize.X) + 16;
                    if (id == "actions")
                        narrow = 2 * items.Max(c => c.DesiredSize.X) + 20;
                    void Resize(float width)
                    {
                        controller.Place(entry, new Vector2(240, 200), new Vector2(width, 96), true);
                        Settle();
                        Assert.That(CmuHudReflow.IsRoot(root), Is.True, id);
                        for (var i = 0; i < items.Length; i++)
                        {
                            Assert.That(items[i].Parent, Is.SameAs(parents[i]), id + " ownership");
                            var bounds = items[i].GlobalRect;
                            Assert.That(bounds.Left, Is.GreaterThanOrEqualTo(root.GlobalRect.Left - 2), id + " left");
                            Assert.That(bounds.Right, Is.LessThanOrEqualTo(root.GlobalRect.Right + 2), id + " right");
                            Assert.That(bounds.Top, Is.GreaterThanOrEqualTo(root.GlobalRect.Top - 2), id + " top");
                            Assert.That(bounds.Bottom, Is.LessThanOrEqualTo(root.GlobalRect.Bottom + 2), id + " bottom");
                        }
                    }
                    Resize(wide);
                    var wideRows = items.Select(c => c.GlobalPosition.Y).Distinct().Count();
                    if (id == "actions" && screen.Width >= 590 && screen.Height >= 132)
                    {
                        Resize(590);
                        Assert.That(items.Select(c => c.GlobalPosition.Y).Distinct().Count(), Is.EqualTo(2),
                            "The reported 590px action frame must use two rows, not the six native rows.");
                    }
                    Resize(narrow);
                    Assert.That(items.Select(c => c.GlobalPosition.Y).Distinct().Count(), Is.GreaterThan(wideRows), id + " narrow");
                    var moved = new Vector2(160, 80);
                    controller.Place(entry, moved, root.Size);
                    Settle();
                    var expectedPosition = Vector2.Min(moved, Vector2.Max(Vector2.Zero, screen.Size - root.Size));
                    var actualPosition = root.GlobalPosition - screen.GlobalPosition;
                    Assert.That(actualPosition.X, Is.EqualTo(expectedPosition.X).Within(2), id + " move X");
                    Assert.That(actualPosition.Y, Is.EqualTo(expectedPosition.Y).Within(2), id + " move Y");
                    if (id == "actions")
                    {
                        var point = new ScreenCoordinates(items[0].GlobalPixelRect.Center, items[0].Root!.Window!.Id);
                        Assert.That(ui.MouseGetControl(point)?.GetSelfAndLogicalAncestors().Contains(items[0]), Is.True,
                            "Action hit regions must follow the wrapped icons.");
                        var added = new ActionButton(CEntMan) { MinSize = new Vector2(64) };
                        actions.ActionsContainer.AddChild(added);
                        Settle();
                        Assert.That(added.GlobalRect.Right, Is.LessThanOrEqualTo(root.GlobalRect.Right + 2));
                        Assert.That(added.GlobalRect.Bottom, Is.LessThanOrEqualTo(root.GlobalRect.Bottom + 2));
                        added.Orphan();
                        Settle();
                    }
                    Resize(wide);
                    Assert.That(items.Select(c => c.GlobalPosition.Y).Distinct().Count(), Is.EqualTo(wideRows), id + " wider again");
                    controller.Reset(id);
                    Settle();
                    Assert.That(CmuHudReflow.IsRoot(root), Is.False, id + " reset");
                    Assert.That(root.SetWidth.Equals(oldSetSize.X), Is.True, id + " original width");
                    Assert.That(root.SetHeight.Equals(oldSetSize.Y), Is.True, id + " original height");
                    Assert.That(root.MaxSize, Is.EqualTo(oldMaxSize), id + " original maximum");
                    Assert.That(root.HorizontalAlignment, Is.EqualTo(oldHorizontal), id + " original alignment");
                }
            }
            finally
            {
                cfg.SetCVar(Content.Shared.CCVar.CCVars.CMUHudLayout, previous);
                cfg.SetCVar(Content.Shared.CCVar.CCVars.CrtUiEnabled, previousCrt);
                ui.UnloadScreen();
            }
        });
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (var child in root.Children)
        {
            yield return child;
            foreach (var descendant in Descendants(child))
                yield return descendant;
        }
    }
}

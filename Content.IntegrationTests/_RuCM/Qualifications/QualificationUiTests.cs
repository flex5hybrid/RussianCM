using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Content.Client._RuCM.Qualifications;
using Content.IntegrationTests.Fixtures;
using Content.Shared._RuCM.Qualifications;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.IntegrationTests._RuCM.Qualifications;

[TestFixture, NonParallelizable]
public sealed class QualificationUiTests : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };

    [TestCase("ru-RU", 760, 550)]
    [TestCase("ru-RU", 1080, 740)]
    [TestCase("en-US", 760, 550)]
    public async Task AllAuthorizedPagesFitNarrowViewportAndWritesRequireExplicitConfirmation(string culture, int width, int height)
    {
        TestContext.Progress.WriteLine($"UI case: {culture} {width}x{height}");
        await Client.WaitAssertion(() =>
        {
            TestContext.Progress.WriteLine("UI client assertion started");
            var loc = Client.ResolveDependency<ILocalizationManager>();
            loc.DefaultCulture = CultureInfo.GetCultureInfo(culture, predefinedOnly: false);
            TestContext.Progress.WriteLine("UI culture loaded");
            var view = QualificationPreviewCommand.CreateView(Client.ResolveDependency<IPrototypeManager>());
            var sent = new List<(QualificationAction Action, QualificationRequest Request)>();
            using var window = new QualificationWindow((a, r) => sent.Add((a, r)));
            var ui = Client.ResolveDependency<IUserInterfaceManager>();
            window.Open();
            var size = new Vector2(width, height);
            window.SetSize = size;
            window.Update(view);
            TestContext.Progress.WriteLine("UI window built");
            void Layout()
            {
                // Headless clients have no render frame to drain the deferred layout queues.
                foreach (var control in Descendants(window).Prepend(window))
                { control.InvalidateMeasure(); control.InvalidateArrange(); }
                window.Measure(size); window.Arrange(new UIBox2(Vector2.Zero, size));
            }
            Layout();
            var pages = Descendants(window).OfType<Button>().Where(b => b.Name.StartsWith("nav-")).Select(b => b.Name).ToArray();
            Assert.That(pages, Has.Length.EqualTo(14));
            foreach (var name in pages)
            {
                TestContext.Progress.WriteLine("UI page: " + name);
                Click(Descendants(window).OfType<Button>().Single(b => b.Name == name)); Layout();
                var scroll = Descendants(window).OfType<ScrollContainer>().Single(c => c.Name == "page-scroll");
                Assert.That(scroll.HScrollEnabled, Is.False);
                Assert.That(scroll.Width, Is.GreaterThan(380));
                Assert.That(scroll.Children.First(c => c is BoxContainer).Width, Is.LessThanOrEqualTo(scroll.Width + 1), name);
                Assert.That(scroll.Height, Is.GreaterThan(150), name);
                if (name == "nav-recruit-reset" && width >= 1080)
                {
                    var reset = Descendants(scroll).OfType<Button>().Single(b => b.Name == "reset-recruit");
                    Assert.That(reset.GlobalPosition.Y + reset.Height, Is.LessThanOrEqualTo(scroll.GlobalPosition.Y + scroll.Height + 1), "Primary reset action must fit the standard viewport");
                }
                foreach (var rich in Descendants(scroll).OfType<RichTextLabel>().Where(c => c.Visible))
                    Assert.That(rich.Width, Is.LessThanOrEqualTo(scroll.Width + 1), name);
                foreach (var button in Descendants(scroll).OfType<Button>().Where(b => b.VisibleInTree))
                {
                    Assert.That(button.GlobalPosition.X, Is.GreaterThanOrEqualTo(scroll.GlobalPosition.X - 1), name + "/" + button.Name);
                    Assert.That(button.GlobalPosition.X + button.Width, Is.LessThanOrEqualTo(scroll.GlobalPosition.X + scroll.Width + 1), name + "/" + button.Name);
                }
            }
            Click(Descendants(window).OfType<Button>().Single(b => b.Name == "nav-player-management")); Layout();
            var grant = Descendants(window).OfType<Button>().Single(b => b.Name == "grant");
            Assert.That(grant.Disabled, Is.True);
            var reason = Descendants(window).OfType<LineEdit>().Single(e => e.Name == "action-reason");
            reason.SetText("Personally verified skills", true);
            Assert.That(grant.Disabled, Is.True, "Reason alone must not approve a mutation");
            Click(Descendants(window).OfType<CheckBox>().Single(c => c.Name == "action-confirm"));
            Assert.That(grant.Disabled, Is.False);
            Click(grant);
            Assert.That(sent, Has.Count.EqualTo(1));
            Assert.That(sent[0].Request.Target, Is.EqualTo(view.Target));
            Assert.That(sent[0].Request.Reason, Is.EqualTo("Personally verified skills"));
            Assert.That(grant.Disabled, Is.True, "Pending requests must disable repeat writes");
            view.ResponseId = sent[0].Request.RequestId;
            window.Update(view); Layout();
            Assert.That(Descendants(window).OfType<CheckBox>().Single(c => c.Name == "action-confirm").Pressed, Is.False);
            view.Management = false; view.Instructor = false; view.Officer = false;
            window.Update(view); Layout();
            Assert.That(Descendants(window).OfType<Button>().Where(b => b.Name.StartsWith("nav-")).Select(b => b.Name),
                Is.EquivalentTo(new[] { "nav-dossier", "nav-role-access" }));
        });
    }

    [Test]
    public async Task RefreshKeepsDraftButSwitchingTargetClearsItAndPreviewDoesNotEnableWrites()
    {
        await Client.WaitAssertion(() =>
        {
            var view = QualificationPreviewCommand.CreateView(Client.ResolveDependency<IPrototypeManager>());
            using var window = new QualificationWindow((_, _) => Assert.Fail("Read-only preview cannot send mutations"), preview: true);
            window.Open();
            window.Update(view); Layout(window);
            Click(Descendants(window).OfType<Button>().Single(b => b.Name == "nav-player-management")); Layout(window);
            Descendants(window).OfType<LineEdit>().Single(e => e.Name == "action-reason").SetText("Draft", true);
            window.Update(view); Layout(window);
            Assert.That(Descendants(window).OfType<LineEdit>().Single(e => e.Name == "action-reason").Text, Is.EqualTo("Draft"));
            Click(Descendants(window).OfType<CheckBox>().Single(c => c.Name == "action-confirm"));
            Assert.That(Descendants(window).OfType<Button>().Single(b => b.Name == "grant").Disabled, Is.True);
            view.Target = view.Viewer; window.Update(view); Layout(window);
            Assert.That(Descendants(window).OfType<LineEdit>().Single(e => e.Name == "action-reason").Text, Is.Empty);
        });
    }

    [Test]
    public async Task NicknameSelectionOpensRecruitAndUnsolicitedUpdatesCannotReleasePendingWrites()
    {
        await Client.WaitAssertion(() =>
        {
            var view = QualificationPreviewCommand.CreateView(Client.ResolveDependency<IPrototypeManager>());
            var recruit = view.Target;
            view.Target = view.Viewer; view.Management = false;
            view.Store.Instructors[view.Viewer] = new(true, InstructorRank.Instructor, view.Viewer, DateTimeOffset.UtcNow, view.Viewer, DateTimeOffset.UtcNow);
            var sent = new List<(QualificationAction Action, QualificationRequest Request)>();
            using var window = new QualificationWindow((a, r) => sent.Add((a, r)));
            window.Open(); window.Update(view); Layout(window);
            var search = Descendants(window).OfType<LineEdit>().Single(e => e.Name == "player-search");
            search.SetText("moroz", true);
            Layout(window);
            Assert.That(Descendants(window).OfType<Button>().Where(b => b.Name.StartsWith("select-player-")).ToArray(), Has.Length.EqualTo(1));
            Click(Descendants(window).OfType<Button>().Single(b => b.Name == "select-player-" + recruit));
            Assert.That(sent.Single().Request.Target, Is.EqualTo(recruit));
            window.Update(view); Layout(window); // A roster notification is not a selection acknowledgement.
            Assert.That(Descendants(window).OfType<Button>().Single(b => b.Name == "select-player-" + recruit).Disabled, Is.True);
            view.Target = recruit; view.ResponseId = sent[0].Request.RequestId;
            window.Update(view); Layout(window);
            Assert.That(Descendants(window).OfType<Button>().Single(b => b.Name == "nav-training").Pressed, Is.True);
            var complete = Descendants(window).OfType<Button>().First(b => b.Name.StartsWith("complete-"));
            Assert.That(complete.Disabled, Is.True);
            Descendants(window).OfType<LineEdit>().Single(e => e.Name == "action-reason").SetText("Practical check", true);
            Click(Descendants(window).OfType<CheckBox>().Single(c => c.Name == "action-confirm"));
            Assert.That(complete.Disabled, Is.False); Click(complete);
            Assert.That(sent.Last().Request.Target, Is.EqualTo(recruit));
            Assert.That(sent.Last().Action, Is.EqualTo(QualificationAction.Complete));
            window.Update(view); Layout(window);
            Assert.That(Descendants(window).OfType<Button>().First(b => b.Name.StartsWith("complete-")).Disabled, Is.True);
            view.ResponseId = sent.Last().Request.RequestId; view.TargetOnline = false;
            window.Update(view); Layout(window);
            Descendants(window).OfType<LineEdit>().Single(e => e.Name == "action-reason").SetText("Stale assessment", true);
            Click(Descendants(window).OfType<CheckBox>().Single(c => c.Name == "action-confirm"));
            Assert.That(Descendants(window).OfType<Button>().First(b => b.Name.StartsWith("complete-")).Disabled, Is.True);
            Descendants(window).OfType<LineEdit>().Single(e => e.Name == "note-text").SetText("Stale note", true);
            Assert.That(Descendants(window).OfType<Button>().Single(b => b.Name == "save-note").Disabled, Is.True);
        });
    }

    // CMU14: historical preview must reach the server without a roster; execution stays explicit.
    [Test]
    public async Task FullHistoricalPreviewNeedsNoRosterAndExecuteRequiresConfirmation()
    {
        await Client.WaitAssertion(() =>
        {
            var view = QualificationPreviewCommand.CreateView(Client.ResolveDependency<IPrototypeManager>());
            view.Preview = null; view.PreviewToken = "";
            var sent = new List<(QualificationAction Action, QualificationRequest Request)>();
            using var window = new QualificationWindow((a, r) => sent.Add((a, r)));
            window.Open(); window.Update(view); Layout(window);
            Click(Descendants(window).OfType<Button>().Single(b => b.Name == "nav-migration")); Layout(window);
            Assert.That(Descendants(window).OfType<Button>().Single(b => b.Name == "execute-migration").Disabled, Is.True);
            Click(Descendants(window).OfType<Button>().Single(b => b.Name == "scan-historical"));
            Assert.That(sent.Single().Action, Is.EqualTo(QualificationAction.MigrationPreview));
            Assert.That(sent.Single().Request.Configuration.ScanAllHistorical, Is.True);
            Assert.That(sent.Single().Request.Configuration.Roster, Is.Null);
            view.ResponseId = sent.Last().Request.RequestId;
            view.Preview = new() { AccountsScanned = 10003, PlayersReceiving = 2, Records = 3, Counts = new() { ["enlisted"] = 2, ["medical"] = 1 } };
            view.PreviewToken = "verified-token";
            window.Update(view); Layout(window);
            var execute = Descendants(window).OfType<Button>().Single(b => b.Name == "execute-migration");
            Assert.That(execute.Disabled, Is.True);
            Descendants(window).OfType<LineEdit>().Single(e => e.Name == "action-reason").SetText("Verified historical Dry Run", true);
            Click(Descendants(window).OfType<CheckBox>().Single(c => c.Name == "action-confirm"));
            Assert.That(execute.Disabled, Is.False);
            Click(execute);
            Assert.That(sent.Last().Action, Is.EqualTo(QualificationAction.MigrationExecute));
            Assert.That(sent.Last().Request.PreviewToken, Is.EqualTo("verified-token"));
            view.ResponseId = sent.Last().Request.RequestId; view.Preview.Completed = true;
            window.Update(view); Layout(window);
            Assert.That(Descendants(window).OfType<Button>().Single(b => b.Name == "execute-migration").Disabled, Is.True);
        });
    }

    private static void Layout(QualificationWindow window)
    {
        var size = new Vector2(1080, 740);
        foreach (var control in Descendants(window).Prepend(window))
        { control.InvalidateMeasure(); control.InvalidateArrange(); }
        window.SetSize = size; window.Measure(size); window.Arrange(new UIBox2(Vector2.Zero, size));
    }

    internal static void Click(BaseButton button)
    {
        var pointer = new ScreenCoordinates(button.GlobalPixelPosition + new Vector2(2, 2), default);
        var down = new GUIBoundKeyEventArgs(EngineKeyFunctions.UIClick, BoundKeyState.Down, pointer, true, new(2, 2), new(2, 2));
        var up = new GUIBoundKeyEventArgs(EngineKeyFunctions.UIClick, BoundKeyState.Up, pointer, true, new(2, 2), new(2, 2));
        typeof(BaseButton).GetMethod("KeyBindDown", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(button, new object[] { down });
        typeof(BaseButton).GetMethod("KeyBindUp", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(button, new object[] { up });
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (var child in root.Children)
        {
            yield return child;
            foreach (var control in Descendants(child)) yield return control;
        }
    }
}

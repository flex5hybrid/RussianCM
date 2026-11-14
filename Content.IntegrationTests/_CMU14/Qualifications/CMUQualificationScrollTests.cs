using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Client._RuCM.Qualifications;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests._RuCM.Qualifications;
using Content.Shared._RuCM.Qualifications;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.CMU14.Qualifications;

[TestFixture, NonParallelizable]
public sealed class CMUQualificationScrollTests : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };

    [TestCase(QualificationAction.Complete, "complete-military_regulations", "military_regulations")]
    [TestCase(QualificationAction.Certify, "certify", "")]
    public async Task TrainingResponsePreservesScrollbarCapture(QualificationAction action, string buttonName, string item)
    {
        await Client.WaitAssertion(() =>
        {
            var view = CreateLongView();
            var state = view.Store.Players[view.Target];
            var definition = view.Store.Definitions["enlisted"];
            var context = state.Progress[definition.Id]["firearms"].By;
            foreach (var step in definition.Items.Where(step => step.Id != item))
                state.Progress[definition.Id][step.Id] = new(definition.Id, step.Id, context, "");
            var sent = new List<(QualificationAction Action, QualificationRequest Request)>();
            using var window = new QualificationWindow((kind, request) => sent.Add((kind, request)));
            window.Open();
            window.Update(view);
            Navigate(window, "training");
            Descendants(window).OfType<LineEdit>().Single(e => e.Name == "action-reason").SetText("Verified", true);
            QualificationUiTests.Click(Descendants(window).OfType<CheckBox>().Single(c => c.Name == "action-confirm"));
            var button = Descendants(window).OfType<Button>().Single(b => b.Name == buttonName);
            Assert.That(button.Disabled, Is.False);
            QualificationUiTests.Click(button);
            Assert.That(sent.Single().Action, Is.EqualTo(action));

            var scroll = Scroll(window, "page-scroll");
            scroll.VScroll = 300;
            var scrollbar = scroll.Children.OfType<VScrollBar>().Single();
            var ui = IoCManager.Resolve<IUserInterfaceManager>();
            ui.ControlFocused = scrollbar;
            try
            {
                if (action == QualificationAction.Complete)
                    state.Progress[definition.Id][item] = new(definition.Id, item, context, "Verified");
                else
                    state.Grants[definition.Id] = new(definition.Id, QualificationStatus.Active, definition.Version, [], context, "Verified");
                view.ResponseId = sent.Single().Request.RequestId;
                window.Update(view);
                Layout(window);
                Assert.That(ui.ControlFocused, Is.SameAs(scrollbar));
                Assert.That(scroll.VScroll, Is.EqualTo(300));
                Assert.That(Descendants(window).OfType<Button>().Where(b => b.Name == buttonName).All(b => b.Disabled), Is.True);
            }
            finally
            {
                ui.ControlFocused = null;
            }
        });
    }

    [TestCase("dossier")]
    [TestCase("training")]
    [TestCase("players")]
    [TestCase("definitions")]
    public async Task ViewUpdatesKeepPageAndNavigationScrollIncludingAnimationTarget(string page)
    {
        await Client.WaitAssertion(() =>
        {
            var view = CreateLongView();
            var sent = new List<QualificationRequest>();
            using var window = new QualificationWindow((_, request) => sent.Add(request));
            window.Open(); window.Update(view); Navigate(window, page);
            var scroll = Scroll(window, "page-scroll");
            var navigation = Scroll(window, "navigation-scroll");
            scroll.SetScrollValue(new Vector2(0, 300)); scroll.VScrollTarget = 350;
            navigation.SetScrollValue(new Vector2(0, 80)); navigation.VScrollTarget = 100;
            Assert.That(scroll.GetScrollValue().Y, Is.EqualTo(300), "Test must scroll an overflowing page");
            Assert.That(navigation.GetScrollValue().Y, Is.EqualTo(80), "Test must scroll overflowing navigation");

            for (var update = 0; update < 5; update++)
            {
                view.Store.Revision++;
                view.OnlinePlayers[view.Target] = "Updated recruit " + update;
                window.Update(view); Layout(window);
                Assert.That(Scroll(window, "page-scroll").GetScrollValue().Y, Is.EqualTo(300));
                Assert.That(Scroll(window, "page-scroll").VScrollTarget, Is.EqualTo(350));
                Assert.That(Scroll(window, "navigation-scroll").GetScrollValue().Y, Is.EqualTo(80));
                Assert.That(Scroll(window, "navigation-scroll").VScrollTarget, Is.EqualTo(100));
                Assert.That(Descendants(window).OfType<RichTextLabel>().Any(label =>
                    label.Text?.Contains(view.OnlinePlayers[view.Target]) == true), Is.True,
                    "Preserving scroll must still display authoritative changes");
            }
            Assert.That(sent, Is.Empty, "Server notifications must not submit actions");

            if (page == "players") return; // Selecting an account from the roster intentionally opens another page.
            QualificationUiTests.Click(Descendants(window).OfType<Button>().Single(b => b.Name == "refresh"));
            view.ResponseId = sent.Single().RequestId;
            window.Update(view); Layout(window);
            Assert.That(Scroll(window, "page-scroll").GetScrollValue().Y, Is.EqualTo(300), "Manual refresh acknowledgement");
            Assert.That(Scroll(window, "page-scroll").VScrollTarget, Is.EqualTo(350));
        });
    }

    [Test]
    public async Task ChangingPageOrAccountResetsPageScrollButKeepsNavigationPosition()
    {
        await Client.WaitAssertion(() =>
        {
            var view = CreateLongView();
            using var window = new QualificationWindow((_, _) => Assert.Fail("Navigation must remain local"));
            window.Open(); window.Update(view); Navigate(window, "dossier");
            Scroll(window, "page-scroll").SetScrollValue(new Vector2(0, 300));
            Scroll(window, "navigation-scroll").SetScrollValue(new Vector2(0, 80));
            Navigate(window, "training");
            Assert.That(Scroll(window, "page-scroll").VScroll, Is.Zero);
            Assert.That(Scroll(window, "page-scroll").VScrollTarget, Is.Zero);
            Assert.That(Scroll(window, "navigation-scroll").VScroll, Is.EqualTo(80));

            Scroll(window, "page-scroll").SetScrollValue(new Vector2(0, 300));
            view.Target = view.Viewer;
            window.Update(view); Layout(window);
            Assert.That(Scroll(window, "page-scroll").VScroll, Is.Zero);
            Assert.That(Scroll(window, "page-scroll").VScrollTarget, Is.Zero);
            Assert.That(Scroll(window, "navigation-scroll").VScroll, Is.EqualTo(80));
        });
    }

    private QualificationView CreateLongView()
    {
        var view = QualificationPreviewCommand.CreateView(Client.ResolveDependency<IPrototypeManager>());
        for (var item = 0; item < 30; item++)
        {
            view.Store.Definitions["enlisted"].Items.Add(new ChecklistItem
            { Id = "scroll-step-" + item, Name = "Training step " + item, Description = "Training details" });
            view.OnlinePlayers[Guid.NewGuid()] = "Recruit " + item.ToString("D2");
        }
        return view;
    }

    private static ScrollContainer Scroll(Control window, string name) =>
        Descendants(window).OfType<ScrollContainer>().Single(c => c.Name == name);

    private static void Navigate(QualificationWindow window, string page)
    {
        Layout(window); // Native clicks need arranged button bounds, including after scrolling navigation.
        QualificationUiTests.Click(Descendants(window).OfType<Button>().Single(b => b.Name == "nav-" + page));
        Layout(window);
        Assert.That(Descendants(window).OfType<Button>().Single(b => b.Name == "nav-" + page).Pressed, Is.True,
            "The requested page must actually open before checking its scroll position");
    }

    private static void Layout(QualificationWindow window)
    {
        var size = new Vector2(1080, 740);
        foreach (var control in Descendants(window).Prepend(window))
        { control.InvalidateMeasure(); control.InvalidateArrange(); }
        window.SetSize = size; window.Measure(size); window.Arrange(new UIBox2(Vector2.Zero, size));
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (var child in root.Children)
        {
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
}

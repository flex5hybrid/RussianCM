using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Content.Client._RuCM.Guidebook;
using Content.Client._RuCM.Qualifications;
using Content.Client.Guidebook;
using Content.Client.Guidebook.Richtext;
using Content.Client.Guidebook.Controls;
using Content.Client.UserInterface.Systems.Guidebook;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests._RuCM.Qualifications;
using Content.Shared._RuCM.Guidebook;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Guidebook;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests._RuCM.Guidebook;

[TestFixture, NonParallelizable]
public sealed class GOVFORGuidebookTests : GameTest
{
    private static int _ttsOverrideIndex;

    public override PoolSettings PoolSettings => new() { InLobby = true, Fresh = true, Destructive = true };

    [OneTimeSetUp]
    public static void ConfigureLobbyFixture()
    {
        // The full lobby announces joins before per-test overrides. This fixture has no external TTS provider.
        _ttsOverrideIndex = PoolManager.Instance.DefaultCvars.Count;
        PoolManager.Instance.DefaultCvars.Add((CCCVars.TTSEnabled.Name, "false"));
    }

    [OneTimeTearDown]
    public static void RestoreLobbyFixture()
    {
        PoolManager.Instance.DefaultCvars.RemoveAt(_ttsOverrideIndex);
    }

    [TestCase("ru-RU")]
    [TestCase("en-US")]
    public async Task NormalGuidebookShowsGOVFORButIndividualBooksRemainScoped(string culture)
    {
        await Client.WaitAssertion(() =>
        {
            var loc = Client.ResolveDependency<ILocalizationManager>();
            loc.DefaultCulture = CultureInfo.GetCultureInfo(culture, predefinedOnly: false);
            var ui = Client.ResolveDependency<IUserInterfaceManager>();
            var controller = ui.GetUIController<GuidebookUIController>();
            controller.OpenGuidebook();
            var window = ui.WindowRoot.Children.OfType<GuidebookWindow>().Single();
            Assert.That(window.Tree.Items.Count(i => i.Metadata is GuideEntry e && e.Id == GOVFORTrainingGuides.Root), Is.EqualTo(1));
            Assert.That(window.Tree.Items.Single(i => i.Metadata is GuideEntry e && e.Id == GOVFORTrainingGuides.Basic)
                .Label.Text, Is.EqualTo(loc.GetString("rucm-guide-entry-basic")));
            var drill = window.Tree.Items.Single(i => i.Metadata is GuideEntry e &&
                e.Id == GOVFORGuidebookSystem.DrillRegulations);
            Assert.That(drill.Label.Text, Is.EqualTo(loc.GetString("rucm-guide-entry-govfor-drill-regulations")));
            Assert.That(window.Tree.Items.Any(i => i.Metadata is GuideEntry e && e.Id == "AU14SOP"), Is.True);
            Assert.That(window.Tree.Items.Any(i => i.Metadata is GuideEntry e && e.Id == "AU14UCMJ"), Is.True);
            window.HandleClick(GOVFORTrainingGuides.Drill);
            Assert.That(window.Selected?.Id, Is.EqualTo(GOVFORGuidebookSystem.DrillRegulations));
            Assert.That(Descendants(window).OfType<GuidebookError>(), Is.Empty);
            Assert.That(Descendants(window).OfType<Label>().Select(i => i.Text), Does.Contain("Строевая подготовка"));
            Assert.That(Descendants(window).OfType<Label>().Select(i => i.Text), Does.Contain("Дух Устава"));

            window.Close();
            controller.OpenGuidebook();
            Assert.That(window.Tree.Items.Count(i => i.Metadata is GuideEntry e &&
                e.Id == GOVFORGuidebookSystem.DrillRegulations), Is.EqualTo(1));
            // The upstream controller resets an unknown root to its default when reopening.
            Assert.That(window.Selected?.Id, Is.EqualTo("CMUGuidebook"));

            window.Close();
            controller.OpenGuidebook(new List<ProtoId<GuideEntryPrototype>> { "AU14UCMJ" }, selected: "AU14UCMJ");
            Assert.That(window.Tree.Items.Any(i => i.Metadata is GuideEntry e &&
                e.Id == GOVFORGuidebookSystem.DrillRegulations), Is.False);
            Assert.That(window.Selected?.Id, Is.EqualTo("AU14UCMJ"));

            window.Close();
            controller.OpenGuidebook();
            Assert.That(window.Tree.Items.Count(i => i.Metadata is GuideEntry e &&
                e.Id == GOVFORGuidebookSystem.DrillRegulations), Is.EqualTo(1));
            Assert.That(window.Selected?.Id, Is.EqualTo("AU14UCMJ"));
            window.Close();
        });
    }

    [TestCase("ru-RU")]
    [TestCase("en-US")]
    public async Task EveryDocumentParsesCrossLinksWorkAndOrdinaryPlayerReadingCannotMutateTraining(string culture)
    {
        await Client.WaitAssertion(() =>
        {
            Client.ResolveDependency<ILocalizationManager>().DefaultCulture = CultureInfo.GetCultureInfo(culture, false);
            var ui = Client.ResolveDependency<IUserInterfaceManager>();
            var controller = ui.GetUIController<GuidebookUIController>();
            var prototypes = Client.ResolveDependency<IPrototypeManager>();
            var parser = Client.ResolveDependency<DocumentParsingManager>();
            var owned = prototypes.EnumeratePrototypes<GuideEntryPrototype>()
                .Where(p => p.Id.StartsWith("RuCMGOVFOR")).Select(p => p.Id).Order().ToArray();
            Assert.That(owned, Has.Length.EqualTo(66));
            foreach (var id in owned)
            {
                var prototype = prototypes.Index<GuideEntryPrototype>(id);
                Assert.That(prototype.RuleEntry, Is.False, id);
                foreach (var child in prototype.Children)
                    Assert.That(prototypes.HasIndex(child), Is.True, id + "/" + child);
                var document = new Document();
                Assert.That(parser.TryAddMarkup(document, new ProtoId<GuideEntryPrototype>(id)), Is.True, id);
                Assert.That(Descendants(document).OfType<GuidebookError>(), Is.Empty, id);
                // CMU14: the published recruit restriction must survive guidebook parsing in both cultures.
                if (id is "RuCMGOVFORTrainingQualifications" or "RuCMGOVFORRecruitCourseIntroduction" or "RuCMGOVFORInstructorHandbookWork")
                {
                    var text = string.Join(" ", Descendants(document).OfType<RichTextLabel>().Select(l => l.GetMessage().ToString()));
                    Assert.That(text, Does.Contain("вне базы"), id);
                    Assert.That(text, Does.Contain("FOB"), id);
                    Assert.That(text, Does.Contain("чрезвычайны"), id);
                }
            }
            // CMU14: mode rules use the same document parser as the training handbook.
            var rulesDocument = new Document();
            Assert.That(parser.TryAddMarkup(rulesDocument, new ProtoId<GuideEntryPrototype>("AU14GamemodeRulesInsurgency")), Is.True);
            Assert.That(Descendants(rulesDocument).OfType<GuidebookError>(), Is.Empty);
            Assert.That(string.Join(" ", Descendants(rulesDocument).OfType<RichTextLabel>().Select(l => l.GetMessage().ToString())),
                Does.Contain("использование рекрутов вне базы"));

            controller.OpenGuidebook();
            var guides = ui.WindowRoot.Children.OfType<GuidebookWindow>().Single();
            foreach (var id in owned)
            {
                guides.HandleClick(id);
                Assert.That(guides.Selected?.Id, Is.EqualTo(id));
                Assert.That(Descendants(guides).OfType<GuidebookError>(), Is.Empty, id);
                var links = Descendants(guides).OfType<Button>().Where(b => b.Name.StartsWith("govfor-guide-"))
                    .Select(b => b.Name).ToArray();
                foreach (var name in links)
                {
                    guides.HandleClick(id);
                    Layout(guides);
                    QualificationUiTests.Click(Descendants(guides).OfType<Button>().Single(b => b.Name == name));
                    Assert.That(guides.Selected?.Id.ToString(), Is.EqualTo(name["govfor-guide-".Length..]));
                    Assert.That(Descendants(guides).OfType<GuidebookError>(), Is.Empty, name);
                }
            }

            using var invalidLink = new GOVFORGuideLink();
            Assert.That(invalidLink.TryParseTag(new() { ["Guide"] = "../../admin" }, out _), Is.False);
            Assert.That(invalidLink.TryParseTag(new() { ["Guide"] = GOVFORTrainingGuides.Drill, ["Player"] = "secret" }, out _), Is.False);
            using var invalidText = new GOVFORLocalizedText();
            Assert.That(invalidText.TryParseTag(new() { ["Key"] = "rucm-qualifications-audit" }, out _), Is.False);

            var view = QualificationPreviewCommand.CreateView(prototypes);
            view.Management = false; view.Instructor = false; view.Officer = false;
            view.Target = view.Viewer;
            view.Store.Notes.Clear(); // The ordinary viewer receives no other player's private notes.
            var before = System.Text.Json.JsonSerializer.Serialize(view.Store);
            var sent = 0;
            using var dossier = new QualificationWindow((_, _) => sent++);
            dossier.Open(); dossier.Update(view);
            Layout(dossier);
            Assert.That(Descendants(dossier).OfType<Button>().Where(b => b.Name.StartsWith("nav-")).Select(b => b.Name),
                Is.EquivalentTo(new[] { "nav-dossier", "nav-role-access" }));
            var reading = Descendants(dossier).OfType<Button>().Single(b => b.Name ==
                "qualification-guide-enlisted-communications-" + GOVFORTrainingGuides.Communications);
            Assert.That(reading.Disabled, Is.False);
            QualificationUiTests.Click(reading);
            Assert.That(guides.Selected?.Id, Is.EqualTo(GOVFORTrainingGuides.Communications));
            Assert.That(sent, Is.Zero, "Reading never sends qualification mutations");
            Assert.That(System.Text.Json.JsonSerializer.Serialize(view.Store), Is.EqualTo(before));
            TestContext.Progress.WriteLine($"GOVFOR {culture}: {owned.Length} prototypes parsed, all cross-links navigated, ordinary-player dossier reading sent zero mutations.");
            dossier.Close(); guides.Close();
        });
    }

    private static void Layout(Control window)
    {
        // Headless clients do not render frames to drain the normal layout queues before a mouse release.
        var size = new Vector2(1080, 740);
        foreach (var control in Descendants(window).Prepend(window))
        { control.InvalidateMeasure(); control.InvalidateArrange(); }
        window.Measure(size); window.Arrange(new UIBox2(Vector2.Zero, size));
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

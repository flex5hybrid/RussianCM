using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Content.Shared._RuCM.Guidebook;
using Content.Shared._RuCM.Qualifications;
using NUnit.Framework;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Tests._RuCM.Guidebook;

public sealed class GOVFORRegulationsTests : ContentUnitTest
{
    internal static string Find(string relative)
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, relative);
            if (File.Exists(path)) return path;
        }
        throw new FileNotFoundException(relative);
    }

    private static string Prose(string text)
    {
        text = Regex.Replace(text, @"^\s*(?:#+ |> |---\s*$)", "", RegexOptions.Multiline);
        text = text.Replace("**", "");
        text = Regex.Replace(text, @"\[/?(?:bold|italic)\]", "");
        return Regex.Replace(text, @"\s+", " ").Trim();
    }

    [Test]
    public void PublishedTextsPreserveAllApprovedParagraphsAndContainOnlyPublicReferences()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(Find("Resources/RuCM/GOVFOR/Regulations/manifest.json")));
        Assert.That(manifest.RootElement.GetArrayLength(), Is.EqualTo(4));
        foreach (var document in manifest.RootElement.EnumerateArray())
        {
            var sourceBytes = File.ReadAllBytes(Find(document.GetProperty("source").GetString()));
            var source = Encoding.UTF8.GetString(sourceBytes);
            var bytes = File.ReadAllBytes(Find(document.GetProperty("xml").GetString()));
            var xml = Encoding.UTF8.GetString(bytes);
            Assert.That(bytes.Take(3), Is.Not.EqualTo(new byte[] { 239, 187, 191 }), "No UTF-8 BOM");
            Assert.That(xml, Does.Not.Contain("\r"));
            Assert.That(xml, Does.Contain("GOVFOR"));
            Assert.That(Regex.IsMatch(xml, "[А-Яа-яЁё]"), Is.True);
            Assert.That(Convert.ToHexString(SHA256.HashData(sourceBytes)).ToLowerInvariant(),
                Is.EqualTo(document.GetProperty("source_sha256").GetString()));
            var parsed = XDocument.Parse(xml);
            Assert.That(parsed.Root.Name.LocalName, Is.EqualTo("Document"));
            var approved = string.Concat(parsed.Root.Nodes().TakeWhile(n => n is not XComment c || c.Value.Trim() != "end-approved-text")
                .OfType<XText>().Select(n => n.Value));
            Assert.That(Prose(approved), Is.EqualTo(Prose(source)), document.GetProperty("id").GetString());
            Assert.That(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Prose(source)))).ToLowerInvariant(),
                Is.EqualTo(document.GetProperty("prose_sha256").GetString()));
            Assert.That(parsed.Root.Elements().Select(e => e.Name.LocalName),
                Is.All.AnyOf("GOVFORGuideLink", "GOVFORLocalizedText"), "No embedded entities, privileged UI, or player data");
            foreach (var link in parsed.Descendants("GOVFORGuideLink"))
                Assert.That(GOVFORTrainingGuides.IsReference(link.Attribute("Guide").Value), Is.True);
        }
    }

    [TestCase("ru-RU")]
    [TestCase("en-US")]
    public void NavigationAndReadingActionsResolveInRealFluentManager(string culture)
    {
        var file = Find($"Resources/Locale/{culture}/_RuCM/guidebook/govfor_documents.ftl");
        var root = new MemoryContentRoot();
        root.AddOrUpdateFile(new ResPath($"Locale/{culture}/govfor.ftl"), File.ReadAllBytes(file));
        var courseFile = Find($"Resources/Locale/{culture}/_RuCM/guidebook/govfor_recruit_course.ftl");
        root.AddOrUpdateFile(new ResPath($"Locale/{culture}/course.ftl"), File.ReadAllBytes(courseFile));
        var instructorFile = Find($"Resources/Locale/{culture}/_RuCM/guidebook/govfor_instructor_handbook.ftl");
        root.AddOrUpdateFile(new ResPath($"Locale/{culture}/instructor.ftl"), File.ReadAllBytes(instructorFile));
        var advancedFile = Find($"Resources/Locale/{culture}/_RuCM/guidebook/govfor_advanced_training.ftl");
        root.AddOrUpdateFile(new ResPath($"Locale/{culture}/advanced.ftl"), File.ReadAllBytes(advancedFile));
        IoCManager.Resolve<IResourceManager>().AddRoot(new ResPath("/"), root);
        var loc = IoCManager.Resolve<ILocalizationManager>();
        loc.Initialize(); loc.LoadCulture(new CultureInfo(culture, false));
        var keys = Regex.Matches(File.ReadAllText(file) + File.ReadAllText(courseFile) + File.ReadAllText(instructorFile) + File.ReadAllText(advancedFile), @"(?m)^([a-zA-Z][a-zA-Z0-9_-]*) =").Select(m => m.Groups[1].Value).ToArray();
        Assert.That(keys.Distinct().Count(), Is.EqualTo(keys.Length));
        foreach (var key in keys)
        {
            Assert.That(loc.HasString(key), Is.True, key);
            Assert.That(loc.GetString(key), Is.Not.Empty, key);
        }
        foreach (var xml in Directory.GetFiles(Path.GetDirectoryName(Find("Resources/ServerInfo/Guidebook/_RuCM/GOVFOR/Index.xml")), "*.xml", SearchOption.AllDirectories))
        foreach (var text in XDocument.Load(xml).Descendants("GOVFORLocalizedText"))
            Assert.That(loc.HasString(text.Attribute("Key").Value), Is.True, xml);
    }

    [Test]
    public void RecruitCourseChaptersPreserveCompleteApprovedTextWithoutDuplicationAndCoverActualEnlistedChecklist()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(Find("Resources/RuCM/GOVFOR/Regulations/recruit_course_manifest.json")));
        var sourceBytes = File.ReadAllBytes(Find(manifest.RootElement.GetProperty("source").GetString()));
        Assert.That(Convert.ToHexString(SHA256.HashData(sourceBytes)).ToLowerInvariant(),
            Is.EqualTo(manifest.RootElement.GetProperty("source_sha256").GetString()));
        var chapters = manifest.RootElement.GetProperty("chapters").EnumerateArray().ToArray();
        Assert.That(chapters, Has.Length.EqualTo(11));
        var combined = new StringBuilder();
        var items = new System.Collections.Generic.List<string>();
        foreach (var chapter in chapters)
        {
            var xml = File.ReadAllText(Find(chapter.GetProperty("xml").GetString()));
            Assert.That(xml, Does.Not.Contain("\r"));
            var parsed = XDocument.Parse(xml);
            var text = string.Concat(parsed.Root.Nodes().TakeWhile(n => n is not XComment c || c.Value.Trim() != "end-approved-text")
                .OfType<XText>().Select(n => n.Value));
            Assert.That(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Prose(text)))).ToLowerInvariant(),
                Is.EqualTo(chapter.GetProperty("prose_sha256").GetString()));
            combined.AppendLine(text);
            Assert.That(GOVFORTrainingGuides.IsReference(chapter.GetProperty("id").GetString()), Is.True);
            foreach (var link in parsed.Descendants("GOVFORGuideLink"))
                Assert.That(GOVFORTrainingGuides.IsReference(link.Attribute("Guide").Value), Is.True);
            if (chapter.GetProperty("checklist_item").ValueKind != JsonValueKind.Null)
            {
                var item = chapter.GetProperty("checklist_item").GetString();
                items.Add(item);
                Assert.That(GOVFORTrainingGuides.ForChecklistItem("enlisted", item), Does.Contain(chapter.GetProperty("id").GetString()));
            }
        }
        Assert.That(Prose(combined.ToString()), Is.EqualTo(Prose(Encoding.UTF8.GetString(sourceBytes))), "Every paragraph once, in approved order");
        var seed = JsonSerializer.Deserialize<QualificationStore>(File.ReadAllText(Find("Resources/RuCM/Qualifications/seed.json")));
        Assert.That(items, Is.EquivalentTo(seed.Definitions["enlisted"].Items.Select(i => i.Id)));
        Assert.That(items.Distinct().Count(), Is.EqualTo(9));
    }

    [TestCase("enlisted", "drill", GOVFORTrainingGuides.Drill)]
    [TestCase("enlisted", "communications", GOVFORTrainingGuides.Communications)]
    [TestCase("enlisted", "military_regulations", GOVFORTrainingGuides.MilitaryCode)]
    [TestCase("sergeant", "tactics", GOVFORTrainingGuides.Command)]
    [TestCase("sergeant", "teaching", GOVFORTrainingGuides.InstructorHandbook)] // CMU14: teaching now opens the instructor handbook.
    [TestCase("sergeant", "unit_command", GOVFORTrainingGuides.Command)]
    [TestCase("officer", "operations", GOVFORTrainingGuides.Command)]
    [TestCase("officer", "staff_work", GOVFORTrainingGuides.Command)] // CMU14: current advanced-course references.
    [TestCase("officer", "military_regulations", GOVFORTrainingGuides.MilitaryCode)]
    public void ReadingReferencesUseRealUnchangedChecklistIds(string qualification, string item, string expected)
    {
        var seed = JsonSerializer.Deserialize<QualificationStore>(File.ReadAllText(Find("Resources/RuCM/Qualifications/seed.json")));
        Assert.That(seed.Definitions[qualification].Items.Any(i => i.Id == item), Is.True);
        var before = JsonSerializer.Serialize(seed);
        var links = GOVFORTrainingGuides.ForChecklistItem(qualification, item);
        Assert.That(links, Does.Contain(expected));
        Assert.That(links.All(GOVFORTrainingGuides.IsReference), Is.True);
        Assert.That(JsonSerializer.Serialize(seed), Is.EqualTo(before));
    }

    [Test]
    public void CustomProgrammeAliasesAndUnknownItemsCannotOpenArbitraryEntries()
    {
        Assert.That(GOVFORTrainingGuides.ForChecklistItem("sergeant", "methodical_training"),
            Is.EqualTo(GOVFORTrainingGuides.ForChecklistItem("sergeant", "teaching")));
        Assert.That(GOVFORTrainingGuides.ForChecklistItem("sergeant", "unit_management"),
            Is.EqualTo(GOVFORTrainingGuides.ForChecklistItem("sergeant", "unit_command")));
        Assert.That(GOVFORTrainingGuides.ForChecklistItem("officer", "operational_management"),
            Is.EqualTo(GOVFORTrainingGuides.ForChecklistItem("officer", "operations")));
        Assert.That(GOVFORTrainingGuides.ForChecklistItem("admin", "secrets"), Is.Empty);
        Assert.That(GOVFORTrainingGuides.IsReference("../../admin"), Is.False);
    }
}

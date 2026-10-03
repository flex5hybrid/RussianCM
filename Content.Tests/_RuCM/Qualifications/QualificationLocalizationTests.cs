using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Content.Shared._RuCM.Qualifications;
using NUnit.Framework;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Tests._RuCM.Qualifications;

public sealed class QualificationLocalizationTests : ContentUnitTest
{
    [TestCase("en-US")]
    [TestCase("ru-RU")]
    public void AllOwnedFluentMessagesAndSeedNamesLoadInRealLocalizationManager(string culture)
    {
        var ftl = Find("Resources", "Locale", culture, "_RuCM", "qualifications", "qualifications.ftl");
        var contents = File.ReadAllText(ftl);
        var root = new MemoryContentRoot();
        root.AddOrUpdateFile(new ResPath("Locale/" + culture + "/qualifications.ftl"), File.ReadAllBytes(ftl));
        IoCManager.Resolve<IResourceManager>().AddRoot(new ResPath("/"), root);
        var loc = IoCManager.Resolve<ILocalizationManager>(); loc.Initialize(); loc.LoadCulture(new CultureInfo(culture, false));
        var keys = Regex.Matches(contents, "(?m)^([a-zA-Z][a-zA-Z0-9_-]*) =").Select(m => m.Groups[1].Value).ToArray();
        Assert.That(keys.Distinct().Count(), Is.EqualTo(keys.Length), "No duplicate Fluent messages");
        foreach (var key in keys) Assert.That(loc.HasString(key), Is.True, key);
        var seed = JsonSerializer.Deserialize<QualificationStore>(File.ReadAllText(Find("Resources", "RuCM", "Qualifications", "seed.json")));
        foreach (var definition in seed.Definitions.Values)
        {
            Assert.That(loc.HasString(definition.Name), Is.True, definition.Id);
            Assert.That(loc.HasString(definition.Description), Is.True, definition.Id);
            foreach (var item in definition.Items) { Assert.That(loc.HasString(item.Name), Is.True, item.Id); Assert.That(loc.HasString(item.Description), Is.True, item.Id); }
        }
        Assert.That(loc.GetString("rucm-qualifications-job-denied", ("requirements", "test")), Does.Contain("test"));
    }
    private static string Find(params string[] parts)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            var path = parts.Aggregate(directory.FullName, Path.Combine);
            if (File.Exists(path)) return path; directory = directory.Parent;
        }
        Assert.Fail("Qualification resource not found"); return "";
    }
}

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Content.Client.Changelog;
using NUnit.Framework;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;
using Robust.UnitTesting;

namespace Content.Tests.CMU14.Changelog;

[TestFixture]
public sealed class ChangelogLoadingTest : RobustUnitTest
{
    public override UnitTestProject Project => UnitTestProject.Client;

    protected override void OverrideIoC()
    {
        IoCManager.Instance!.Register<ChangelogManager, ChangelogManager>();
    }

    [Test]
    public async Task ClientLoadsAllPackagedChangelogSections()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Resources", "Changelog")))
            directory = directory.Parent;

        Assert.That(directory, Is.Not.Null);
        using var root = new MemoryContentRoot();
        var source = Path.Combine(directory!.FullName, "Resources", "Changelog");
        foreach (var path in Directory.GetFiles(source, "*.yml"))
            root.AddOrUpdateFile(new ResPath($"Changelog/{Path.GetFileName(path)}"), File.ReadAllBytes(path));
        IoCManager.Resolve<IResourceManager>().AddRoot(ResPath.Root, root);
        IoCManager.Resolve<IReflectionManager>().LoadAssemblies(typeof(ChangelogManager).Assembly);
        IoCManager.Resolve<ISerializationManager>().Initialize();

        var changelogs = await IoCManager.Resolve<ChangelogManager>().LoadChangelog();

        Assert.That(changelogs.Select(log => log.Name),
            Is.EquivalentTo(new[] { "CMU", "RMC14", "Changelog", "Maps", "Admin", "Rules" }));
        Assert.That(changelogs.Single(log => log.Name == "Admin").AdminOnly, Is.True);
        foreach (var changelog in changelogs)
        {
            Assert.That(changelog.Entries, Is.Not.Empty, changelog.Name);
            Assert.That(changelog.Entries.Select(entry => entry.Id), Is.Unique, changelog.Name);
        }
    }
}

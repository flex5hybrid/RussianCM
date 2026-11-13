using System.Globalization;
using System.IO;
using Content.Tests;
using NUnit.Framework;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Content.Tests.Shared._RMC14.DonorCapes;

[TestFixture]
public sealed class DonorCapeEntityLocalizationTest : ContentUnitTest
{
    [Test]
    public void CapeEntityUsesLocalizedNameAndDescription()
    {
        var serialization = IoCManager.Resolve<ISerializationManager>();
        serialization.Initialize();

        var resources = IoCManager.Resolve<IResourceManager>();
        var contentRoot = new MemoryContentRoot();
        contentRoot.AddOrUpdateFile(
            new ResPath("Locale/ru-RU/donor-capes.ftl"),
            File.ReadAllBytes(GetResourcePath("Resources", "Locale", "ru-RU", "_RuMC14", "donor-capes.ftl")));
        resources.AddRoot(new ResPath("/"), contentRoot);

        var localization = IoCManager.Resolve<ILocalizationManager>();
        localization.Initialize();
        localization.LoadCulture(new CultureInfo("ru-RU", false));

        var prototypes = IoCManager.Resolve<IPrototypeManager>();
        prototypes.Initialize();
        prototypes.LoadString(@"
- type: entity
  id: RMCDonorCapeItem08
  name: rmc-donor-cape-08-name
  description: rmc-donor-cape-08-description
");
        prototypes.ResolveResults();

        var capeId = new ProtoId<EntityPrototype>("RMCDonorCapeItem08");
        var cape = prototypes.Index(capeId);

        Assert.Multiple(() =>
        {
            Assert.That(cape.Name, Is.EqualTo("Плащ ИТМ"));
            Assert.That(cape.Description, Is.EqualTo("Плащ ИТМ с фирменной символикой."));
        });
    }

    private static string GetResourcePath(params string[] parts)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            var candidate = directory.FullName;
            foreach (var part in parts)
                candidate = Path.Combine(candidate, part);

            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        Assert.Fail($"Could not locate resource: {string.Join(Path.DirectorySeparatorChar, parts)}");
        return string.Empty;
    }
}

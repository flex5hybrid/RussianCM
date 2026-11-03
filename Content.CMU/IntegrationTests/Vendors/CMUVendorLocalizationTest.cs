using System.Globalization;
using System.Linq;
using Content.Client.CMU14.Vendors;
using Content.IntegrationTests.Fixtures;
using Content.Shared._RMC14.Vendors;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.CMU14.Vendors;

[TestFixture, NonParallelizable]
public sealed class CMUVendorLocalizationTest : GameTest
{
    public override PoolSettings PoolSettings => new() { InLobby = true, Dirty = true };

    [TestCase("en-US")]
    [TestCase("ru-RU")]
    public async Task EveryVendorCategoryResolvesInTheSelectedLanguage(string culture)
    {
        await Client.WaitAssertion(() =>
        {
            var localization = Client.ResolveDependency<ILocalizationManager>();
            localization.SetCulture(CultureInfo.GetCultureInfo(culture));
            var prototypes = Client.ResolveDependency<IPrototypeManager>();
            var factory = Client.ResolveDependency<IComponentFactory>();
            var names = prototypes.EnumeratePrototypes<EntityPrototype>()
                .Where(prototype => !prototype.Abstract && !Pair.IsTestPrototype(prototype))
                .Select(prototype => prototype.TryComp<CMAutomatedVendorComponent>(out var vendor, factory) ? vendor : null)
                .Where(vendor => vendor != null)
                .SelectMany(vendor => vendor!.Sections.Select(section => section.Name))
                .ToHashSet();
            Assert.That(names, Is.Not.Empty);
            Assert.Multiple(() =>
            {
                foreach (var name in names.Order())
                {
                    var text = CMUVendorLocalization.GetSectionName(name, localization);
                    Assert.That(text, Is.Not.Empty, name);
                    Assert.That(text, Does.Not.Contain("vendor-section-"), name);
                    if (culture == "ru-RU")
                        Assert.That(text, Does.Match(@"\p{IsCyrillic}"), $"Untranslated category: {name}");
                }
            });
        });
    }

    [TestCase("en-US")]
    [TestCase("ru-RU")]
    public async Task EveryExplicitItemLabelUsesTheSelectedLanguage(string culture)
    {
        await Client.WaitAssertion(() =>
        {
            var localization = Client.ResolveDependency<ILocalizationManager>();
            localization.SetCulture(CultureInfo.GetCultureInfo(culture));
            var prototypes = Client.ResolveDependency<IPrototypeManager>();
            var factory = Client.ResolveDependency<IComponentFactory>();
            var entries = prototypes.EnumeratePrototypes<EntityPrototype>()
                .Where(prototype => !prototype.Abstract && !Pair.IsTestPrototype(prototype))
                .Select(prototype => prototype.TryComp<CMAutomatedVendorComponent>(out var vendor, factory) ? vendor : null)
                .Where(vendor => vendor != null)
                .SelectMany(vendor => vendor!.Sections.SelectMany(section => section.Entries))
                .Where(entry => entry.Name != null)
                .GroupBy(entry => entry.Name).Select(group => group.First()).ToArray();
            Assert.That(entries, Is.Not.Empty);
            Assert.Multiple(() =>
            {
                foreach (var entry in entries)
                {
                    if (!prototypes.TryIndex(entry.Id, out var prototype))
                        continue;

                    var text = CMUVendorLocalization.GetItemName(entry, prototype, localization);
                    Assert.That(text, Is.Not.Empty, entry.Name);
                    Assert.That(text, Does.Not.Contain("cmu-vendor-item-"), entry.Name);
                    if (culture == "ru-RU")
                        Assert.That(text, Does.Match(@"\p{IsCyrillic}"), $"Untranslated item override: {entry.Name}");
                }
            });
        });
    }
}

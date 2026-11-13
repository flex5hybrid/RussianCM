using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Content.Server.Database;
using Content.Shared._RMC14.DonorCapes;
using Content.Shared.Preferences;
using NUnit.Framework;
using Robust.Shared.Prototypes;

namespace Content.Tests.Server.Database;

[TestFixture]
public sealed class DonorCapeProfilePersistenceTest
{
    [Test]
    public void SelectedDonorCapeSurvivesDatabaseRoundTrip()
    {
        var selectedCape = new ProtoId<RMCDonorCapePrototype>("rmc-donor-cape-16");
        var original = new HumanoidCharacterProfile()
            .WithSelectedDonorCape(selectedCape);

        var stored = DonorCapeProfilePersistence.Serialize(original.SelectedDonorCape);
        var restored = original.WithSelectedDonorCape(DonorCapeProfilePersistence.Deserialize(stored));

        Assert.Multiple(() =>
        {
            Assert.That(stored, Is.EqualTo("rmc-donor-cape-16"));
            Assert.That(restored.SelectedDonorCape, Is.EqualTo(selectedCape));
            Assert.That(restored.MemberwiseEquals(original), Is.True);
        });
    }

    [Test]
    public void ProfileModelStoresSelectedDonorCapeColumn()
    {
        var property = typeof(Profile).GetProperty(
            nameof(Profile.SelectedDonorCape),
            BindingFlags.Public | BindingFlags.Instance);
        var column = property?.GetCustomAttribute<ColumnAttribute>();

        Assert.Multiple(() =>
        {
            Assert.That(property, Is.Not.Null);
            Assert.That(column?.Name, Is.EqualTo("selected_donor_cape"));
        });
    }
}

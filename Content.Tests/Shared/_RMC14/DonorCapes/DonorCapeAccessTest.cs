using Content.Shared._RMC14.DonorCapes;
using Content.Shared._RMC14.LinkAccount;
using Content.Shared.Preferences;
using NUnit.Framework;
using Robust.Shared.Prototypes;

namespace Content.Tests.Shared._RMC14.DonorCapes;

[TestFixture]
public sealed class DonorCapeAccessTest
{
    [TestCase(1, 1, true)]
    [TestCase(1, 4, true)]
    [TestCase(3, 3, true)]
    [TestCase(3, 4, true)]
    [TestCase(4, 4, true)]
    [TestCase(4, 3, false)]
    [TestCase(5, 4, false)]
    public void TierPriorityControlsCapeAccess(int patronPriority, int requiredPriority, bool expected)
    {
        var tier = new SharedRMCPatronTier(false, false, false, false, false, false, "Test", patronPriority);

        Assert.That(DonorCapeAccess.HasAccess(tier, requiredPriority), Is.EqualTo(expected));
    }

    [Test]
    public void MissingTierDoesNotGrantCapeAccess()
    {
        Assert.That(DonorCapeAccess.HasAccess(null, 1), Is.False);
    }

    [Test]
    public void SelectedCapeIsCopiedAndIncludedInProfileEquality()
    {
        var selected = new ProtoId<RMCDonorCapePrototype>("rmc-donor-cape-16");
        var profile = new HumanoidCharacterProfile().WithSelectedDonorCape(selected);
        var clone = profile.Clone();

        Assert.Multiple(() =>
        {
            Assert.That(profile.SelectedDonorCape, Is.EqualTo(selected));
            Assert.That(clone.SelectedDonorCape, Is.EqualTo(selected));
            Assert.That(profile.MemberwiseEquals(clone), Is.True);
            Assert.That(profile.MemberwiseEquals(new HumanoidCharacterProfile()), Is.False);
        });
    }
}

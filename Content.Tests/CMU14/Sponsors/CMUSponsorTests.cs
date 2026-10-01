using System;
using System.IO;
using Content.Shared._RMC14.LinkAccount;
using Content.Shared.CMU14.Sponsors;
using NUnit.Framework;

namespace Content.Tests.CMU14.Sponsors;

[TestFixture]
public sealed class CMUSponsorTests
{
    private static SharedRMCPatronTier Tier(int priority) => new(true, priority <= 6, priority <= 5,
        priority <= 4, priority <= 3, priority <= 1, $"tier-{priority}", priority);

    [TestCase(7, false, false, false, false)]
    [TestCase(6, true, false, false, false)]
    [TestCase(5, true, true, false, false)]
    [TestCase(4, true, true, true, false)]
    [TestCase(3, true, true, true, true)]
    [TestCase(1, true, true, true, true)]
    public void PerksFollowAllSixTiers(int priority, bool ghost, bool patch, bool cape, bool customCape)
    {
        var tier = Tier(priority);
        var empty = new CMUSponsorSettings();
        Assert.Multiple(() =>
        {
            Assert.That(CMUSponsorCatalog.TryValidate(tier, empty, empty with { Ghost = "flame" }, out _), Is.EqualTo(ghost));
            Assert.That(CMUSponsorCatalog.TryValidate(tier, empty, empty with { Patch = "peace", Inscription = "Remember us" }, out _), Is.EqualTo(patch));
            Assert.That(CMUSponsorCatalog.TryValidate(tier, empty, empty with { Cape = "01" }, out _), Is.EqualTo(cape));
            Assert.That(CMUSponsorCatalog.TryValidate(tier, empty, empty with { Cape = "16", Trim = "gold", Emblem = "skull" }, out _), Is.EqualTo(customCape));
        });
    }

    [Test]
    public void DowngradePreservesChoicesWithoutApplyingLockedPerks()
    {
        var saved = new CMUSponsorSettings("spark", "Memento", "branch", "16", "silver", "peace", "My figurine");
        Assert.That(CMUSponsorCatalog.TryValidate(null, saved, saved, out var restored), Is.True);
        Assert.That(restored, Is.EqualTo(saved));
        Assert.That(CMUSponsorCatalog.CanUseCape(null, saved.Cape), Is.False);
        Assert.That(CMUSponsorCatalog.CanUseCape(Tier(4), saved.Cape), Is.False);
        Assert.That(CMUSponsorCatalog.TryValidate(Tier(7), saved, saved with { Ghost = "flame" }, out _), Is.False);
        Assert.That(CMUSponsorCatalog.TryValidate(Tier(7), saved, new CMUSponsorSettings(), out _), Is.True);
    }

    [Test]
    public void MalformedChoicesAndLongOrMultilineTextAreRejected()
    {
        var empty = new CMUSponsorSettings();
        foreach (var request in new[]
        {
            empty with { Ghost = "MobAdminGhost" },
            empty with { Patch = "CMWeaponRifleM54C" },
            empty with { Cape = "1" },
            empty with { Cape = "17" },
            empty with { Trim = "rainbow" },
            empty with { Emblem = "unknown" },
            empty with { Inscription = new string('x', CMUSponsorCatalog.InscriptionLimit + 1) },
            empty with { Inscription = "first\nsecond" },
            empty with { FigurineDescription = new string('x', CMUSponsorCatalog.DescriptionLimit + 1) },
            empty with { Ghost = null! },
        })
            Assert.That(CMUSponsorCatalog.TryValidate(Tier(1), empty, request, out _), Is.False, request.ToString());
    }

    [Test]
    public void ColorCannotHideGhostAndIsUnavailableToColonist()
    {
        var empty = new CMUSponsorSettings();
        var transparent = empty with { GhostColor = 0x00010203 };
        Assert.That(CMUSponsorCatalog.TryValidate(Tier(7), empty, transparent, out _), Is.False);
        Assert.That(CMUSponsorCatalog.TryValidate(Tier(6), empty, transparent, out var saved), Is.True);
        Assert.That((uint)saved.GhostColor!.Value >> 24, Is.EqualTo(0x88));
        Assert.That((uint)saved.GhostColor!.Value & 0x00FFFFFF, Is.EqualTo(0x00010203));
    }

    [Test]
    public void ExistingTierFlagsRemainAuthoritative()
    {
        var disabled = Tier(1) with { GhostColor = false, NamedItems = false, Figurines = false, LobbyMessage = false };
        var request = new CMUSponsorSettings("spark", "text", "peace", "01");
        Assert.That(CMUSponsorCatalog.TryValidate(disabled, new(), request, out _), Is.False);
        Assert.That(CMUSponsorCatalog.CanUseCapes(disabled), Is.False);
        Assert.That(CMUSponsorCatalog.CanCustomizeCape(disabled), Is.False);
    }
}

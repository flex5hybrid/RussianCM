using System.Linq;
using Content.Client._RMC14.DonorCapes;
using NUnit.Framework;
using Robust.Shared.Utility;

namespace Content.Tests.Shared._RMC14.DonorCapes;

[TestFixture]
public sealed class DonorCapeLayoutTest
{
    private sealed record Cape(int Number, int RequiredPriority);

    [Test]
    public void SectionsAreOrderedByTierThenCapeNumber()
    {
        var capes = new[]
        {
            new Cape(16, 1),
            new Cape(8, 4),
            new Cape(3, 1),
            new Cape(5, 4),
            new Cape(1, 3),
        };

        var sections = DonorCapeLayout.BuildSections(
            capes,
            cape => cape.RequiredPriority,
            cape => cape.Number);

        Assert.That(sections.Select(section => section.RequiredPriority), Is.EqualTo(new[] { 4, 3, 1 }));
        Assert.That(sections[0].Capes.Select(cape => cape.Number), Is.EqualTo(new[] { 5, 8 }));
        Assert.That(sections[2].Capes.Select(cape => cape.Number), Is.EqualTo(new[] { 3, 16 }));
    }

    [Test]
    public void MissingPreviewFallsBackToStaticIcon()
    {
        var fallback = new SpriteSpecifier.Rsi(new("_RuMC14/DonorCapes/cape01.rsi"), "icon");
        var preview = new SpriteSpecifier.Rsi(new("_RuMC14/DonorCapes/cape01.rsi"), "equipped-NECK");

        Assert.Multiple(() =>
        {
            Assert.That(DonorCapePreview.GetPreview(preview, fallback), Is.SameAs(preview));
            Assert.That(DonorCapePreview.GetPreview(null, fallback), Is.SameAs(fallback));
        });
    }
}

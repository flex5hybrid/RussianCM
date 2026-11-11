using Content.Client._RMC14.DonorCapes;
using NUnit.Framework;
using Robust.Shared.Graphics.RSI;

namespace Content.Tests.Shared._RMC14.DonorCapes;

[TestFixture]
public sealed class DonorCapePreviewAnimationTest
{
    [Test]
    public void BackViewRangeSkipsTransitionFrames()
    {
        var range = DonorCapePreviewAnimation.GetBackViewFrameRange(15);

        Assert.That(range.Start, Is.EqualTo(2));
        Assert.That(range.Count, Is.EqualTo(11));
        Assert.That(range.Start + range.Count - 1, Is.EqualTo(12));
    }

    [Test]
    public void ShortAnimationsKeepAllFrames()
    {
        var range = DonorCapePreviewAnimation.GetBackViewFrameRange(4);

        Assert.That(range.Start, Is.EqualTo(0));
        Assert.That(range.Count, Is.EqualTo(4));
    }

    [Test]
    public void DirectionalStatesKeepAllRearFrames()
    {
        var range = DonorCapePreviewAnimation.GetBackViewFrameRange(12, RsiDirectionType.Dir4);

        Assert.That(range.Start, Is.EqualTo(0));
        Assert.That(range.Count, Is.EqualTo(12));
    }

    [Test]
    public void DirectionalRowsUseDedicatedFrontSidesAndRearAnimation()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DonorCapeDirectionalLayout.GetSourceFrame(RsiDirection.South, 0), Is.EqualTo(0));
            Assert.That(DonorCapeDirectionalLayout.GetSourceFrame(RsiDirection.South, 11), Is.EqualTo(0));
            Assert.That(DonorCapeDirectionalLayout.GetSourceFrame(RsiDirection.North, 0), Is.EqualTo(1));
            Assert.That(DonorCapeDirectionalLayout.GetSourceFrame(RsiDirection.North, 11), Is.EqualTo(12));
            Assert.That(DonorCapeDirectionalLayout.GetSourceFrame(RsiDirection.East, 0), Is.EqualTo(13));
            Assert.That(DonorCapeDirectionalLayout.GetSourceFrame(RsiDirection.West, 0), Is.EqualTo(14));
        });
    }
}

using System.Numerics;
using Content.Client.CMU14.Interface.Hud;
using NUnit.Framework;
using Robust.Shared.Maths;

namespace Content.IntegrationTests._CMU14.Hud;

[TestFixture]
public sealed class CmuHudLayoutSettingsTest
{
    [TestCase(0, 0, 400, 300)]
    [TestCase(1180, 10, 420, 458)]
    [TestCase(12, 600, 216, 290)]
    public void CapturedPanelUsesTheSameBottomOriginUvAsClyde(int x, int y, int width, int height)
    {
        var origin = Content.Client.CMU14.Interface.CmuCrtCapture.TextureOrigin(new Vector2i(x, y),
            new Vector2i(width, height), new Vector2i(1600, 900));
        Assert.That(origin.X * 1600, Is.EqualTo(x).Within(0.01));
        Assert.That((1 - origin.Y) * 900, Is.EqualTo(y + height).Within(0.01),
            "The bottom edge of the UI rectangle is the lower texture-UV origin.");
        Assert.That((1 - (origin.Y + height / 900f)) * 900, Is.EqualTo(y).Within(0.01));
    }

    [Test]
    public void IndependentProfilesRoundTripWithoutChangingPlacement()
    {
        var settings = CmuHudLayoutSettings.Parse("Default/chat:0.75:0.25:400:300;Separated/chat:1:0:0:0");
        var loaded = CmuHudLayoutSettings.Parse(settings.Serialize());
        Assert.That(loaded.Items.Count, Is.EqualTo(2));
        Assert.That(loaded.Items["Default/chat"], Is.EqualTo(settings.Items["Default/chat"]));
        Assert.That(loaded.Items["Separated/chat"], Is.EqualTo(settings.Items["Separated/chat"]));
    }

    [Test]
    public void CorruptPreferencesCannotMoveControlsOffScreen()
    {
        var settings = CmuHudLayoutSettings.Parse("broken;Default/chat:NaN:0:1:2;Default/status:-5:20:9000:9000;Default/menu:0:Infinity:0:0");
        Assert.That(settings.Items.Count, Is.EqualTo(1));
        var rect = CmuHudLayoutSettings.Resolve(settings.Items["Default/status"], new Vector2(800, 600), new Vector2(64));
        Assert.That(rect.TopLeft, Is.EqualTo(Vector2.Zero));
        Assert.That(rect.Size, Is.EqualTo(new Vector2(800, 600)));
    }

    [TestCase(1920, 1080)]
    [TestCase(640, 480)]
    [TestCase(100, 100)]
    public void BottomRightAnchorSurvivesResolutionAndInventorySizeChanges(int width, int height)
    {
        var placement = new CmuHudLayoutSettings.Placement(1, 1, 0, 0);
        foreach (var natural in new[] { new Vector2(64), new Vector2(216, 340), new Vector2(900) })
        {
            var available = new Vector2(width, height);
            var rect = CmuHudLayoutSettings.Resolve(placement, available, natural);
            Assert.That(rect.BottomRight, Is.EqualTo(available));
            Assert.That(rect.Left, Is.GreaterThanOrEqualTo(0));
            Assert.That(rect.Top, Is.GreaterThanOrEqualTo(0));
        }
    }
}

using Content.Client._RMC14.Announce.Styling;
using NUnit.Framework;
using Robust.Shared.Maths;

namespace Content.IntegrationTests._CMU14.Hud;

[TestFixture]
public sealed class CmuAnnouncementMarkupTest
{
    [TestCase("Объявление[/font][/font]")]
    [TestCase("[font size=20]Первая строка")]
    [TestCase("Вторая строка[/font]")]
    [TestCase("[font size=20][bold]Текст[/font][/bold]")]
    [TestCase("[bold]Важно[/bold] [font size=16]Сообщение[/font]")]
    [TestCase("Печать незаконченного [font size=")]
    public void AnnouncementLineCannotPopItsOuterFontOrColor(string text)
    {
        var message = AnnouncementStyling.CreateFormattedMessage(text, 18, Color.Green);
        var open = new Stack<string>();
        foreach (var node in message)
        {
            if (node.Name == null)
                continue;
            if (!node.Closing)
            {
                open.Push(node.Name);
                continue;
            }
            Assert.That(open.Count, Is.GreaterThan(0), "An orphan closing node would underflow the rich-text context.");
            Assert.That(open.Pop(), Is.EqualTo(node.Name));
        }
        Assert.That(open, Is.Empty);
    }
}

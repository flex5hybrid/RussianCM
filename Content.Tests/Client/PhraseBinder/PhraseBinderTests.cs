using System;
using System.IO;
using System.Linq;
using Content.Client.CMU14.PhraseBinder;
using Content.Shared.Chat;
using NUnit.Framework;

namespace Content.Tests.Client.PhraseBinder;

[TestFixture]
public sealed class PhraseBinderTests
{
    [TestCase(1)]
    [TestCase(8)]
    [TestCase(9)]
    [TestCase(37)]
    public void CustomSlotCountAndEmptySlotsRoundTrip(int count)
    {
        var slots = Enumerable.Range(0, count)
            .Select(i => i % 2 == 0
                ? new BoundPhrase($"Слот {i}", "Нужен медик! [bold] \"цитата\" \\ :m ;", (PhraseChannel) (i % 4))
                : new BoundPhrase())
            .ToArray();
        Assert.That(PhraseBinderData.Deserialize(PhraseBinderData.Serialize(slots)), Is.EqualTo(slots));
    }

    [TestCase(0, 1)]
    [TestCase(1, 1)]
    [TestCase(8, 1)]
    [TestCase(9, 2)]
    [TestCase(16, 2)]
    [TestCase(17, 3)]
    [TestCase(100, 13)]
    public void PageCountIncludesPartialFinalPage(int count, int expected)
    {
        Assert.That(PhraseBinderData.PageCount(count), Is.EqualTo(expected));
    }

    [TestCase(-1, 17, 2)]
    [TestCase(3, 17, 0)]
    [TestCase(1, 1, 0)]
    [TestCase(-1, 0, 0)]
    public void ScrollingWrapsInBothDirections(int page, int count, int expected)
    {
        Assert.That(PhraseBinderData.WrapPage(page, count), Is.EqualTo(expected));
    }

    [Test]
    public void PartialPagesCoverEveryPhraseExactlyOnce()
    {
        const int count = 37;
        var indexes = Enumerable.Range(0, PhraseBinderData.PageCount(count))
            .SelectMany(page => Enumerable.Range(page * PhraseBinderData.PageSize, PhraseBinderData.PageSize))
            .Where(index => index < count);
        Assert.That(indexes, Is.EqualTo(Enumerable.Range(0, count)));
    }

    [TestCase("Медика!", ";Медика!")]
    [TestCase(";Общий", ";Общий")]
    [TestCase(":m Медик", ":m Медик")]
    [TestCase(".m Медик", ".m Медик")]
    public void RadioAddsOnlyMissingPrefix(string input, string expected)
    {
        Assert.That(new BoundPhrase(Text: input, Channel: PhraseChannel.Radio).MessageText, Is.EqualTo(expected));
    }

    [TestCase(PhraseChannel.Local, ChatSelectChannel.Local)]
    [TestCase(PhraseChannel.Whisper, ChatSelectChannel.Whisper)]
    [TestCase(PhraseChannel.Radio, ChatSelectChannel.Radio)]
    [TestCase(PhraseChannel.Emote, ChatSelectChannel.Emotes)]
    public void SendingRequiresChannelPermissionAndRespectsLength(PhraseChannel channel, ChatSelectChannel chat)
    {
        var phrase = new BoundPhrase(Text: "Hello", Channel: channel);
        Assert.That(phrase.ChatChannel, Is.EqualTo(chat));
        Assert.That(phrase.CanSend(chat, phrase.MessageText.Length), Is.True);
        Assert.That(phrase.CanSend(chat, phrase.MessageText.Length - 1), Is.False);
        Assert.That(phrase.CanSend(ChatSelectChannel.OOC, 1000), Is.False);
        Assert.That(new BoundPhrase(Text: " ").CanSend(chat, 1000), Is.False);
        Assert.That(new BoundPhrase(Text: "Hello", Channel: (PhraseChannel) 255)
            .CanSend(ChatSelectChannel.Console | chat, 1000), Is.False);
    }

    [Test]
    public void TextIsKeptLiteralRatherThanTreatedAsCommands()
    {
        const string text = "\"; quit; say \"hello\" \\ [bold] :m";
        foreach (var channel in new[] { PhraseChannel.Local, PhraseChannel.Whisper, PhraseChannel.Emote })
            Assert.That(new BoundPhrase(Text: text, Channel: channel).MessageText, Is.EqualTo(text));
    }

    [Test]
    public void UnknownChannelCannotSelectConsoleAndBadSlotsKeepTheirPositions()
    {
        var slots = PhraseBinderData.Deserialize("- title: Bad channel\n  text: Hello\n  channel: Console\n- invalid\n- text: Third\n  channel: 255\n");
        Assert.That(slots.Length, Is.EqualTo(3));
        Assert.That(slots[0].ChatChannel, Is.EqualTo(ChatSelectChannel.Local));
        Assert.That(slots[1], Is.EqualTo(new BoundPhrase()));
        Assert.That(slots[2].Text, Is.EqualTo("Third"));
        Assert.That(slots[2].ChatChannel, Is.EqualTo(ChatSelectChannel.Local));
    }

    [Test]
    public void EmptyOrInvalidFileIsHandledWithoutAZeroPageWheel()
    {
        Assert.That(PhraseBinderData.Deserialize("").Length, Is.EqualTo(PhraseBinderData.DefaultSlotCount));
        Assert.That(PhraseBinderData.Deserialize("[]").Length, Is.EqualTo(PhraseBinderData.DefaultSlotCount));
        Assert.Throws<InvalidDataException>(() => PhraseBinderData.Deserialize("invalid: mapping"));
    }

    [Test]
    public void PastedMultilineTextBecomesOneMessage()
    {
        Assert.That(PhraseBinderData.CleanLine("  Первая\r\nВторая\nТретья  "), Is.EqualTo("Первая  Вторая Третья"));
    }
}

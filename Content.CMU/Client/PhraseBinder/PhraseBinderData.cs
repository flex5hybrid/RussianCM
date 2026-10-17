using System.IO;
using Content.Shared.Chat;
using YamlDotNet.RepresentationModel;

namespace Content.Client.CMU14.PhraseBinder;

public enum PhraseChannel : byte
{
    Local,
    Whisper,
    Radio,
    Emote,
}

public sealed record BoundPhrase(string Title = "", string Text = "", PhraseChannel Channel = PhraseChannel.Local)
{
    public ChatSelectChannel ChatChannel => Channel switch
    {
        PhraseChannel.Local => ChatSelectChannel.Local,
        PhraseChannel.Whisper => ChatSelectChannel.Whisper,
        PhraseChannel.Radio => ChatSelectChannel.Radio,
        PhraseChannel.Emote => ChatSelectChannel.Emotes,
        _ => ChatSelectChannel.None,
    };

    public bool CanSend(ChatSelectChannel allowedChannels, int maxLength) =>
        !string.IsNullOrWhiteSpace(Text) && (allowedChannels & ChatChannel) != 0 && MessageText.Length <= maxLength;

    // Radio and local chat share the say command. Preserve explicit radio prefixes (e.g. :m).
    // No console/chat command parsing is performed on player-authored phrases.
    public string MessageText => Channel == PhraseChannel.Radio &&
                                 !Text.StartsWith(SharedChatSystem.RadioCommonPrefix) &&
                                 !Text.StartsWith(SharedChatSystem.RadioChannelPrefix) &&
                                 !Text.StartsWith(SharedChatSystem.RadioChannelAltPrefix)
        ? SharedChatSystem.RadioCommonPrefix + Text
        : Text;
}

/// <summary>Local phrase file format. Slot positions are preserved, including empty slots.</summary>
public static class PhraseBinderData
{
    public const int DefaultSlotCount = 8;
    public const int PageSize = 8;
    public const int MaxTitleLength = 24;

    public static BoundPhrase[] EmptySlots()
    {
        var slots = new BoundPhrase[DefaultSlotCount];
        Array.Fill(slots, new BoundPhrase());
        return slots;
    }

    public static string CleanLine(string text) => text.Replace('\r', ' ').Replace('\n', ' ').Trim();

    public static int PageCount(int count) => Math.Max(1, (count + PageSize - 1) / PageSize);

    public static int WrapPage(int page, int count)
    {
        var pages = PageCount(count);
        return (page % pages + pages) % pages;
    }

    public static BoundPhrase[] Deserialize(string yaml)
    {
        var stream = new YamlStream();
        using var reader = new StringReader(yaml);
        stream.Load(reader);
        if (stream.Documents.Count == 0)
            return EmptySlots();

        if (stream.Documents[0].RootNode is not YamlSequenceNode sequence)
            throw new InvalidDataException("Expected a phrase slot sequence.");

        if (sequence.Children.Count == 0)
            return EmptySlots();

        var slots = new BoundPhrase[sequence.Children.Count];
        Array.Fill(slots, new BoundPhrase());
        for (var i = 0; i < sequence.Children.Count; i++)
        {
            if (sequence.Children[i] is not YamlMappingNode map)
                continue;

            string Value(string key)
            {
                // Enumerate through the sandbox-approved API; Children exposes IOrderedDictionary
                // in the bundled YamlDotNet version, which is not on the content whitelist.
                foreach (var pair in map)
                {
                    if (pair.Key is YamlScalarNode scalar && scalar.Value == key)
                        return (pair.Value as YamlScalarNode)?.Value ?? string.Empty;
                }

                return string.Empty;
            }

            if (!Enum.TryParse<PhraseChannel>(Value("channel"), out var channel) || !Enum.IsDefined(channel))
                channel = PhraseChannel.Local;

            var title = CleanLine(Value("title"));
            if (title.Length > MaxTitleLength)
                title = title[..MaxTitleLength];
            slots[i] = new BoundPhrase(title, CleanLine(Value("text")), channel);
        }

        return slots;
    }

    public static string Serialize(IReadOnlyList<BoundPhrase> slots)
    {
        var sequence = new YamlSequenceNode();
        for (var i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            sequence.Add(new YamlMappingNode
            {
                { "title", slot.Title },
                { "text", slot.Text },
                { "channel", slot.Channel.ToString() },
            });
        }

        using var writer = new StringWriter();
        new YamlStream(new YamlDocument(sequence)).Save(writer, false);
        return writer.ToString();
    }
}

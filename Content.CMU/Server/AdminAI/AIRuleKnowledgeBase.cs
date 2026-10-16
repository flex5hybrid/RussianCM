using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Content.Server.CMU14.AdminAI;

public sealed record AIRuleDocument(string GuideId, string Title, string Path, byte[] Content);
public sealed record AIRulePassage(string GuideId, string Title, string Version, string Fragment, string Path = "", string Section = "");

/// <summary>Immutable bounded index of published local guidebooks. No network or privileged data.</summary>
public sealed class AIRuleKnowledgeBase
{
    public const int MaxDocumentBytes = 65536;
    public const int MaxDocuments = 384;
    public const int MaxPassages = 8192;
    public const int FragmentChars = 600;
    private static readonly Regex Words = new(@"[\p{L}\p{N}]{3,}", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
    private static readonly Regex Markup = new(@"\[(?:textlink=""([^""]+)""[^\]]*|[^\]]+)\]", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "как", "где", "чем", "что", "это", "для", "при", "или", "его", "она", "они", "мне", "мой", "свой", "ваш",
        "этого", "того", "если", "когда", "какой", "какие", "можно", "нужно", "меня", "почему", "чтобы", "такое",
        "пожалуйста", "подскажите", "работает", "сделать", "использовать", "лечить", "вылечить", "with", "this", "that", "have", "what", "how",
    };
    private static readonly string[] Endings = { "иями", "ями", "ами", "ого", "ему", "ение", "ения", "ать", "ять", "ить", "ться", "ется", "ются", "ете", "ите", "ов", "ев", "ая", "яя", "ый", "ий", "ой", "ые", "ие", "ом", "ем", "ах", "ях", "ам", "ям", "ей", "ою", "ею", "ит", "ет", "ат", "ут", "а", "я", "ы", "и", "у", "ю", "е", "ь" };
    private static readonly HashSet<string> HelpWords = new(StringComparer.Ordinal)
    {
        "привет", "здравствуйте", "здравствуй", "помогите", "помоги", "пожалуйста", "нужна", "нужен", "нужны",
        "помощь", "мне", "нам", "добрый", "доброе", "день", "вечер", "утро", "приветствую",
    };
    private static readonly HashSet<string> FollowUpWords = new(StringComparer.Ordinal)
    {
        "я", "мы", "мне", "не", "нет", "да", "знаю", "знаем", "понял", "поняла", "понимаю", "понимаем",
        "новичок", "новичка", "новенький", "новенькая", "впервые", "дальше", "затем", "потом", "а", "и", "но",
        "это", "этот", "эта", "эти", "этого", "этим", "тут", "там", "где", "как", "что", "чем", "какой",
        "какая", "какие", "делать", "сделать", "найти", "так", "теперь", "можно", "пожалуйста", "подробнее",
        "объясни", "объясните", "помогите", "помоги", "первый", "раз",
    };
    private readonly Entry[] _entries;
    private readonly Dictionary<string, int> _frequency = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Posting[]> _postings = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AIRulePassage[]> _documents = new(StringComparer.Ordinal);
    public int Count => _entries.Length;
    public int Documents { get; }

    public AIRuleKnowledgeBase(IEnumerable<AIRuleDocument> documents)
    {
        var entries = new List<Entry>(); var accepted = 0;
        foreach (var document in documents.Take(MaxDocuments))
        {
            if (!IsAllowedPath(document.Path) || document.Content.Length is 0 or > MaxDocumentBytes
                || document.GuideId.Length is < 1 or > 128 || document.Title.Length > 256) continue;
            try
            {
                using var stream = new MemoryStream(document.Content, false);
                using var reader = XmlReader.Create(stream, new XmlReaderSettings
                { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = MaxDocumentBytes });
                var xml = XDocument.Load(reader);
                if (xml.Root?.Name.LocalName != "Document") continue;
                var text = Render(xml.Root);
                var lines = text.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim()).ToArray();
                var title = lines.FirstOrDefault(line => line.StartsWith('#'))?.TrimStart('#', ' ') ?? document.Title;
                var version = Convert.ToHexString(SHA256.HashData(document.Content)).ToLowerInvariant();
                var section = title;
                foreach (var paragraph in lines)
                {
                    if (paragraph.StartsWith('#')) { section = paragraph.TrimStart('#', ' '); continue; }
                    if (paragraph.Length < 20) continue;
                    for (var offset = 0; offset < paragraph.Length && entries.Count < MaxPassages;)
                    {
                        var length = Math.Min(FragmentChars, paragraph.Length - offset);
                        if (offset + length < paragraph.Length)
                        {
                            var boundary = paragraph.LastIndexOf(' ', offset + length - 1, length);
                            if (boundary >= offset + FragmentChars / 2) length = boundary - offset;
                        }
                        var fragment = paragraph.Substring(offset, length).Trim();
                        entries.Add(new(new(document.GuideId, title, version, fragment, document.Path, section), Tokenize(fragment), Tokenize(section)));
                        offset += Math.Max(1, length);
                    }
                }
                accepted++;
            }
            catch (Exception exception) when (exception is XmlException or IOException or RegexMatchTimeoutException) { }
        }
        Documents = accepted; _entries = entries.ToArray();
        foreach (var entry in _entries)
            foreach (var term in entry.Terms) _frequency[term] = _frequency.GetValueOrDefault(term) + 1;
        var postings = new Dictionary<string, List<Posting>>(StringComparer.Ordinal);
        for (var index = 0; index < _entries.Length; index++)
            foreach (var term in _entries[index].Terms)
            {
                if (!postings.TryGetValue(term, out var list)) postings[term] = list = new();
                var weight = Math.Log(1 + (double)(_entries.Length + 1) / (_frequency[term] + 1));
                list.Add(new(index, weight * (_entries[index].Heading.Contains(term) ? 1.5 : 1)));
            }
        foreach (var pair in postings) _postings[pair.Key] = pair.Value.ToArray();
        foreach (var group in _entries.GroupBy(entry => entry.Passage.GuideId, StringComparer.Ordinal))
            _documents[group.Key] = group.Select(entry => entry.Passage).ToArray();
    }

    public AIRulePassage[] Search(string query, int maxChars = 1500)
    {
        // A greeting/help request has no topic. Matching "help" in lore would invent its subject.
        if (maxChars < 100 || IsGenericHelp(query)) return Array.Empty<AIRulePassage>();
        var tokens = Tokenize(query.Length > 4096 ? query[..4096] : query);
        var scores = new Dictionary<int, double>();
        foreach (var token in tokens)
            if (_postings.TryGetValue(token, out var postings))
                foreach (var posting in postings) scores[posting.Index] = scores.GetValueOrDefault(posting.Index) + posting.Weight;
        var ranked = new List<(int Index, double Score)>(3);
        foreach (var candidate in scores)
        {
            var position = 0;
            while (position < ranked.Count && Compare(candidate.Key, candidate.Value, ranked[position].Index, ranked[position].Score) >= 0) position++;
            if (position >= 3) continue;
            ranked.Insert(position, (candidate.Key, candidate.Value));
            if (ranked.Count > 3) ranked.RemoveAt(3);
        }
        var results = new List<AIRulePassage>(); var length = 0;
        foreach (var row in ranked)
        {
            var passage = _entries[row.Index].Passage;
            if (length + passage.Fragment.Length > maxChars) continue;
            results.Add(passage); length += passage.Fragment.Length;
        }
        return results.ToArray();
    }

    public static bool IsGenericHelp(string query)
    {
        if (query.Length is < 1 or > 160 || query.Contains('\n') || query.Contains('\r')) return false;
        var words = Words.Matches(query.ToLowerInvariant());
        return words.Count > 0 && words.All(word => HelpWords.Contains(word.Value));
    }

    // Only private drafts use dialogue retrieval. Automatic FAQ still examines the current question alone.
    public AIRulePassage[] SearchDraft(AIDraftTicket ticket, int maxChars)
    {
        var question = ticket.Question.Length > 0 ? ticket.Question : ticket.Conversation;
        if (!IsFollowUp(question) || ticket.PlayerMessages.Length < 2)
        {
            var current = AIResponsePolicy.FindAnswer(question, this);
            if (current.Length > 0) return FitDraftSources(current, maxChars);
            // A single generic term must not import unrelated lore or rules into a complaint.
            var direct = Search(question, maxChars);
            if (direct.Length == 0 || !IsMechanicsPath(direct[0].Path) || !HasSpecificMatch(question, direct[0]))
                return Array.Empty<AIRulePassage>();
            return direct.Where(source => source.GuideId == direct[0].GuideId).ToArray();
        }
        foreach (var previous in ticket.PlayerMessages.Reverse().Skip(1))
        {
            if (!AIResponsePolicy.IsInformationalQuestion(previous) || IsGenericHelp(previous) || IsFollowUp(previous)) continue;
            var routed = AIResponsePolicy.FindAnswer(previous, this);
            if (routed.Length > 0)
            {
                // Unknown injuries need the published examination step, not guessed treatment options.
                if (routed[0].GuideId == "CMUGuideMedicalInfantry"
                    && (question.Contains("не знаю", StringComparison.OrdinalIgnoreCase) || question.Contains("новичок", StringComparison.OrdinalIgnoreCase)))
                    return FitDraftSources([routed[0]], maxChars);
                if (routed[0].GuideId == "AU14CommsHeadsets")
                    return FitDraftSources([routed[0]], maxChars);
                return FitDraftSources(routed, maxChars);
            }
            var sources = Search(previous, maxChars);
            if (sources.Length > 0 && HasSpecificMatch(previous, sources[0]))
                return sources.Where(source => source.GuideId == sources[0].GuideId).ToArray();
        }
        return Array.Empty<AIRulePassage>();
    }

    private static AIRulePassage[] FitDraftSources(AIRulePassage[] passages, int maxChars)
    {
        var selected = new List<AIRulePassage>();
        var used = 0;
        foreach (var passage in passages)
        {
            var cost = passage.Title.Length + passage.Fragment.Length + 3;
            if (used + cost > maxChars) break;
            selected.Add(passage); used += cost;
        }
        return selected.ToArray();
    }

    public static string DraftQuestion(AIDraftTicket ticket)
    {
        if (!IsFollowUp(ticket.Question)) return ticket.Question;
        return ticket.PlayerMessages.Reverse().Skip(1).FirstOrDefault(previous => AIResponsePolicy.IsInformationalQuestion(previous)
            && !IsGenericHelp(previous) && !IsFollowUp(previous)) ?? ticket.Question;
    }

    private static bool IsFollowUp(string query)
    {
        if (query.Length is < 1 or > 160 || query.Contains('\n') || query.Contains('\r') || IsGenericHelp(query)) return false;
        var words = Words.Matches(query.ToLowerInvariant());
        return words.Count > 0 && words.All(word => FollowUpWords.Contains(word.Value));
    }

    // A source must match a substantive term in its actual text, not just its document heading.
    public bool HasSpecificMatch(string query, AIRulePassage source)
    {
        var tokens = Tokenize(query); var terms = Tokenize(source.Fragment);
        return tokens.Count > 0 && tokens.Count(terms.Contains) > tokens.Count / 2
            && tokens.Any(term => terms.Contains(term) && _frequency.TryGetValue(term, out var frequency)
            && frequency <= Math.Max(32, Count / 12));
    }

    public AIRulePassage[] Browse(string guideId, string section, int limit = 3) => _documents.TryGetValue(guideId, out var passages)
        ? passages.Where(passage => passage.Section == section).Take(Math.Clamp(limit, 1, 3)).ToArray() : Array.Empty<AIRulePassage>();

    public AIRulePassage? FindPublishedLine(string guideId, string phrase) => _documents.TryGetValue(guideId, out var passages)
        ? passages.FirstOrDefault(passage => passage.Fragment.Contains(phrase, StringComparison.Ordinal)) : null;

    private int Compare(int left, double leftScore, int right, double rightScore)
    {
        var score = rightScore.CompareTo(leftScore);
        if (score != 0) return score;
        var guide = StringComparer.Ordinal.Compare(_entries[left].Passage.GuideId, _entries[right].Passage.GuideId);
        return guide != 0 ? guide : left.CompareTo(right);
    }

    public static bool IsAllowedPath(string path) => path.Length <= 256 && !path.Contains("..", StringComparison.Ordinal)
        && !path.Contains('\\') && !path.Contains('\0') && path.EndsWith(".xml", StringComparison.Ordinal)
        && (path.StartsWith("/ServerInfo/Guidebook/CMU14/", StringComparison.Ordinal)
            || path.StartsWith("/ServerInfo/Guidebook/_RuCM/GOVFOR/", StringComparison.Ordinal));

    public static bool IsMechanicsPath(string path) => IsAllowedPath(path)
        && !path.Contains("/Rules/", StringComparison.Ordinal) && !path.Contains("/SOP/", StringComparison.Ordinal)
        && !path.Contains("/UCMJ/", StringComparison.Ordinal) && !path.Contains("/CCLaw/", StringComparison.Ordinal)
        && !path.Contains("/Lore/", StringComparison.Ordinal) && !path.Contains("Law", StringComparison.Ordinal);

    private static string Render(XElement element)
    {
        if (element.Name.LocalName == "Table")
        {
            if (!int.TryParse((string?)element.Attribute("Columns"), out var columns) || columns is < 1 or > 8) return "";
            var cells = element.Elements().Select(cell => string.Join(" ", Render(cell).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))).ToArray();
            var rows = new StringBuilder();
            // Keep each value tied to its header. Do not drop short cells such as a dose or a range.
            for (var offset = columns; offset + columns <= cells.Length; offset += columns)
            {
                for (var column = 0; column < columns; column++)
                    rows.Append(cells[column]).Append(": ").Append(cells[offset + column]).Append("; ");
                rows.Append('\n');
            }
            return rows.ToString();
        }
        if (element.Name.LocalName == "GuideEntityEmbed") return "\n" + (string?)element.Attribute("Caption") + "\n";
        // Do not turn widget identifiers, attributes or executable UI instructions into knowledge.
        if (element.Name.LocalName is "GuideReagentEmbed" or "GuideComponentEmbed") return "";
        var text = new StringBuilder();
        foreach (var node in element.Nodes())
            if (node is XText value) text.Append(value.Value);
            else if (node is XElement child) text.Append('\n').Append(Render(child)).Append('\n');
        return Markup.Replace(text.ToString(), match => match.Groups[1].Success ? match.Groups[1].Value : "");
    }

    private static HashSet<string> Tokenize(string text) => Words.Matches(text.ToLower(CultureInfo.InvariantCulture).Replace('ё', 'е'))
        .Select(match => match.Value).Where(word => !StopWords.Contains(word)).Take(128).Select(Normalize).ToHashSet(StringComparer.Ordinal);

    private static string Normalize(string word)
    {
        // Local vocabulary aliases affect matching only; they never add statements to an excerpt.
        if (word.StartsWith("бинт", StringComparison.Ordinal) || word.StartsWith("перевяз", StringComparison.Ordinal)) return "марл";
        if (word.StartsWith("радиостанц", StringComparison.Ordinal) || word == "рация" || word == "рацию") return "радиостанц";
        if (word.StartsWith("дефиб", StringComparison.Ordinal) || word.StartsWith("реаним", StringComparison.Ordinal) || word.StartsWith("ожив", StringComparison.Ordinal)) return "реаним";
        if (word.Any(character => character is < 'а' or > 'я')) return word;
        foreach (var ending in Endings)
            if (word.Length - ending.Length >= 3 && word.EndsWith(ending, StringComparison.Ordinal)) return word[..^ending.Length];
        return word;
    }
    private sealed record Entry(AIRulePassage Passage, HashSet<string> Terms, HashSet<string> Heading);
    private readonly record struct Posting(int Index, double Weight);
}

public static class AIDraftPrompt
{
    public static AIInferenceRequest? Request(AIDraftTicket ticket, AIRulePassage[] sources, int maxChars, int outputTokens)
    {
        if (ticket.Dialogue.IsDefaultOrEmpty)
        {
            var legacy = Build(ticket, sources, maxChars);
            return legacy.Length == 0 ? null : new(ticket.Id, legacy, outputTokens);
        }
        var context = new StringBuilder("Опубликованные игровые источники (не свидетельства произошедшего):\n");
        foreach (var source in sources)
            context.Append(source.Title).Append(":\n").Append(source.Fragment).Append('\n');
        if (sources.Length == 0) context.Append("Источники отсутствуют.\n");
        var question = AIRuleKnowledgeBase.DraftQuestion(ticket);
        if (question != ticket.Question)
            context.Append("Вопрос из этой же переписки, который уточняет игрок: ").Append(question).Append('\n');
        var request = new AIInferenceRequest(ticket.Id, context.ToString(), outputTokens) { Dialogue = ticket.Dialogue };
        return request.IsInputValid(maxChars) ? request : null;
    }

    public static string Build(AIDraftTicket ticket, AIRulePassage[] sources, int maxChars)
    {
        // The transport supplies the system policy once; this body contains bounded case data only.
        var prompt = new StringBuilder("Переписка текущего обращения, от ранних реплик к последним:\n");
        prompt.Append(ticket.Conversation);
        prompt.Append("\nОпубликованные локальные источники (не свидетельства произошедшего):\n");
        foreach (var source in sources)
            prompt.Append(source.Title).Append(':').Append('\n').Append(source.Fragment).Append('\n');
        if (sources.Length == 0) prompt.Append("Источника нет: не утверждай правила или механику, предложи уточнение.\n");
        return prompt.Length <= maxChars ? prompt.ToString() : "";
    }
}

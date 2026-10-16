using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Content.Server.CMU14.AdminAI;

/// <summary>Automatic output is an attributable public excerpt, never unchecked model prose.</summary>
public static class AIResponsePolicy
{
    private static readonly Regex Question = new(@"^(?:привет[,! ]*|подскажите[, ]*|пожалуйста[, ]*)*(?:как|где|чем|что такое|зачем|какой|какая|какие|сколько|how|where|what)\b",
        RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
    private static readonly Regex Dispute = new(@"меня|мне\s+(?:нанес|нанёс|не\s+дают)|мой\s+(?:персонаж|убийца)|убил|убили|убий|гриф|жалоб|наруш|наказ|бан|кик|мут|винов|админ|игрок|никнейм|\bчит\w*|оскорб|написал|сказал|расстрел|арест|плен|тимкил|логи|заметк|промпт|игнорир|инструкц|ignore|prompt|punish|report|player|banned|killed|https?://|www\.",
        RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));

    public static bool IsInformationalQuestion(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 512 || text.Contains('\n') || text.Contains('\r')) return false;
        try { return Question.IsMatch(text.Trim()) && !Dispute.IsMatch(text); }
        catch (RegexMatchTimeoutException) { return false; }
    }

    public static AIRulePassage[] FindAnswer(string question, AIRuleKnowledgeBase knowledge)
    {
        if (!IsInformationalQuestion(question)) return Array.Empty<AIRulePassage>();
        var plain = question.Trim().TrimEnd('?', '.', '!').ToLowerInvariant();
        // Broad questions have explicit public-document routes, not invented treatment or arbitrary ROE.
        if (plain is "как вылечить морпеха" or "как лечить морпеха")
            return Lines(knowledge, "CMUGuideMedicalInfantry", "Осмотреть ранения", "Пакет гемостатической марли", "Мазь");
        if (plain is "как пользоваться радио" or "как пользоваться гарнитурой")
        {
            var lines = Lines(knowledge, "AU14CommsHeadsets", "Наденьте гарнитуру", "Гарнитура может использовать только сеть", "зависят от ретрансляторов");
            var example = knowledge.FindPublishedLine("AU14CommsHeadsets", ":h Контакт спереди");
            if (lines.Length == 0 || example == null) return Array.Empty<AIRulePassage>();
            lines[0] = lines[0] with { Fragment = lines[0].Fragment + "\n" + example.Fragment };
            return lines;
        }
        if (plain is "какие правила открытия огня" or "какие правила применения силы")
        {
            var caveat = knowledge.Browse("AU14SOPRulesofEngagement", "Правила применения силы", 1);
            var rules = knowledge.Browse("AU14SOPRulesofEngagement", "Применение силы", 3);
            // Keep the full published section and its command-override caveat together.
            return caveat.Length == 1 && rules.Length == 3
                ? new[] { caveat[0], rules[0] with { Fragment = string.Join("\n", rules.Select(source => source.Fragment)) } }
                : Array.Empty<AIRulePassage>();
        }
        var sources = knowledge.Search(question, 1200);
        if (sources.Length == 0 || !AIRuleKnowledgeBase.IsMechanicsPath(sources[0].Path)
            || !knowledge.HasSpecificMatch(question, sources[0])) return Array.Empty<AIRulePassage>();
        // Keep a single document. Mixing unrelated guides can turn a procedure into misleading advice.
        return sources.Where(source => source.GuideId == sources[0].GuideId).Take(2).ToArray();
    }

    public static string Excerpt(AIRulePassage[] sources) => string.Join("\n\n", sources.GroupBy(source => source.GuideId)
        .Select(group => group.First().Title + ":\n" + string.Join("\n", group.Select(source => source.Fragment))));

    private static AIRulePassage[] Lines(AIRuleKnowledgeBase knowledge, string guideId, params string[] phrases)
    {
        var lines = phrases.Select(phrase => knowledge.FindPublishedLine(guideId, phrase)).ToArray();
        return lines.All(line => line != null) ? lines.Select(line => line!).ToArray() : Array.Empty<AIRulePassage>();
    }

    public static bool IsMapQuestion(string question) => question.Trim().TrimEnd('?', '.', '!').ToLowerInvariant()
        is "на какой карте мы сейчас играем" or "какая сейчас карта" or "какая карта" or "на какой карте играем";

    public static bool NeedsClarification(string question)
    {
        if (question.Length is < 1 or > 512) return false;
        var text = question.ToLowerInvariant();
        return text.Contains("убили", StringComparison.Ordinal) || text.Contains("убил", StringComparison.Ordinal)
            || text.Contains("рдм", StringComparison.Ordinal) || text.Contains("жалоб", StringComparison.Ordinal)
            || text.Contains("забанили", StringComparison.Ordinal) || text.Contains("злоупотреб", StringComparison.Ordinal);
    }
}

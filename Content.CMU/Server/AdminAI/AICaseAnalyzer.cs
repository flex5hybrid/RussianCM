using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Content.Server.CMU14.AdminAI;

public sealed record AIContextRow(int Id, DateTime Date, string Type, string Message, Guid[] Players);
public sealed record AIContextQuery(int Round, DateTime After, DateTime Before, Guid[] Players, int MaxLogs, int MaxChars);
public sealed record AICaseReport(string Text, AIContextRow[] Evidence, bool Truncated);

/// <summary>Evidence is assembled from actual rows; the model cannot supply IDs, dates or disciplinary conclusions.</summary>
public static class AICaseAnalyzer
{
    public static bool Valid(AIContextQuery query) => query.Round >= 0 && query.Players.Length is >= 1 and <= 4
        && query.Players.All(player => player != Guid.Empty) && query.Players.Distinct().Count() == query.Players.Length
        && query.After.Kind == DateTimeKind.Utc && query.Before.Kind == DateTimeKind.Utc && query.Before >= query.After
        && query.Before - query.After <= TimeSpan.FromMinutes(30) && query.MaxLogs is >= 1 and <= 128
        && query.MaxChars is >= 4096 and <= 32768;

    public static AICaseReport Build(AIContextQuery query, IEnumerable<AIContextRow> rows, string claim, AIRulePassage[] rules,
        IReadOnlyDictionary<string, string> labels)
    {
        if (!Valid(query)) throw new ArgumentException("Invalid bounded evidence query.");
        var summaryLimit = Math.Min(4096, query.MaxChars / 2);
        var evidence = new List<AIContextRow>(); var seen = new HashSet<int>(); var used = 0; var truncated = false;
        foreach (var row in rows.Take(query.MaxLogs + 1).OrderBy(row => row.Date).ThenBy(row => row.Id))
        {
            // Producers use UTC; SQLite can return Unspecified ticks representing that same UTC instant.
            var date = DateTime.SpecifyKind(row.Date, DateTimeKind.Utc);
            if (row.Id < 0 || row.Date.Kind == DateTimeKind.Local || date < query.After || date > query.Before
                || !row.Players.Any(query.Players.Contains) || !seen.Add(row.Id)) continue;
            var message = Plain(row.Message, 512);
            if (message.Length == 0) continue;
            if (evidence.Count >= query.MaxLogs || used + message.Length + 100 > query.MaxChars - summaryLimit)
            { truncated = true; break; }
            used += message.Length + 100;
            evidence.Add(row with { Date = date, Type = Plain(row.Type, 64), Message = message, Players = row.Players.Take(16).ToArray() });
        }
        var report = new StringBuilder();
        var claimLimit = Math.Min(400, summaryLimit / 5);
        truncated |= claim.Length > claimLimit;
        var notice = "\n" + labels["truncated"];
        var tail = "\n\n" + labels["unknown"] + "\n\n" + labels["questions"] + "\n\n" + labels["draft"];
        report.Append(labels["claim"]).Append('\n').Append(Plain(claim, claimLimit));
        report.Append("\n\n").Append(labels["selection"]).Append(' ').Append(query.Round).Append(", ")
            .Append(Stamp(query.After)).Append(" — ").Append(Stamp(query.Before)).Append(". ").Append(labels["count"]).Append(' ').Append(evidence.Count).Append('.');
        report.Append('\n').Append(labels["participants"]).Append(' ').Append(string.Join(", ", query.Players));
        report.Append("\n\n").Append(labels["confirmed"]);
        foreach (var row in evidence.Take(3))
        {
            var line = $"\n{Reference(query.Round, row)} — {Stamp(row.Date)} — {row.Type}: {Plain(row.Message, 160)}";
            if (report.Length + line.Length + tail.Length + notice.Length > summaryLimit) { truncated = true; continue; }
            report.Append(line);
        }
        // Uncertainty and neutral questions remain visible even at the smallest permitted context budget.
        report.Append(tail);
        if (rules.Length > 0)
        {
            var header = "\n\n" + labels["sources"] + "\n";
            var added = false;
            foreach (var rule in rules.Take(3))
            {
                // Plain metadata must survive the same markup sanitizer used by the client.
                var block = $"{rule.Title}\n{rule.GuideId} sha256={rule.Version}\n{rule.Fragment}\n";
                if (report.Length + (added ? 0 : header.Length) + block.Length + notice.Length > summaryLimit)
                { truncated = true; continue; }
                if (!added) { report.Append(header); added = true; }
                report.Append(block);
            }
        }
        truncated |= evidence.Count >= query.MaxLogs;
        if (truncated && report.Length + notice.Length <= summaryLimit) report.Append(notice);
        return new(Plain(report.ToString(), summaryLimit), evidence.ToArray(), truncated || evidence.Count >= query.MaxLogs);
    }

    public static string Reference(int round, AIContextRow row) => $"{round}:{row.Id}";
    public static string Stamp(DateTime date) => date.ToString("yyyy-MM-dd HH:mm:ss.fff 'UTC'", CultureInfo.InvariantCulture);
    private static string Plain(string text, int limit)
    {
        var bounded = text.Length > limit ? text[..limit] : text;
        return Content.Shared.Administration.AdminAI.AdminAIDraftText.Sanitize(bounded);
    }
}

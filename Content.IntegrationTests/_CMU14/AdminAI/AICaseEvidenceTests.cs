using System;
using System.Linq;
using Content.Server.CMU14.AdminAI;
using NUnit.Framework;

namespace Content.IntegrationTests.CMU14.AdminAI;

[TestFixture]
public sealed class AICaseEvidenceTests
{
    private static readonly Guid Subject = Guid.NewGuid(), Other = Guid.NewGuid();
    private static readonly DateTime Center = new(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc);
    private static System.Collections.Generic.Dictionary<string, string> Labels()
        => System.IO.File.ReadAllLines(System.IO.Path.Combine(AdminAITestPaths.Repository(), "Content.CMU/Resources/Locale/ru-RU/CMU14/admin-ai.ftl"))
            .Where(line => line.StartsWith("cmu-admin-ai-report-"))
            .Select(line => line.Split(" = ", 2))
            .ToDictionary(parts => parts[0]["cmu-admin-ai-report-".Length..], parts => parts[1]);

    private static AIContextQuery Query(int maxLogs = 64, int maxChars = 24000) => new(37, Center.AddMinutes(-5), Center.AddMinutes(2), new[] { Subject }, maxLogs, maxChars);

    [Test]
    public void ChronologyReferencesOnlyActualRowsAndKeepsUtcTicks()
    {
        var report = AICaseAnalyzer.Build(Query(), new[]
        {
            new AIContextRow(92, Center, "Chat", "Участник утверждает, что его убили.", new[] { Subject }),
            new AIContextRow(91, Center.AddSeconds(-3), "Damage", "Системная запись урона.", new[] { Subject }),
            new AIContextRow(999, Center, "Damage", "Другой игрок, чужая выборка.", new[] { Other }),
            new AIContextRow(100, Center.AddHours(-1), "Damage", "За пределами интервала.", new[] { Subject }),
        }, "Я утверждаю, что меня убили.", Array.Empty<AIRulePassage>(), Labels());
        Assert.That(report.Evidence.Select(row => row.Id), Is.EqualTo(new[] { 91, 92 }));
        Assert.That(AICaseAnalyzer.Reference(37, report.Evidence[0]), Is.EqualTo("37:91"));
        Assert.That(report.Evidence[0].Date, Is.EqualTo(Center.AddSeconds(-3)));
        Assert.That(report.Text, Does.Contain("не установленный факт"));
        Assert.That(report.Text, Does.Contain("не истинность"));
        Assert.That(report.Text, Does.Not.Contain("37:999"));
    }

    [Test]
    public void EmptySelectionDoesNotProveNoIncidentAndNeverFabricatesEvidence()
    {
        var report = AICaseAnalyzer.Build(Query(), Array.Empty<AIContextRow>(), "РДМ", Array.Empty<AIRulePassage>(), Labels());
        Assert.That(report.Evidence, Is.Empty);
        Assert.That(report.Text, Does.Contain("не доказывает отсутствие события"));
        Assert.That(report.Text, Does.Contain("виновность"));
    }

    [Test]
    public void LimitsAndDuplicateIdsPreserveBoundedUntrustedData()
    {
        var rows = Enumerable.Range(0, 129).Select(index => new AIContextRow(index, Center, "Damage",
            "[cmdlink] https://invalid.example " + new string('я', 600), new[] { Subject })).ToArray();
        var report = AICaseAnalyzer.Build(Query(8, 4096), rows, new string('a', 2000), Array.Empty<AIRulePassage>(), Labels());
        Assert.That(report.Evidence.Length, Is.LessThanOrEqualTo(8));
        Assert.That(report.Evidence.Sum(row => row.Message.Length + 100) + report.Text.Length, Is.LessThanOrEqualTo(4096));
        Assert.That(report.Truncated, Is.True);
        Assert.That(report.Text, Does.Contain("не доказывает отсутствие события"));
        Assert.That(report.Evidence.All(row => !row.Message.Contains("https://") && !row.Message.Contains('[')), Is.True);
        var duplicate = AICaseAnalyzer.Build(Query(), new[] { rows[0], rows[0] }, "", Array.Empty<AIRulePassage>(), Labels());
        Assert.That(duplicate.Evidence, Has.Length.EqualTo(1));
    }

    [Test]
    public void LocalClockAndInvalidQueryCannotCreateMisdatedReferences()
    {
        var row = new AIContextRow(42, DateTime.SpecifyKind(Center, DateTimeKind.Local), "Damage", "A real log.", new[] { Subject });
        Assert.That(AICaseAnalyzer.Build(Query(), new[] { row }, "", Array.Empty<AIRulePassage>(), Labels()).Evidence, Is.Empty);
        Assert.That(AICaseAnalyzer.Valid(Query() with { Players = Array.Empty<Guid>() }), Is.False);
        Assert.That(AICaseAnalyzer.Valid(Query() with { Before = Center.AddHours(1) }), Is.False);
    }

    [Test]
    public void NeutralClarificationIsOncePerCaseAndDoesNotBecomeAFaqAnswer()
    {
        var store = new AISafeFaqSessionStore(); var caseId = Guid.NewGuid();
        var ticket = store.Begin(Subject, 37, TimeSpan.Zero, caseId)!;
        Assert.That(store.Complete(ticket, 37, TimeSpan.FromSeconds(1), true), Is.True);
        Assert.That(store.Begin(Subject, 37, TimeSpan.FromSeconds(20), caseId), Is.Null);
        store.Reset(false);
        Assert.That(store.Begin(Subject, 37, TimeSpan.FromSeconds(30), caseId), Is.Null);
        Assert.That(AIResponsePolicy.NeedsClarification("Меня убили без причины"), Is.True);
        Assert.That(AIResponsePolicy.NeedsClarification("Как пользоваться радио?"), Is.False);
    }

    [Test]
    public void SourceIdentitySurvivesSanitizationAndBudgetNeverCutsTheVersion()
    {
        var version = new string('a', 64);
        var rule = new AIRulePassage("ActualPublishedRule", "Published rule", version, new string('x', 600));
        var complete = AICaseAnalyzer.Build(Query(), Array.Empty<AIContextRow>(), "A claim", new[] { rule }, Labels());
        Assert.That(complete.Text, Does.Contain("ActualPublishedRule sha256=" + version));
        var bounded = AICaseAnalyzer.Build(Query(64, 4096), Enumerable.Range(0, 4).Select(id =>
            new AIContextRow(id, Center, "Damaged", new string('я', 512), new[] { Subject })), new string('я', 2000),
            new[] { rule, rule, rule }, Labels());
        Assert.That(bounded.Text, Does.Contain("не доказывает отсутствие события"));
        Assert.That(bounded.Text, Does.Contain("Уточняющие вопросы"));
        if (bounded.Text.Contains("sha256=", StringComparison.Ordinal))
            Assert.That(bounded.Text, Does.Contain("sha256=" + version));
        Assert.That(bounded.Text.Length, Is.LessThanOrEqualTo(2048));
    }
}

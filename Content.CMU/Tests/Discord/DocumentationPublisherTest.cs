using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.CMU14.Discord;
using Linguini.Syntax.Parser;
using NUnit.Framework;

namespace Content.Tests.CMU14.Discord;

[TestFixture]
public sealed class DocumentationPublisherTest
{
    private const string Webhook = "https://discord.com/api/webhooks/123/test-token";
    private static readonly DocumentationPresentation Presentation = new("RuCMU", "Rules", "Guides",
        "Server rules.", "CMU guides.", "Contents", "Source");

    [Test]
    public void DiscordTranslationsAreValidAndHaveMatchingKeys()
    {
        var keys = new List<string[]>();
        foreach (var culture in new[] { "en-US", "ru-RU" })
        {
            var path = Path.Combine(RepositoryRoot(), "Content.CMU", "Resources", "Locale", culture,
                "CMU14", "discord", "documentation.ftl");
            var source = File.ReadAllText(path);
            var resource = new LinguiniParser(source).Parse();
            Assert.That(resource.Errors, Is.Empty, path);
            keys.Add(source.Split('\n').Where(p => p.StartsWith("cmu-discord-docs-"))
                .Select(p => p.Split('=')[0].Trim()).OrderBy(p => p).ToArray());
        }
        Assert.That(keys[0], Is.EqualTo(keys[1]));
    }

    [Test]
    public void RendererKeepsExceptionsNestedListsTablesAndLinks()
    {
        var text = DocumentationRenderer.Render("""
            <Document>
            #Правила
            - Основное правило.
              - Исключение: A & B.
            <Table Columns="2">
              <Box>[bold]Нарушение[/bold]</Box><Box>Наказание</Box>
              <Box>Кража</Box><Box>5 минут</Box>
            </Table>
            [textlink="Подробнее" link="Other"] [color=red][bold]Важно[/bold][/color]
            <GuideEntityEmbed Entity="Test"/>
            </Document>
            """, _ => "Аптечка — описание");
        text = DocumentationRenderer.Markdown(text, _ => "https://discord.com/channels/1/2/3");
        Assert.That(text, Does.Contain("# Правила"));
        Assert.That(text, Does.Contain("  - Исключение: A & B."));
        Assert.That(text, Does.Contain("**Нарушение**: Кража\nНаказание: 5 минут"));
        Assert.That(text, Does.Contain("[Подробнее](https://discord.com/channels/1/2/3)"));
        Assert.That(text, Does.Contain("**Важно**"));
        Assert.That(text, Does.Contain("Аптечка — описание"));
        Assert.That(text, Does.Not.Contain("[color="));
    }

    [Test]
    public void AllProjectDocumentsCanBeReadWithoutDroppingControls()
    {
        var root = RepositoryRoot();
        var files = Directory.GetFiles(Path.Combine(root, "Content.CMU", "Resources", "ServerInfo", "Guidebook", "CMU14"), "*.xml", SearchOption.AllDirectories)
            .Append(Path.Combine(root, "Resources", "ServerInfo", "Guidebook", "_RMC14", "Rules", "RMCOverview.xml"));
        var controls = new HashSet<string>
        {
            "GuideEntityEmbed", "GuideReagentEmbed", "GuideReagentGroupEmbed", "GuideMicrowaveEmbed",
            "GuideMicrowaveGroupEmbed", "CMUGuideImage", "CMUGuideZoomImage", "CMUGuideOutfitEmbed",
        };
        var count = 0;
        foreach (var file in files)
        {
            var text = DocumentationRenderer.Render(File.ReadAllText(file), e =>
            {
                Assert.That(controls.Contains(e.Name.LocalName), Is.True, $"Unsupported control in {file}: {e.Name}");
                return e.Attribute("Caption")?.Value ?? e.Name.LocalName;
            });
            Assert.That(text, Is.Not.Empty, file);
            foreach (var page in DocumentationRenderer.Pages(DocumentationRenderer.Markdown(text, _ => null)))
                Assert.That(page.Length, Is.LessThanOrEqualTo(3800), file);
            count++;
        }
        Assert.That(count, Is.GreaterThan(65));
        TestContext.Out.WriteLine($"Read {count} project documents."); // RuMC edit: NUnit1033
    }

    [Test]
    public void PaginationRetainsLongWordsAndUnicode()
    {
        var text = string.Concat(Enumerable.Repeat("А😀Б", 5000));
        var pages = DocumentationRenderer.Pages(text);
        Assert.That(string.Concat(pages), Is.EqualTo(text));
        Assert.That(pages.All(p => p.Length <= 3800 && !char.IsHighSurrogate(p[^1]) && !char.IsLowSurrogate(p[0])), Is.True);
    }

    [TestCase("http://discord.com/api/webhooks/123/token")]
    [TestCase("https://example.com/api/webhooks/123/token")]
    [TestCase("https://discord.com.evil.test/api/webhooks/123/token")]
    [TestCase("https://discord.com/api/webhooks/123/token?wait=true")]
    [TestCase("https://discord.com/api/webhooks/123/token?thread_id=bad")]
    public void RejectsInvalidDestinationsWithoutLeakingTokens(string url)
    {
        var error = Assert.Throws<FormatException>(() => DocumentationPublisher.ParseWebhook(url));
        Assert.That(error!.Message, Does.Not.Contain("token"));
    }

    [Test]
    public void SupportsExistingThreadsAndRedactsEndpointDisplay()
    {
        var endpoint = DocumentationPublisher.ParseWebhook(Webhook + "?thread_id=456");
        Assert.That(endpoint.Message("789"), Is.EqualTo("https://discord.com/api/v10/webhooks/123/test-token/messages/789?thread_id=456"));
        Assert.That(endpoint.ToString(), Does.Not.Contain("test-token"));
    }

    [Test]
    public async Task RestartIsIdempotentUpdatesEditsRepairsMissingAndRemovesOnlyOwnedMessages()
    {
        using var fixture = new Fixture();
        var documents = Documents();
        await fixture.Run(documents);
        var created = fixture.Discord.Created;
        var edited = fixture.Discord.Edited;
        await fixture.Run(documents); // New publisher and reloaded disk journal, like a restart.
        Assert.That(fixture.Discord.Created, Is.EqualTo(created));
        Assert.That(fixture.Discord.Edited, Is.EqualTo(edited));

        var unrelated = "999999";
        fixture.Discord.Messages[unrelated] = "an administrator's message";
        var state = fixture.Store.Load("123:20:");
        var firstId = state.Messages["rules/First/0"].Id!;
        var secondId = state.Messages["rules/Second/0"].Id!;
        fixture.Discord.Messages.Remove(firstId);
        await fixture.Run(new[] { documents[0] with { Text = "Обновлённые правила." } });
        Assert.That(fixture.Discord.Created, Is.EqualTo(created + 1));
        Assert.That(fixture.Discord.Messages.ContainsKey(secondId), Is.False);
        Assert.That(fixture.Discord.Messages.ContainsKey(unrelated), Is.True);
        var descriptions = fixture.Discord.Messages.Where(p => p.Key != unrelated).Select(p =>
            JsonDocument.Parse(p.Value).RootElement.GetProperty("embeds")[0].GetProperty("description").GetString());
        Assert.That(string.Join("\n", descriptions), Does.Contain("Обновлённые правила."));
    }

    [Test]
    public async Task RulesAndGuidesCanShareWebhookWithoutDeletingEachOther()
    {
        using var fixture = new Fixture();
        await fixture.Run(Documents());
        var ids = fixture.Discord.Messages.Keys.ToArray();
        await fixture.Run(Documents(), false);
        Assert.That(ids.All(fixture.Discord.Messages.ContainsKey), Is.True);
        Assert.That(fixture.Store.Load("123:20:").Messages.Keys.Any(p => p.StartsWith("guides/")), Is.True);
    }

    [Test]
    public async Task RateLimitsRetryAndAmbiguousCreatesDoNotDuplicate()
    {
        using var fixture = new Fixture();
        fixture.Discord.RateLimitNext = true;
        await fixture.Run(Documents());
        Assert.That(fixture.Discord.RateLimits, Is.EqualTo(1));
        fixture.Discord.LoseCreateResponse = true;
        var expanded = Documents().Append(Documents()[0] with { Id = "Third" }).ToArray();
        Assert.ThrowsAsync<HttpRequestException>(async () => await fixture.Run(expanded));
        var created = fixture.Discord.Created;
        Assert.ThrowsAsync<InvalidOperationException>(async () => await fixture.Run(expanded));
        Assert.That(fixture.Discord.Created, Is.EqualTo(created));
    }

    [Test]
    public async Task DefinitiveCreateRejectionCanRetryAfterAccessIsFixed()
    {
        using var fixture = new Fixture();
        fixture.Discord.FailCreates = true;
        Assert.ThrowsAsync<DocumentationHttpException>(async () => await fixture.Run(Documents()));
        Assert.That(fixture.Store.Load("123:20:").Messages.Values.All(p => !p.Pending), Is.True);
        fixture.Discord.FailCreates = false;
        await fixture.Run(Documents());
        Assert.That(fixture.Discord.Created, Is.GreaterThan(0));
    }

    [Test]
    public async Task InvalidJournalAndFailedUpdatesNeverDeletePublishedDocumentation()
    {
        using var fixture = new Fixture();
        await fixture.Run(Documents());
        var before = fixture.Discord.Messages.Keys.ToHashSet();
        fixture.Discord.FailEdits = true;
        Assert.ThrowsAsync<DocumentationHttpException>(async () => await fixture.Run(new[] { Documents()[0] with { Text = "Изменено" } }));
        Assert.That(before.SetEquals(fixture.Discord.Messages.Keys), Is.True);
        foreach (var file in Directory.GetFiles(fixture.Directory, "*.json"))
            File.WriteAllText(file, "broken journal");
        Assert.ThrowsAsync<JsonException>(async () => await fixture.Run(Documents()));
        Assert.That(before.SetEquals(fixture.Discord.Messages.Keys), Is.True);
    }

    [Test]
    public void PayloadsHaveNavigationColorsAndFitDiscordLimits()
    {
        var documents = Documents().Select(p => p with { Text = string.Concat(Enumerable.Repeat("Правило.\n", 2000)) }).ToArray();
        var plan = DocumentationPublisher.BuildMessages(documents, true, _ => "https://discord.com/channels/10/20/30", Presentation);
        Assert.That(plan[0].Value.Embeds![0].Description, Does.Contain("https://discord.com/channels/10/20/30"));
        foreach (var (_, payload) in plan)
        {
            Assert.That(payload.Content?.Length ?? 0, Is.LessThanOrEqualTo(2000));
            Assert.That(payload.AllowedMentions.Parse, Is.Empty);
            foreach (var embed in payload.Embeds!)
            {
                Assert.That(embed.Title.Length, Is.LessThanOrEqualTo(256));
                Assert.That(embed.Description.Length, Is.LessThanOrEqualTo(4096));
                Assert.That(embed.Title.Length + embed.Description.Length + embed.Footer!.Value.Text.Length, Is.LessThanOrEqualTo(6000));
                Assert.That(embed.Color, Is.Not.Zero);
            }
        }
    }

    private static DocumentationPage[] Documents() =>
    [
        new("First", "Общие правила", "Правила. [textlink=\"Другой раздел\" link=\"Second\"]", "https://example.test/first", "Правила сервера", 0xD9A441),
        new("Second", "Ролевые правила", "Исключение из правила.", "https://example.test/second", "Правила сервера", 0xD9A441),
    ];

    private static string RepositoryRoot([CallerFilePath] string file = "")
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "Content.CMU", "Resources")))
                return directory.FullName;
        }
        return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, "..", "..", ".."));
    }

    private sealed class Fixture : IDisposable
    {
        public string Directory { get; } = Path.Combine(Path.GetTempPath(), "cmu-doc-test-" + Guid.NewGuid());
        public readonly FakeDiscord Discord = new();
        public DocumentationFileStore Store => new(Directory);
        public async Task Run(IReadOnlyList<DocumentationPage> documents, bool rules = true)
        {
            using var http = new HttpClient(Discord, false);
            await new DocumentationPublisher(http, Store, Presentation).Synchronize(Webhook, documents, rules, CancellationToken.None);
        }
        public void Dispose()
        {
            Discord.Dispose();
            if (System.IO.Directory.Exists(Directory))
                System.IO.Directory.Delete(Directory, true);
        }
    }

    private sealed class FakeDiscord : HttpMessageHandler
    {
        public readonly Dictionary<string, string> Messages = new();
        public int Created;
        public int Edited;
        public int RateLimits;
        public bool RateLimitNext;
        public bool LoseCreateResponse;
        public bool FailEdits;
        public bool FailCreates;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (RateLimitNext)
            {
                RateLimitNext = false;
                RateLimits++;
                return Response(HttpStatusCode.TooManyRequests, "{\"retry_after\":0.001}");
            }
            var method = request.Method;
            var path = request.RequestUri!.AbsolutePath;
            if (!path.Contains("/messages/") && method == HttpMethod.Get)
                return Response(HttpStatusCode.OK, "{\"id\":\"123\",\"channel_id\":\"20\",\"guild_id\":\"10\"}");
            if (method == HttpMethod.Post)
            {
                if (FailCreates)
                    return Response(HttpStatusCode.Forbidden);
                var id = (++Created).ToString();
                Messages[id] = await request.Content!.ReadAsStringAsync(cancellationToken);
                if (LoseCreateResponse)
                {
                    LoseCreateResponse = false;
                    throw new HttpRequestException("Simulated lost response containing test-token");
                }
                return Response(HttpStatusCode.OK, $"{{\"id\":\"{id}\"}}");
            }
            var messageId = path.Split('/')[^1];
            if (!Messages.ContainsKey(messageId))
                return Response(HttpStatusCode.NotFound);
            if (method == HttpMethod.Patch)
            {
                if (FailEdits)
                    return Response(HttpStatusCode.Forbidden);
                Edited++;
                Messages[messageId] = await request.Content!.ReadAsStringAsync(cancellationToken);
            }
            if (method == HttpMethod.Delete)
                Messages.Remove(messageId);
            return Response(HttpStatusCode.OK, $"{{\"id\":\"{messageId}\"}}");
        }

        private static HttpResponseMessage Response(HttpStatusCode status, string body = "{}") =>
            new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
    }
}

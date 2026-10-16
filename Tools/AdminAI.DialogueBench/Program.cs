using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Content.Server.CMU14.AdminAI;

// Operator-only diagnostics. No downloads, game sessions, runtime configuration edits or real player data.
if (args.Length is < 2 or > 3 || !Directory.Exists(args[0])
    || !new AIInferenceOptions(SocketPath: args[1]).IsValid()
    || args.Length == 3 && (!int.TryParse(args[2], out var repeats) || repeats is < 1 or > 10))
{
    Console.Error.WriteLine("Usage: AdminAI.DialogueBench <repository root> </run/rucmu-ai/name.sock> [repeats 1..10, default 3]");
    return 2;
}
var root = Path.GetFullPath(args[0]);
var repeatCount = args.Length == 3 ? int.Parse(args[2]) : 3;
var knowledge = new AIRuleKnowledgeBase(ReadPublicDocuments(root));
if (knowledge.Documents == 0)
{
    Console.Error.WriteLine("No allowlisted public guide entries found. This diagnostic requires the complete repository resources.");
    return 2;
}
using var lifetime = new CancellationTokenSource(TimeSpan.FromMinutes(25));
Console.CancelKeyPress += (_, e) => { e.Cancel = true; lifetime.Cancel(); };
using var client = new UdsAIInferenceClient(new(Enabled: true, SocketPath: args[1],
    MaxInputChars: 2300, MaxOutputTokens: 128, TimeoutSeconds: 20));
var health = await client.CheckHealthAsync(lifetime.Token);
Print(new { synthetic = true, documents = knowledge.Documents, passages = knowledge.Count, health = health.ToString() });
if (health != AIWorkerHealth.Ready) return 2;

(string Name, (bool Admin, string Text)[] Turns)[] cases =
[
    ("greeting", [(false, "Привет помогите")]),
    ("medical-novice", [(false, "Как вылечить морпеха?"), (true, "Какой вид травмы или симптом у персонажа?"), (false, "Я не знаю, я новичок")]),
    ("radio-followup", [(false, "Как пользоваться радио?"), (true, "Наденьте гарнитуру и выберите сеть."), (false, "Не понял, как это сделать?")]),
    ("topic-change", [(false, "Как вылечить морпеха?"), (true, "Начните с осмотра."), (false, "Как пользоваться радио?")]),
    ("burn-answer", [(false, "Как вылечить морпеха?"), (true, "Какой вид травмы?"), (false, "Это ожог")]),
    ("unknown", [(false, "Как создать квантового вомбата?")]),
    ("complaint", [(false, "Меня убил союзник"), (true, "Когда и где это произошло?"), (false, "На базе только что")]),
];
var durations = new List<double>();
var completed = 0;
for (var cycle = 0; cycle < repeatCount; cycle++)
foreach (var item in cases)
{
    var store = new AIDraftSessionStore(new());
    var channel = Guid.NewGuid();
    var owner = Guid.NewGuid();
    foreach (var turn in item.Turns)
        store.Accept(channel, 1, turn.Admin, turn.Text, false, TimeSpan.Zero);
    var view = store.Open(owner, channel, Guid.NewGuid(), 1, TimeSpan.Zero, true)!;
    var ticket = store.Begin(view, TimeSpan.Zero, 1200)!;
    var sources = knowledge.SearchDraft(ticket, 2300 - ticket.Conversation.Length - 350);
    var request = AIDraftPrompt.Request(ticket, sources, 2300, 128)
        ?? throw new InvalidOperationException("Synthetic dialogue exceeded production input limits.");
    var timer = Stopwatch.StartNew();
    var result = await client.InferAsync(request, lifetime.Token);
    timer.Stop();
    durations.Add(timer.Elapsed.TotalMilliseconds);
    if (result.Status == AIInferenceStatus.Success) completed++;
    Print(new { cycle, name = item.Name, status = result.Status.ToString(), ms = Math.Round(timer.Elapsed.TotalMilliseconds, 2),
        inputChars = request.InputChars, contextChars = ticket.Conversation.Length,
        sources = sources.Select(source => source.GuideId).ToArray(), output = result.UntrustedText });
}
durations.Sort();
var over3s = durations.Count(duration => duration > 3000);
Print(new { count = durations.Count, completed, p95 = durations[(int)Math.Ceiling(durations.Count * .95) - 1],
    max = durations[^1], over3s, semanticAcceptance = "manual-review-required",
    timingScope = "UDS and model; excludes queue, game handler, network and UI" });
// Passing transport/latency is necessary, but never sufficient for model acceptance.
return completed == durations.Count && over3s == 0 ? 0 : 1;

static void Print(object value) => Console.WriteLine(JsonSerializer.Serialize(value));

static IEnumerable<AIRuleDocument> ReadPublicDocuments(string root)
{
    // Diagnostic-only reader for this repository's simple guideEntry ID/text declarations.
    // The game uses its ordinary prototype loader; no private files or unrestricted XML are imported here.
    var directories = new[] { "Content.CMU/Resources/Prototypes", "Resources/Prototypes/_RuCM" };
    foreach (var directory in directories.Select(path => Path.Combine(root, path)).Where(Directory.Exists))
    foreach (var path in Directory.EnumerateFiles(directory, "*.yml", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
    foreach (var block in File.ReadAllText(path).Split("- type: guideEntry", StringSplitOptions.None).Skip(1))
    {
        var id = Regex.Match(block, @"(?m)^  id: ([^\r\n]+)").Groups[1].Value.Trim();
        var resourcePath = Regex.Match(block, "(?m)^  text: \"?([^\" \r\n]+)").Groups[1].Value;
        if (id.Length == 0 || !AIRuleKnowledgeBase.IsAllowedPath(resourcePath)) continue;
        var relative = resourcePath.TrimStart('/');
        var file = Path.Combine(root, "Content.CMU/Resources", relative);
        if (!File.Exists(file)) file = Path.Combine(root, "Resources", relative);
        if (File.Exists(file)) yield return new(id, id, resourcePath, File.ReadAllBytes(file));
    }
}

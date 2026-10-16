using System.Reflection;
using System.Xml;
using NUnit.Framework.Api;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

// Run the same NUnit fixtures in-process when VSTest/testhost IPC is unavailable.
// No permission changes, network connections, dynamic loading of engine code or sandbox bypass.
if (args.Length != 1 || args[0] is not ("boundary" or "ipc"))
{
    Console.Error.WriteLine("Usage: AdminAI.Tests <boundary|ipc>");
    return 1;
}

var runner = new NUnitTestAssemblyRunner(new DefaultTestAssemblyBuilder());
runner.Load(Assembly.GetExecutingAssembly(), new Dictionary<string, object> { ["NumberOfTestWorkers"] = 0 });
var filter = TestFilter.FromXml(args[0] == "ipc"
    ? "<filter><cat>LocalIPC</cat></filter>"
    : "<filter><not><cat>LocalIPC</cat></not></filter>");
var result = runner.Run(new Listener(), filter);
PrintFailures(result);
var resultsDir = Path.Combine(".codex-build", "admin-ai");
Directory.CreateDirectory(resultsDir);
var resultsPath = Path.Combine(resultsDir, args[0] + "-nunit.xml");
using (var writer = XmlWriter.Create(resultsPath, new XmlWriterSettings { Indent = true }))
    result.ToXml(true).WriteTo(writer);
Console.WriteLine($"NUnit: passed={result.PassCount}, failed={result.FailCount}, skipped={result.SkipCount}, total={result.TotalCount}");
Console.WriteLine($"Results: {resultsPath}");
return result.FailCount == 0 && result.PassCount > 0 && result.SkipCount == 0 ? 0 : 1;

static void PrintFailures(ITestResult result)
{
    if (!result.Test.IsSuite && result.ResultState.Status != TestStatus.Passed)
        Console.WriteLine($"{result.ResultState}: {result.FullName}\n{result.Message}\n{result.StackTrace}");
    foreach (var child in result.Children)
        PrintFailures(child);
}

internal sealed class Listener : ITestListener
{
    public void TestStarted(ITest test) { }
    public void TestFinished(ITestResult result) { }
    public void TestOutput(TestOutput output) => Console.Write(output.Text);
    public void SendMessage(TestMessage message) { }
}

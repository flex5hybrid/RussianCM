using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Content.Packaging;
using NUnit.Framework;
using Robust.Packaging;
using Robust.Packaging.AssetProcessing;
using Robust.Packaging.AssetProcessing.Passes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Tests.CMU14.Packaging;

[TestFixture]
public sealed class CMUResourceInputTest
{
    [TestCase("Textures/table.rsi/meta.json")]
    [TestCase("Textures/table.rsi/sewing_table.png")]
    [TestCase("Audio/test.ogg")]
    [TestCase("Prototypes/test.yml")]
    [TestCase("Locale/ru-RU/test.ftl")]
    public async Task FirstMountWinsAndUniqueFilesSurvive(string path)
    {
        var input = new CMUResourceInput();
        var collector = new Collector();
        collector.AddDependency(input);
        AssetGraph.CalculateGraph([input, collector]);

        input.InjectFileFromText(path, "Resources");
        input.InjectFileFromText(path, "Content.CMU");
        input.InjectFileFromText(path + ".cmu", "CMU only");
        input.InjectFileFromText(path, "Content.RUMC");
        input.InjectFileFromText(path + ".cmu", "Shadowed by CMU");
        input.InjectFileFromText(path + ".rumc", "RUMC only");
        input.InjectFinished();
        await collector.FinishedTask.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.That(collector.Files, Has.Count.EqualTo(3));
        Assert.That(collector.ReadText(path), Is.EqualTo("Resources"));
        Assert.That(collector.ReadText(path + ".cmu"), Is.EqualTo("CMU only"));
        Assert.That(collector.ReadText(path + ".rumc"), Is.EqualTo("RUMC only"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task DuplicateRsiAndTextFilesSurviveParallelGraphs(bool server)
    {
        var input = new CMUResourceInput();
        var collector = new Collector();
        AssetPassPipe? core = null;
        if (server)
        {
            var graph = new RobustServerAssetGraph();
            graph.InputResources.AddDependency(input);
            core = graph.InputCore;
            collector.AddDependency(graph.Output);
            AssetGraph.CalculateGraph([input, collector, ..graph.AllPasses]);
        }
        else
        {
            var graph = new RobustClientAssetGraph();
            graph.Input.AddDependency(input);
            collector.AddDependency(graph.Output);
            AssetGraph.CalculateGraph([input, collector, ..graph.AllPasses]);
        }

        const string rsi = "Textures/table.rsi/";
        const string meta = """
            {"version":1,"size":{"x":1,"y":1},"states":[{"name":"sewing_table"}]}
            """;
        using var image = new Image<Rgba32>(1, 1, new Rgba32(255, 0, 0));
        using var png = new MemoryStream();
        image.SaveAsPng(png);
        for (var mount = 0; mount < 3; mount++)
        {
            input.InjectFileFromText(rsi + "meta.json", mount == 0 ? meta : "{}");
            input.InjectFileFromMemory(rsi + "sewing_table.png", mount == 0 ? png.ToArray() : []);
            input.InjectFileFromText("Prototypes/test.yml", mount == 0 ? "first-prototype\n" : "shadowed-prototype\n");
            input.InjectFileFromText("Locale/ru-RU/test.ftl", mount == 0 ? "first-locale\n" : "shadowed-locale\n");
        }
        input.InjectFileFromText("Prototypes/unique.yml", "unique-prototype\n");
        input.InjectFinished();
        core?.InjectFinished();
        await collector.FinishedTask.WaitAsync(TimeSpan.FromSeconds(10));

        var prefix = server ? "Resources/" : "";
        var prototypePath = prefix + (server ? "Prototypes/test.yml" : "Prototypes/__merged.yml");
        var localePath = prefix + (server ? "Locale/ru-RU/test.ftl" : "Locale/ru-RU/__merged.ftl");
        Assert.That(collector.ReadText(prototypePath), Does.Contain("first-prototype").And.Not.Contain("shadowed"));
        Assert.That(collector.ReadText(localePath), Does.Contain("first-locale").And.Not.Contain("shadowed"));
        var uniquePath = server ? prefix + "Prototypes/unique.yml" : prototypePath;
        Assert.That(collector.ReadText(uniquePath), Does.Contain("unique-prototype"));
        var imagePath = server ? prefix + rsi + "sewing_table.png" : "Textures/table.rsic";
        using var stream = collector.Files.Single(file => file.Path == imagePath).Open();
        using var packed = Image.Load<Rgba32>(stream);
        Assert.That(packed[0, 0], Is.EqualTo(new Rgba32(255, 0, 0)));
        Assert.That(collector.Files.Select(file => file.Path), Is.Unique);
    }

    private sealed class Collector : AssetPass
    {
        public readonly List<AssetFile> Files = [];

        protected override AssetFileAcceptResult AcceptFile(AssetFile file)
        {
            lock (Files)
                Files.Add(file);
            return AssetFileAcceptResult.Consumed;
        }

        public string ReadText(string path)
        {
            using var stream = Files.Single(file => file.Path == path).Open();
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}

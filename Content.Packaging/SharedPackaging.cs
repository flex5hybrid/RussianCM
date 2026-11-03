using Robust.Packaging.AssetProcessing;

namespace Content.Packaging;

public sealed class SharedPackaging
{
    public static readonly IReadOnlySet<string> AdditionalIgnoredResources = new HashSet<string>
    {
        // MapRenderer outputs into Resources. Avoid these getting included in packaging.
        "MapImages",
    };

    // CMU14 method: avoid injecting the same resource path from multiple content roots.
    public static Task CopyAdditionalResources(string contentDir, AssetPass pass, CancellationToken cancel)
    {
        var baseResources = Path.Combine(contentDir, "Resources");
        var includedPaths = new HashSet<string>(StringComparer.Ordinal);

        // RUMC takes priority over CMU for shared paths, matching the previous copy order.
        foreach (var project in new[] { "Content.RUMC", "Content.CMU" })
        {
            var source = Path.Combine(contentDir, project, "Resources");

            foreach (var entry in Directory.EnumerateFileSystemEntries(source))
            {
                // Match RobustSharedPackaging.DoResourceCopy's file selection.
                var files = Directory.Exists(entry)
                    ? Directory.EnumerateFiles(entry, "*.*", SearchOption.AllDirectories)
                    : new[] { entry };

                foreach (var file in files)
                {
                    cancel.ThrowIfCancellationRequested();

                    var relativePath = Path.GetRelativePath(source, file);
                    if (File.Exists(Path.Combine(baseResources, relativePath)) ||
                        !includedPaths.Add(relativePath))
                        continue;

                    pass.InjectFileFromDisk(relativePath.Replace(Path.DirectorySeparatorChar, '/'), file);
                }
            }
        }

        return Task.CompletedTask;
    }
}

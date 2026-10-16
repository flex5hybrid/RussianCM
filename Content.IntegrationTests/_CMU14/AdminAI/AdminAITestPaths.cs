using System;
using System.IO;

namespace Content.IntegrationTests.CMU14.AdminAI;

internal static class AdminAITestPaths
{
    public static string Repository()
    {
        var directory = new DirectoryInfo(Environment.CurrentDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Content.Shared", "Content.Shared.csproj")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new InvalidOperationException("Run repository-backed knowledge tests within the checkout.");
    }
}

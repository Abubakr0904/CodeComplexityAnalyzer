using CodeComplexityAnalyzer.Core.Models;

namespace CodeComplexityAnalyzer.Cli;

internal static class FileSourceCollector
{
    public static IEnumerable<SourceFile> Collect(string rootPath, IReadOnlyList<string> excludeDirectories)
    {
        if (File.Exists(rootPath))
        {
            yield return new SourceFile(rootPath, File.ReadAllText(rootPath));
            yield break;
        }

        var excludeSet = new HashSet<string>(excludeDirectories, StringComparer.OrdinalIgnoreCase);

        foreach (var path in Directory.EnumerateFiles(rootPath, "*.cs", SearchOption.AllDirectories))
        {
            if (IsExcluded(path, rootPath, excludeSet))
            {
                continue;
            }
            yield return new SourceFile(path, File.ReadAllText(path));
        }
    }

    private static bool IsExcluded(string filePath, string rootPath, HashSet<string> excludes)
    {
        var relative = Path.GetRelativePath(rootPath, filePath);
        var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(excludes.Contains);
    }
}

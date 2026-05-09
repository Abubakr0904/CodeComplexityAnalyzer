using CodeComplexityAnalyzer.Core.Models;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeComplexityAnalyzer.Core.Analysis;

public sealed class SyntaxAnalyzer
{
    public AnalysisReport Analyze(AnalysisOptions options)
    {
        var files = EnumerateCsharpFiles(options).ToArray();
        var methods = new List<MethodMetrics>();

        foreach (var file in files)
        {
            methods.AddRange(AnalyzeFile(file));
        }

        var hotspots = methods
            .Where(m => IsHotspot(m, options.Thresholds))
            .OrderByDescending(m => m.CyclomaticComplexity)
            .ToArray();

        return new AnalysisReport(
            RootPath: options.RootPath,
            FilesAnalyzed: files.Length,
            MethodsAnalyzed: methods.Count,
            Methods: methods,
            Hotspots: hotspots);
    }

    private static IEnumerable<MethodMetrics> AnalyzeFile(string filePath)
    {
        var source = File.ReadAllText(filePath);
        var tree = CSharpSyntaxTree.ParseText(source, path: filePath);
        var root = tree.GetRoot();

        return root
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Select(m => MethodAnalyzer.Analyze(m, filePath))
            .ToArray();
    }

    private static IEnumerable<string> EnumerateCsharpFiles(AnalysisOptions options)
    {
        if (File.Exists(options.RootPath))
        {
            yield return options.RootPath;
            yield break;
        }

        var excludeSet = new HashSet<string>(options.ExcludeDirectories, StringComparer.OrdinalIgnoreCase);

        foreach (var path in Directory.EnumerateFiles(options.RootPath, "*.cs", SearchOption.AllDirectories))
        {
            if (IsExcluded(path, options.RootPath, excludeSet))
            {
                continue;
            }
            yield return path;
        }
    }

    private static bool IsExcluded(string filePath, string rootPath, HashSet<string> excludes)
    {
        var relative = Path.GetRelativePath(rootPath, filePath);
        var segments = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(excludes.Contains);
    }

    private static bool IsHotspot(MethodMetrics m, Thresholds t) =>
        m.CyclomaticComplexity > t.CyclomaticComplexity ||
        m.LineCount > t.LineCount ||
        m.ParameterCount > t.ParameterCount;
}

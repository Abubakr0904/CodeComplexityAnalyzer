using CodeComplexityAnalyzer.Core.Models;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeComplexityAnalyzer.Core.Analysis;

public sealed class SyntaxAnalyzer : IAnalyzer
{
    public IReadOnlyList<MethodMetrics> AnalyzeSource(string sourceCode, string filePath)
    {
        var tree = CSharpSyntaxTree.ParseText(sourceCode, path: filePath);
        var root = tree.GetRoot();

        return root
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Select(m => MethodAnalyzer.Analyze(m, filePath))
            .ToArray();
    }

    public AnalysisReport AnalyzeSources(
        IEnumerable<SourceFile> sources,
        string rootPath,
        Thresholds thresholds)
    {
        var sourceList = sources.ToArray();
        var methods = new List<MethodMetrics>();

        foreach (var source in sourceList)
        {
            methods.AddRange(AnalyzeSource(source.SourceCode, source.FilePath));
        }

        var hotspots = methods
            .Where(m => IsHotspot(m, thresholds))
            .OrderByDescending(m => m.CyclomaticComplexity)
            .ToArray();

        return new AnalysisReport(
            RootPath: rootPath,
            FilesAnalyzed: sourceList.Length,
            MethodsAnalyzed: methods.Count,
            Methods: methods,
            Hotspots: hotspots);
    }

    private static bool IsHotspot(MethodMetrics m, Thresholds t) =>
        m.CyclomaticComplexity > t.CyclomaticComplexity ||
        m.LineCount > t.LineCount ||
        m.ParameterCount > t.ParameterCount ||
        // MI is inverted: lower values indicate worse maintainability
        m.MaintainabilityIndex < t.MaintainabilityIndex;
}

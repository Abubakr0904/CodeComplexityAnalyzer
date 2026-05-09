using CodeComplexityAnalyzer.Core.Models;

namespace CodeComplexityAnalyzer.Core.Analysis;

public interface IAnalyzer
{
    /// Single source file. Pure parsing — no I/O.
    IReadOnlyList<MethodMetrics> AnalyzeSource(string sourceCode, string filePath);

    /// Multiple sources; produces an aggregated report.
    AnalysisReport AnalyzeSources(
        IEnumerable<SourceFile> sources,
        string rootPath,
        Thresholds thresholds);
}

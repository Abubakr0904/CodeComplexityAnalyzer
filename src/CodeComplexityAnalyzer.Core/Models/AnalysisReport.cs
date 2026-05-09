namespace CodeComplexityAnalyzer.Core.Models;

public sealed record AnalysisReport(
    string RootPath,
    int FilesAnalyzed,
    int MethodsAnalyzed,
    IReadOnlyList<MethodMetrics> Methods,
    IReadOnlyList<MethodMetrics> Hotspots);

namespace CodeComplexityAnalyzer.Core.Models;

public sealed record MethodMetrics(
    string MethodName,
    string ContainingType,
    string FilePath,
    int LineNumber,
    int CyclomaticComplexity,
    int LineCount,
    int ParameterCount);

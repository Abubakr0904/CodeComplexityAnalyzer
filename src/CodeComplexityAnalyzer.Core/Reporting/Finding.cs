namespace CodeComplexityAnalyzer.Core.Reporting;

public sealed record Finding(
    string FilePath,
    int LineNumber,
    string MethodName,
    string ContainingType,
    MetricType MetricType,
    int Value,
    int Threshold,
    Severity Severity);

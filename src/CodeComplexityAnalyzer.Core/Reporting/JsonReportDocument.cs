namespace CodeComplexityAnalyzer.Core.Reporting;

public sealed record JsonReportDocument(
    string SchemaVersion,
    string RootPath,
    int FilesAnalyzed,
    int MethodsAnalyzed,
    IReadOnlyList<Finding> Findings);

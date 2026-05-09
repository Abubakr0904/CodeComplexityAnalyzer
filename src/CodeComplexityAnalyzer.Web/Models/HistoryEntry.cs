using CodeComplexityAnalyzer.Core.Reporting;

namespace CodeComplexityAnalyzer.Web.Models;

public sealed record HistoryEntry(
    string Id,
    DateTime TimestampUtc,
    string Mode,
    string SourceId,
    int ErrorCount,
    int WarningCount,
    int FileCount,
    int MaxCc,
    int MaxLines,
    int MaxParams,
    int MinMi,
    List<Finding> Findings);

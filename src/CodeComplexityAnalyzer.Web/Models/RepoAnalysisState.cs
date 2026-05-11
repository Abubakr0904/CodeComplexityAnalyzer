namespace CodeComplexityAnalyzer.Web.Models;

/// <summary>
/// Discriminated union describing every phase of a GitHub-repo analysis run.
/// Replaces the scattered <c>_isAnalyzingRepo</c>, <c>_repoStatus</c>, and
/// <c>_failedDownloads</c> fields that previously lived on the Index page.
/// </summary>
public abstract record RepoAnalysisState
{
    private RepoAnalysisState() { }

    public sealed record Idle : RepoAnalysisState;

    public sealed record Resolving(string Message) : RepoAnalysisState;

    public sealed record Fetching(string Branch) : RepoAnalysisState;

    public sealed record Analyzing(
        int Done,
        int Total,
        string CurrentPath,
        int FailedDownloads) : RepoAnalysisState;

    public sealed record Completed(
        string Branch,
        int Analyzed,
        int FailedDownloads,
        int FindingsCount) : RepoAnalysisState;

    public sealed record Cancelled : RepoAnalysisState;

    public sealed record Failed(string Message) : RepoAnalysisState;
}

public static class RepoAnalysisStateExtensions
{
    /// <summary>True while a run is in flight (resolving, fetching, or analyzing).</summary>
    public static bool IsRunning(this RepoAnalysisState state) => state is
        RepoAnalysisState.Resolving or
        RepoAnalysisState.Fetching or
        RepoAnalysisState.Analyzing;

    /// <summary>Renders the state as the human-readable status string shown beside the Analyze button.</summary>
    public static string ToStatusText(this RepoAnalysisState state) => state switch
    {
        RepoAnalysisState.Idle => string.Empty,
        RepoAnalysisState.Resolving r => r.Message,
        RepoAnalysisState.Fetching f => $"Analyzing branch '{f.Branch}'...",
        RepoAnalysisState.Analyzing a => $"Analyzing {a.Done}/{a.Total}: {a.CurrentPath}",
        RepoAnalysisState.Cancelled => "Cancelled.",
        RepoAnalysisState.Failed f => f.Message,
        RepoAnalysisState.Completed c => FormatCompleted(c),
        _ => string.Empty,
    };

    private static string FormatCompleted(RepoAnalysisState.Completed c)
    {
        var failSuffix = c.FailedDownloads > 0
            ? $" ({c.FailedDownloads} file(s) could not be downloaded)"
            : string.Empty;

        return c.FindingsCount == 0
            ? $"Analyzed {c.Analyzed} files on branch '{c.Branch}' — no findings (all within thresholds).{failSuffix}"
            : $"Analyzed {c.Analyzed} files on branch '{c.Branch}' — {c.FindingsCount} finding(s).{failSuffix}";
    }
}

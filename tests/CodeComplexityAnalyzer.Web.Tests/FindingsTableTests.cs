using AngleSharp.Dom;
using Bunit;
using CodeComplexityAnalyzer.Core.Reporting;
using CodeComplexityAnalyzer.Web.Components;

namespace CodeComplexityAnalyzer.Web.Tests;

public sealed class FindingsTableTests : IDisposable
{
    private readonly TestContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Finding Err(string file = "src/A.cs", string method = "BigMethod",
        MetricType metric = MetricType.CyclomaticComplexity, int line = 10,
        int value = 25, int threshold = 10) =>
        new(file, line, method, "TypeA", metric, value, threshold, Severity.Error);

    private static Finding Warn(string file = "src/B.cs", string method = "MidMethod",
        MetricType metric = MetricType.LineCount, int line = 30,
        int value = 70, int threshold = 60) =>
        new(file, line, method, "TypeB", metric, value, threshold, Severity.Warning);

    // -----------------------------------------------------------------------
    // Placeholder
    // -----------------------------------------------------------------------

    [Fact]
    public void Renders_placeholder_when_Findings_is_null()
    {
        var cut = _ctx.RenderComponent<FindingsTable>(p => p
            .Add(c => c.Findings, null));

        var placeholder = cut.Find(".placeholder-card");
        Assert.Contains("Run an analysis to see findings.", placeholder.TextContent);
    }

    // -----------------------------------------------------------------------
    // Summary counts + badges
    // -----------------------------------------------------------------------

    [Fact]
    public void Shows_correct_summary_counts_and_badges_for_one_error_and_one_warning()
    {
        var findings = new List<Finding> { Err(), Warn() };

        var cut = _ctx.RenderComponent<FindingsTable>(p => p
            .Add(c => c.Findings, findings));

        var summary = cut.Find(".findings-summary");
        Assert.Contains("1 error", summary.TextContent);
        Assert.Contains("1 warning", summary.TextContent);

        var errorBadge = summary.QuerySelector(".badge-error");
        Assert.NotNull(errorBadge);
        Assert.Contains("1 error", errorBadge!.TextContent);

        var warningBadge = summary.QuerySelector(".badge-warning");
        Assert.NotNull(warningBadge);
        Assert.Contains("1 warning", warningBadge!.TextContent);

        // Across two files
        Assert.Contains("2 files", summary.TextContent);
    }

    // -----------------------------------------------------------------------
    // Severity filter
    // -----------------------------------------------------------------------

    [Fact]
    public void Severity_filter_errors_only_hides_warning_rows()
    {
        var findings = new List<Finding> { Err(), Warn() };

        var cut = _ctx.RenderComponent<FindingsTable>(p => p
            .Add(c => c.Findings, findings));

        // Sanity: both rows visible initially
        Assert.Equal(2, cut.FindAll("tbody tr").Count);

        // Click "Errors" button
        var errorsButton = cut.FindAll(".filter-group button")
            .First(b => b.TextContent.Trim() == "Errors");
        errorsButton.Click();

        var rows = cut.FindAll("tbody tr");
        Assert.Single(rows);
        Assert.Contains("BigMethod", rows[0].TextContent);
        Assert.DoesNotContain("MidMethod", cut.Markup);
    }

    // -----------------------------------------------------------------------
    // Metric filter
    // -----------------------------------------------------------------------

    [Fact]
    public void Unchecking_a_metric_hides_rows_of_that_metric_type()
    {
        var findings = new List<Finding>
        {
            Err(metric: MetricType.CyclomaticComplexity, method: "CcMethod"),
            Warn(metric: MetricType.LineCount, method: "LineMethod"),
        };

        var cut = _ctx.RenderComponent<FindingsTable>(p => p
            .Add(c => c.Findings, findings));

        Assert.Equal(2, cut.FindAll("tbody tr").Count);

        // Find the LineCount checkbox and uncheck it
        var lineCheckbox = cut.FindAll(".metric-checkbox")
            .First(l => l.TextContent.Contains("LineCount"))
            .QuerySelector("input[type=checkbox]")!;

        lineCheckbox.Change(false);

        var rows = cut.FindAll("tbody tr");
        Assert.Single(rows);
        Assert.Contains("CcMethod", rows[0].TextContent);
        Assert.DoesNotContain("LineMethod", cut.Markup);
    }

    // -----------------------------------------------------------------------
    // CSV download
    // -----------------------------------------------------------------------

    [Fact]
    public void Csv_download_produces_header_line_and_escaped_row()
    {
        // A method name containing a comma forces CSV quoting/escaping.
        var findings = new List<Finding>
        {
            new(
                FilePath: "src/Has,Comma.cs",
                LineNumber: 12,
                MethodName: "Method,WithComma",
                ContainingType: "TypeX",
                MetricType: MetricType.CyclomaticComplexity,
                Value: 25,
                Threshold: 10,
                Severity: Severity.Error),
        };

        var invocation = _ctx.JSInterop
            .SetupVoid("ccaDownload", _ => true);

        var cut = _ctx.RenderComponent<FindingsTable>(p => p
            .Add(c => c.Findings, findings));

        var csvButton = cut.FindAll("button")
            .First(b => b.TextContent.Contains("CSV"));
        csvButton.Click();

        var call = invocation.Invocations.Single();
        Assert.Equal("ccaDownload", call.Identifier);

        var filename = (string)call.Arguments[0]!;
        var csv = (string)call.Arguments[1]!;
        var mime = (string)call.Arguments[2]!;

        Assert.StartsWith("cca-findings-", filename);
        Assert.EndsWith(".csv", filename);
        Assert.Equal("text/csv", mime);

        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.TrimEnd('\r'))
            .ToList();

        // Header line
        Assert.Equal("severity,metric,file,line,method,type,value,threshold", lines[0]);

        // Escaped row: commas inside quoted fields
        Assert.Equal(2, lines.Count);
        Assert.Contains("\"Method,WithComma\"", lines[1]);
        Assert.Contains("\"src/Has,Comma.cs\"", lines[1]);
        Assert.StartsWith("Error,CyclomaticComplexity,", lines[1]);
    }
}

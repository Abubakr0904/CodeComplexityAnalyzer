using System.Text.Json;
using CodeComplexityAnalyzer.Core.Models;
using CodeComplexityAnalyzer.Core.Reporting;

namespace CodeComplexityAnalyzer.Tests;

public class JsonReporterTests
{
    private static readonly Thresholds DefaultThresholds = new(10, 60, 5);

    private static string Render(AnalysisReport report, Thresholds thresholds)
    {
        using var sw = new StringWriter();
        new JsonReporter(thresholds).Render(report, sw);
        return sw.ToString();
    }

    private static JsonElement Parse(string json) =>
        JsonDocument.Parse(json).RootElement;

    [Fact]
    public void EmptyReportProducesEmptyFindingsArray()
    {
        var report = new AnalysisReport(
            RootPath: "p",
            FilesAnalyzed: 0,
            MethodsAnalyzed: 0,
            Methods: Array.Empty<MethodMetrics>(),
            Hotspots: Array.Empty<MethodMetrics>());

        var json = Parse(Render(report, DefaultThresholds));

        Assert.Equal("1.0", json.GetProperty("schemaVersion").GetString());
        Assert.Equal("p", json.GetProperty("rootPath").GetString());
        Assert.Equal(0, json.GetProperty("filesAnalyzed").GetInt32());
        Assert.Equal(0, json.GetProperty("methodsAnalyzed").GetInt32());
        Assert.Equal(0, json.GetProperty("findings").GetArrayLength());
    }

    [Fact]
    public void HotspotsProduceFindingsWithCamelCasePropertiesAndEnumStrings()
    {
        var hotspot = new MethodMetrics(
            MethodName: "DoStuff",
            ContainingType: "Worker",
            FilePath: "src/Worker.cs",
            LineNumber: 42,
            CyclomaticComplexity: 16,
            LineCount: 1,
            ParameterCount: 0);

        var report = new AnalysisReport(
            RootPath: "src",
            FilesAnalyzed: 1,
            MethodsAnalyzed: 1,
            Methods: new[] { hotspot },
            Hotspots: new[] { hotspot });

        var json = Parse(Render(report, DefaultThresholds));
        var findings = json.GetProperty("findings");

        Assert.Equal(1, findings.GetArrayLength());
        var f = findings[0];
        Assert.Equal("src/Worker.cs", f.GetProperty("filePath").GetString());
        Assert.Equal(42, f.GetProperty("lineNumber").GetInt32());
        Assert.Equal("DoStuff", f.GetProperty("methodName").GetString());
        Assert.Equal("Worker", f.GetProperty("containingType").GetString());
        Assert.Equal("cyclomaticComplexity", f.GetProperty("metricType").GetString());
        Assert.Equal(16, f.GetProperty("value").GetInt32());
        Assert.Equal(10, f.GetProperty("threshold").GetInt32());
        Assert.Equal("error", f.GetProperty("severity").GetString());
    }

    [Fact]
    public void MethodViolatingAllThreeThresholdsProducesThreeFindings()
    {
        var hotspot = new MethodMetrics(
            MethodName: "M",
            ContainingType: "C",
            FilePath: "f.cs",
            LineNumber: 1,
            CyclomaticComplexity: 20,
            LineCount: 100,
            ParameterCount: 8);

        var report = new AnalysisReport(
            RootPath: "p",
            FilesAnalyzed: 1,
            MethodsAnalyzed: 1,
            Methods: new[] { hotspot },
            Hotspots: new[] { hotspot });

        var findings = Parse(Render(report, DefaultThresholds)).GetProperty("findings");

        Assert.Equal(3, findings.GetArrayLength());
    }

    [Fact]
    public void NonHotspotsAreNotIncludedInFindings()
    {
        var clean = new MethodMetrics("Clean", "C", "f.cs", 1, 1, 1, 0);
        var report = new AnalysisReport(
            RootPath: "p",
            FilesAnalyzed: 1,
            MethodsAnalyzed: 1,
            Methods: new[] { clean },
            Hotspots: Array.Empty<MethodMetrics>());

        var json = Parse(Render(report, DefaultThresholds));

        Assert.Equal(0, json.GetProperty("findings").GetArrayLength());
    }

    [Fact]
    public void OutputIsValidJson()
    {
        var report = new AnalysisReport("p", 0, 0, Array.Empty<MethodMetrics>(), Array.Empty<MethodMetrics>());
        var raw = Render(report, DefaultThresholds);

        var ex = Record.Exception(() => JsonDocument.Parse(raw));
        Assert.Null(ex);
    }
}

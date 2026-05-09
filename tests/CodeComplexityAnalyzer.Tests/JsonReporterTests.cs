using System.Text.Json;
using CodeComplexityAnalyzer.Core.Models;
using CodeComplexityAnalyzer.Core.Reporting;

namespace CodeComplexityAnalyzer.Tests;

public class JsonReporterTests
{
    private static readonly Thresholds DefaultThresholds = new(10, 60, 5);

    private static MethodMetrics MakeMethod(int cc = 1, int lines = 1, int parameters = 0, int mi = 100,
        string name = "M", string type = "C", string file = "f.cs", int line = 1) =>
        new(MethodName: name, ContainingType: type, FilePath: file, LineNumber: line,
            CyclomaticComplexity: cc, LineCount: lines, ParameterCount: parameters,
            MaintainabilityIndex: mi);

    private static string Render(AnalysisReport report, Thresholds thresholds)
    {
        using var sw = new StringWriter();
        new JsonReporter(thresholds).Render(report, sw);
        return sw.ToString();
    }

    private static JsonElement Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

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
            ParameterCount: 0,
            MaintainabilityIndex: 100);

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
            ParameterCount: 8,
            MaintainabilityIndex: 100);

        var report = new AnalysisReport(
            RootPath: "p",
            FilesAnalyzed: 1,
            MethodsAnalyzed: 1,
            Methods: new[] { hotspot },
            Hotspots: new[] { hotspot });

        var findings = Parse(Render(report, DefaultThresholds)).GetProperty("findings");

        Assert.Equal(3, findings.GetArrayLength());

        var metricTypes = findings.EnumerateArray()
            .Select(x => x.GetProperty("metricType").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal)
            {
                "cyclomaticComplexity",
                "lineCount",
                "parameterCount",
            },
            metricTypes);
    }

    [Fact]
    public void NonHotspotsAreNotIncludedInFindings()
    {
        var clean = new MethodMetrics("Clean", "C", "f.cs", 1, 1, 1, 0, 100);
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

    [Fact]
    public void MiFindingRendersWithCamelCaseMetricType()
    {
        // MI=20, threshold=50 → Error (20 <= 25 = 50/2)
        var hotspot = MakeMethod(cc: 1, lines: 1, mi: 20);
        var report = new AnalysisReport(
            RootPath: "p",
            FilesAnalyzed: 1,
            MethodsAnalyzed: 1,
            Methods: new[] { hotspot },
            Hotspots: new[] { hotspot });

        var json = Parse(Render(report, DefaultThresholds));
        var findings = json.GetProperty("findings");

        Assert.Equal(1, findings.GetArrayLength());
        var f = findings[0];
        Assert.Equal("maintainabilityIndex", f.GetProperty("metricType").GetString());
        Assert.Equal(20, f.GetProperty("value").GetInt32());
        Assert.Equal(50, f.GetProperty("threshold").GetInt32());
        Assert.Equal("error", f.GetProperty("severity").GetString());
    }
}

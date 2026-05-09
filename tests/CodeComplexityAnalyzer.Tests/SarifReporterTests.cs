using System.Text.Json;
using CodeComplexityAnalyzer.Core.Models;
using CodeComplexityAnalyzer.Core.Reporting;

namespace CodeComplexityAnalyzer.Tests;

public class SarifReporterTests
{
    private static readonly Thresholds DefaultThresholds = new(10, 60, 5);

    private static string Render(AnalysisReport report, Thresholds? thresholds = null)
    {
        using var sw = new StringWriter();
        new SarifReporter(thresholds ?? DefaultThresholds).Render(report, sw);
        return sw.ToString();
    }

    private static JsonElement Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    private static AnalysisReport EmptyReport(string rootPath = "C:\\repo") =>
        new(
            RootPath: rootPath,
            FilesAnalyzed: 0,
            MethodsAnalyzed: 0,
            Methods: Array.Empty<MethodMetrics>(),
            Hotspots: Array.Empty<MethodMetrics>());

    private static AnalysisReport ReportWith(string rootPath, params MethodMetrics[] hotspots) =>
        new(
            RootPath: rootPath,
            FilesAnalyzed: hotspots.Length,
            MethodsAnalyzed: hotspots.Length,
            Methods: hotspots,
            Hotspots: hotspots);

    [Fact]
    public void EmptyReportProducesValidSarifDocument()
    {
        var json = Parse(Render(EmptyReport()));

        Assert.Equal("2.1.0", json.GetProperty("version").GetString());
        Assert.Equal(
            "https://raw.githubusercontent.com/oasis-tcs/sarif-spec/master/Schemata/sarif-schema-2.1.0.json",
            json.GetProperty("$schema").GetString());

        var runs = json.GetProperty("runs");
        Assert.Equal(1, runs.GetArrayLength());

        var driver = runs[0].GetProperty("tool").GetProperty("driver");
        Assert.Equal("cca", driver.GetProperty("name").GetString());

        var rules = driver.GetProperty("rules");
        Assert.Equal(4, rules.GetArrayLength());

        var results = runs[0].GetProperty("results");
        Assert.Equal(0, results.GetArrayLength());
    }

    [Fact]
    public void EachHotspotProducesSarifResult()
    {
        // CC=16 > 10*1.5=15 → Error
        var hotspot = new MethodMetrics(
            MethodName: "Foo",
            ContainingType: "Bar",
            FilePath: "C:\\repo\\src\\Foo.cs",
            LineNumber: 42,
            CyclomaticComplexity: 16,
            LineCount: 1,
            ParameterCount: 0,
            MaintainabilityIndex: 100);

        var report = ReportWith("C:\\repo", hotspot);
        var json = Parse(Render(report));
        var results = json.GetProperty("runs")[0].GetProperty("results");

        Assert.Equal(1, results.GetArrayLength());

        var result = results[0];
        Assert.Equal("CCA001", result.GetProperty("ruleId").GetString());
        Assert.Equal("error", result.GetProperty("level").GetString());

        var location = result.GetProperty("locations")[0]
            .GetProperty("physicalLocation");

        Assert.Equal("src/Foo.cs", location.GetProperty("artifactLocation").GetProperty("uri").GetString());
        Assert.Equal(42, location.GetProperty("region").GetProperty("startLine").GetInt32());
    }

    [Fact]
    public void MethodViolatingAllThreeMetricsProducesThreeResults()
    {
        var hotspot = new MethodMetrics(
            MethodName: "M",
            ContainingType: "C",
            FilePath: "C:\\repo\\f.cs",
            LineNumber: 1,
            CyclomaticComplexity: 20,
            LineCount: 100,
            ParameterCount: 8,
            MaintainabilityIndex: 100);

        var report = ReportWith("C:\\repo", hotspot);
        var json = Parse(Render(report));
        var results = json.GetProperty("runs")[0].GetProperty("results");

        Assert.Equal(3, results.GetArrayLength());

        var ruleIds = results.EnumerateArray()
            .Select(r => r.GetProperty("ruleId").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal) { "CCA001", "CCA002", "CCA003" },
            ruleIds);
    }

    [Fact]
    public void WarningSeverityMapsToSarifWarning()
    {
        // CC=14: 14*2=28 < 10*3=30 → Warning
        var hotspot = new MethodMetrics(
            MethodName: "M",
            ContainingType: "C",
            FilePath: "C:\\repo\\f.cs",
            LineNumber: 1,
            CyclomaticComplexity: 14,
            LineCount: 1,
            ParameterCount: 0,
            MaintainabilityIndex: 100);

        var report = ReportWith("C:\\repo", hotspot);
        var json = Parse(Render(report));
        var results = json.GetProperty("runs")[0].GetProperty("results");

        Assert.Equal(1, results.GetArrayLength());
        Assert.Equal("warning", results[0].GetProperty("level").GetString());
        Assert.Equal("CCA001", results[0].GetProperty("ruleId").GetString());
    }

    [Fact]
    public void RelativePathsUseForwardSlashes()
    {
        var hotspot = new MethodMetrics(
            MethodName: "M",
            ContainingType: "C",
            FilePath: "C:\\repo\\src\\deep\\File.cs",
            LineNumber: 5,
            CyclomaticComplexity: 16,
            LineCount: 1,
            ParameterCount: 0,
            MaintainabilityIndex: 100);

        var report = ReportWith("C:\\repo", hotspot);
        var json = Parse(Render(report));
        var results = json.GetProperty("runs")[0].GetProperty("results");

        var uri = results[0]
            .GetProperty("locations")[0]
            .GetProperty("physicalLocation")
            .GetProperty("artifactLocation")
            .GetProperty("uri")
            .GetString()!;

        Assert.DoesNotContain('\\', uri);
        Assert.Equal("src/deep/File.cs", uri);
    }

    [Fact]
    public void MiFindingProducesCca004RuleId()
    {
        // MI=20, threshold default 50; 20 <= 25 → Error
        var hotspot = new MethodMetrics(
            MethodName: "M",
            ContainingType: "C",
            FilePath: "C:\\repo\\f.cs",
            LineNumber: 1,
            CyclomaticComplexity: 1,
            LineCount: 1,
            ParameterCount: 0,
            MaintainabilityIndex: 20);

        var report = ReportWith("C:\\repo", hotspot);
        var json = Parse(Render(report));
        var results = json.GetProperty("runs")[0].GetProperty("results");

        Assert.Equal(1, results.GetArrayLength());
        Assert.Equal("CCA004", results[0].GetProperty("ruleId").GetString());
        Assert.Equal("error", results[0].GetProperty("level").GetString());
    }
}

using CodeComplexityAnalyzer.Core.Models;
using CodeComplexityAnalyzer.Core.Reporting;

namespace CodeComplexityAnalyzer.Tests;

public class FindingFactoryTests
{
    private static readonly Thresholds DefaultThresholds = new(10, 60, 5);

    private static MethodMetrics Method(int cc = 1, int lines = 1, int parameters = 0) =>
        new(
            MethodName: "M",
            ContainingType: "C",
            FilePath: "f.cs",
            LineNumber: 1,
            CyclomaticComplexity: cc,
            LineCount: lines,
            ParameterCount: parameters);

    [Fact]
    public void NoViolationsProducesNoFindings()
    {
        var findings = FindingFactory.Create(Method(cc: 5, lines: 10, parameters: 2), DefaultThresholds);
        Assert.Empty(findings);
    }

    [Fact]
    public void OnlyCyclomaticComplexityViolatedProducesOneFinding()
    {
        var findings = FindingFactory.Create(Method(cc: 11, lines: 10, parameters: 2), DefaultThresholds).ToList();

        Assert.Single(findings);
        Assert.Equal(MetricType.CyclomaticComplexity, findings[0].MetricType);
        Assert.Equal(11, findings[0].Value);
        Assert.Equal(10, findings[0].Threshold);
        Assert.Equal(Severity.Warning, findings[0].Severity);
    }

    [Fact]
    public void AllThreeMetricsViolatedProducesThreeFindings()
    {
        var findings = FindingFactory.Create(Method(cc: 20, lines: 100, parameters: 8), DefaultThresholds).ToList();

        Assert.Equal(3, findings.Count);
        Assert.Contains(findings, f => f.MetricType == MetricType.CyclomaticComplexity);
        Assert.Contains(findings, f => f.MetricType == MetricType.LineCount);
        Assert.Contains(findings, f => f.MetricType == MetricType.ParameterCount);
    }

    [Fact]
    public void SeverityIsErrorAtExactlyOneAndAHalfTimesThreshold()
    {
        // threshold=10, value=15 -> 15*2=30 >= 10*3=30 -> Error
        var findings = FindingFactory.Create(Method(cc: 15), DefaultThresholds).ToList();
        Assert.Equal(Severity.Error, findings[0].Severity);
    }

    [Fact]
    public void SeverityIsWarningJustBelowOneAndAHalfTimesThreshold()
    {
        // threshold=10, value=14 -> 14*2=28 < 10*3=30 -> Warning
        var findings = FindingFactory.Create(Method(cc: 14), DefaultThresholds).ToList();
        Assert.Equal(Severity.Warning, findings[0].Severity);
    }

    [Fact]
    public void EachFindingCarriesMethodIdentity()
    {
        var method = new MethodMetrics(
            MethodName: "DoStuff",
            ContainingType: "Worker",
            FilePath: "src/Worker.cs",
            LineNumber: 42,
            CyclomaticComplexity: 11,
            LineCount: 1,
            ParameterCount: 0);

        var finding = FindingFactory.Create(method, DefaultThresholds).Single();

        Assert.Equal("DoStuff", finding.MethodName);
        Assert.Equal("Worker", finding.ContainingType);
        Assert.Equal("src/Worker.cs", finding.FilePath);
        Assert.Equal(42, finding.LineNumber);
    }
}

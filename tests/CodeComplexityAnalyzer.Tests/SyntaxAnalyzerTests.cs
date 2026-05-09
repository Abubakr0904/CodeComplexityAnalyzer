using CodeComplexityAnalyzer.Core.Analysis;
using CodeComplexityAnalyzer.Core.Models;

namespace CodeComplexityAnalyzer.Tests;

public class SyntaxAnalyzerTests
{
    private static readonly Thresholds DefaultThresholds = new(10, 60, 5);

    [Fact]
    public void AnalyzeSourceReturnsMetricsForEachMethod()
    {
        var analyzer = new SyntaxAnalyzer();

        var metrics = analyzer.AnalyzeSource(
            "class A { void M1() { } void M2(int x) { if (x > 0) { } } }",
            "a.cs");

        Assert.Equal(2, metrics.Count);
        Assert.Contains(metrics, m => m.MethodName == "M1");
        Assert.Contains(metrics, m => m.MethodName == "M2" && m.CyclomaticComplexity == 2);
    }

    [Fact]
    public void AnalyzeSourcesAggregatesMethodsFromAllInputs()
    {
        var analyzer = new SyntaxAnalyzer();

        var report = analyzer.AnalyzeSources(
            new[]
            {
                new SourceFile("a.cs", "class A { void M1() { } void M2(int x) { if (x > 0) { } } }"),
                new SourceFile("b.cs", "class B { int M3(int a, int b) => a + b; }"),
            },
            rootPath: "test-root",
            thresholds: DefaultThresholds);

        Assert.Equal(2, report.FilesAnalyzed);
        Assert.Equal(3, report.MethodsAnalyzed);
        Assert.Equal("test-root", report.RootPath);
    }

    [Fact]
    public void AnalyzeSourcesFlagsMethodsAboveThresholds()
    {
        var analyzer = new SyntaxAnalyzer();

        var report = analyzer.AnalyzeSources(
            new[]
            {
                new SourceFile(
                    "a.cs",
                    "class A { void Tiny() { } void Big(int a, int b, int c, int d, int e, int f, int g) { } }"),
            },
            rootPath: "test-root",
            thresholds: DefaultThresholds);

        Assert.Single(report.Hotspots);
        Assert.Equal("Big", report.Hotspots[0].MethodName);
    }
}

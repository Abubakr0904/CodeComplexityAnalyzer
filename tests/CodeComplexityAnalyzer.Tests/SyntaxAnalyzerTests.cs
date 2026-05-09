using CodeComplexityAnalyzer.Core.Analysis;
using CodeComplexityAnalyzer.Core.Models;

namespace CodeComplexityAnalyzer.Tests;

public class SyntaxAnalyzerTests : IDisposable
{
    private readonly string _tempDir;

    public SyntaxAnalyzerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "cca-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public void AnalyzesAllMethodsInDirectory()
    {
        WriteFile("a.cs", "class A { void M1() { } void M2(int x) { if (x > 0) { } } }");
        WriteFile("b.cs", "class B { int M3(int a, int b) => a + b; }");

        var report = new SyntaxAnalyzer().Analyze(AnalysisOptions.ForPath(_tempDir));

        Assert.Equal(2, report.FilesAnalyzed);
        Assert.Equal(3, report.MethodsAnalyzed);
    }

    [Fact]
    public void FlagsMethodsAboveThresholds()
    {
        WriteFile("a.cs", "class A { void Tiny() { } void Big(int a, int b, int c, int d, int e, int f, int g) { } }");

        var options = new AnalysisOptions(
            RootPath: _tempDir,
            Thresholds: new Thresholds(CyclomaticComplexity: 10, LineCount: 60, ParameterCount: 5),
            ExcludeDirectories: []);

        var report = new SyntaxAnalyzer().Analyze(options);

        Assert.Single(report.Hotspots);
        Assert.Equal("Big", report.Hotspots[0].MethodName);
    }

    [Fact]
    public void SkipsExcludedDirectories()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, "bin"));
        WriteFile("bin/Generated.cs", "class G { void M() { } }");
        WriteFile("real.cs", "class R { void M() { } }");

        var report = new SyntaxAnalyzer().Analyze(AnalysisOptions.ForPath(_tempDir));

        Assert.Equal(1, report.FilesAnalyzed);
        Assert.Equal("R", report.Methods[0].ContainingType);
    }

    private void WriteFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_tempDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }
}

using CodeComplexityAnalyzer.Cli;

namespace CodeComplexityAnalyzer.Tests;

public class CliRunnerTests : IDisposable
{
    private readonly string _tempDir;

    public CliRunnerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "cca-cli-tests-" + Guid.NewGuid().ToString("N"));
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
    public async Task ReturnsTwoWhenPathMissing()
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exit = await Runner.RunAsync(
            new[] { Path.Combine(_tempDir, "does-not-exist") },
            stdout,
            stderr);

        Assert.Equal(2, exit);
        Assert.Contains("Path not found", stderr.ToString());
    }

    [Fact]
    public async Task ReturnsZeroWhenNoHotspots()
    {
        var path = Path.Combine(_tempDir, "clean.cs");
        File.WriteAllText(path, "class A { void M() { } }");

        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exit = await Runner.RunAsync(new[] { path }, stdout, stderr);

        Assert.Equal(0, exit);
    }

    [Fact]
    public async Task ReturnsOneWhenHotspotsFound()
    {
        var path = Path.Combine(_tempDir, "hotspot.cs");
        // Method with 8 parameters (threshold default = 5)
        File.WriteAllText(
            path,
            "class A { void Big(int a, int b, int c, int d, int e, int f, int g, int h) { } }");

        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exit = await Runner.RunAsync(new[] { path }, stdout, stderr);

        Assert.Equal(1, exit);
    }

    [Fact]
    public async Task JsonFormatStillProducesCorrectExitCode()
    {
        var path = Path.Combine(_tempDir, "hotspot.cs");
        File.WriteAllText(
            path,
            "class A { void Big(int a, int b, int c, int d, int e, int f, int g, int h) { } }");

        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exit = await Runner.RunAsync(new[] { path, "--format", "json" }, stdout, stderr);

        Assert.Equal(1, exit);
        Assert.Contains("\"schemaVersion\"", stdout.ToString());
    }
}

using CodeComplexityAnalyzer.Cli;
using CodeComplexityAnalyzer.Core.Models;

namespace CodeComplexityAnalyzer.Cli.Tests;

public sealed class FileSourceCollectorTests : IDisposable
{
    private readonly string _tempDir;

    public FileSourceCollectorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"cca-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private string WriteFile(string relativePath, string content = "// stub")
    {
        var fullPath = Path.Combine(_tempDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
        return fullPath;
    }

    // -----------------------------------------------------------------------
    // Tests
    // -----------------------------------------------------------------------

    [Fact]
    public void Collect_SingleFilePath_ReturnsThatFile()
    {
        const string content = "public class Foo {}";
        var filePath = WriteFile("Single.cs", content);

        var results = FileSourceCollector.Collect(filePath, Array.Empty<string>()).ToList();

        Assert.Single(results);
        Assert.Equal(filePath, results[0].FilePath);
        Assert.Equal(content, results[0].SourceCode);
    }

    [Fact]
    public void Collect_Directory_ReturnsAllCsFilesRecursively()
    {
        WriteFile("Root.cs");
        WriteFile(Path.Combine("sub", "Sub.cs"));
        WriteFile(Path.Combine("sub", "deep", "Deep.cs"));
        // Non-.cs file should NOT be returned
        WriteFile(Path.Combine("sub", "readme.txt"));

        var results = FileSourceCollector.Collect(_tempDir, Array.Empty<string>()).ToList();

        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.EndsWith(".cs", r.FilePath, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Collect_ExcludesNamedDirectories()
    {
        // Files that should be returned
        var keepPath = WriteFile("Keep.cs");

        // Files that should be excluded
        WriteFile(Path.Combine("bin", "Compiled.cs"));
        WriteFile(Path.Combine("obj", "Generated.cs"));
        WriteFile(Path.Combine("node_modules", "Vendor.cs"));

        var excludes = new[] { "bin", "obj", "node_modules" };
        var results = FileSourceCollector.Collect(_tempDir, excludes).ToList();

        Assert.Single(results);
        Assert.Equal(keepPath, results[0].FilePath);
    }

    [Fact]
    public void Collect_ExcludesNamedDirectories_CaseInsensitive()
    {
        WriteFile("Keep.cs");
        WriteFile(Path.Combine("Bin", "Compiled.cs"));  // Different casing

        var excludes = new[] { "bin" };
        var results = FileSourceCollector.Collect(_tempDir, excludes).ToList();

        Assert.Single(results);
    }

    [Fact]
    public void Collect_NonExistentPath_ThrowsDirectoryNotFoundException()
    {
        var nonExistentPath = Path.Combine(_tempDir, "does-not-exist");

        // FileSourceCollector passes a non-existent directory path straight to
        // Directory.EnumerateFiles, which throws DirectoryNotFoundException.
        Assert.Throws<DirectoryNotFoundException>(() =>
            FileSourceCollector.Collect(nonExistentPath, Array.Empty<string>()).ToList());
    }

    [Fact]
    public void Collect_EmptyDirectory_ReturnsEmpty()
    {
        var results = FileSourceCollector.Collect(_tempDir, Array.Empty<string>()).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void Collect_ExcludeDoesNotMatchPartialSegmentNames()
    {
        // "bin" should not match a directory called "binary"
        WriteFile(Path.Combine("binary", "Source.cs"));

        var excludes = new[] { "bin" };
        var results = FileSourceCollector.Collect(_tempDir, excludes).ToList();

        Assert.Single(results);
    }
}

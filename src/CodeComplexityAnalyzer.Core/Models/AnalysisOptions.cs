namespace CodeComplexityAnalyzer.Core.Models;

public sealed record Thresholds(
    int CyclomaticComplexity = 10,
    int LineCount = 60,
    int ParameterCount = 5);

public sealed record AnalysisOptions(
    string RootPath,
    Thresholds Thresholds,
    IReadOnlyList<string> ExcludeDirectories)
{
    public static AnalysisOptions ForPath(string rootPath) => new(
        RootPath: rootPath,
        Thresholds: new Thresholds(),
        ExcludeDirectories: ["bin", "obj", ".git", "node_modules"]);
}

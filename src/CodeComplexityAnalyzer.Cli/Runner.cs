using System.CommandLine;
using CodeComplexityAnalyzer.Core.Analysis;
using CodeComplexityAnalyzer.Core.Models;
using CodeComplexityAnalyzer.Core.Reporting;

namespace CodeComplexityAnalyzer.Cli;

public static class Runner
{
    public static async Task<int> RunAsync(string[] args, TextWriter? stdout = null, TextWriter? stderr = null)
    {
        var outWriter = stdout ?? Console.Out;
        var errWriter = stderr ?? Console.Error;

        var pathArg = new Argument<string>(
            name: "path",
            description: "Path to a .cs file or directory of C# source.");

        var formatOption = new Option<string>(
            name: "--format",
            getDefaultValue: () => "console",
            description: "Output format: console or json.");
        formatOption.AddAlias("-f");

        var maxCcOption = new Option<int>(
            name: "--max-cc",
            getDefaultValue: () => 10,
            description: "Cyclomatic complexity threshold; methods above this are flagged.");

        var maxLinesOption = new Option<int>(
            name: "--max-lines",
            getDefaultValue: () => 60,
            description: "Method line count threshold.");

        var maxParamsOption = new Option<int>(
            name: "--max-params",
            getDefaultValue: () => 5,
            description: "Method parameter count threshold.");

        var rootCommand = new RootCommand("Static code complexity analyzer for C#.")
        {
            pathArg,
            formatOption,
            maxCcOption,
            maxLinesOption,
            maxParamsOption,
        };

        var exitCode = 0;

        rootCommand.SetHandler(
            (path, format, maxCc, maxLines, maxParams) =>
            {
                if (!File.Exists(path) && !Directory.Exists(path))
                {
                    errWriter.WriteLine($"Path not found: {path}");
                    exitCode = 2;
                    return;
                }

                var options = new AnalysisOptions(
                    RootPath: path,
                    Thresholds: new Thresholds(maxCc, maxLines, maxParams),
                    ExcludeDirectories: ["bin", "obj", ".git", "node_modules"]);

                var sources = FileSourceCollector.Collect(options.RootPath, options.ExcludeDirectories);
                var report = new SyntaxAnalyzer().AnalyzeSources(sources, options.RootPath, options.Thresholds);

                IReporter reporter = format.ToLowerInvariant() switch
                {
                    "json" => new JsonReporter(options.Thresholds),
                    "console" => new ConsoleReporter(),
                    _ => throw new ArgumentException($"Unknown format: {format}"),
                };

                reporter.Render(report, outWriter);
                outWriter.WriteLine();

                if (report.Hotspots.Count > 0)
                {
                    exitCode = 1;
                }
            },
            pathArg, formatOption, maxCcOption, maxLinesOption, maxParamsOption);

        var parseResult = await rootCommand.InvokeAsync(args);
        return parseResult != 0 ? parseResult : exitCode;
    }
}

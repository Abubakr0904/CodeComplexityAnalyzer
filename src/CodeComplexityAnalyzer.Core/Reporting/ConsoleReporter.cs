using CodeComplexityAnalyzer.Core.Models;

namespace CodeComplexityAnalyzer.Core.Reporting;

public sealed class ConsoleReporter : IReporter
{
    public void Render(AnalysisReport report, TextWriter writer)
    {
        writer.WriteLine($"Root: {report.RootPath}");
        writer.WriteLine($"Files analyzed: {report.FilesAnalyzed}");
        writer.WriteLine($"Methods analyzed: {report.MethodsAnalyzed}");
        writer.WriteLine($"Hotspots: {report.Hotspots.Count}");
        writer.WriteLine();

        if (report.Hotspots.Count == 0)
        {
            writer.WriteLine("No hotspots above thresholds.");
            return;
        }

        writer.WriteLine($"{"CC",-5} {"Lines",-6} {"Params",-7} Method");
        writer.WriteLine(new string('-', 80));

        foreach (var m in report.Hotspots)
        {
            var location = $"{m.ContainingType}.{m.MethodName}  ({m.FilePath}:{m.LineNumber})";
            writer.WriteLine($"{m.CyclomaticComplexity,-5} {m.LineCount,-6} {m.ParameterCount,-7} {location}");
        }
    }
}

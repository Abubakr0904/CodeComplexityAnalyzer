using System.Text.Json;
using CodeComplexityAnalyzer.Core.Models;

namespace CodeComplexityAnalyzer.Core.Reporting;

public sealed class JsonReporter : IReporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
    };

    public void Render(AnalysisReport report, TextWriter writer)
    {
        writer.Write(JsonSerializer.Serialize(report, Options));
    }
}

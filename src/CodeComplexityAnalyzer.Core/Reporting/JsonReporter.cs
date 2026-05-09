using System.Text.Json;
using System.Text.Json.Serialization;
using CodeComplexityAnalyzer.Core.Models;

namespace CodeComplexityAnalyzer.Core.Reporting;

public sealed class JsonReporter : IReporter
{
    private const string SchemaVersion = "1.0";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
        },
    };

    private readonly Thresholds _thresholds;

    public JsonReporter(Thresholds thresholds)
    {
        _thresholds = thresholds;
    }

    public void Render(AnalysisReport report, TextWriter writer)
    {
        var findings = report.Hotspots
            .SelectMany(m => FindingFactory.Create(m, _thresholds))
            .ToList();

        var document = new JsonReportDocument(
            SchemaVersion: SchemaVersion,
            RootPath: report.RootPath,
            FilesAnalyzed: report.FilesAnalyzed,
            MethodsAnalyzed: report.MethodsAnalyzed,
            Findings: findings);

        writer.Write(JsonSerializer.Serialize(document, Options));
    }
}

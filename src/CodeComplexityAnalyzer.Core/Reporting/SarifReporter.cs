using System.Text.Json;
using System.Text.Json.Serialization;
using CodeComplexityAnalyzer.Core.Models;

namespace CodeComplexityAnalyzer.Core.Reporting;

public sealed class SarifReporter : IReporter
{
    private const string SarifVersion = "2.1.0";
    private const string SarifSchema =
        "https://raw.githubusercontent.com/oasis-tcs/sarif-spec/master/Schemata/sarif-schema-2.1.0.json";
    private const string ToolVersion = "1.0.0";
    private const string InformationUri = "https://github.com/Abubakr0904/CodeComplexityAnalyzer";
    private const string HelpUri = "https://github.com/Abubakr0904/CodeComplexityAnalyzer#metrics";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly SarifRule[] Rules =
    [
        new SarifRule(
            Id: "CCA001",
            Name: "CyclomaticComplexity",
            ShortDescription: new SarifMessage("Method exceeds cyclomatic complexity threshold."),
            FullDescription: new SarifMessage("Decision points in the method body exceed the configured threshold. High cyclomatic complexity makes code harder to test and understand."),
            DefaultConfiguration: new SarifRuleConfiguration("warning"),
            HelpUri: HelpUri),

        new SarifRule(
            Id: "CCA002",
            Name: "LineCount",
            ShortDescription: new SarifMessage("Method exceeds line count threshold."),
            FullDescription: new SarifMessage("Method body length exceeds the configured threshold. Long methods often combine multiple responsibilities."),
            DefaultConfiguration: new SarifRuleConfiguration("warning"),
            HelpUri: HelpUri),

        new SarifRule(
            Id: "CCA003",
            Name: "ParameterCount",
            ShortDescription: new SarifMessage("Method exceeds parameter count threshold."),
            FullDescription: new SarifMessage("Method has more parameters than the configured threshold. Excess parameters often signal a missing data abstraction."),
            DefaultConfiguration: new SarifRuleConfiguration("warning"),
            HelpUri: HelpUri),
    ];

    private readonly Thresholds _thresholds;

    public SarifReporter(Thresholds thresholds)
    {
        _thresholds = thresholds;
    }

    public void Render(AnalysisReport report, TextWriter writer)
    {
        var findings = report.Hotspots
            .SelectMany(m => FindingFactory.Create(m, _thresholds))
            .ToList();

        var results = findings
            .Select(f => ToSarifResult(f, report.RootPath))
            .ToList();

        var document = new SarifDocument(
            Version: SarifVersion,
            Schema: SarifSchema,
            Runs:
            [
                new SarifRun(
                    Tool: new SarifTool(
                        Driver: new SarifDriver(
                            Name: "cca",
                            Version: ToolVersion,
                            SemanticVersion: ToolVersion,
                            InformationUri: InformationUri,
                            Rules: Rules)),
                    Results: results)
            ]);

        writer.Write(JsonSerializer.Serialize(document, SerializerOptions));
    }

    private static SarifResult ToSarifResult(Finding finding, string rootPath)
    {
        var ruleId = finding.MetricType switch
        {
            MetricType.CyclomaticComplexity => "CCA001",
            MetricType.LineCount => "CCA002",
            MetricType.ParameterCount => "CCA003",
            _ => throw new InvalidOperationException($"Unknown MetricType: {finding.MetricType}"),
        };

        var level = finding.Severity switch
        {
            Severity.Error => "error",
            Severity.Warning => "warning",
            _ => throw new InvalidOperationException($"Unknown Severity: {finding.Severity}"),
        };

        var metricLabel = finding.MetricType switch
        {
            MetricType.CyclomaticComplexity => "cyclomatic complexity",
            MetricType.LineCount => "line count",
            MetricType.ParameterCount => "parameter count",
            _ => throw new InvalidOperationException($"Unknown MetricType: {finding.MetricType}"),
        };

        var messageText =
            $"Method '{finding.MethodName}' has {metricLabel} {finding.Value} (threshold: {finding.Threshold}).";

        var uri = BuildUri(finding.FilePath, rootPath);

        return new SarifResult(
            RuleId: ruleId,
            Level: level,
            Message: new SarifMessage(messageText),
            Locations:
            [
                new SarifLocation(
                    PhysicalLocation: new SarifPhysicalLocation(
                        ArtifactLocation: new SarifArtifactLocation(Uri: uri),
                        Region: new SarifRegion(StartLine: finding.LineNumber)))
            ]);
    }

    private static string BuildUri(string filePath, string rootPath)
    {
        // rootPath may be a single file — use filename only in that case
        string relative;
        try
        {
            if (File.Exists(rootPath))
            {
                relative = Path.GetFileName(filePath);
            }
            else
            {
                relative = Path.GetRelativePath(rootPath, filePath);
            }
        }
        catch
        {
            relative = filePath;
        }

        return relative.Replace('\\', '/');
    }

    // ── DTOs ─────────────────────────────────────────────────────────────────

    private sealed record SarifDocument(
        string Version,
        [property: JsonPropertyName("$schema")] string Schema,
        IReadOnlyList<SarifRun> Runs);

    private sealed record SarifRun(
        SarifTool Tool,
        IReadOnlyList<SarifResult> Results);

    private sealed record SarifTool(SarifDriver Driver);

    private sealed record SarifDriver(
        string Name,
        string Version,
        string SemanticVersion,
        string InformationUri,
        IReadOnlyList<SarifRule> Rules);

    private sealed record SarifRule(
        string Id,
        string Name,
        SarifMessage ShortDescription,
        SarifMessage FullDescription,
        SarifRuleConfiguration DefaultConfiguration,
        string HelpUri);

    private sealed record SarifRuleConfiguration(string Level);

    private sealed record SarifResult(
        string RuleId,
        string Level,
        SarifMessage Message,
        IReadOnlyList<SarifLocation> Locations);

    private sealed record SarifMessage(string Text);

    private sealed record SarifLocation(SarifPhysicalLocation PhysicalLocation);

    private sealed record SarifPhysicalLocation(
        SarifArtifactLocation ArtifactLocation,
        SarifRegion Region);

    private sealed record SarifArtifactLocation(string Uri);

    private sealed record SarifRegion(int StartLine);
}

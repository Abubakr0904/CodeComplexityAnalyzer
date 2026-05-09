using CodeComplexityAnalyzer.Core.Models;

namespace CodeComplexityAnalyzer.Core.Reporting;

public static class FindingFactory
{
    public static IEnumerable<Finding> Create(MethodMetrics method, Thresholds thresholds)
    {
        if (method.CyclomaticComplexity > thresholds.CyclomaticComplexity)
        {
            yield return Build(method, MetricType.CyclomaticComplexity,
                method.CyclomaticComplexity, thresholds.CyclomaticComplexity);
        }

        if (method.LineCount > thresholds.LineCount)
        {
            yield return Build(method, MetricType.LineCount,
                method.LineCount, thresholds.LineCount);
        }

        if (method.ParameterCount > thresholds.ParameterCount)
        {
            yield return Build(method, MetricType.ParameterCount,
                method.ParameterCount, thresholds.ParameterCount);
        }

        // MI is inverted: lower value means worse maintainability, so flag when value < threshold.
        if (method.MaintainabilityIndex < thresholds.MaintainabilityIndex)
        {
            yield return BuildMi(method, thresholds.MaintainabilityIndex);
        }
    }

    private static Finding Build(MethodMetrics m, MetricType type, int value, int threshold) =>
        new(
            FilePath: m.FilePath,
            LineNumber: m.LineNumber,
            MethodName: m.MethodName,
            ContainingType: m.ContainingType,
            MetricType: type,
            Value: value,
            Threshold: threshold,
            Severity: SeverityFor(value, threshold));

    private static Finding BuildMi(MethodMetrics m, int threshold) =>
        new(
            FilePath: m.FilePath,
            LineNumber: m.LineNumber,
            MethodName: m.MethodName,
            ContainingType: m.ContainingType,
            MetricType: MetricType.MaintainabilityIndex,
            Value: m.MaintainabilityIndex,
            Threshold: threshold,
            // Inverted severity: the lower the MI, the worse; Error when MI <= threshold/2.
            Severity: m.MaintainabilityIndex <= threshold / 2 ? Severity.Error : Severity.Warning);

    private static Severity SeverityFor(int value, int threshold) =>
        value * 2 >= threshold * 3 ? Severity.Error : Severity.Warning;
}

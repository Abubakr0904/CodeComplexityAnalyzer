namespace CodeComplexityAnalyzer.Core.Metrics;

public static class MaintainabilityIndexCalculator
{
    // Simplified MI formula — Halstead Volume term omitted because computing HV
    // requires counting distinct operators/operands, which is not trivially available
    // from a syntax-only Roslyn walk (no semantic model). The original Microsoft formula
    // is: MI = 171 - 5.2*ln(HV) - 0.23*CC - 16.2*ln(LOC). We drop the HV term.
    // This produces a consistently higher MI than the full formula; set thresholds
    // accordingly (default 50 is calibrated for this simplified version).
    public static int Calculate(int cyclomaticComplexity, int lineCount)
    {
        if (lineCount <= 0) return 100;
        var raw = 171.0 - 0.23 * cyclomaticComplexity - 16.2 * Math.Log(lineCount);
        if (raw < 0) return 0;
        if (raw > 100) return 100;
        return (int)Math.Round(raw);
    }
}

using CodeComplexityAnalyzer.Core.Metrics;

namespace CodeComplexityAnalyzer.Tests;

public class MaintainabilityIndexCalculatorTests
{
    [Fact]
    public void ZeroLineCountReturns100()
    {
        Assert.Equal(100, MaintainabilityIndexCalculator.Calculate(cyclomaticComplexity: 1, lineCount: 0));
    }

    [Fact]
    public void SimpleOneLineMethodHasHighMaintainability()
    {
        // CC=1, LOC=1 → 171 - 0.23*1 - 16.2*ln(1) = 171 - 0.23 - 0 ≈ 171 → clamped to 100
        var mi = MaintainabilityIndexCalculator.Calculate(cyclomaticComplexity: 1, lineCount: 1);
        Assert.Equal(100, mi);
    }

    [Fact]
    public void HighComplexityLongMethodHasLowMaintainability()
    {
        // CC=30, LOC=200 → 171 - 0.23*30 - 16.2*ln(200) ≈ 171 - 6.9 - 85.7 ≈ 78.4 → 78
        // Still reasonably high without Halstead term but confirm it's lower than simple method
        var simple = MaintainabilityIndexCalculator.Calculate(cyclomaticComplexity: 1, lineCount: 1);
        var complex = MaintainabilityIndexCalculator.Calculate(cyclomaticComplexity: 30, lineCount: 200);
        Assert.True(complex < simple, $"Complex MI ({complex}) should be less than simple MI ({simple})");
    }

    [Fact]
    public void ResultIsClampedToZeroMinimum()
    {
        // Extremely large values — result must never go negative
        var mi = MaintainabilityIndexCalculator.Calculate(cyclomaticComplexity: 1000, lineCount: 100_000);
        Assert.Equal(0, mi);
    }

    [Fact]
    public void ResultIsClampedTo100Maximum()
    {
        // Very simple method — raw value exceeds 100, must be clamped
        var mi = MaintainabilityIndexCalculator.Calculate(cyclomaticComplexity: 1, lineCount: 2);
        Assert.Equal(100, mi);
    }

    [Fact]
    public void IntermediateMethodHasMidRangeMaintainability()
    {
        // CC=10, LOC=50 → 171 - 0.23*10 - 16.2*ln(50) ≈ 171 - 2.3 - 63.2 ≈ 105.5 → clamped 100
        // CC=15, LOC=80 → 171 - 0.23*15 - 16.2*ln(80) ≈ 171 - 3.45 - 70.9 ≈ 96.6
        // Use CC=20, LOC=200 to get into mid range: 171 - 4.6 - 85.7 ≈ 80.7
        var mi = MaintainabilityIndexCalculator.Calculate(cyclomaticComplexity: 20, lineCount: 200);
        Assert.InRange(mi, 1, 99);
    }
}

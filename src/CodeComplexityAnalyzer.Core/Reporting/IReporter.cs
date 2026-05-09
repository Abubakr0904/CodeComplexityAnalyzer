using CodeComplexityAnalyzer.Core.Models;

namespace CodeComplexityAnalyzer.Core.Reporting;

public interface IReporter
{
    void Render(AnalysisReport report, TextWriter writer);
}

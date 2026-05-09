using CodeComplexityAnalyzer.Core.Metrics;
using CodeComplexityAnalyzer.Core.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeComplexityAnalyzer.Core.Analysis;

public static class MethodAnalyzer
{
    public static MethodMetrics Analyze(MethodDeclarationSyntax method, string filePath)
    {
        var startLine = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        var containingType = FindContainingType(method);

        var cc = CyclomaticComplexityCalculator.Calculate(method);
        var loc = MethodLengthCalculator.Calculate(method);

        return new MethodMetrics(
            MethodName: method.Identifier.Text,
            ContainingType: containingType,
            FilePath: filePath,
            LineNumber: startLine,
            CyclomaticComplexity: cc,
            LineCount: loc,
            ParameterCount: ParameterCountCalculator.Calculate(method),
            MaintainabilityIndex: MaintainabilityIndexCalculator.Calculate(cc, loc));
    }

    private static string FindContainingType(MethodDeclarationSyntax method)
    {
        for (var node = method.Parent; node is not null; node = node.Parent)
        {
            if (node is TypeDeclarationSyntax type)
            {
                return type.Identifier.Text;
            }
        }
        return "<global>";
    }
}

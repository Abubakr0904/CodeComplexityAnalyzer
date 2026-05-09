using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeComplexityAnalyzer.Core.Metrics;

public static class ParameterCountCalculator
{
    public static int Calculate(MethodDeclarationSyntax method) =>
        method.ParameterList.Parameters.Count;
}

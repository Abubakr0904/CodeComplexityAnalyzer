using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeComplexityAnalyzer.Core.Metrics;

public static class MethodLengthCalculator
{
    public static int Calculate(MethodDeclarationSyntax method)
    {
        if (method.Body is { } body)
        {
            var span = body.GetLocation().GetLineSpan();
            return span.EndLinePosition.Line - span.StartLinePosition.Line + 1;
        }

        if (method.ExpressionBody is { } expr)
        {
            var span = expr.GetLocation().GetLineSpan();
            return span.EndLinePosition.Line - span.StartLinePosition.Line + 1;
        }

        return 0;
    }
}

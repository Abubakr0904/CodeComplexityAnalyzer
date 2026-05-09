using CodeComplexityAnalyzer.Core.Metrics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeComplexityAnalyzer.Tests;

public class CyclomaticComplexityCalculatorTests
{
    [Fact]
    public void StraightLineMethodHasComplexityOne()
    {
        var method = ParseMethod("void M() { var x = 1; }");
        Assert.Equal(1, CyclomaticComplexityCalculator.Calculate(method));
    }

    [Fact]
    public void IfStatementAddsOne()
    {
        var method = ParseMethod("void M(int x) { if (x > 0) { } }");
        Assert.Equal(2, CyclomaticComplexityCalculator.Calculate(method));
    }

    [Fact]
    public void IfElseIfAddsTwo()
    {
        var method = ParseMethod("void M(int x) { if (x > 0) { } else if (x < 0) { } }");
        Assert.Equal(3, CyclomaticComplexityCalculator.Calculate(method));
    }

    [Fact]
    public void LogicalAndAddsOne()
    {
        var method = ParseMethod("void M(bool a, bool b) { if (a && b) { } }");
        Assert.Equal(3, CyclomaticComplexityCalculator.Calculate(method));
    }

    [Fact]
    public void SwitchExpressionArmAddsOnePerArm()
    {
        var method = ParseMethod("""
            string M(int x) => x switch
            {
                1 => "one",
                2 => "two",
                _ => "other",
            };
            """);
        Assert.Equal(4, CyclomaticComplexityCalculator.Calculate(method));
    }

    [Fact]
    public void ForeachAndCatchAddOneEach()
    {
        var method = ParseMethod("""
            void M(System.Collections.Generic.List<int> xs)
            {
                try
                {
                    foreach (var x in xs) { }
                }
                catch { }
            }
            """);
        Assert.Equal(3, CyclomaticComplexityCalculator.Calculate(method));
    }

    private static MethodDeclarationSyntax ParseMethod(string source)
    {
        var tree = CSharpSyntaxTree.ParseText("class C { " + source + " }");
        return tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
    }
}

# CodeComplexityAnalyzer

## Codebase Overview

CodeComplexityAnalyzer (`cca`) is a .NET 10 CLI that uses Roslyn's syntax-only API to scan C# source files and flag methods that exceed configurable thresholds for cyclomatic complexity, line count, and parameter count. It does not load MSBuild projects or build a semantic model — it parses raw `.cs` files, walks `MethodDeclarationSyntax` nodes, and emits either an aligned console table or indented JSON. Exit codes: `0` clean, `1` hotspots found, `2` path missing.

**Stack**: .NET 10 (`net10.0`, SDK pinned via [global.json](global.json)), Roslyn (`Microsoft.CodeAnalysis.CSharp`), `System.CommandLine`, xUnit. Nullable reference types, `TreatWarningsAsErrors`, and Central Package Management ([Directory.Packages.props](Directory.Packages.props)) are enabled solution-wide.

**Structure**:
- [src/CodeComplexityAnalyzer.Cli/](src/CodeComplexityAnalyzer.Cli/) — entry point (`AssemblyName=cca`)
- [src/CodeComplexityAnalyzer.Core/](src/CodeComplexityAnalyzer.Core/) — engine: `Analysis/`, `Metrics/`, `Models/`, `Reporting/`
- [tests/CodeComplexityAnalyzer.Tests/](tests/CodeComplexityAnalyzer.Tests/) — xUnit tests, references Core only
- [samples/UglyCode.cs](samples/UglyCode.cs) — fixture with deliberate hotspots

For detailed architecture, per-file analysis, gotchas, and navigation recipes, see [docs/CODEBASE_MAP.md](docs/CODEBASE_MAP.md).

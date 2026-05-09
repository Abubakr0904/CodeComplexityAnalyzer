# CodeComplexityAnalyzer (cca)

Static code complexity analyzer for C# source. Built on Roslyn syntax APIs (no MSBuild / no semantic model required).

**Live web demo:** https://abubakr0904.github.io/CodeComplexityAnalyzer/

## Run

```pwsh
dotnet run --project src/CodeComplexityAnalyzer.Cli -- samples
dotnet run --project src/CodeComplexityAnalyzer.Cli -- samples --format json
dotnet run --project src/CodeComplexityAnalyzer.Cli -- <path> --max-cc 8 --max-lines 40 --max-params 4
```

Exits with code `1` when hotspots are found, `0` when clean, `2` when the path is invalid.

## Metrics

| Metric | Description |
|---|---|
| Cyclomatic complexity | Decision points (`if`, `for`, `while`, `case`, `catch`, `&&`, `\|\|`, `??`, `?:`) + 1 |
| Line count | Lines spanned by the method body |
| Parameter count | Number of parameters declared |

## Layout

- `src/CodeComplexityAnalyzer.Core` — analyzer engine, metrics, reporters
- `src/CodeComplexityAnalyzer.Cli` — `cca` command-line entry point
- `tests/CodeComplexityAnalyzer.Tests` — xUnit tests
- `samples/` — input fixtures

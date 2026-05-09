# CodeComplexityAnalyzer (cca)

Static code complexity analyzer for C# source. Built on Roslyn syntax APIs (no MSBuild / no semantic model required).

**Live web demo:** https://abubakr0904.github.io/CodeComplexityAnalyzer/

## Run

```pwsh
dotnet run --project src/CodeComplexityAnalyzer.Cli -- samples
dotnet run --project src/CodeComplexityAnalyzer.Cli -- samples --format json
dotnet run --project src/CodeComplexityAnalyzer.Cli -- samples --format sarif
dotnet run --project src/CodeComplexityAnalyzer.Cli -- <path> --max-cc 8 --max-lines 40 --max-params 4
```

Exits with code `1` when hotspots are found, `0` when clean, `2` when the path is invalid.

## CI integration via SARIF

Upload findings to GitHub code scanning:

```yaml
- run: dotnet run --project src/CodeComplexityAnalyzer.Cli -- src --format sarif > cca.sarif
- uses: github/codeql-action/upload-sarif@v3
  with:
    sarif_file: cca.sarif
    category: cca
```

## PR comment bot

The workflow `.github/workflows/pr-complexity-comment.yml` runs automatically on every pull request targeting `main`. It runs `cca` against `src/`, then posts (or updates) a sticky comment on the PR summarising findings grouped by severity. If the analysis is clean, the comment says so. The comment is updated in place on subsequent pushes — it does not stack.

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

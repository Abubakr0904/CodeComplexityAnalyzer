# JSON Output Format Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the current "dump the whole `AnalysisReport`" JSON output with a structured per-violation `findings[]` schema (file path, metric type, value, threshold, severity), and add unit tests for the JSON reporter.

**Architecture:** New types live alongside existing reporting (`Core/Reporting/`). A small `FindingFactory` converts a `MethodMetrics` + `Thresholds` into zero or more `Finding` records (one per violated metric). `JsonReporter` becomes stateful (constructed with `Thresholds`) so it can build the per-finding shape; `IReporter`'s interface is unchanged. CLI passes `Thresholds` into `JsonReporter`'s constructor.

**Tech Stack:** .NET 10, C#, System.Text.Json, xUnit (existing), no new dependencies.

**Source spec:** Abubakr0904/CodeComplexityAnalyzer#1; DoR output dated 2026-05-09.

**Branch:** `feature/issue-1-json-output` (already created).

---

## File Structure

**Create:**
- `src/CodeComplexityAnalyzer.Core/Reporting/MetricType.cs` — enum: `CyclomaticComplexity`, `LineCount`, `ParameterCount`
- `src/CodeComplexityAnalyzer.Core/Reporting/Severity.cs` — enum: `Warning`, `Error`
- `src/CodeComplexityAnalyzer.Core/Reporting/Finding.cs` — single-violation record
- `src/CodeComplexityAnalyzer.Core/Reporting/JsonReportDocument.cs` — top-level DTO with `schemaVersion`, summary, and `findings[]`
- `src/CodeComplexityAnalyzer.Core/Reporting/FindingFactory.cs` — pure logic: `MethodMetrics` + `Thresholds` → `IEnumerable<Finding>`
- `tests/CodeComplexityAnalyzer.Tests/JsonReporterTests.cs` — xUnit tests for the reporter and factory

**Modify:**
- `src/CodeComplexityAnalyzer.Core/Reporting/JsonReporter.cs` — new shape; ctor takes `Thresholds`
- `src/CodeComplexityAnalyzer.Cli/Program.cs:57-62` — pass `Thresholds` into `new JsonReporter(...)`

**Untouched (per spec):**
- `src/CodeComplexityAnalyzer.Core/Models/AnalysisReport.cs`
- `src/CodeComplexityAnalyzer.Core/Models/MethodMetrics.cs`
- `src/CodeComplexityAnalyzer.Core/Reporting/IReporter.cs`
- `src/CodeComplexityAnalyzer.Core/Reporting/ConsoleReporter.cs`

---

## Severity rule (used throughout)

Given a violation (`value > threshold`):
- **Error** when `value * 2 >= threshold * 3` (i.e. `value ≥ 1.5 × threshold`)
- **Warning** otherwise

Integer comparison only — no floating-point math. Examples:
- threshold=10, value=14 → Warning (14×2=28, 10×3=30, 28<30)
- threshold=10, value=15 → Error (15×2=30, 10×3=30, 30≥30)
- threshold=5, value=7 → Warning (14<15)
- threshold=5, value=8 → Error (16≥15)

---

## JSON shape (target)

```json
{
  "schemaVersion": "1.0",
  "rootPath": "samples",
  "filesAnalyzed": 2,
  "methodsAnalyzed": 7,
  "findings": [
    {
      "filePath": "samples/Bad.cs",
      "lineNumber": 12,
      "methodName": "DoEverything",
      "containingType": "BadClass",
      "metricType": "cyclomaticComplexity",
      "value": 18,
      "threshold": 10,
      "severity": "error"
    }
  ]
}
```

camelCase property names, enums serialized as camelCase strings, indented output preserved.

---

## Task 1: Add `Severity` enum

**Files:**
- Create: `src/CodeComplexityAnalyzer.Core/Reporting/Severity.cs`

- [ ] **Step 1: Create the enum**

```csharp
namespace CodeComplexityAnalyzer.Core.Reporting;

public enum Severity
{
    Warning,
    Error,
}
```

- [ ] **Step 2: Verify it compiles**

Run: `dotnet build src/CodeComplexityAnalyzer.Core/CodeComplexityAnalyzer.Core.csproj`
Expected: Build succeeded. 0 Warning(s) 0 Error(s).

- [ ] **Step 3: Commit**

```bash
git add src/CodeComplexityAnalyzer.Core/Reporting/Severity.cs
git commit -m "Add Severity enum for JSON findings"
```

---

## Task 2: Add `MetricType` enum

**Files:**
- Create: `src/CodeComplexityAnalyzer.Core/Reporting/MetricType.cs`

- [ ] **Step 1: Create the enum**

```csharp
namespace CodeComplexityAnalyzer.Core.Reporting;

public enum MetricType
{
    CyclomaticComplexity,
    LineCount,
    ParameterCount,
}
```

- [ ] **Step 2: Verify it compiles**

Run: `dotnet build src/CodeComplexityAnalyzer.Core/CodeComplexityAnalyzer.Core.csproj`
Expected: Build succeeded. 0 Warning(s) 0 Error(s).

- [ ] **Step 3: Commit**

```bash
git add src/CodeComplexityAnalyzer.Core/Reporting/MetricType.cs
git commit -m "Add MetricType enum for JSON findings"
```

---

## Task 3: Add `Finding` record

**Files:**
- Create: `src/CodeComplexityAnalyzer.Core/Reporting/Finding.cs`

- [ ] **Step 1: Create the record**

```csharp
namespace CodeComplexityAnalyzer.Core.Reporting;

public sealed record Finding(
    string FilePath,
    int LineNumber,
    string MethodName,
    string ContainingType,
    MetricType MetricType,
    int Value,
    int Threshold,
    Severity Severity);
```

- [ ] **Step 2: Verify it compiles**

Run: `dotnet build src/CodeComplexityAnalyzer.Core/CodeComplexityAnalyzer.Core.csproj`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add src/CodeComplexityAnalyzer.Core/Reporting/Finding.cs
git commit -m "Add Finding record for JSON output schema"
```

---

## Task 4: Add `JsonReportDocument` top-level DTO

**Files:**
- Create: `src/CodeComplexityAnalyzer.Core/Reporting/JsonReportDocument.cs`

- [ ] **Step 1: Create the DTO**

```csharp
namespace CodeComplexityAnalyzer.Core.Reporting;

public sealed record JsonReportDocument(
    string SchemaVersion,
    string RootPath,
    int FilesAnalyzed,
    int MethodsAnalyzed,
    IReadOnlyList<Finding> Findings);
```

- [ ] **Step 2: Verify it compiles**

Run: `dotnet build src/CodeComplexityAnalyzer.Core/CodeComplexityAnalyzer.Core.csproj`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add src/CodeComplexityAnalyzer.Core/Reporting/JsonReportDocument.cs
git commit -m "Add JsonReportDocument top-level DTO"
```

---

## Task 5: Add `FindingFactory` (TDD)

**Files:**
- Create: `src/CodeComplexityAnalyzer.Core/Reporting/FindingFactory.cs`
- Create: `tests/CodeComplexityAnalyzer.Tests/FindingFactoryTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/CodeComplexityAnalyzer.Tests/FindingFactoryTests.cs`:

```csharp
using CodeComplexityAnalyzer.Core.Models;
using CodeComplexityAnalyzer.Core.Reporting;

namespace CodeComplexityAnalyzer.Tests;

public class FindingFactoryTests
{
    private static readonly Thresholds DefaultThresholds = new(10, 60, 5);

    private static MethodMetrics Method(int cc = 1, int lines = 1, int parameters = 0) =>
        new(
            MethodName: "M",
            ContainingType: "C",
            FilePath: "f.cs",
            LineNumber: 1,
            CyclomaticComplexity: cc,
            LineCount: lines,
            ParameterCount: parameters);

    [Fact]
    public void NoViolationsProducesNoFindings()
    {
        var findings = FindingFactory.Create(Method(cc: 5, lines: 10, parameters: 2), DefaultThresholds);
        Assert.Empty(findings);
    }

    [Fact]
    public void OnlyCyclomaticComplexityViolatedProducesOneFinding()
    {
        var findings = FindingFactory.Create(Method(cc: 11, lines: 10, parameters: 2), DefaultThresholds).ToList();

        Assert.Single(findings);
        Assert.Equal(MetricType.CyclomaticComplexity, findings[0].MetricType);
        Assert.Equal(11, findings[0].Value);
        Assert.Equal(10, findings[0].Threshold);
        Assert.Equal(Severity.Warning, findings[0].Severity);
    }

    [Fact]
    public void AllThreeMetricsViolatedProducesThreeFindings()
    {
        var findings = FindingFactory.Create(Method(cc: 20, lines: 100, parameters: 8), DefaultThresholds).ToList();

        Assert.Equal(3, findings.Count);
        Assert.Contains(findings, f => f.MetricType == MetricType.CyclomaticComplexity);
        Assert.Contains(findings, f => f.MetricType == MetricType.LineCount);
        Assert.Contains(findings, f => f.MetricType == MetricType.ParameterCount);
    }

    [Fact]
    public void SeverityIsErrorAtExactlyOneAndAHalfTimesThreshold()
    {
        // threshold=10, value=15 -> 15*2=30 >= 10*3=30 -> Error
        var findings = FindingFactory.Create(Method(cc: 15), DefaultThresholds).ToList();
        Assert.Equal(Severity.Error, findings[0].Severity);
    }

    [Fact]
    public void SeverityIsWarningJustBelowOneAndAHalfTimesThreshold()
    {
        // threshold=10, value=14 -> 14*2=28 < 10*3=30 -> Warning
        var findings = FindingFactory.Create(Method(cc: 14), DefaultThresholds).ToList();
        Assert.Equal(Severity.Warning, findings[0].Severity);
    }

    [Fact]
    public void EachFindingCarriesMethodIdentity()
    {
        var method = new MethodMetrics(
            MethodName: "DoStuff",
            ContainingType: "Worker",
            FilePath: "src/Worker.cs",
            LineNumber: 42,
            CyclomaticComplexity: 11,
            LineCount: 1,
            ParameterCount: 0);

        var finding = FindingFactory.Create(method, DefaultThresholds).Single();

        Assert.Equal("DoStuff", finding.MethodName);
        Assert.Equal("Worker", finding.ContainingType);
        Assert.Equal("src/Worker.cs", finding.FilePath);
        Assert.Equal(42, finding.LineNumber);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/CodeComplexityAnalyzer.Tests/CodeComplexityAnalyzer.Tests.csproj`
Expected: Build error — `FindingFactory` does not exist. (Compile failure is acceptable for the failing-test step in C#.)

- [ ] **Step 3: Implement `FindingFactory`**

Create `src/CodeComplexityAnalyzer.Core/Reporting/FindingFactory.cs`:

```csharp
using CodeComplexityAnalyzer.Core.Models;

namespace CodeComplexityAnalyzer.Core.Reporting;

public static class FindingFactory
{
    public static IEnumerable<Finding> Create(MethodMetrics method, Thresholds thresholds)
    {
        if (method.CyclomaticComplexity > thresholds.CyclomaticComplexity)
        {
            yield return Build(method, MetricType.CyclomaticComplexity,
                method.CyclomaticComplexity, thresholds.CyclomaticComplexity);
        }

        if (method.LineCount > thresholds.LineCount)
        {
            yield return Build(method, MetricType.LineCount,
                method.LineCount, thresholds.LineCount);
        }

        if (method.ParameterCount > thresholds.ParameterCount)
        {
            yield return Build(method, MetricType.ParameterCount,
                method.ParameterCount, thresholds.ParameterCount);
        }
    }

    private static Finding Build(MethodMetrics m, MetricType type, int value, int threshold) =>
        new(
            FilePath: m.FilePath,
            LineNumber: m.LineNumber,
            MethodName: m.MethodName,
            ContainingType: m.ContainingType,
            MetricType: type,
            Value: value,
            Threshold: threshold,
            Severity: SeverityFor(value, threshold));

    private static Severity SeverityFor(int value, int threshold) =>
        value * 2 >= threshold * 3 ? Severity.Error : Severity.Warning;
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/CodeComplexityAnalyzer.Tests/CodeComplexityAnalyzer.Tests.csproj`
Expected: All tests pass (existing tests + 6 new `FindingFactoryTests`).

- [ ] **Step 5: Commit**

```bash
git add src/CodeComplexityAnalyzer.Core/Reporting/FindingFactory.cs tests/CodeComplexityAnalyzer.Tests/FindingFactoryTests.cs
git commit -m "Add FindingFactory: build per-violation Findings from MethodMetrics + Thresholds"
```

---

## Task 6: Rewrite `JsonReporter` to emit the new shape (TDD)

**Files:**
- Modify: `src/CodeComplexityAnalyzer.Core/Reporting/JsonReporter.cs`
- Create: `tests/CodeComplexityAnalyzer.Tests/JsonReporterTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/CodeComplexityAnalyzer.Tests/JsonReporterTests.cs`:

```csharp
using System.Text.Json;
using CodeComplexityAnalyzer.Core.Models;
using CodeComplexityAnalyzer.Core.Reporting;

namespace CodeComplexityAnalyzer.Tests;

public class JsonReporterTests
{
    private static readonly Thresholds DefaultThresholds = new(10, 60, 5);

    private static string Render(AnalysisReport report, Thresholds thresholds)
    {
        using var sw = new StringWriter();
        new JsonReporter(thresholds).Render(report, sw);
        return sw.ToString();
    }

    private static JsonElement Parse(string json) =>
        JsonDocument.Parse(json).RootElement;

    [Fact]
    public void EmptyReportProducesEmptyFindingsArray()
    {
        var report = new AnalysisReport(
            RootPath: "p",
            FilesAnalyzed: 0,
            MethodsAnalyzed: 0,
            Methods: Array.Empty<MethodMetrics>(),
            Hotspots: Array.Empty<MethodMetrics>());

        var json = Parse(Render(report, DefaultThresholds));

        Assert.Equal("1.0", json.GetProperty("schemaVersion").GetString());
        Assert.Equal("p", json.GetProperty("rootPath").GetString());
        Assert.Equal(0, json.GetProperty("filesAnalyzed").GetInt32());
        Assert.Equal(0, json.GetProperty("methodsAnalyzed").GetInt32());
        Assert.Equal(0, json.GetProperty("findings").GetArrayLength());
    }

    [Fact]
    public void HotspotsProduceFindingsWithCamelCasePropertiesAndEnumStrings()
    {
        var hotspot = new MethodMetrics(
            MethodName: "DoStuff",
            ContainingType: "Worker",
            FilePath: "src/Worker.cs",
            LineNumber: 42,
            CyclomaticComplexity: 16,
            LineCount: 1,
            ParameterCount: 0);

        var report = new AnalysisReport(
            RootPath: "src",
            FilesAnalyzed: 1,
            MethodsAnalyzed: 1,
            Methods: new[] { hotspot },
            Hotspots: new[] { hotspot });

        var json = Parse(Render(report, DefaultThresholds));
        var findings = json.GetProperty("findings");

        Assert.Equal(1, findings.GetArrayLength());
        var f = findings[0];
        Assert.Equal("src/Worker.cs", f.GetProperty("filePath").GetString());
        Assert.Equal(42, f.GetProperty("lineNumber").GetInt32());
        Assert.Equal("DoStuff", f.GetProperty("methodName").GetString());
        Assert.Equal("Worker", f.GetProperty("containingType").GetString());
        Assert.Equal("cyclomaticComplexity", f.GetProperty("metricType").GetString());
        Assert.Equal(16, f.GetProperty("value").GetInt32());
        Assert.Equal(10, f.GetProperty("threshold").GetInt32());
        Assert.Equal("error", f.GetProperty("severity").GetString());
    }

    [Fact]
    public void MethodViolatingAllThreeThresholdsProducesThreeFindings()
    {
        var hotspot = new MethodMetrics(
            MethodName: "M",
            ContainingType: "C",
            FilePath: "f.cs",
            LineNumber: 1,
            CyclomaticComplexity: 20,
            LineCount: 100,
            ParameterCount: 8);

        var report = new AnalysisReport(
            RootPath: "p",
            FilesAnalyzed: 1,
            MethodsAnalyzed: 1,
            Methods: new[] { hotspot },
            Hotspots: new[] { hotspot });

        var findings = Parse(Render(report, DefaultThresholds)).GetProperty("findings");

        Assert.Equal(3, findings.GetArrayLength());
    }

    [Fact]
    public void NonHotspotsAreNotIncludedInFindings()
    {
        var clean = new MethodMetrics("Clean", "C", "f.cs", 1, 1, 1, 0);
        var report = new AnalysisReport(
            RootPath: "p",
            FilesAnalyzed: 1,
            MethodsAnalyzed: 1,
            Methods: new[] { clean },
            Hotspots: Array.Empty<MethodMetrics>());

        var json = Parse(Render(report, DefaultThresholds));

        Assert.Equal(0, json.GetProperty("findings").GetArrayLength());
    }

    [Fact]
    public void OutputIsValidJson()
    {
        var report = new AnalysisReport("p", 0, 0, Array.Empty<MethodMetrics>(), Array.Empty<MethodMetrics>());
        var raw = Render(report, DefaultThresholds);

        var ex = Record.Exception(() => JsonDocument.Parse(raw));
        Assert.Null(ex);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/CodeComplexityAnalyzer.Tests/CodeComplexityAnalyzer.Tests.csproj`
Expected: Build error — `JsonReporter` constructor does not take `Thresholds`.

- [ ] **Step 3: Rewrite `JsonReporter`**

Replace the contents of `src/CodeComplexityAnalyzer.Core/Reporting/JsonReporter.cs` with:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeComplexityAnalyzer.Core.Models;

namespace CodeComplexityAnalyzer.Core.Reporting;

public sealed class JsonReporter : IReporter
{
    private const string SchemaVersion = "1.0";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
        },
    };

    private readonly Thresholds _thresholds;

    public JsonReporter(Thresholds thresholds)
    {
        _thresholds = thresholds;
    }

    public void Render(AnalysisReport report, TextWriter writer)
    {
        var findings = report.Hotspots
            .SelectMany(m => FindingFactory.Create(m, _thresholds))
            .ToList();

        var document = new JsonReportDocument(
            SchemaVersion: SchemaVersion,
            RootPath: report.RootPath,
            FilesAnalyzed: report.FilesAnalyzed,
            MethodsAnalyzed: report.MethodsAnalyzed,
            Findings: findings);

        writer.Write(JsonSerializer.Serialize(document, Options));
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/CodeComplexityAnalyzer.Tests/CodeComplexityAnalyzer.Tests.csproj`
Expected: All tests pass (existing + 6 `FindingFactoryTests` + 5 `JsonReporterTests`).

- [ ] **Step 5: Commit**

```bash
git add src/CodeComplexityAnalyzer.Core/Reporting/JsonReporter.cs tests/CodeComplexityAnalyzer.Tests/JsonReporterTests.cs
git commit -m "Rewrite JsonReporter: per-violation findings with severity tiers"
```

---

## Task 7: Wire `Thresholds` into `JsonReporter` from CLI

**Files:**
- Modify: `src/CodeComplexityAnalyzer.Cli/Program.cs:57-62`

- [ ] **Step 1: Update the format switch**

In `src/CodeComplexityAnalyzer.Cli/Program.cs`, change lines 57–62 from:

```csharp
IReporter reporter = format.ToLowerInvariant() switch
{
    "json" => new JsonReporter(),
    "console" => new ConsoleReporter(),
    _ => throw new ArgumentException($"Unknown format: {format}"),
};
```

to:

```csharp
IReporter reporter = format.ToLowerInvariant() switch
{
    "json" => new JsonReporter(options.Thresholds),
    "console" => new ConsoleReporter(),
    _ => throw new ArgumentException($"Unknown format: {format}"),
};
```

(`options` is the existing `AnalysisOptions` local already in scope from line 50.)

- [ ] **Step 2: Build the solution**

Run: `dotnet build`
Expected: Build succeeded. 0 Warning(s) 0 Error(s).

- [ ] **Step 3: Run the full test suite**

Run: `dotnet test`
Expected: All tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/CodeComplexityAnalyzer.Cli/Program.cs
git commit -m "Pass Thresholds from CLI into JsonReporter"
```

---

## Task 8: End-to-end verification on samples

**Files:** none (manual verification only)

- [ ] **Step 1: Run against samples in console mode (regression check)**

Run: `dotnet run --project src/CodeComplexityAnalyzer.Cli -- samples`
Expected: Existing console table output. No JSON. No errors. Same shape as before this branch.

- [ ] **Step 2: Run against samples in JSON mode**

Run: `dotnet run --project src/CodeComplexityAnalyzer.Cli -- samples --format json`
Expected: Single JSON document on stdout. Top-level keys: `schemaVersion`, `rootPath`, `filesAnalyzed`, `methodsAnalyzed`, `findings`. Each finding has the 8 properties listed in the spec, all camelCase.

- [ ] **Step 3: Confirm JSON parses cleanly**

If `jq` is available:

```bash
dotnet run --project src/CodeComplexityAnalyzer.Cli -- samples --format json | jq '.findings | length'
```

Otherwise pipe to a file and parse with PowerShell:

```powershell
dotnet run --project src/CodeComplexityAnalyzer.Cli -- samples --format json > /tmp/out.json
Get-Content /tmp/out.json | ConvertFrom-Json | Select-Object -ExpandProperty findings | Measure-Object
```

Expected: A non-negative integer count of findings; no parse errors.

- [ ] **Step 4: Confirm exit codes**

Run: `dotnet run --project src/CodeComplexityAnalyzer.Cli -- samples --format json; echo "exit=$?"`
Expected: `exit=1` if `samples/` contains any hotspot, `exit=0` otherwise. (Same semantics as console mode.)

- [ ] **Step 5: Commit nothing (verification only)**

No commit. If any step fails, return to the relevant earlier task and fix.

---

## Task 9: Push branch and open PR

**Files:** none (git/PR work)

- [ ] **Step 1: Push branch**

```bash
git push -u origin feature/issue-1-json-output
```

- [ ] **Step 2: Open PR**

```bash
gh pr create --title "Add structured JSON output format (#1)" --body "$(cat <<'EOF'
## Summary
- New per-violation JSON schema (`schemaVersion: "1.0"`) with `findings[]` carrying file path, line, method, containing type, metric type, value, threshold, severity
- Severity tiers: `error` at ≥1.5× threshold, `warning` otherwise
- 11 new unit tests (FindingFactory + JsonReporter)

## Test plan
- [x] `dotnet test` passes
- [x] `dotnet run -- samples` console output unchanged
- [x] `dotnet run -- samples --format json` parses as valid JSON

Closes #1
EOF
)"
```

- [ ] **Step 3: Wait for CodeRabbit review**

CodeRabbit will post automatically. Address its comments before requesting human review.

---

## Self-review notes

- Spec coverage: every AC item maps to a task (schema → Tasks 3-4-6, severity → Task 5, default unchanged → Task 8 step 1, camelCase → Task 6 tests, schemaVersion → Task 6, tests → Tasks 5-6, valid JSON → Task 6)
- No placeholders — all code blocks are complete
- Type names consistent across tasks: `Finding`, `JsonReportDocument`, `MetricType`, `Severity`, `FindingFactory.Create`, `JsonReporter(Thresholds)`
- One judgment call to flag during review: severity boundary uses integer math `value * 2 >= threshold * 3`. Documented. If you want strict `>` (so value=15 at threshold=10 is Warning, not Error), change to `value * 2 > threshold * 3` in Task 5 step 3 and update Task 5 boundary tests.

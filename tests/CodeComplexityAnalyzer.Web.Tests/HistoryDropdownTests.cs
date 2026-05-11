using Bunit;
using CodeComplexityAnalyzer.Core.Reporting;
using CodeComplexityAnalyzer.Web.Components;
using CodeComplexityAnalyzer.Web.Models;
using Microsoft.AspNetCore.Components;

namespace CodeComplexityAnalyzer.Web.Tests;

public sealed class HistoryDropdownTests : IDisposable
{
    private readonly TestContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static HistoryEntry MakeEntry(string id, string sourceId = "owner/repo@main") =>
        new(
            Id: id,
            TimestampUtc: new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            Mode: "github",
            SourceId: sourceId,
            ErrorCount: 1,
            WarningCount: 2,
            FileCount: 3,
            MaxCc: 10,
            MaxLines: 60,
            MaxParams: 5,
            MinMi: 50,
            Findings: new List<Finding>());

    // -----------------------------------------------------------------------
    // Disabled when empty
    // -----------------------------------------------------------------------

    [Fact]
    public void History_trigger_is_disabled_when_history_is_empty()
    {
        var cut = _ctx.RenderComponent<HistoryDropdown>(p => p
            .Add(c => c.History, Array.Empty<HistoryEntry>()));

        var trigger = cut.Find(".history-trigger");
        Assert.True(trigger.HasAttribute("disabled"));
    }

    // -----------------------------------------------------------------------
    // Panel renders one item per entry; click restores correct entry
    // -----------------------------------------------------------------------

    [Fact]
    public void Panel_renders_one_item_per_entry_and_click_invokes_OnRestore_with_correct_entry()
    {
        var first = MakeEntry("entry-1", "owner/repoA@main");
        var second = MakeEntry("entry-2", "owner/repoB@dev");
        var entries = new[] { first, second };

        HistoryEntry? restored = null;

        var cut = _ctx.RenderComponent<HistoryDropdown>(p => p
            .Add(c => c.History, entries)
            .Add(c => c.OnRestore, EventCallback.Factory.Create<HistoryEntry>(this, e => restored = e)));

        // Open the panel.
        cut.Find(".history-trigger").Click();

        var items = cut.FindAll(".history-item");
        Assert.Equal(2, items.Count);
        Assert.Contains("owner/repoA@main", items[0].TextContent);
        Assert.Contains("owner/repoB@dev", items[1].TextContent);

        // Click the second entry.
        items[1].Click();

        Assert.NotNull(restored);
        Assert.Equal("entry-2", restored!.Id);
        Assert.Equal("owner/repoB@dev", restored.SourceId);
    }
}

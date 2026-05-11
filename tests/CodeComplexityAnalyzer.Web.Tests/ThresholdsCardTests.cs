using Bunit;
using CodeComplexityAnalyzer.Web.Components;
using Microsoft.AspNetCore.Components;

namespace CodeComplexityAnalyzer.Web.Tests;

public sealed class ThresholdsCardTests : IDisposable
{
    private readonly TestContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    // -----------------------------------------------------------------------
    // Defaults
    // -----------------------------------------------------------------------

    [Fact]
    public void Default_values_render_in_inputs()
    {
        var cut = _ctx.RenderComponent<ThresholdsCard>();

        var inputs = cut.FindAll("input[type=number]");
        Assert.Equal(4, inputs.Count);
        Assert.Equal("10", inputs[0].GetAttribute("value"));
        Assert.Equal("60", inputs[1].GetAttribute("value"));
        Assert.Equal("5", inputs[2].GetAttribute("value"));
        Assert.Equal("50", inputs[3].GetAttribute("value"));
    }

    // -----------------------------------------------------------------------
    // Reset fires every callback with defaults
    // -----------------------------------------------------------------------

    [Fact]
    public void Reset_button_restores_defaults_and_fires_all_four_callbacks()
    {
        int? cc = null, lines = null, prms = null, mi = null;

        var cut = _ctx.RenderComponent<ThresholdsCard>(p => p
            .Add(c => c.MaxCc, 99)
            .Add(c => c.MaxLines, 200)
            .Add(c => c.MaxParams, 12)
            .Add(c => c.MinMi, 5)
            .Add(c => c.MaxCcChanged, EventCallback.Factory.Create<int>(this, v => cc = v))
            .Add(c => c.MaxLinesChanged, EventCallback.Factory.Create<int>(this, v => lines = v))
            .Add(c => c.MaxParamsChanged, EventCallback.Factory.Create<int>(this, v => prms = v))
            .Add(c => c.MinMiChanged, EventCallback.Factory.Create<int>(this, v => mi = v)));

        var resetBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Reset"));
        resetBtn.Click();

        Assert.Equal(10, cc);
        Assert.Equal(60, lines);
        Assert.Equal(5, prms);
        Assert.Equal(50, mi);
    }

    // -----------------------------------------------------------------------
    // Out-of-range MI is ignored — no event fires
    // -----------------------------------------------------------------------

    [Fact]
    public void Out_of_range_mi_input_is_ignored_and_fires_no_event()
    {
        var fireCount = 0;
        int? lastValue = null;

        var cut = _ctx.RenderComponent<ThresholdsCard>(p => p
            .Add(c => c.MinMi, 50)
            .Add(c => c.MinMiChanged, EventCallback.Factory.Create<int>(this, v =>
            {
                fireCount++;
                lastValue = v;
            })));

        // 4th input is MinMi.
        var miInput = cut.FindAll("input[type=number]")[3];
        miInput.Input("150");

        Assert.Equal(0, fireCount);
        Assert.Null(lastValue);
    }
}

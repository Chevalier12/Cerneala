using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

public sealed class ControlTests
{
    [Fact]
    public void DefaultsExposeCommonControlProperties()
    {
        Control control = new();

        Assert.Equal(Color.Transparent, control.Background);
        Assert.Equal(Color.Black, control.Foreground);
        Assert.Equal(Color.Transparent, control.BorderColor);
        Assert.Equal(Thickness.Zero, control.BorderThickness);
        Assert.Equal(Thickness.Zero, control.Padding);
        Assert.Equal("Default", control.FontFamily);
        Assert.Equal(16, control.FontSize);
    }

    [Fact]
    public void RenderPropertyInvalidatesRenderWithoutMeasure()
    {
        Control control = new();

        control.Background = Color.White;

        Assert.True(control.DirtyState.Has(InvalidationFlags.Render));
        Assert.True(control.DirtyState.Has(InvalidationFlags.InputVisual));
        Assert.False(control.DirtyState.Has(InvalidationFlags.Measure));
    }

    [Fact]
    public void ForegroundInvalidatesRenderOnly()
    {
        Control control = new();

        control.Foreground = Color.White;

        Assert.True(control.DirtyState.Has(InvalidationFlags.Render));
        Assert.False(control.DirtyState.Has(InvalidationFlags.InputVisual));
        Assert.False(control.DirtyState.Has(InvalidationFlags.Measure));
    }

    [Fact]
    public void MetricPropertyInvalidatesMeasureAndRender()
    {
        Control control = new();

        control.Padding = new Thickness(2);

        Assert.True(control.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(control.DirtyState.Has(InvalidationFlags.Render));
    }

    [Fact]
    public void RejectsInvalidFontProperties()
    {
        Control control = new();

        Assert.Throws<ArgumentException>(() => control.FontFamily = "");
        Assert.Throws<ArgumentException>(() => control.FontSize = 0);
    }

    [Fact]
    public void RejectsInvalidInsetProperties()
    {
        Control control = new();

        Assert.Throws<ArgumentException>(() => control.Padding = new Thickness(-1));
        Assert.Throws<ArgumentException>(() => control.Padding = new Thickness(float.NaN));
        Assert.Throws<ArgumentException>(() => control.BorderThickness = new Thickness(-1));
        Assert.Throws<ArgumentException>(() => control.BorderThickness = new Thickness(float.PositiveInfinity));
    }
}

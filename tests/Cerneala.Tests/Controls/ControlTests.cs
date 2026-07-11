using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.Tests.Controls;

public sealed class ControlTests
{
    [Fact]
    public void DefaultsExposeCommonControlProperties()
    {
        Control control = new();

        Assert.Null(control.Background);
        Assert.Equal(Color.Black, control.Foreground);
        Assert.Null(control.BorderBrush);
        Assert.Equal(Thickness.Zero, control.BorderThickness);
        Assert.Equal(Thickness.Zero, control.Padding);
        Assert.Equal("Default", control.FontFamily);
        Assert.Equal(16, control.FontSize);
    }

    [Fact]
    public void BackgroundAndBorderBrushApisUseNullableBrushesWithoutLegacyBorderColor()
    {
        Assert.Equal(typeof(Brush), Control.BackgroundProperty.ValueType);
        Assert.Null(Control.BackgroundProperty.Metadata.DefaultValue);
        Assert.Equal(typeof(Brush), Control.BorderBrushProperty.ValueType);
        Assert.Null(Control.BorderBrushProperty.Metadata.DefaultValue);
        Assert.Null(typeof(Control).GetProperty("BorderColor"));
        Assert.Null(typeof(Control).GetField("BorderColorProperty"));
    }

    [Fact]
    public void RenderPropertyInvalidatesRenderWithoutMeasure()
    {
        Control control = new();

        control.Background = new SolidColorBrush(Color.White);

        Assert.True(control.DirtyState.Has(InvalidationFlags.Render));
        Assert.True(control.DirtyState.Has(InvalidationFlags.InputVisual));
        Assert.False(control.DirtyState.Has(InvalidationFlags.Measure));
    }

    [Fact]
    public void BorderBrushInvalidatesRenderAndInputVisualWithoutMeasure()
    {
        Control control = new();

        control.BorderBrush = new SolidColorBrush(Color.White);

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

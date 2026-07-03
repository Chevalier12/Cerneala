using Cerneala.UI.Layout;

namespace Cerneala.Tests.UI.Layout;

public sealed class LayoutPrimitiveTests
{
    [Fact]
    public void LayoutSizeCanRepresentUnconstrainedDimensions()
    {
        LayoutSize size = LayoutSize.Unconstrained;

        Assert.True(size.IsWidthUnconstrained);
        Assert.True(size.IsHeightUnconstrained);
    }

    [Fact]
    public void LayoutPointAndRectAreLayoutTypes()
    {
        LayoutPoint point = new(2, 3);
        LayoutRect rect = new(point.X, point.Y, 10, 20);

        Assert.Equal(point, rect.Location);
        Assert.Equal(new LayoutSize(10, 20), rect.Size);
    }

    [Fact]
    public void ThicknessReportsTotalEdges()
    {
        Thickness thickness = new(1, 2, 3, 4);

        Assert.Equal(4, thickness.Horizontal);
        Assert.Equal(6, thickness.Vertical);
    }

    [Fact]
    public void LayoutRoundingIsExplicit()
    {
        LayoutRounding rounding = LayoutRounding.Enabled;

        Assert.Equal(new LayoutSize(2, 4), rounding.Round(new LayoutSize(1.6f, 3.5f)));
        Assert.Equal(new LayoutSize(1.6f, 3.5f), LayoutRounding.Disabled.Round(new LayoutSize(1.6f, 3.5f)));
    }

    [Fact]
    public void VisibilityHasThreeStates()
    {
        Assert.Contains(Visibility.Visible, Enum.GetValues<Visibility>());
        Assert.Contains(Visibility.Hidden, Enum.GetValues<Visibility>());
        Assert.Contains(Visibility.Collapsed, Enum.GetValues<Visibility>());
    }
}

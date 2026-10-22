using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.UI.Layout;

public sealed class UIElementMinMaxSizeTests
{
    [Fact]
    public void ButtonWithMinWidthAndShortContentMeasuresAtMinWidth()
    {
        Button plain = new() { Content = "Save" };
        Button limited = new() { Content = "Save", MinWidth = 120 };
        StackPanel panel = new() { HorizontalAlignment = HorizontalAlignment.Left };
        panel.Children.Add(plain);
        panel.Children.Add(limited);
        UIRoot root = new(400, 200);
        root.VisualChildren.Add(panel);

        root.ProcessFrame();

        Assert.InRange(plain.DesiredSize.Width, 1f, 119f);
        Assert.Equal(120f, limited.DesiredSize.Width);
        Assert.Equal(120f, limited.ArrangedBounds.Width);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MaxDimensionLimitsAStretchedElementAndCentersItInItsSlot(bool vertical)
    {
        // As in WPF, a stretched element narrower than its slot is centered: a 300-pixel StackPanel arranges a Border
        // with MaxWidth = 100 at x 100..200.
        Border border = new();
        SetAxis(border, vertical, UIElement.MaxWidthProperty, UIElement.MaxHeightProperty, 100);
        SetAxis(border, !vertical, UIElement.WidthProperty, UIElement.HeightProperty, 20);
        StackPanel panel = new() { Orientation = vertical ? Orientation.Horizontal : Orientation.Vertical };
        panel.Children.Add(border);
        UIRoot root = new(300, 300);
        root.VisualChildren.Add(panel);

        root.ProcessFrame();

        LayoutRect bounds = border.ArrangedBounds;
        Assert.Equal(
            (100f, 100f),
            vertical ? (bounds.Height, bounds.Y) : (bounds.Width, bounds.X));
    }

    [Theory]
    [InlineData(false, 50f, 80f, float.PositiveInfinity, 80f)]
    [InlineData(true, 50f, 80f, float.PositiveInfinity, 80f)]
    [InlineData(false, 200f, 0f, 120f, 120f)]
    [InlineData(true, 200f, 0f, 120f, 120f)]
    [InlineData(false, float.NaN, 80f, 60f, 80f)]
    [InlineData(true, float.NaN, 80f, 60f, 80f)]
    [InlineData(false, 200f, 150f, 100f, 150f)]
    [InlineData(false, 90f, 80f, 100f, 90f)]
    public void MinAndMaxClampTheExplicitDimensionWithMinimumWinning(
        bool vertical,
        float dimension,
        float minimum,
        float maximum,
        float expected)
    {
        // MinWidth wins over a smaller Width, MaxWidth wins over a larger Width, and MinWidth wins over a smaller MaxWidth.
        SizeRecordingElement element = new()
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        SetAxis(element, vertical, UIElement.WidthProperty, UIElement.HeightProperty, dimension);
        SetAxis(element, vertical, UIElement.MinWidthProperty, UIElement.MinHeightProperty, minimum);
        SetAxis(element, vertical, UIElement.MaxWidthProperty, UIElement.MaxHeightProperty, maximum);

        element.Measure(new MeasureContext(new LayoutSize(300, 300)));
        element.Arrange(new ArrangeContext(new LayoutRect(0, 0, 300, 300)));

        Assert.Equal(
            (expected, expected, expected),
            (Length(element.MeasureSize!.Value, vertical), Length(element.DesiredSize, vertical), Length(element.ArrangedBounds.Size, vertical)));
    }

    [Theory]
    [InlineData(false, HorizontalAlignment.Left, VerticalAlignment.Top, 0f)]
    [InlineData(true, HorizontalAlignment.Left, VerticalAlignment.Top, 0f)]
    [InlineData(false, HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 0f)]
    [InlineData(true, HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 0f)]
    [InlineData(false, HorizontalAlignment.Center, VerticalAlignment.Center, -10f)]
    [InlineData(true, HorizontalAlignment.Right, VerticalAlignment.Bottom, -20f)]
    public void MinDimensionLargerThanTheSlotOverflowsItAsInWpf(
        bool vertical,
        HorizontalAlignment horizontalAlignment,
        VerticalAlignment verticalAlignment,
        float expectedOffset)
    {
        // MinWidth = 120 in a 100-pixel slot is measured and arranged at 120. A stretched element wider than its slot
        // starts at the slot's start; a centered one sticks out 10 pixels on each side.
        SizeRecordingElement element = new()
        {
            HorizontalAlignment = horizontalAlignment,
            VerticalAlignment = verticalAlignment
        };
        SetAxis(element, vertical, UIElement.MinWidthProperty, UIElement.MinHeightProperty, 120);

        element.Measure(new MeasureContext(new LayoutSize(100, 100)));
        element.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        LayoutRect bounds = element.ArrangedBounds;
        Assert.Equal(
            (120f, 120f, 120f, 120f, expectedOffset),
            (Length(element.MeasureSize!.Value, vertical),
                Length(element.DesiredSize, vertical),
                Length(element.ArrangeSize!.Value, vertical),
                Length(bounds.Size, vertical),
                vertical ? bounds.Y : bounds.X));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MinDimensionIsMeasuredAndArrangedAtItsNearestDevicePixel(bool vertical)
    {
        // As an explicit Width, MinWidth = 49.7 at scale 2 (99.4 device pixels) is measured and arranged at 49.5.
        SizeRecordingElement element = new()
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        SetAxis(element, vertical, UIElement.MinWidthProperty, UIElement.MinHeightProperty, 49.7f);
        LayoutRounding rounding = LayoutRounding.ForScale(2);

        element.Measure(new MeasureContext(new LayoutSize(10, 10), rounding));
        element.Arrange(new ArrangeContext(new LayoutRect(0, 0, 10, 10), rounding));

        Assert.Equal(
            (49.5f, 49.5f, 49.5f),
            (Length(element.MeasureSize!.Value, vertical), Length(element.DesiredSize, vertical), Length(element.ArrangedBounds.Size, vertical)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ChangingMinDimensionMeasuresOnceAndTheIdleFrameDoesNoLayoutWork(bool vertical)
    {
        SizeRecordingElement element = new();
        UIRoot root = new(300, 300);
        root.VisualChildren.Add(element);
        root.ProcessFrame();
        int measuresBefore = element.MeasureCount;

        SetAxis(element, vertical, UIElement.MinWidthProperty, UIElement.MinHeightProperty, 80);
        Assert.True(element.DirtyState.Has(InvalidationFlags.Measure));
        root.ProcessFrame();
        FrameStats idle = root.ProcessFrame();

        Assert.Equal(1, element.MeasureCount - measuresBefore);
        Assert.Equal((0, 0), (idle.MeasureCalls, idle.ArrangeCalls));
    }

    [Fact]
    public void ClearingTheLimitsRestoresUnconstrainedLayout()
    {
        SizeRecordingElement element = new()
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            MinWidth = 120,
            MaxHeight = 10
        };
        element.Measure(new MeasureContext(new LayoutSize(100, 100)));
        LayoutSize limited = element.MeasureSize!.Value;

        element.ClearValue(UIElement.MinWidthProperty);
        element.Measure(new MeasureContext(new LayoutSize(100, 100)));
        LayoutSize widthCleared = element.MeasureSize!.Value;

        element.ClearValue(UIElement.MaxHeightProperty);
        element.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(
            (new LayoutSize(120, 10), new LayoutSize(100, 10), new LayoutSize(100, 100), LayoutSize.Zero),
            (limited, widthCleared, element.MeasureSize!.Value, element.DesiredSize));
    }

    [Fact]
    public void MinWidthFromAnAspectLimitsLayout()
    {
        Border border = new()
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            Aspect = new Cerneala.UI.Aspect.ElementAspect([new Cerneala.UI.Aspect.ElementAspectValue(UIElement.MinWidthProperty, 120f)])
        };
        UIRoot root = new(300, 300);
        root.VisualChildren.Add(border);

        root.ProcessFrame();

        Assert.Equal((120f, 120f), (border.DesiredSize.Width, border.ArrangedBounds.Width));
    }

    [Fact]
    public void DefaultsLeaveLayoutUnconstrained()
    {
        UIElement element = new();

        Assert.Equal(
            (0f, float.PositiveInfinity, 0f, float.PositiveInfinity),
            (element.MinWidth, element.MaxWidth, element.MinHeight, element.MaxHeight));
    }

    [Theory]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void MinDimensionRejectsNegativeNaNAndInfiniteValues(float value)
    {
        UIElement element = new();

        Assert.ThrowsAny<ArgumentException>(() => element.MinWidth = value);
        Assert.ThrowsAny<ArgumentException>(() => element.MinHeight = value);
    }

    [Theory]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    public void MaxDimensionRejectsNegativeAndNaNValues(float value)
    {
        UIElement element = new();

        Assert.ThrowsAny<ArgumentException>(() => element.MaxWidth = value);
        Assert.ThrowsAny<ArgumentException>(() => element.MaxHeight = value);
    }

    [Fact]
    public void MinDimensionAboveMaxDimensionIsAllowed()
    {
        UIElement element = new() { MaxWidth = 60, MaxHeight = 60 };

        element.MinWidth = 80;
        element.MinHeight = 80;

        Assert.Equal((80f, 80f), (element.MinWidth, element.MinHeight));
    }

    [Fact]
    public void WindowAndOverlayKeepTheirOwnSizeLimits()
    {
        // Window's limits stay native-window limits and Overlay.MaxHeight stays the limit of its projected content;
        // they do not set the UIElement properties of the same name.
        Window window = new() { MinWidth = 640, MaxWidth = 1000, MinHeight = 480, MaxHeight = 900 };
        Overlay overlay = new() { MaxHeight = 120 };
        UIElement windowElement = window;
        UIElement overlayElement = overlay;

        Assert.NotSame(UIElement.MinWidthProperty, Window.MinWidthProperty);
        Assert.NotSame(UIElement.MaxHeightProperty, Overlay.MaxHeightProperty);
        Assert.Equal(
            (0f, float.PositiveInfinity, 0f, float.PositiveInfinity, float.PositiveInfinity),
            (windowElement.MinWidth, windowElement.MaxWidth, windowElement.MinHeight, windowElement.MaxHeight, overlayElement.MaxHeight));
        Assert.Throws<ArgumentOutOfRangeException>(() => window.MinWidth = 2000);
    }

    private static void SetAxis(UIElement element, bool vertical, UiProperty<float> horizontal, UiProperty<float> verticalProperty, float value)
    {
        element.SetValue(vertical ? verticalProperty : horizontal, value);
    }

    private static float Length(LayoutSize size, bool vertical) => vertical ? size.Height : size.Width;

    private sealed class SizeRecordingElement : UIElement
    {
        public int MeasureCount { get; private set; }

        public LayoutSize? MeasureSize { get; private set; }

        public LayoutSize? ArrangeSize { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureCount++;
            MeasureSize = context.AvailableSize;
            return LayoutSize.Zero;
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            ArrangeSize = context.FinalRect.Size;
            return context.FinalRect;
        }
    }
}

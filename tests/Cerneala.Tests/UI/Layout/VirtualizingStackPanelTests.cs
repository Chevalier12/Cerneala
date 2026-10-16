using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Layout.Virtualization;

namespace Cerneala.Tests.UI.Layout;

public sealed class VirtualizingStackPanelTests
{
    [Fact]
    public void VirtualizingStackPanelMeasuresRealizedChildrenOnly()
    {
        VirtualizingStackPanel panel = new()
        {
            VirtualizationContext = new VirtualizationContext(10, 10, 20, 20),
            FirstRealizedIndex = 0
        };
        CountingElement first = new();
        CountingElement second = new();
        CountingElement third = new();
        panel.VisualChildren.Add(first);
        panel.VisualChildren.Add(second);
        panel.VisualChildren.Add(third);

        panel.Measure(new MeasureContext(new LayoutSize(100, 20)));

        Assert.Equal(0, first.MeasureCount);
        Assert.Equal(0, second.MeasureCount);
        Assert.Equal(1, third.MeasureCount);
        Assert.Equal(100, panel.DesiredSize.Height);
    }

    [Fact]
    public void VirtualizingStackPanelArrangesChildrenInContentCoordinates()
    {
        VirtualizingStackPanel panel = new()
        {
            VirtualizationContext = new VirtualizationContext(10, 10, 20, 20),
            FirstRealizedIndex = 2
        };
        CountingElement child = new();
        panel.VisualChildren.Add(child);
        panel.Measure(new MeasureContext(new LayoutSize(100, 20)));

        panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 20)));

        Assert.Equal(new LayoutRect(0, 20, 100, 10), child.ArrangedBounds);
        Assert.Equal(100, panel.TotalExtent);
    }

    [Fact]
    public void VirtualizingStackPanelDoesNotApplyScrollOffsetTwiceWhenPresenterOffsetsContent()
    {
        VirtualizingStackPanel panel = new()
        {
            VirtualizationContext = new VirtualizationContext(10, 10, 20, 20),
            FirstRealizedIndex = 2
        };
        CountingElement child = new();
        panel.VisualChildren.Add(child);
        panel.Measure(new MeasureContext(new LayoutSize(100, 20)));

        panel.Arrange(new ArrangeContext(new LayoutRect(0, -20, 100, 100)));

        Assert.Equal(new LayoutRect(0, 0, 100, 10), child.ArrangedBounds);
    }

    [Fact]
    public void VirtualizingStackPanelArrangesUnrealizedChildrenToEmptyBounds()
    {
        VirtualizingStackPanel panel = new()
        {
            VirtualizationContext = new VirtualizationContext(10, 10, 20, 20),
            FirstRealizedIndex = 0
        };
        CountingElement first = new();
        CountingElement second = new();
        CountingElement third = new();
        panel.VisualChildren.Add(first);
        panel.VisualChildren.Add(second);
        panel.VisualChildren.Add(third);
        panel.Measure(new MeasureContext(new LayoutSize(100, 20)));

        panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 20)));

        Assert.Equal(LayoutRect.Empty, first.ArrangedBounds);
        Assert.Equal(LayoutRect.Empty, second.ArrangedBounds);
        Assert.Equal(new LayoutRect(0, 20, 100, 10), third.ArrangedBounds);
    }

    [Fact]
    public void FullyRealizedPanelUsesNaturalItemHeightsForAutoSize()
    {
        VirtualizingStackPanel panel = new()
        {
            VirtualizationContext = new VirtualizationContext(3, 28, 300, 0),
            FirstRealizedIndex = 0
        };
        panel.VisualChildren.Add(new FixedHeightElement(35));
        panel.VisualChildren.Add(new FixedHeightElement(40));
        panel.VisualChildren.Add(new FixedHeightElement(30));

        panel.Measure(new MeasureContext(new LayoutSize(100, 300)));
        panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, panel.DesiredSize.Height)));

        Assert.Equal(105, panel.DesiredSize.Height);
        Assert.Equal(new LayoutRect(0, 0, 100, 35), panel.VisualChildren[0].ArrangedBounds);
        Assert.Equal(new LayoutRect(0, 35, 100, 40), panel.VisualChildren[1].ArrangedBounds);
        Assert.Equal(new LayoutRect(0, 75, 100, 30), panel.VisualChildren[2].ArrangedBounds);
    }

    [Fact]
    public void PanelWithoutContextOrViewportRealizesEveryChildForAnyFirstRealizedIndex()
    {
        VirtualizingStackPanel panel = new()
        {
            FirstRealizedIndex = 29
        };
        for (int index = 0; index < 10; index++)
        {
            panel.VisualChildren.Add(new FixedHeightElement(10));
        }

        panel.Measure(new MeasureContext(new LayoutSize(100, 1000)));
        panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, panel.DesiredSize.Height)));

        Assert.Equal(new RealizationWindow(29, 39), panel.RealizationWindow);
        Assert.Equal(100, panel.DesiredSize.Height);
        Assert.Equal(100, panel.TotalExtent);
        for (int index = 0; index < 10; index++)
        {
            Assert.Equal(new LayoutSize(20, 10), panel.VisualChildren[index].DesiredSize);
            Assert.Equal(new LayoutRect(0, index * 10, 100, 10), panel.VisualChildren[index].ArrangedBounds);
        }
    }

    [Theory]
    [InlineData(-3, 0, 7)]
    [InlineData(int.MaxValue - 5, int.MaxValue - 5, int.MaxValue)]
    public void PanelWithoutContextOrViewportRealizesEveryChildForNegativeOrOverflowingFirstRealizedIndex(
        int firstRealizedIndex,
        int expectedStartIndex,
        int expectedEndIndexExclusive)
    {
        VirtualizingStackPanel panel = new()
        {
            FirstRealizedIndex = firstRealizedIndex
        };
        for (int index = 0; index < 10; index++)
        {
            panel.VisualChildren.Add(new FixedHeightElement(10));
        }

        panel.Measure(new MeasureContext(new LayoutSize(100, 1000)));
        panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, panel.DesiredSize.Height)));

        Assert.Equal(new RealizationWindow(expectedStartIndex, expectedEndIndexExclusive), panel.RealizationWindow);
        Assert.Equal(100, panel.DesiredSize.Height);
        Assert.Equal(100, panel.TotalExtent);
        for (int index = 0; index < 10; index++)
        {
            Assert.Equal(new LayoutSize(20, 10), panel.VisualChildren[index].DesiredSize);
            Assert.Equal(new LayoutRect(0, index * 10, 100, 10), panel.VisualChildren[index].ArrangedBounds);
        }
    }

    [Fact]
    public void LayoutRoundingDesiresTheSumOfNaturalItemDevicePixelsWhenTheFloatSumIsOneStepAboveIt()
    {
        // At scale 1.75 the items desire 57 / 1.75f and 1 / 1.75f. Their float sum is one step above 58 / 1.75f,
        // and rounding that sum up must not add a 59th device pixel that leaves a gap before the next sibling.
        UIRoot root = new(200, 200, 1.75f);
        StackPanel outer = new()
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        VirtualizingStackPanel panel = new();
        UIElement first = new() { Width = 10, Height = 32.5f };
        UIElement second = new() { Width = 10, Height = 0.5f };
        UIElement next = new() { Width = 10, Height = 10 };
        panel.VisualChildren.Add(first);
        panel.VisualChildren.Add(second);
        outer.VisualChildren.Add(panel);
        outer.VisualChildren.Add(next);
        root.VisualChildren.Add(outer);

        root.ProcessFrame();

        Assert.Equal(
            (57f, 58f, 58f, 58f),
            (MathF.Round((first.ArrangedBounds.Y + first.ArrangedBounds.Height) * 1.75f),
                MathF.Round((second.ArrangedBounds.Y + second.ArrangedBounds.Height) * 1.75f),
                MathF.Round(panel.DesiredSize.Height * 1.75f),
                MathF.Round(next.ArrangedBounds.Y * 1.75f)));
    }

    [Fact]
    public void LayoutRoundingDesiresTheSumOfMeasuredItemDevicePixelsWithAnAutomaticViewport()
    {
        // With an automatic viewport the desired height is the estimated total extent. Its measured items add device
        // pixels as the natural heights do, so 57 / 1.75f and 1 / 1.75f give 58 / 1.75f, not a float that rounds up to 59.
        UIRoot root = new(200, 200, 1.75f);
        StackPanel outer = new()
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        VirtualizingStackPanel panel = new();
        panel.UpdateViewport(new ItemsVirtualizationViewport(2, 200, 0, 0));
        UIElement first = new() { Width = 10, Height = 32.5f };
        UIElement second = new() { Width = 10, Height = 0.5f };
        UIElement next = new() { Width = 10, Height = 10 };
        panel.VisualChildren.Add(first);
        panel.VisualChildren.Add(second);
        outer.VisualChildren.Add(panel);
        outer.VisualChildren.Add(next);
        root.VisualChildren.Add(outer);

        root.ProcessFrame();

        Assert.Equal(new RealizationWindow(0, 2), panel.RealizationWindow);
        Assert.Equal(58 / 1.75f, panel.TotalExtent);
        Assert.Equal(
            (0f, 57f, 57f, 58f, 58f, 58f),
            (MathF.Round(first.ArrangedBounds.Y * 1.75f),
                MathF.Round((first.ArrangedBounds.Y + first.ArrangedBounds.Height) * 1.75f),
                MathF.Round(second.ArrangedBounds.Y * 1.75f),
                MathF.Round((second.ArrangedBounds.Y + second.ArrangedBounds.Height) * 1.75f),
                MathF.Round(panel.DesiredSize.Height * 1.75f),
                MathF.Round(next.ArrangedBounds.Y * 1.75f)));
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.75f)]
    public void AutomaticViewportKeepsRealizedRowsContiguousAfterAnEstimatedPrefixThatIsNotAWholeDevicePixel(float scale)
    {
        // Heights are in device pixels. Once items 1 (11) and 2 (10) are measured, the unrealized item 0 is
        // estimated at 10.5 device pixels, so the realized rows start after a prefix that is not a whole device pixel.
        LayoutRounding rounding = LayoutRounding.ForScale(scale);
        VirtualizingStackPanel panel = new() { FirstRealizedIndex = 0 };
        foreach (float deviceHeight in new[] { 20f, 11f, 10f, 20f, 20f })
        {
            panel.VisualChildren.Add(new FixedHeightElement(deviceHeight / scale));
        }

        // With the initial estimate of 28, offset 28 and viewport 29 realize items 1 and 2.
        panel.UpdateViewport(new ItemsVirtualizationViewport(5, 29, 28, 0));
        panel.Measure(new MeasureContext(new LayoutSize(100, 29), rounding));
        panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, panel.DesiredSize.Height), rounding));

        // Width 99 keeps the arrange cache from reusing the first pass.
        panel.UpdateViewport(new ItemsVirtualizationViewport(5, 15 / scale, 11 / scale, 0));
        panel.Measure(new MeasureContext(new LayoutSize(100, 15 / scale), rounding));
        panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 99, panel.DesiredSize.Height), rounding));

        Assert.Equal(new RealizationWindow(1, 3), panel.RealizationWindow);
        Assert.Equal(52.5f, panel.TotalExtent * scale, 3);
        // The first realized row starts at the device pixel nearest to 10.5 (ties to even), and the second touches it.
        Assert.Equal("10..21, 21..31", GetVerticalDeviceEdges([panel.VisualChildren[1], panel.VisualChildren[2]], scale));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FrameDoesNotMeasureChildrenOutsideTheRealizationWindowOfADirectlyUsedPanel(bool rowsAddedBeforePanelJoinsTree)
    {
        // Rows of 28 match the initial estimate, so viewport 400 realizes items 0..14 and keeps that window after measure.
        UIRoot root = new(200, 400, 1);
        VirtualizingStackPanel panel = new() { FirstRealizedIndex = 0 };
        List<ConstraintRecordingElement> rows = [.. Enumerable.Range(0, 30).Select(_ => new ConstraintRecordingElement(28))];
        if (!rowsAddedBeforePanelJoinsTree)
        {
            root.VisualChildren.Add(panel);
            root.ProcessFrame();
        }

        panel.UpdateViewport(new ItemsVirtualizationViewport(30, 400, 0, 0));
        rows.ForEach(panel.VisualChildren.Add);
        if (rowsAddedBeforePanelJoinsTree)
        {
            root.VisualChildren.Add(panel);
        }

        root.ProcessFrame();

        Assert.Equal(new RealizationWindow(0, 15), panel.RealizationWindow);
        Assert.Equal("", string.Join(", ", rows.Skip(15).Select((row, offset) => (row, index: offset + 15))
            .Where(entry => entry.row.MeasureCount > 0)
            .Select(entry => $"row {entry.index} measured {entry.row.MeasureCount}x at {entry.row.LastAvailableSize}")));
        Assert.All(rows.Take(15), row =>
        {
            Assert.True(row.MeasureCount > 0);
            Assert.Equal(new LayoutSize(200, float.PositiveInfinity), row.LastAvailableSize);
        });
    }

    [Fact]
    public void FrameDoesNotMeasureAChildOutsideTheRealizationWindowWhenOnlyThatChildIsInvalidated()
    {
        UIRoot root = new(200, 400, 1);
        VirtualizingStackPanel panel = new() { FirstRealizedIndex = 0 };
        root.VisualChildren.Add(panel);
        panel.UpdateViewport(new ItemsVirtualizationViewport(30, 400, 0, 0));
        List<ConstraintRecordingElement> rows = [.. Enumerable.Range(0, 30).Select(_ => new ConstraintRecordingElement(28))];
        rows.ForEach(panel.VisualChildren.Add);
        root.ProcessFrame();
        ConstraintRecordingElement outside = rows[20];
        int measuresBefore = outside.MeasureCount;

        outside.Width = 30;
        root.ProcessFrame();

        Assert.Equal(new RealizationWindow(0, 15), panel.RealizationWindow);
        Assert.Equal((0, 0), (measuresBefore, outside.MeasureCount));
    }

    [Fact]
    public void FrameDoesNotMeasureAChildMovedOutsideTheRealizationWindow()
    {
        UIRoot root = new(200, 400, 1);
        VirtualizingStackPanel panel = new() { FirstRealizedIndex = 0 };
        root.VisualChildren.Add(panel);
        panel.UpdateViewport(new ItemsVirtualizationViewport(30, 400, 0, 0));
        List<ConstraintRecordingElement> rows = [.. Enumerable.Range(0, 30).Select(_ => new ConstraintRecordingElement(28))];
        rows.ForEach(panel.VisualChildren.Add);
        root.ProcessFrame();
        ConstraintRecordingElement moved = rows[0];
        int measuresBefore = moved.MeasureCount;

        panel.VisualChildren.Move(0, 20);
        moved.Width = 30;
        root.ProcessFrame();

        Assert.Equal(new RealizationWindow(0, 15), panel.RealizationWindow);
        Assert.Equal((1, 1), (measuresBefore, moved.MeasureCount));
    }

    private static string GetVerticalDeviceEdges(IEnumerable<UIElement> elements, float scale)
    {
        return string.Join(", ", elements.Select(element =>
            $"{MathF.Round(element.ArrangedBounds.Y * scale)}..{MathF.Round((element.ArrangedBounds.Y + element.ArrangedBounds.Height) * scale)}"));
    }

    private sealed class CountingElement : UIElement
    {
        public int MeasureCount { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureCount++;
            return new LayoutSize(20, 10);
        }
    }

    private sealed class ConstraintRecordingElement(float height) : UIElement
    {
        public int MeasureCount { get; private set; }

        public LayoutSize? LastAvailableSize { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureCount++;
            LastAvailableSize = context.AvailableSize;
            return new LayoutSize(20, height);
        }
    }

    private sealed class FixedHeightElement(float height) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return new LayoutSize(20, height);
        }
    }
}

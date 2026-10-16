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
        // Item 2 is the last item, so the window that the first measure corrects contains no other unmeasured item.
        LayoutRounding rounding = LayoutRounding.ForScale(scale);
        VirtualizingStackPanel panel = new() { FirstRealizedIndex = 0 };
        foreach (float deviceHeight in new[] { 20f, 11f, 10f })
        {
            panel.VisualChildren.Add(new FixedHeightElement(deviceHeight / scale));
        }

        // With the initial estimate of 28, offset 28 and viewport 29 realize items 1 and 2.
        panel.UpdateViewport(new ItemsVirtualizationViewport(3, 29, 28, 0));
        panel.Measure(new MeasureContext(new LayoutSize(100, 29), rounding));
        panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, panel.DesiredSize.Height), rounding));

        // Width 99 keeps the arrange cache from reusing the first pass.
        panel.UpdateViewport(new ItemsVirtualizationViewport(3, 15 / scale, 11 / scale, 0));
        panel.Measure(new MeasureContext(new LayoutSize(100, 15 / scale), rounding));
        panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 99, panel.DesiredSize.Height), rounding));

        Assert.Equal(new RealizationWindow(1, 3), panel.RealizationWindow);
        Assert.Equal(31.5f, panel.TotalExtent * scale, 3);
        // The first realized row starts at the device pixel nearest to 10.5 (ties to even), and the second touches it.
        Assert.Equal("10..21, 21..31", GetVerticalDeviceEdges([panel.VisualChildren[1], panel.VisualChildren[2]], scale));
    }

    [Fact]
    public void AutomaticViewportMeasuresTheRowsThatItsCorrectedWindowAddsBeforeArrange()
    {
        // With the initial estimate of 28, offset 280 and viewport 400 realize items 10..25. Their measured height of 50
        // corrects the estimate and moves the window back to 5..14, so items 5..9 join the window during this measure.
        LayoutRounding rounding = LayoutRounding.ForScale(1);
        VirtualizingStackPanel panel = new() { FirstRealizedIndex = 0 };
        for (int i = 0; i < 30; i++)
        {
            panel.VisualChildren.Add(new FixedHeightElement(50));
        }

        for (int pass = 0; pass < 2; pass++)
        {
            panel.UpdateViewport(new ItemsVirtualizationViewport(30, 400, 280, 0));
            panel.Measure(new MeasureContext(new LayoutSize(100, 400), rounding));
            panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, panel.DesiredSize.Height), rounding));

            Assert.Equal(new RealizationWindow(5, 14), panel.RealizationWindow);
            Assert.Equal(
                string.Join(", ", Enumerable.Range(5, 9).Select(index => $"{index * 50}..{(index + 1) * 50}")),
                GetVerticalDeviceEdges(panel.VisualChildren.Skip(5).Take(9), 1));
            Assert.All(panel.VisualChildren.Skip(5).Take(9), child => Assert.Equal(50, child.DesiredSize.Height));
            if (pass == 0)
            {
                // Items 14..24 were measured before the window moved past them, so they keep that result instead of
                // a zero that no measure produced. Items 0..4 and 25..29 were never measured.
                Assert.All(panel.VisualChildren.Skip(14).Take(11), child => Assert.Equal(50, child.DesiredSize.Height));
                Assert.All(panel.VisualChildren.Take(5).Concat(panel.VisualChildren.Skip(25)), child => Assert.Equal(0, child.DesiredSize.Height));
            }
        }
    }

    [Fact]
    public void AutomaticViewportScrolledWithTheSameConstraintMeasuresTheRowsThatEnterTheWindow()
    {
        // Only the scroll offset changes between the passes: the available size and the item count stay the same. The
        // window moves from 5..14 to 0..8, so the measure with the same constraint must not reuse the cached measure,
        // which never measured items 0..4.
        LayoutRounding rounding = LayoutRounding.ForScale(1);
        VirtualizingStackPanel panel = new() { FirstRealizedIndex = 0 };
        for (int i = 0; i < 30; i++)
        {
            panel.VisualChildren.Add(new FixedHeightElement(50));
        }

        foreach (float offset in new[] { 280f, 0f })
        {
            panel.UpdateViewport(new ItemsVirtualizationViewport(30, 400, offset, 0));
            panel.Measure(new MeasureContext(new LayoutSize(100, 400), rounding));
            panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, panel.DesiredSize.Height), rounding));
        }

        Assert.Equal(new RealizationWindow(0, 8), panel.RealizationWindow);
        Assert.All(panel.VisualChildren.Take(8), child => Assert.Equal(50, child.DesiredSize.Height));
        Assert.Equal(
            string.Join(", ", Enumerable.Range(0, 8).Select(index => $"{index * 50}..{(index + 1) * 50}")),
            GetVerticalDeviceEdges(panel.VisualChildren.Take(8), 1));
    }

    [Fact]
    public void AutomaticViewportScrolledWithinTheSameWindowReusesThePanelMeasure()
    {
        // Offsets 10 and 20 with a viewport of 400 both realize items 0..8 of rows that are 50 tall, so the second
        // scroll changes nothing that the panel measures or arranges.
        LayoutRounding rounding = LayoutRounding.ForScale(1);
        MeasureCountingVirtualizingStackPanel panel = new() { FirstRealizedIndex = 0 };
        for (int i = 0; i < 30; i++)
        {
            panel.VisualChildren.Add(new FixedHeightElement(50));
        }

        foreach (float offset in new[] { 10f, 20f })
        {
            panel.UpdateViewport(new ItemsVirtualizationViewport(30, 400, offset, 0));
            panel.Measure(new MeasureContext(new LayoutSize(100, 400), rounding));
            panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, panel.DesiredSize.Height), rounding));
        }

        Assert.Equal(new RealizationWindow(0, 9), panel.RealizationWindow);
        Assert.Equal(1, panel.MeasureCount);
    }

    [Fact]
    public void RowRealizedAgainWithTheSameConstraintReportsItsMeasuredHeight()
    {
        // Rows 0 and 1 are measured with (100, infinity), get the zero desired size while the window is 3..4, and
        // are measured again with (100, infinity) after the scroll back. The zero was never a measure result, so the
        // measure cache must not return it.
        VirtualizingStackPanel panel = new();
        FixedHeightElement[] rows = [.. Enumerable.Range(0, 6).Select(_ => new FixedHeightElement(40))];
        foreach (FixedHeightElement row in rows)
        {
            panel.VisualChildren.Add(row);
        }

        // The panel's own constraint alternates so that its measure cache does not skip the pass; its children
        // always get (100, infinity).
        void ScrollTo(float offset, float panelHeight)
        {
            panel.UpdateViewport(new ItemsVirtualizationViewport(6, 80, offset, 0));
            panel.Measure(new MeasureContext(new LayoutSize(100, panelHeight)));
            panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, panel.DesiredSize.Height)));
        }

        ScrollTo(0, 80);
        ScrollTo(120, 81);
        Assert.Equal(new RealizationWindow(3, 5), panel.RealizationWindow);
        Assert.Equal(0, rows[0].DesiredSize.Height);

        ScrollTo(0, 80);

        Assert.Equal(new RealizationWindow(0, 2), panel.RealizationWindow);
        Assert.Equal([40f, 40f], rows[..2].Select(row => row.DesiredSize.Height));
        Assert.Equal(240, panel.TotalExtent);
        Assert.Equal(new LayoutRect(0, 0, 100, 40), rows[0].ArrangedBounds);
        Assert.Equal(new LayoutRect(0, 40, 100, 40), rows[1].ArrangedBounds);
    }

    [Theory]
    [InlineData(1f, 33f, 1_000_000)]
    [InlineData(1.75f, 28f, 400_000)]
    [InlineData(1.25f, 41 / 1.25f, 100_000)]
    public void AutomaticViewportScrolledToTheEndOfALongListRealizesAndArrangesTheLastItemsAtTheTotalExtent(
        float scale,
        float itemHeight,
        int itemCount)
    {
        // Past 2^24 device pixels a float holds only even integers, so a float sum of odd device-pixel heights loses a
        // pixel per item: 33 at scale 1 and 28 at scale 1.75 (49 device pixels) are odd. A height of 41 device pixels at
        // scale 1.25 is 32.8, which a float cannot hold, so a float sum of it drifts much sooner, at a few million.
        // The realization window and the arrange offsets must add the heights as TotalExtent does, so the end of the
        // scroll range realizes the last items and the last item ends at TotalExtent, within the float precision at
        // that magnitude (2 pixels here).
        LayoutRounding rounding = LayoutRounding.ForScale(scale);
        float viewportExtent = 10 * itemHeight;
        VirtualizingStackPanel panel = new() { FirstRealizedIndex = 0 };
        for (int index = 0; index < 10; index++)
        {
            panel.VisualChildren.Add(new FixedHeightElement(itemHeight));
        }

        panel.UpdateViewport(new ItemsVirtualizationViewport(itemCount, viewportExtent, 0, 0));
        panel.Measure(new MeasureContext(new LayoutSize(100, viewportExtent), rounding));
        float totalExtent = panel.TotalExtent;
        Assert.Equal((float)((double)itemCount * itemHeight), totalExtent);

        panel.UpdateViewport(new ItemsVirtualizationViewport(itemCount, viewportExtent, totalExtent - viewportExtent, 0));
        Assert.Equal(new RealizationWindow(itemCount - 10, itemCount), panel.RealizationWindow);

        // The same ten children now represent the last ten items. Width 99 keeps the layout caches from reusing the first pass.
        panel.FirstRealizedIndex = itemCount - 10;
        panel.Measure(new MeasureContext(new LayoutSize(99, viewportExtent), rounding));
        panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 99, panel.DesiredSize.Height), rounding));

        LayoutRect first = panel.VisualChildren[0].ArrangedBounds;
        LayoutRect last = panel.VisualChildren[9].ArrangedBounds;
        Assert.InRange(first.Y, totalExtent - viewportExtent - 2, totalExtent - viewportExtent + 2);
        Assert.InRange(last.Y + last.Height, totalExtent - 2, totalExtent + 2);
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

    private sealed class MeasureCountingVirtualizingStackPanel : VirtualizingStackPanel
    {
        public int MeasureCount { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureCount++;
            return base.MeasureCore(context);
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

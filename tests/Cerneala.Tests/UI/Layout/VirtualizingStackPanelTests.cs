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

    [Theory]
    [InlineData(1f, 33f, 33f, 1_000_000)]
    [InlineData(1.25f, 41f, 41f, 1_000_000)]
    [InlineData(1.75f, 49f, 49f, 400_000)]
    [InlineData(1f, 33f, 34f, 1_000_000)]
    [InlineData(1.25f, 41f, 42f, 1_000_000)]
    public void AutomaticWindowMatchesAWalkThatConvertsEveryDevicePixelPrefixToAnExtent(
        float scale,
        float evenItemDevicePixels,
        float oddItemDevicePixels,
        int itemCount)
    {
        // The window must be the one found by adding each item's device pixels in a double and converting every
        // prefix to a float extent before comparing it with the scroll offset and the viewport end, also next to
        // item edges past 2^24 device pixels, where neighbouring float offsets are several device pixels apart.
        // Items 0 to 9 are measured. With two heights the estimate of the other items, their average, is half a
        // device pixel off a whole one: 33.5 at scale 1, and 32.8 and 33.6 average to 33.2 (41.5 device pixels) at 1.25.
        LayoutRounding rounding = LayoutRounding.ForScale(scale);
        float[] heights = [.. Enumerable.Range(0, 10).Select(index => (index % 2 == 0 ? evenItemDevicePixels : oddItemDevicePixels) / scale)];
        float estimate = heights.Average();
        double estimateDevicePixels = MathF.Round(estimate * scale) / scale == estimate ? MathF.Round(estimate * scale) : estimate * scale;
        double GetItemDevicePixels(int index) => index < 10 ? MathF.Round(heights[index] * scale) : estimateDevicePixels;
        float viewportExtent = 330 / scale;
        VirtualizingStackPanel panel = new() { FirstRealizedIndex = 0 };
        foreach (float height in heights)
        {
            panel.VisualChildren.Add(new FixedHeightElement(height));
        }

        panel.UpdateViewport(new ItemsVirtualizationViewport(itemCount, viewportExtent, 0, 0));
        panel.Measure(new MeasureContext(new LayoutSize(100, viewportExtent), rounding));

        int pastFloatIntegers = (int)Math.Ceiling(16_777_216 / estimateDevicePixels);
        List<float> offsets = [16_000_000f, 16_000_000f - 1, panel.TotalExtent - viewportExtent, float.MaxValue];
        foreach (int itemIndex in new[] { 0, 1, 2, 11, itemCount / 2, pastFloatIntegers, pastFloatIntegers + 1, itemCount - 10, itemCount - 1, itemCount })
        {
            double prefix = 0;
            for (int index = 0; index < itemIndex; index++)
            {
                prefix += GetItemDevicePixels(index);
            }

            float edge = PrefixToExtent(prefix, scale);
            offsets.AddRange([edge, MathF.BitDecrement(edge), MathF.BitIncrement(edge), edge - viewportExtent, MathF.BitIncrement(edge - viewportExtent)]);
        }

        foreach (float offset in offsets.Where(offset => offset >= 0))
        {
            RealizationWindow expected = GetPrefixWalkWindow(itemCount, GetItemDevicePixels, scale, offset, viewportExtent);
            panel.UpdateViewport(new ItemsVirtualizationViewport(itemCount, viewportExtent, offset, 0));
            Assert.True(expected == panel.RealizationWindow, $"offset {offset:R}: expected {expected}, got {panel.RealizationWindow}");
        }
    }

    // The window of PR #292: every item adds its device pixels to a double prefix, and each prefix is converted to a
    // float extent before it is compared.
    private static RealizationWindow GetPrefixWalkWindow(
        int itemCount,
        Func<int, double> getItemDevicePixels,
        float scale,
        float offset,
        float viewportExtent)
    {
        float viewportEnd = offset + viewportExtent;
        int start = 0;
        double cursor = 0;
        while (start < itemCount && PrefixToExtent(cursor + getItemDevicePixels(start), scale) <= offset)
        {
            cursor += getItemDevicePixels(start);
            start++;
        }

        int end = start;
        while (end < itemCount && (PrefixToExtent(cursor, scale) < viewportEnd || end == start))
        {
            cursor += getItemDevicePixels(end);
            end++;
        }

        return new RealizationWindow(start, end);
    }

    private static float PrefixToExtent(double devicePixels, float scale)
    {
        float extent = (float)devicePixels / scale;
        return float.IsFinite(extent) ? extent : float.MaxValue;
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

    private sealed class FixedHeightElement(float height) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return new LayoutSize(20, height);
        }
    }
}

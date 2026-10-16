using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Layout.Virtualization;
using Cerneala.UI.Media;

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

    [Fact]
    public void SkippedChildrenOfADirectlyUsedPanelDrawNothing()
    {
        // Rows 5..29 lie outside the realization window and are arranged into zero-sized bounds at the panel origin,
        // where their labels would pile up on the realized "Row 0".
        UIRoot root = new(320, 200);
        VirtualizingStackPanel panel = new();
        for (int index = 0; index < 30; index++)
        {
            panel.VisualChildren.Add(new Cerneala.UI.Controls.Border
            {
                Height = 40,
                Child = new Cerneala.UI.Controls.TextBlock
                {
                    Text = $"Row {index}",
                    Foreground = new SolidColorBrush(Color.White),
                    Margin = new Thickness(10, 9, 0, 0)
                }
            });
        }

        root.VisualChildren.Add(panel);
        panel.UpdateViewport(new ItemsVirtualizationViewport(30, 200, 0, 0));
        root.ProcessFrame();
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Equal(new RealizationWindow(0, 5), panel.RealizationWindow);
        Assert.Equal(
            ["Row 0", "Row 1", "Row 2", "Row 3", "Row 4"],
            commands.Where(command => command.Kind == DrawCommandKind.DrawText).Select(command => command.Text).ToArray());
    }

    [Fact]
    public void SkippedChildTakesPartInRenderingHitTestingAndInputOnlyWhileRealized()
    {
        UIRoot root = new(100, 100);
        VirtualizingStackPanel panel = new();
        for (int index = 0; index < 10; index++)
        {
            panel.VisualChildren.Add(new UIElement { Height = 20, Focusable = true });
        }

        root.VisualChildren.Add(panel);
        panel.UpdateViewport(new ItemsVirtualizationViewport(10, 100, 0, 0));
        root.ProcessFrame();
        UIElement skipped = panel.VisualChildren[7];

        Assert.Equal(new RealizationWindow(0, 5), panel.RealizationWindow);
        Assert.Equal(
            (false, false, false),
            (UIElementVisibility.ParticipatesInRendering(skipped),
                UIElementVisibility.ParticipatesInHitTest(skipped),
                UIElementVisibility.ParticipatesInInput(skipped)));

        panel.UpdateViewport(new ItemsVirtualizationViewport(10, 100, 100, 0));
        panel.Invalidate(InvalidationFlags.Measure | InvalidationFlags.Arrange, "test");
        root.ProcessFrame();

        Assert.Equal(new RealizationWindow(5, 10), panel.RealizationWindow);
        Assert.Equal(
            (true, true, true),
            (UIElementVisibility.ParticipatesInRendering(skipped),
                UIElementVisibility.ParticipatesInHitTest(skipped),
                UIElementVisibility.ParticipatesInInput(skipped)));
        Assert.False(UIElementVisibility.ParticipatesInRendering(panel.VisualChildren[0]));
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

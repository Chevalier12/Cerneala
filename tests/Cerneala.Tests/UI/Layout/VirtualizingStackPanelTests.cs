using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Layout.Virtualization;
using Cerneala.UI.Media;
using Cerneala.UI.Motion.Presence;
using Cerneala.Tests.UI.Motion.Core;
using Border = Cerneala.UI.Controls.Border;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

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

    [Fact]
    public void ChildRealizedAgainReportsAMeasureChangeOfADescendantMadeOutsideTheRealizationWindow()
    {
        // Each row is row > container > leaf, so the change is two levels below the row the panel skips.
        UIRoot root = new(200, 100, 1);
        VirtualizingStackPanel panel = new() { FirstRealizedIndex = 0 };
        List<UIElement> leaves = [];
        List<Border> rows = [];
        for (int i = 0; i < 20; i++)
        {
            UIElement leaf = new() { Height = 20 };
            Border row = new() { Child = new Border { Child = leaf } };
            leaves.Add(leaf);
            rows.Add(row);
            panel.VisualChildren.Add(row);
        }

        root.VisualChildren.Add(panel);
        panel.UpdateViewport(new ItemsVirtualizationViewport(20, 100, 0, 0));
        root.ProcessFrame();
        panel.UpdateViewport(new ItemsVirtualizationViewport(20, 100, 200, 0));
        panel.Invalidate(InvalidationFlags.Measure | InvalidationFlags.Arrange, "scroll");
        root.ProcessFrame();
        Assert.False(panel.RealizationWindow.Contains(0));

        leaves[0].Height = 60;
        root.ProcessFrame();
        panel.UpdateViewport(new ItemsVirtualizationViewport(20, 100, 0, 0));
        panel.Invalidate(InvalidationFlags.Measure | InvalidationFlags.Arrange, "scroll back");
        root.ProcessFrame();

        Assert.True(panel.RealizationWindow.Contains(0));
        Assert.Equal(
            (60f, 60f, new LayoutRect(0, 0, 200, 60), 60f),
            (leaves[0].DesiredSize.Height, rows[0].DesiredSize.Height, rows[0].ArrangedBounds, rows[1].ArrangedBounds.Y));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ChildRealizedAgainArrangesADescendantBelowACanvasThatChangedOutsideTheRealizationWindow(bool arrangesPanelWhileRowIsSkipped)
    {
        // A Canvas arranges its child at the child's desired size, so the stack below it gets the slot 0, 0, 0, 60
        // both while the row is skipped and after it is realized again: only its children's desired heights change.
        UIRoot root = new(200, 100, 1);
        VirtualizingStackPanel panel = new() { FirstRealizedIndex = 0 };
        UIElement first = new() { Height = 20 };
        UIElement second = new() { Height = 40 };
        Cerneala.UI.Controls.StackPanel stack = new();
        stack.VisualChildren.Add(first);
        stack.VisualChildren.Add(second);
        Cerneala.UI.Controls.Canvas canvas = new() { Height = 60 };
        canvas.VisualChildren.Add(stack);
        panel.VisualChildren.Add(new Border { Child = canvas });
        for (int i = 1; i < 20; i++)
        {
            panel.VisualChildren.Add(new Border { Child = new UIElement { Height = 20 } });
        }

        root.VisualChildren.Add(panel);
        panel.UpdateViewport(new ItemsVirtualizationViewport(20, 100, 0, 0));
        root.ProcessFrame();
        panel.UpdateViewport(new ItemsVirtualizationViewport(20, 100, 200, 0));
        panel.Invalidate(InvalidationFlags.Measure | InvalidationFlags.Arrange, "scroll");
        root.ProcessFrame();
        Assert.False(panel.RealizationWindow.Contains(0));

        first.Height = 40;
        second.Height = 20;
        root.ProcessFrame();
        if (arrangesPanelWhileRowIsSkipped)
        {
            panel.UpdateViewport(new ItemsVirtualizationViewport(20, 100, 220, 0));
            panel.Invalidate(InvalidationFlags.Measure | InvalidationFlags.Arrange, "scroll inside the skipped range");
            root.ProcessFrame();
            Assert.False(panel.RealizationWindow.Contains(0));
        }

        panel.UpdateViewport(new ItemsVirtualizationViewport(20, 100, 0, 0));
        panel.Invalidate(InvalidationFlags.Measure | InvalidationFlags.Arrange, "scroll back");
        root.ProcessFrame();

        Assert.True(panel.RealizationWindow.Contains(0));
        Assert.Equal(
            (40f, new LayoutRect(0, 0, 0, 40), 20f, new LayoutRect(0, 40, 0, 20)),
            (first.DesiredSize.Height, first.ArrangedBounds, second.DesiredSize.Height, second.ArrangedBounds));
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

    [Fact]
    public void RemovingASkippedChildWithPresenceDrawsNothingOfIt()
    {
        // Row 20 is skipped and arranged into zero-sized bounds at the panel origin. An exit would draw its label there,
        // on top of the realized "Row 0"; a skipped row draws nothing, so it is detached at once instead.
        ManualMotionClock clock = new();
        (UIRoot root, VirtualizingStackPanel panel) = CreatePresenceRows(clock);
        UIElement row20 = panel.VisualChildren[20];

        Assert.True(panel.VisualChildren.Remove(row20));

        Assert.Equal(PresenceState.Detached, root.Motion.Presence.GetState(row20));
        Assert.False(row20.IsAttached);
        Assert.Empty(root.Motion.Presence.GetExitingVisualChildren(panel));
        clock.Advance(TimeSpan.FromMilliseconds(100));
        Assert.Equal(["Row 0", "Row 1", "Row 2", "Row 3", "Row 4"], CommitTexts(root));
    }

    [Fact]
    public void RemovingARealizedChildWithPresenceStillRunsItsExit()
    {
        ManualMotionClock clock = new();
        (UIRoot root, VirtualizingStackPanel panel) = CreatePresenceRows(clock);
        UIElement row2 = panel.VisualChildren[2];

        Assert.True(panel.VisualChildren.Remove(row2));
        clock.Advance(TimeSpan.FromMilliseconds(100));

        Assert.Equal(PresenceState.Exiting, root.Motion.Presence.GetState(row2));
        Assert.Contains("Row 2", CommitTexts(root));

        for (int frame = 0; frame < 12; frame++)
        {
            clock.Advance(TimeSpan.FromMilliseconds(100));
            root.ProcessFrame();
        }

        Assert.Equal(PresenceState.Detached, root.Motion.Presence.GetState(row2));
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
    public void AutomaticViewportWithAContextScrolledWithinTheSameContextWindowReusesThePanelMeasure()
    {
        // With a context set, the context supplies RealizationWindow: item extent 50 and viewport 410 give items 0..8 at
        // offsets 0 and 20. The automatic window of the 10 tall rows moves from 0..14 to 2..29, but the panel neither
        // measures nor arranges by it, so the second scroll changes nothing that the panel measures or arranges.
        LayoutRounding rounding = LayoutRounding.ForScale(1);
        MeasureCountingVirtualizingStackPanel panel = new() { FirstRealizedIndex = 0 };
        for (int i = 0; i < 30; i++)
        {
            panel.VisualChildren.Add(new FixedHeightElement(10));
        }

        int? layoutVersion = null;
        foreach (float offset in new[] { 0f, 20f })
        {
            panel.VirtualizationContext = new VirtualizationContext(30, 50, 410, offset);
            panel.UpdateViewport(new ItemsVirtualizationViewport(30, 410, offset, 0));
            Assert.Equal(new RealizationWindow(0, 9), panel.RealizationWindow);
            layoutVersion ??= panel.LayoutVersion;
            Assert.Equal(layoutVersion, panel.LayoutVersion);
            panel.Measure(new MeasureContext(new LayoutSize(100, 410), rounding));
            panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, panel.DesiredSize.Height), rounding));
        }

        Assert.Equal(1, panel.MeasureCount);
    }

    [Fact]
    public void AutomaticViewportWithAContextScrolledIntoTheNextContextWindowMeasuresTheRowThatEntersIt()
    {
        // The context is set before UpdateViewport, as in the scroll above. Offsets 40 and 60 move the context window from
        // 0..8 to 1..9, so the measure with the same constraint must not reuse the cached measure, which never measured
        // item 9.
        LayoutRounding rounding = LayoutRounding.ForScale(1);
        VirtualizingStackPanel panel = new() { FirstRealizedIndex = 0 };
        for (int i = 0; i < 30; i++)
        {
            panel.VisualChildren.Add(new FixedHeightElement(10));
        }

        foreach (float offset in new[] { 40f, 60f })
        {
            panel.VirtualizationContext = new VirtualizationContext(30, 50, 410, offset);
            panel.UpdateViewport(new ItemsVirtualizationViewport(30, 410, offset, 0));
            panel.Measure(new MeasureContext(new LayoutSize(100, 410), rounding));
            panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, panel.DesiredSize.Height), rounding));
        }

        Assert.Equal(new RealizationWindow(1, 10), panel.RealizationWindow);
        Assert.All(panel.VisualChildren.Skip(1).Take(9), child => Assert.Equal(10, child.DesiredSize.Height));
        Assert.Equal(
            string.Join(", ", Enumerable.Range(1, 9).Select(index => $"{index * 10}..{(index + 1) * 10}")),
            GetVerticalDeviceEdges(panel.VisualChildren.Skip(1).Take(9), 1));
    }

    [Fact]
    public void ContextScrolledWithTheSameConstraintMeasuresTheRowsThatEnterTheWindow()
    {
        // Without UpdateViewport the context alone supplies the window: offsets 0 and 100 with item extent 10 and
        // viewport 50 realize items 0..4 and then 10..14, so the measure with the same constraint must measure 10..14.
        LayoutRounding rounding = LayoutRounding.ForScale(1);
        VirtualizingStackPanel panel = new() { FirstRealizedIndex = 0 };
        for (int i = 0; i < 30; i++)
        {
            panel.VisualChildren.Add(new FixedHeightElement(10));
        }

        foreach (float offset in new[] { 0f, 100f })
        {
            panel.VirtualizationContext = new VirtualizationContext(30, 10, 50, offset);
            panel.Measure(new MeasureContext(new LayoutSize(100, 50), rounding));
            panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, panel.DesiredSize.Height), rounding));
        }

        Assert.Equal(new RealizationWindow(10, 15), panel.RealizationWindow);
        Assert.All(panel.VisualChildren.Skip(10).Take(5), child => Assert.Equal(10, child.DesiredSize.Height));
    }

    [Fact]
    public void ContextScrolledWithinTheSameWindowReusesThePanelMeasure()
    {
        // Offsets 1 and 5 with item extent 10 and viewport 50 both realize items 0..5: the new context changes only the
        // scroll offset, which reaches measure and arrange only through the window.
        LayoutRounding rounding = LayoutRounding.ForScale(1);
        MeasureCountingVirtualizingStackPanel panel = new() { FirstRealizedIndex = 0 };
        for (int i = 0; i < 30; i++)
        {
            panel.VisualChildren.Add(new FixedHeightElement(10));
        }

        int? layoutVersion = null;
        foreach (float offset in new[] { 1f, 5f })
        {
            panel.VirtualizationContext = new VirtualizationContext(30, 10, 50, offset);
            Assert.Equal(new RealizationWindow(0, 6), panel.RealizationWindow);
            layoutVersion ??= panel.LayoutVersion;
            Assert.Equal(layoutVersion, panel.LayoutVersion);
            panel.Measure(new MeasureContext(new LayoutSize(100, 50), rounding));
            panel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, panel.DesiredSize.Height), rounding));
        }

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

    // 30 rows of height 40 with a viewport of 200, so rows 0..4 are realized and rows 5..29 are skipped; the enter
    // motion of every row has ended.
    private static (UIRoot Root, VirtualizingStackPanel Panel) CreatePresenceRows(ManualMotionClock clock)
    {
        UIRoot root = new(320, 200, motionClock: clock);
        VirtualizingStackPanel panel = new();
        for (int index = 0; index < 30; index++)
        {
            panel.VisualChildren.Add(new Cerneala.UI.Controls.Border
            {
                Height = 40,
                Presence = PresenceOptions.FadeAndScale(
                    MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)),
                    MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(1000))),
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
        clock.Advance(TimeSpan.FromMilliseconds(200));
        root.ProcessFrame();
        Assert.Equal(["Row 0", "Row 1", "Row 2", "Row 3", "Row 4"], CommitTexts(root));
        return (root, panel);
    }

    private static string?[] CommitTexts(UIRoot root)
    {
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();
        return root.RetainedRenderer.Commit(root)
            .Where(command => command.Kind == DrawCommandKind.DrawText)
            .Select(command => command.Text)
            .ToArray();
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

    private sealed class MeasureCountingVirtualizingStackPanel : VirtualizingStackPanel
    {
        public int MeasureCount { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureCount++;
            return base.MeasureCore(context);
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

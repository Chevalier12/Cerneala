using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Detective;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.Tests.Controls;

public sealed class ScrollViewerTests
{
    [Fact]
    public void PresenterComputesExtentViewportAndClampsOffset()
    {
        ScrollContentPresenter presenter = new()
        {
            Content = new FixedElement(new LayoutSize(200, 150))
        };

        presenter.Measure(new MeasureContext(new LayoutSize(80, 50)));
        presenter.SetHorizontalOffset(500);
        presenter.SetVerticalOffset(500);

        Assert.Equal(200, presenter.ExtentWidth);
        Assert.Equal(150, presenter.ExtentHeight);
        Assert.Equal(80, presenter.ViewportWidth);
        Assert.Equal(50, presenter.ViewportHeight);
        Assert.Equal(120, presenter.HorizontalOffset);
        Assert.Equal(100, presenter.VerticalOffset);
    }

    [Fact]
    public void PresenterArrangesContentWithNegativeOffsetAndClip()
    {
        ScrollContentPresenter presenter = new()
        {
            Content = new FixedElement(new LayoutSize(200, 150))
        };
        presenter.Measure(new MeasureContext(new LayoutSize(80, 50)));
        presenter.SetHorizontalOffset(20);
        presenter.SetVerticalOffset(10);

        presenter.Arrange(new ArrangeContext(new LayoutRect(5, 6, 80, 50)));

        UIElement child = Assert.IsType<FixedElement>(presenter.Content);
        Assert.Equal(new LayoutRect(-15, -4, 200, 150), child.ArrangedBounds);
    }

    [Fact]
    public void PresenterArrangesDisabledHorizontalAxisToViewportWidth()
    {
        ScrollContentPresenter presenter = new()
        {
            CanHorizontallyScroll = false,
            CanVerticallyScroll = true,
            Content = new FixedElement(new LayoutSize(800, 200))
        };

        presenter.Measure(new MeasureContext(new LayoutSize(100, 80)));
        presenter.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 80)));

        UIElement child = Assert.IsType<FixedElement>(presenter.Content);
        Assert.Equal(new LayoutRect(0, 0, 100, 200), child.ArrangedBounds);
    }

    [Fact]
    public void PresenterRoundedClipMatchesRoundedHitTestBounds()
    {
        UIRoot root = new(100, 100);
        FixedElement content = new(new LayoutSize(20, 20));
        ScrollContentPresenter presenter = new()
        {
            Content = content
        };
        root.VisualChildren.Add(presenter);
        LayoutRounding rounding = LayoutRounding.Enabled;
        presenter.Measure(new MeasureContext(new LayoutSize(10.4f, 10.4f), rounding));
        presenter.Arrange(new ArrangeContext(new LayoutRect(0.4f, 0.4f, 10.4f, 10.4f), rounding));

        HitTestResult? result = new HitTestService().HitTest(root, 0.2f, 0.2f);

        Assert.Same(content, result!.Element);
    }

    // The content fits, so the scroll bar stays Collapsed in both modes and only the presenter's scroll capability
    // changes (#561). WPF's ScrollContentPresenter.CanVerticallyScroll and CanHorizontallyScroll call InvalidateMeasure.
    // The constraint is the one of the content's last Measure call: switching back can reuse an older cached measure
    // of the content without running its MeasureCore.
    [Theory]
    [InlineData(false, ScrollBarVisibility.Disabled, ScrollBarVisibility.Auto, 320f, float.PositiveInfinity)]
    [InlineData(false, ScrollBarVisibility.Auto, ScrollBarVisibility.Disabled, float.PositiveInfinity, 320f)]
    [InlineData(true, ScrollBarVisibility.Disabled, ScrollBarVisibility.Auto, 200f, float.PositiveInfinity)]
    [InlineData(true, ScrollBarVisibility.Auto, ScrollBarVisibility.Disabled, float.PositiveInfinity, 200f)]
    public void ScrollViewerMeasuresContentWithNewConstraintWhenScrollingIsEnabledOrDisabled(
        bool horizontal,
        ScrollBarVisibility from,
        ScrollBarVisibility to,
        float before,
        float after)
    {
        UIRoot root = new(400, 400);
        FixedElement content = new(new LayoutSize(200, 320));
        ScrollViewer viewer = new()
        {
            Width = 200,
            Height = 320,
            HorizontalScrollBarVisibility = horizontal ? from : ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = horizontal ? ScrollBarVisibility.Disabled : from,
            Content = content
        };
        root.VisualChildren.Add(viewer);
        root.ProcessFrame();
        float first = LastConstraint();

        Switch(to);
        float switched = LastConstraint();
        Switch(from);
        float switchedBack = LastConstraint();

        Assert.Equal((before, after, before), (first, switched, switchedBack));

        void Switch(ScrollBarVisibility visibility)
        {
            if (horizontal)
            {
                viewer.HorizontalScrollBarVisibility = visibility;
            }
            else
            {
                viewer.VerticalScrollBarVisibility = visibility;
            }

            root.ProcessFrame();
            root.ProcessFrame();
        }

        float LastConstraint()
        {
            LayoutSize constraint = LayoutDiagnostics.Capture(content).LastMeasureAvailableSize!.Value;
            return horizontal ? constraint.Width : constraint.Height;
        }
    }

    [Fact]
    public void ScrollViewerWheelScrollsVerticalOffset()
    {
        UIRoot root = new(100, 100);
        ScrollViewer viewer = new()
        {
            Content = new FixedElement(new LayoutSize(80, 300)),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        viewer.Measure(new MeasureContext(new LayoutSize(80, 80)));
        viewer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 80, 80)));
        root.VisualChildren.Add(viewer);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerWheelFrame(10, 10, -120));

        Assert.Equal(48, viewer.Presenter.VerticalOffset);
    }

    [Fact]
    public void ScrollViewerScrollsOneStepForEachWheelEventOfOneFrame()
    {
        UIRoot root = new(100, 100);
        ScrollViewer viewer = new()
        {
            Content = new FixedElement(new LayoutSize(80, 1000)),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        viewer.Measure(new MeasureContext(new LayoutSize(80, 80)));
        viewer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 80, 80)));
        root.VisualChildren.Add(viewer);
        List<int> deltas = [];
        root.AddHandler(InputEvents.MouseWheelEvent, (_, args) => deltas.Add(((MouseWheelEventArgs)args).Delta), handledEventsToo: true);
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(10, 10);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, new InputFrame(
            previous,
            previous.WithWheelValue(-360),
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            [],
            [],
            [-120, -120, -120]));

        Assert.Equal([-120, -120, -120], deltas);
        Assert.Equal(144, viewer.Presenter.VerticalOffset);
    }

    [Fact]
    public void TemplatePartsBecomeTheActiveScrollViewerParts()
    {
        ScrollContentPresenter presenter = new();
        ScrollBar horizontal = new() { Orientation = Orientation.Horizontal };
        ScrollBar vertical = new() { Orientation = Orientation.Vertical };
        StackPanel root = new();
        root.VisualChildren.Add(presenter);
        root.VisualChildren.Add(horizontal);
        root.VisualChildren.Add(vertical);
        ScrollViewer viewer = new()
        {
            ComponentTemplate = new ComponentTemplate<ScrollViewer>("custom", context =>
            {
                context.RequirePart("PART_ScrollContentPresenter", presenter);
                context.RequirePart("PART_HorizontalScrollBar", horizontal);
                context.RequirePart("PART_VerticalScrollBar", vertical);
                return root;
            })
        };

        Assert.Same(presenter, viewer.Presenter);
        Assert.Same(horizontal, viewer.HorizontalScrollBar);
        Assert.Same(vertical, viewer.VerticalScrollBar);
    }

    [Fact]
    public void DefaultTemplatePartsAreAvailableBeforeMeasure()
    {
        ScrollViewer viewer = new();

        Assert.NotNull(viewer.Presenter);
        Assert.NotNull(viewer.HorizontalScrollBar);
        Assert.NotNull(viewer.VerticalScrollBar);
        Assert.Same(viewer.Presenter, viewer.ScrollInfo);
    }

    [Fact]
    public void CustomTemplateKeepsContentOffsetsAndScrollBarValuesSynchronized()
    {
        ScrollContentPresenter presenter = new();
        ScrollBar horizontal = new();
        ScrollBar vertical = new();
        ScrollViewer viewer = new()
        {
            Content = new FixedElement(new LayoutSize(200, 300)),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            ComponentTemplate = ViewerTemplate("custom", presenter, horizontal, vertical)
        };

        viewer.Measure(new MeasureContext(new LayoutSize(100, 100)));
        viewer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));
        vertical.Value = 40;
        horizontal.Value = 30;

        Assert.Equal(40, presenter.VerticalOffset);
        Assert.Equal(30, presenter.HorizontalOffset);
        Assert.Same(viewer.Content, presenter.Content);
    }

    [Fact]
    public void OldViewerPartsStopSynchronizingAfterTemplateSwap()
    {
        ScrollViewer viewer = new() { Content = new FixedElement(new LayoutSize(200, 300)) };
        ScrollContentPresenter oldPresenter = viewer.Presenter;
        ScrollBar oldVertical = viewer.VerticalScrollBar;
        ScrollContentPresenter newPresenter = new();
        ScrollBar newHorizontal = new();
        ScrollBar newVertical = new();

        viewer.ComponentTemplate = ViewerTemplate("new", newPresenter, newHorizontal, newVertical);
        oldPresenter.SetVerticalOffset(50);
        oldVertical.Value = 50;

        Assert.Same(newPresenter, viewer.Presenter);
        Assert.Same(viewer.Content, newPresenter.Content);
        Assert.Equal(0, newPresenter.VerticalOffset);
        Assert.Equal(0, newVertical.Value);
    }

    [Fact]
    public void ClearingCustomViewerTemplateRestoresDefaultParts()
    {
        ScrollContentPresenter customPresenter = new();
        ScrollViewer viewer = new()
        {
            ComponentTemplate = ViewerTemplate("custom", customPresenter, new ScrollBar(), new ScrollBar())
        };

        viewer.ComponentTemplate = null;

        Assert.NotSame(customPresenter, viewer.Presenter);
        Assert.NotNull(viewer.ComponentTemplate);
    }

    [Fact]
    public void ViewerTemplateWithWrongPartTypeFailsEarly()
    {
        ScrollViewer viewer = new();
        ScrollContentPresenter presenter = new();
        UIElement wrongHorizontal = new();
        ScrollBar vertical = new();
        StackPanel root = new();
        root.VisualChildren.Add(presenter);
        root.VisualChildren.Add(wrongHorizontal);
        root.VisualChildren.Add(vertical);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
            viewer.ComponentTemplate = new ComponentTemplate<ScrollViewer>("wrong", context =>
            {
                context.RequirePart("PART_ScrollContentPresenter", presenter);
                context.RequirePart("PART_HorizontalScrollBar", wrongHorizontal);
                context.RequirePart("PART_VerticalScrollBar", vertical);
                return root;
            }));

        Assert.Contains("PART_HorizontalScrollBar", error.Message);
        Assert.Contains(typeof(ScrollBar).FullName!, error.Message);
        Assert.Null(root.VisualParent);
    }

    [Fact]
    public void DisablingVerticalScrollingCoercesExistingOffsetToZero()
    {
        ScrollViewer viewer = new()
        {
            Content = new FixedElement(new LayoutSize(80, 300)),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        viewer.Measure(new MeasureContext(new LayoutSize(80, 80)));
        viewer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 80, 80)));
        viewer.ScrollInfo.SetVerticalOffset(120);

        viewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        viewer.Measure(new MeasureContext(new LayoutSize(80, 80)));
        viewer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 80, 80)));

        Assert.Equal(0, viewer.Presenter.VerticalOffset);
        Assert.Equal(0, viewer.VerticalScrollBar.Value);
        Assert.Equal(Visibility.Collapsed, viewer.VerticalScrollBar.Visibility);
    }

    [Fact]
    public void ScrollBarVisibilityPolicyControlsVisibleAndReservedBars()
    {
        ScrollViewer viewer = new()
        {
            Content = new FixedElement(new LayoutSize(20, 20)),
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden
        };

        viewer.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.True(viewer.IsVerticalScrollBarVisible);
        Assert.False(viewer.IsHorizontalScrollBarVisible);
        Assert.Equal(Visibility.Collapsed, viewer.HorizontalScrollBar.Visibility);
    }

    // As WPF's ComputedHorizontalScrollBarVisibility, a Hidden policy collapses the bar: it takes no space and is not
    // drawn, while the presenter can still scroll on that axis.
    [Fact]
    public void HiddenPolicyTakesNoTemplateGridSpaceAndVisiblePolicyReservesIt()
    {
        ScrollViewer viewer = new()
        {
            Content = new FixedElement(new LayoutSize(20, 20)),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible
        };

        viewer.Measure(new MeasureContext(new LayoutSize(100, 100)));
        viewer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        // The ScrollBar default border is a positive 1, painted outside the bar, so the bar is 10 wide.
        Assert.Equal(new LayoutRect(0, 0, 90, 100), viewer.Presenter.ArrangedBounds);
        Assert.Equal(new LayoutRect(90, 0, 10, 100), viewer.VerticalScrollBar.ArrangedBounds);
        Assert.Equal(Visibility.Collapsed, viewer.HorizontalScrollBar.Visibility);
        Assert.Equal(Visibility.Visible, viewer.VerticalScrollBar.Visibility);
        Assert.True(viewer.Presenter.CanHorizontallyScroll);
    }

    // A left-aligned viewer in a StackPanel desires its content's width, so it grows when its content grows, as WPF's
    // ScrollViewer does for every horizontal policy.
    [Theory]
    [InlineData(ScrollBarVisibility.Auto)]
    [InlineData(ScrollBarVisibility.Hidden)]
    [InlineData(ScrollBarVisibility.Disabled)]
    public void AutoWidthViewerGrowsWhenContentGrows(ScrollBarVisibility horizontal)
    {
        TextBlock block = new() { Text = "ab" };
        ScrollViewer viewer = new()
        {
            Content = block,
            HorizontalAlignment = HorizontalAlignment.Left,
            HorizontalScrollBarVisibility = horizontal,
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden
        };
        StackPanel panel = new();
        panel.Children.Add(viewer);
        UIRoot root = new(300, 200);
        root.VisualChildren.Add(panel);
        root.ProcessFrame();
        float before = viewer.ArrangedBounds.Width;

        block.Text = "abcdefghijkl";
        root.ProcessFrame();

        Assert.True(block.DesiredSize.Width > before);
        Assert.Equal(block.DesiredSize.Width, viewer.ArrangedBounds.Width);
    }

    // As WPF, where a control builds its template in its own measure and a Collapsed element is never measured: a bar
    // that was never shown has no Track, Thumb or RepeatButton (#714).
    [Fact]
    public void HiddenScrollBarsDoNotBuildTheirTemplates()
    {
        ScrollViewer viewer = new()
        {
            Width = 120,
            Content = new TextBlock { Text = "hello world" },
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden
        };
        StackPanel panel = new();
        panel.Children.Add(viewer);
        UIRoot root = new(800, 2000);
        root.VisualChildren.Add(panel);

        root.ProcessFrame();

        Assert.Equal(Visibility.Collapsed, viewer.HorizontalScrollBar.Visibility);
        Assert.Equal(Visibility.Collapsed, viewer.VerticalScrollBar.Visibility);
        Assert.Null(viewer.HorizontalScrollBar.ComponentTemplateInstance);
        Assert.Null(viewer.VerticalScrollBar.ComponentTemplateInstance);
        Assert.DoesNotContain(Descendants(viewer), element => element is Track or Thumb or RepeatButton);
    }

    // An Auto bar whose content grows past the viewport builds its template in the frame that shows it, and is laid
    // out as a bar that was visible from the start (#714).
    [Fact]
    public void AutoScrollBarBuildsItsTemplateInTheFrameThatShowsIt()
    {
        MutableElement content = new(new LayoutSize(20, 20));
        ScrollViewer viewer = new()
        {
            Width = 120,
            Height = 60,
            Content = content,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        UIRoot root = new(300, 200);
        root.VisualChildren.Add(viewer);
        root.ProcessFrame();
        Assert.Null(viewer.VerticalScrollBar.ComponentTemplateInstance);

        content.Resize(new LayoutSize(20, 200));
        root.ProcessFrame();

        ScrollViewer reference = new()
        {
            Width = 120,
            Height = 60,
            Content = new FixedElement(new LayoutSize(20, 200)),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible
        };
        UIRoot referenceRoot = new(300, 200);
        referenceRoot.VisualChildren.Add(reference);
        referenceRoot.ProcessFrame();
        ScrollBar bar = viewer.VerticalScrollBar;
        ScrollBar referenceBar = reference.VerticalScrollBar;
        Assert.Equal(Visibility.Visible, bar.Visibility);
        Assert.NotNull(bar.ComponentTemplateInstance);
        Assert.Equal(referenceBar.ArrangedBounds, bar.ArrangedBounds);
        Assert.Equal(referenceBar.Track.ArrangedBounds, bar.Track.ArrangedBounds);
        Assert.Equal(referenceBar.Track.Thumb.ArrangedBounds, bar.Track.Thumb.ArrangedBounds);
        Assert.True(bar.Track.Thumb.ArrangedBounds.Height > 0);
        Assert.Equal(referenceBar.Maximum, bar.Track.Maximum);
        Assert.Equal(referenceBar.ViewportSize, bar.Track.ViewportSize);
    }

    [Fact]
    public void DefaultTemplateRendersOwnerBackgroundAndBorderBrush()
    {
        SolidColorBrush background = new(Color.FromArgb(0xFF, 1, 2, 3));
        SolidColorBrush borderBrush = new(Color.FromArgb(0xFF, 4, 5, 6));
        ScrollViewer viewer = new()
        {
            Background = background,
            BorderBrush = borderBrush,
            BorderThickness = new Thickness(1),
            Content = new FixedElement(new LayoutSize(20, 20))
        };
        UIRoot root = new(100, 100);
        root.VisualChildren.Add(viewer);
        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Contains(commands, command => command.Kind == DrawCommandKind.FillRectangle && ReferenceEquals(command.Brush, background));
        Assert.Contains(commands, command => command.Kind == DrawCommandKind.DrawRectangle && ReferenceEquals(command.Brush, borderBrush));
    }

    [Fact]
    public void DefaultTemplateInsetsPartsByOwnerBorderThickness()
    {
        ScrollViewer viewer = new()
        {
            // Negative sides are the inward border: they take room from the template parts.
            BorderThickness = new Thickness(-1, -2, -3, -4),
            Content = new FixedElement(new LayoutSize(20, 20)),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible
        };

        viewer.Measure(new MeasureContext(new LayoutSize(100, 100)));
        viewer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        Assert.Equal(new LayoutRect(1, 2, 86, 94), viewer.Presenter.ArrangedBounds);
        Assert.Equal(new LayoutRect(87, 2, 10, 94), viewer.VerticalScrollBar.ArrangedBounds);
        Assert.Equal(Visibility.Collapsed, viewer.HorizontalScrollBar.Visibility);
    }

    [Fact]
    public void AutoScrollbarsReevaluateWhenOneScrollbarConsumesViewportSpace()
    {
        ScrollViewer viewer = new()
        {
            Content = new FixedElement(new LayoutSize(100, 110)),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        viewer.Measure(new MeasureContext(new LayoutSize(100, 100)));
        viewer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        Assert.True(viewer.IsVerticalScrollBarVisible);
        Assert.True(viewer.IsHorizontalScrollBarVisible);
    }

    [Fact]
    public void AutoScrollbarInspectorLayoutStaysWithinFourTreeMeasurePasses()
    {
        UIRoot root = new(400, 240);
        StackPanel content = new();
        for (int index = 0; index < 32; index++)
        {
            Cerneala.UI.Layout.Panels.Grid row = new();
            row.ColumnDefinitions.Add(new Cerneala.UI.Layout.Panels.ColumnDefinition(
                Cerneala.UI.Layout.Panels.GridLength.Auto));
            row.ColumnDefinitions.Add(new Cerneala.UI.Layout.Panels.ColumnDefinition(
                Cerneala.UI.Layout.Panels.GridLength.Star));
            FixedElement label = new(new LayoutSize(96, 24));
            FixedElement editor = new(new LayoutSize(220, 24));
            Cerneala.UI.Layout.Panels.Grid.SetColumn(editor, 1);
            row.VisualChildren.Add(label);
            row.VisualChildren.Add(editor);
            content.VisualChildren.Add(new Border
            {
                Child = row,
                Padding = new Thickness(8, 4, 8, 4)
            });
        }

        ScrollViewer viewer = new()
        {
            Content = content,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        root.VisualChildren.Add(viewer);

        FrameStats frame = root.ProcessFrame();
        int visualElementCount = Descendants(viewer).Count() + 1;
        int measureCallBudget = visualElementCount * 4;

        Assert.True(
            frame.MeasureCalls <= measureCallBudget,
            $"Expected at most {measureCallBudget} measure calls for {visualElementCount} elements, " +
            $"but layout performed {frame.MeasureCalls}.");
    }

    [Fact]
    public void AutoScrollbarsReevaluateAgainstArrangeViewport()
    {
        ScrollViewer viewer = new()
        {
            Content = new FixedElement(new LayoutSize(80, 120)),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        viewer.Measure(new MeasureContext(new LayoutSize(100, float.PositiveInfinity)));
        viewer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 80)));

        Assert.True(viewer.IsVerticalScrollBarVisible);
        Assert.Equal(new LayoutRect(0, 0, 90, 80), viewer.Presenter.ArrangedBounds);
        Assert.Equal(new LayoutRect(90, 0, 10, 80), viewer.VerticalScrollBar.ArrangedBounds);
    }

    [Fact]
    public void AutoScrollbarsCollapseWhenContentNoLongerRequiresScrolling()
    {
        ScrollViewer viewer = new()
        {
            Content = new FixedElement(new LayoutSize(200, 200)),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        viewer.Measure(new MeasureContext(new LayoutSize(100, 100)));
        viewer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));
        Assert.True(viewer.IsHorizontalScrollBarVisible);
        Assert.True(viewer.IsVerticalScrollBarVisible);

        viewer.Content = new FixedElement(new LayoutSize(20, 20));
        viewer.Measure(new MeasureContext(new LayoutSize(100, 100)));
        viewer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        Assert.False(viewer.IsHorizontalScrollBarVisible);
        Assert.False(viewer.IsVerticalScrollBarVisible);
        Assert.Equal(new LayoutRect(0, 0, 100, 100), viewer.Presenter.ArrangedBounds);
    }

    [Fact]
    public void AutoScrollbarsCollapseWhenExistingContentShrinks()
    {
        MutableElement content = new(new LayoutSize(300, 300));
        ScrollViewer viewer = new()
        {
            Content = content,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        viewer.Measure(new MeasureContext(new LayoutSize(100, 100)));
        viewer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));
        viewer.Presenter.SetHorizontalOffset(200);
        viewer.Presenter.SetVerticalOffset(200);

        content.Resize(new LayoutSize(40, 40));
        viewer.Measure(new MeasureContext(new LayoutSize(100, 100)));
        viewer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        Assert.Equal(40, viewer.ScrollInfo.ExtentWidth);
        Assert.Equal(40, viewer.ScrollInfo.ExtentHeight);
        Assert.Equal(0, viewer.Presenter.HorizontalOffset);
        Assert.Equal(0, viewer.Presenter.VerticalOffset);
        Assert.False(viewer.IsHorizontalScrollBarVisible);
        Assert.False(viewer.IsVerticalScrollBarVisible);
    }

    [Fact]
    public void AutoScrollbarsReevaluateWhenExistingPanelGrowsAfterInitialLayout()
    {
        UIRoot root = new(100, 100);
        StackPanel content = new();
        ScrollViewer viewer = new()
        {
            Content = content,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        root.VisualChildren.Add(viewer);
        root.ProcessFrame();
        Assert.False(viewer.IsVerticalScrollBarVisible);

        content.VisualChildren.Add(new FixedElement(new LayoutSize(80, 60)));
        content.VisualChildren.Add(new FixedElement(new LayoutSize(80, 60)));
        root.ProcessFrame();

        Assert.Equal(120, viewer.ScrollInfo.ExtentHeight);
        Assert.Equal(100, viewer.ScrollInfo.ViewportHeight);
        Assert.True(viewer.IsVerticalScrollBarVisible);
    }

    [Fact]
    public void AutoScrollbarRevealedAfterInitialLayoutIsArrangedInsideOwnerBorder()
    {
        UIRoot root = new(100, 100);
        StackPanel content = new();
        ScrollViewer viewer = new()
        {
            // A negative side is the inward border: the parts are arranged inside it.
            BorderThickness = new Thickness(-1),
            Content = content,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        root.VisualChildren.Add(viewer);
        root.ProcessFrame();
        Assert.False(viewer.IsVerticalScrollBarVisible);

        content.VisualChildren.Add(new FixedElement(new LayoutSize(80, 60)));
        content.VisualChildren.Add(new FixedElement(new LayoutSize(80, 60)));
        root.ProcessFrame();

        Assert.True(viewer.IsVerticalScrollBarVisible);
        Assert.Equal(new LayoutRect(1, 1, 88, 98), viewer.Presenter.ArrangedBounds);
        Assert.Equal(new LayoutRect(89, 1, 10, 98), viewer.VerticalScrollBar.ArrangedBounds);
    }

    [Fact]
    public void AutoScrollbarsCollapseDuringUnboundedMeasureWhenExistingContentShrinks()
    {
        MutableElement content = new(new LayoutSize(300, 300));
        ScrollViewer viewer = new()
        {
            Content = content,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        viewer.Measure(new MeasureContext(new LayoutSize(100, 100)));
        viewer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));
        Assert.True(viewer.IsHorizontalScrollBarVisible);
        Assert.True(viewer.IsVerticalScrollBarVisible);

        content.Resize(new LayoutSize(40, 40));
        viewer.Measure(new MeasureContext(new LayoutSize(float.PositiveInfinity, float.PositiveInfinity)));

        Assert.False(viewer.IsHorizontalScrollBarVisible);
        Assert.False(viewer.IsVerticalScrollBarVisible);
        Assert.Equal(new LayoutSize(40, 40), viewer.DesiredSize);
    }

    // As WPF's ScrollContentPresenter: the desired size on each axis is the smaller of the available size and the
    // extent, while the viewport stays the available size.
    [Fact]
    public void PresenterDesiresTheSmallerOfTheAvailableSizeAndTheExtent()
    {
        ScrollContentPresenter presenter = new()
        {
            Content = new FixedElement(new LayoutSize(80, 300))
        };

        presenter.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(new LayoutSize(80, 100), presenter.DesiredSize);
        Assert.Equal(100, presenter.ViewportWidth);
        Assert.Equal(100, presenter.ViewportHeight);
    }

    [Fact]
    public void ViewerWithSmallContentDesiresTheContentSize()
    {
        ScrollViewer viewer = new()
        {
            Content = new FixedElement(new LayoutSize(80, 20)),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        viewer.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(new LayoutSize(80, 20), viewer.DesiredSize);
        Assert.False(viewer.IsHorizontalScrollBarVisible);
        Assert.False(viewer.IsVerticalScrollBarVisible);
    }

    // The 300 px tall content overflows the 100 px viewport, so the vertical bar shows as before and the viewer desires
    // the 100 px height it has, plus the 10 px bar next to the 80 px wide content.
    [Fact]
    public void ViewerWithTallContentDesiresTheAvailableHeightAndShowsTheVerticalBar()
    {
        ScrollViewer viewer = new()
        {
            Content = new FixedElement(new LayoutSize(80, 300)),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        viewer.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(new LayoutSize(90, 100), viewer.DesiredSize);
        Assert.True(viewer.IsVerticalScrollBarVisible);
        Assert.Equal(300, viewer.ScrollInfo.ExtentHeight);
        Assert.Equal(100, viewer.ScrollInfo.ViewportHeight);

        viewer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        Assert.Equal(new LayoutRect(0, 0, 90, 100), viewer.Presenter.ArrangedBounds);
        Assert.Equal(90, viewer.ScrollInfo.ViewportWidth);
        Assert.Equal(100, viewer.ScrollInfo.ViewportHeight);
    }

    [Fact]
    public void ScrollInfoOffsetUpdatesScrollBarValues()
    {
        ScrollViewer viewer = new()
        {
            Content = new FixedElement(new LayoutSize(200, 300)),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        viewer.Measure(new MeasureContext(new LayoutSize(100, 100)));
        viewer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));

        viewer.ScrollInfo.SetHorizontalOffset(40);
        viewer.ScrollInfo.SetVerticalOffset(60);

        Assert.Equal(40, viewer.HorizontalScrollBar.Value);
        Assert.Equal(60, viewer.VerticalScrollBar.Value);
    }

    [Fact]
    public void OffsetChangeInvalidatesArrangeRenderAndHitTestWithoutMeasure()
    {
        UIRoot root = new(100, 100);
        ScrollContentPresenter presenter = new()
        {
            Content = new FixedElement(new LayoutSize(200, 200))
        };
        root.VisualChildren.Add(presenter);
        root.ProcessFrame();
        presenter.Measure(new MeasureContext(new LayoutSize(50, 50)));
        presenter.Arrange(new ArrangeContext(new LayoutRect(0, 0, 50, 50)));

        presenter.SetVerticalOffset(10);

        Assert.DoesNotContain(presenter, root.LayoutQueue.SnapshotMeasure());
        Assert.Contains(presenter, root.LayoutQueue.SnapshotArrange());
        Assert.Contains(presenter, root.RenderQueue.Snapshot());
        Assert.Contains(presenter, root.HitTestQueue.Snapshot());
    }

    [Fact]
    public void UnchangedScrollViewerFrameDoesNotRetainLateArrangeWork()
    {
        UIRoot root = new(100, 100);
        ScrollViewer viewer = new()
        {
            Content = new FixedElement(new LayoutSize(80, 300)),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        root.VisualChildren.Add(viewer);
        root.ProcessFrame();

        FrameStats second = root.ProcessFrame();

        Assert.Equal(0, second.MeasuredElements);
        Assert.Equal(0, second.ArrangedElements);
        Assert.Equal(0, second.RenderedElements);
        Assert.Equal(1, second.NoWorkFrames);
    }

    [Theory]
    [InlineData(0f, 1f)]
    [InlineData(0.5f, 2f)]
    [InlineData(0.4f, 1f)]
    [InlineData(0.3f, 1.5f)]
    public void StretchedViewerWithFractionalMarginSettlesAfterLayout(float marginTop, float scale)
    {
        // A top margin of 0.4 at scale 1 leaves 99.6 pixels, which rounds to 100 when the viewer is arranged.
        // Measure must see the same 100, or the viewport flips between 99.6 and 100 on every frame.
        UIRoot root = new(100, 100, scale);
        ScrollViewer viewer = new()
        {
            Margin = new Thickness(0, marginTop, 0, 0),
            Content = new FixedElement(new LayoutSize(80, 100)),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        root.VisualChildren.Add(viewer);
        root.ProcessFrame();

        FrameStats second = root.ProcessFrame();

        Assert.Equal(0, second.MeasuredElements);
        Assert.Equal(0, second.ArrangedElements);
        Assert.False(root.Scheduler.HasWork);
    }

    [Theory]
    [InlineData(99.4f, 0f, 1f, 100f)]
    [InlineData(49.7f, 0f, 2f, 50f)]
    [InlineData(99.4f, 0.3f, 1f, 100f)]
    public void ViewerWithFractionalExplicitHeightSettlesAfterLayout(float height, float marginTop, float scale, float contentHeight)
    {
        // A Height of 99.4 at scale 1 rounds to 99. Arrange must give the viewer the same 99 that measure used,
        // or the 100 pixel content needs the Auto bar in measure but not in arrange, on every frame.
        UIRoot root = new(100, 100, scale);
        ScrollViewer viewer = new()
        {
            Height = height,
            Margin = new Thickness(0, marginTop, 0, 0),
            Content = new FixedElement(new LayoutSize(80, contentHeight)),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        root.VisualChildren.Add(viewer);
        root.ProcessFrame();

        FrameStats second = root.ProcessFrame();

        Assert.Equal(0, second.MeasuredElements);
        Assert.Equal(0, second.ArrangedElements);
        Assert.False(root.Scheduler.HasWork);
    }

    [Fact]
    public void ViewerInAFractionalStarColumnSettlesAfterLayout()
    {
        // The first of three * columns in 100 px is 34 px wide once rounded. Measured at 33 px, the 34 px content
        // would need the Auto bar in measure but not in arrange, so the bar state would flip on every frame.
        UIRoot root = new(100, 100, 1);
        Cerneala.UI.Layout.Panels.Grid host = new();
        for (int i = 0; i < 3; i++)
        {
            host.ColumnDefinitions.Add(new Cerneala.UI.Layout.Panels.ColumnDefinition(
                Cerneala.UI.Layout.Panels.GridLength.Stars(1)));
        }

        ScrollViewer viewer = new()
        {
            Content = new FixedElement(new LayoutSize(34, 50)),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        host.VisualChildren.Add(viewer);
        root.VisualChildren.Add(host);
        root.ProcessFrame();

        FrameStats second = root.ProcessFrame();

        Assert.Equal(0, second.MeasuredElements);
        Assert.Equal(0, second.ArrangedElements);
        Assert.False(root.Scheduler.HasWork);
        Assert.Equal(new LayoutRect(0, 0, 34, 100), viewer.ArrangedBounds);
    }

    [Fact]
    public void ScrollBarConvergenceDoesNotConsumeExternalAncestorArrangeWork()
    {
        UIRoot root = new(200, 100);
        Cerneala.UI.Layout.Panels.Grid host = new();
        host.ColumnDefinitions.Add(new Cerneala.UI.Layout.Panels.ColumnDefinition(
            Cerneala.UI.Layout.Panels.GridLength.Pixels(50)));
        host.ColumnDefinitions.Add(new Cerneala.UI.Layout.Panels.ColumnDefinition(
            Cerneala.UI.Layout.Panels.GridLength.Star));
        ScrollViewer viewer = new()
        {
            Content = new FixedElement(new LayoutSize(100, 300)),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Visibility = Visibility.Collapsed
        };
        Cerneala.UI.Layout.Panels.Grid.SetColumn(viewer, 1);
        root.VisualChildren.Add(host);
        host.VisualChildren.Add(viewer);
        root.ProcessFrame();

        viewer.Visibility = Visibility.Visible;
        root.ProcessFrame();

        Assert.Equal(new LayoutRect(50, 0, 150, 100), viewer.ArrangedBounds);
        Assert.True(viewer.IsVerticalScrollBarVisible);
    }

    [Fact]
    public void AutoVerticalScrollBarWithoutOverflowDoesNotRetainLateLayoutWork()
    {
        UIRoot root = new(100, 100);
        ScrollViewer viewer = new()
        {
            Content = new FixedElement(new LayoutSize(40, 40)),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        root.VisualChildren.Add(viewer);
        root.ProcessFrame();

        FrameStats second = root.ProcessFrame();

        Assert.False(viewer.IsVerticalScrollBarVisible);
        Assert.Equal(0, second.MeasuredElements);
        Assert.Equal(0, second.ArrangedElements);
        Assert.Equal(0, second.RenderedElements);
        Assert.Equal(1, second.NoWorkFrames);
    }

    [Fact]
    public void HoldingDirectionButtonScrollsViewerByVisibleLineSteps()
    {
        UIRoot root = new(100, 100);
        ScrollViewer viewer = new()
        {
            Content = new FixedElement(new LayoutSize(80, 400)),
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible
        };
        root.VisualChildren.Add(viewer);
        viewer.Measure(new MeasureContext(new LayoutSize(100, 100)));
        viewer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));
        RepeatButton increase = Assert.IsType<RepeatButton>(
            viewer.VerticalScrollBar.ComponentTemplateInstance!.Parts["PART_IncreaseButton"]);
        float x = increase.ArrangedBounds.X + (increase.ArrangedBounds.Width / 2);
        float y = increase.ArrangedBounds.Y + (increase.ArrangedBounds.Height / 2);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true), TimeSpan.Zero);

        Assert.Equal(48, viewer.Presenter.VerticalOffset);

        bridge.Dispatch(
            root,
            PointerFrame(x, y, x, y, previousDown: true, currentDown: true),
            TimeSpan.FromMilliseconds(increase.Delay));

        Assert.Equal(96, viewer.Presenter.VerticalOffset);
    }

    [Fact]
    public void DetachingViewerCancelsDirectionButtonRepeat()
    {
        UIRoot root = new(100, 100);
        ScrollViewer viewer = new()
        {
            Content = new FixedElement(new LayoutSize(80, 400)),
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible
        };
        root.VisualChildren.Add(viewer);
        viewer.Measure(new MeasureContext(new LayoutSize(100, 100)));
        viewer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));
        viewer.Presenter.SetVerticalOffset(100);
        RepeatButton decrease = Assert.IsType<RepeatButton>(
            viewer.VerticalScrollBar.ComponentTemplateInstance!.Parts["PART_DecreaseButton"]);
        float x = decrease.ArrangedBounds.X + (decrease.ArrangedBounds.Width / 2);
        float y = decrease.ArrangedBounds.Y + (decrease.ArrangedBounds.Height / 2);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true), TimeSpan.Zero);
        float valueAfterPress = viewer.VerticalScrollBar.Value;
        Assert.True(decrease.IsPressed);
        Assert.True(valueAfterPress < 100);
        root.VisualChildren.Remove(viewer);
        bridge.Dispatch(
            root,
            PointerFrame(x, y, x, y, previousDown: true, currentDown: true),
            TimeSpan.FromMilliseconds(decrease.Delay));

        Assert.Equal(valueAfterPress, viewer.VerticalScrollBar.Value);
        Assert.False(decrease.IsPressed);
    }

    [Fact]
    public void ChangingContentDuringDirectionButtonRepeatKeepsActivePartsSynchronized()
    {
        UIRoot root = new(100, 100);
        ScrollViewer viewer = new()
        {
            Content = new FixedElement(new LayoutSize(80, 400)),
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible
        };
        root.VisualChildren.Add(viewer);
        viewer.Measure(new MeasureContext(new LayoutSize(100, 100)));
        viewer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));
        viewer.Presenter.SetVerticalOffset(100);
        RepeatButton decrease = Assert.IsType<RepeatButton>(
            viewer.VerticalScrollBar.ComponentTemplateInstance!.Parts["PART_DecreaseButton"]);
        float x = decrease.ArrangedBounds.X + (decrease.ArrangedBounds.Width / 2);
        float y = decrease.ArrangedBounds.Y + (decrease.ArrangedBounds.Height / 2);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true), TimeSpan.Zero);
        float valueAfterPress = viewer.VerticalScrollBar.Value;
        UIElement replacement = new FixedElement(new LayoutSize(80, 600));
        viewer.Content = replacement;
        viewer.Measure(new MeasureContext(new LayoutSize(100, 100)));
        viewer.Arrange(new ArrangeContext(new LayoutRect(0, 0, 100, 100)));
        bridge.Dispatch(
            root,
            PointerFrame(x, y, x, y, previousDown: true, currentDown: true),
            TimeSpan.FromMilliseconds(decrease.Delay));

        Assert.Same(replacement, viewer.Presenter.Content);
        Assert.Equal(valueAfterPress - viewer.VerticalScrollBar.SmallChange, viewer.VerticalScrollBar.Value);
    }

    [Fact]
    public void VerticalTrackClickPagesByViewportHeight()
    {
        UIRoot root = new(200, 120);
        ScrollViewer viewer = new()
        {
            Width = 160,
            Height = 100,
            Content = new FixedElement(new LayoutSize(120, 1000))
        };
        root.VisualChildren.Add(viewer);
        root.ProcessFrame();
        ElementInputBridge bridge = new();

        ClickTrackAfterThumb(root, bridge, viewer.VerticalScrollBar.Track);

        Assert.Equal(100, viewer.Presenter.VerticalOffset);

        ClickTrackBeforeThumb(root, bridge, viewer.VerticalScrollBar.Track);

        Assert.Equal(0, viewer.Presenter.VerticalOffset);
    }

    [Fact]
    public void HorizontalTrackClickPagesByViewportWidth()
    {
        UIRoot root = new(200, 120);
        ScrollViewer viewer = new()
        {
            Width = 160,
            Height = 100,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = new FixedElement(new LayoutSize(1000, 60))
        };
        root.VisualChildren.Add(viewer);
        root.ProcessFrame();
        ElementInputBridge bridge = new();
        Assert.False(viewer.IsVerticalScrollBarVisible);

        ClickTrackAfterThumb(root, bridge, viewer.HorizontalScrollBar.Track);

        Assert.Equal(160, viewer.Presenter.HorizontalOffset);

        ClickTrackBeforeThumb(root, bridge, viewer.HorizontalScrollBar.Track);

        Assert.Equal(0, viewer.Presenter.HorizontalOffset);
    }

    [Fact]
    public void TrackClickPageFollowsViewportResize()
    {
        UIRoot root = new(200, 120);
        ScrollViewer viewer = new()
        {
            Width = 160,
            Height = 100,
            Content = new FixedElement(new LayoutSize(120, 1000))
        };
        root.VisualChildren.Add(viewer);
        root.ProcessFrame();
        ElementInputBridge bridge = new();

        viewer.Height = 60;
        root.ProcessFrame();
        ClickTrackAfterThumb(root, bridge, viewer.VerticalScrollBar.Track);

        Assert.Equal(60, viewer.Presenter.ViewportHeight);
        Assert.Equal(60, viewer.Presenter.VerticalOffset);
    }

    [Fact]
    public void ViewerCanReattachToAnotherRootAfterTemplateLayout()
    {
        UIRoot first = new(100, 100);
        UIRoot second = new(120, 80);
        ScrollViewer viewer = new()
        {
            Content = new FixedElement(new LayoutSize(200, 300)),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        first.VisualChildren.Add(viewer);
        first.ProcessFrame();

        first.VisualChildren.Remove(viewer);
        second.VisualChildren.Add(viewer);
        second.ProcessFrame();

        Assert.Same(second, viewer.Root);
        Assert.Same(second, viewer.Presenter.Root);
        Assert.Equal(new LayoutRect(0, 0, 110, 80), viewer.Presenter.ArrangedBounds);
    }

    // As in WPF, where the RepeatButtons and the Thumb of the default ScrollBar template are not focusable, Tab from a
    // button before the ScrollViewer does not stop on a part of its vertical scroll bar (#700).
    [Fact]
    public void TabDoesNotStopOnThePartsOfTheVerticalScrollBar()
    {
        (UIRoot root, Button before, ScrollViewer viewer) = CreateFocusViewer(new LayoutSize(80, 300), horizontal: false);
        Assert.True(viewer.IsVerticalScrollBarVisible);
        ElementInputBridge bridge = new();
        ClickCenter(root, bridge, before);
        Assert.True(before.IsKeyboardFocused);

        for (int press = 0; press < 3; press++)
        {
            PressKey(root, bridge, InputKey.Tab);

            UIElement focused = Assert.IsAssignableFrom<UIElement>(root.ActiveFocusManager?.FocusedElement);
            Assert.False(
                IsInsideScrollBar(focused),
                $"Tab press {press + 1} focused {focused.GetType().Name} in a scroll bar.");
        }
    }

    // Tab does not stop on the parts of the horizontal bar either (#700).
    [Fact]
    public void TabDoesNotStopOnThePartsOfTheHorizontalScrollBar()
    {
        (UIRoot root, Button before, ScrollViewer viewer) = CreateFocusViewer(new LayoutSize(600, 20), horizontal: true);
        Assert.True(viewer.IsHorizontalScrollBarVisible);
        ElementInputBridge bridge = new();
        ClickCenter(root, bridge, before);
        Assert.True(before.IsKeyboardFocused);

        for (int press = 0; press < 3; press++)
        {
            PressKey(root, bridge, InputKey.Tab);

            UIElement focused = Assert.IsAssignableFrom<UIElement>(root.ActiveFocusManager?.FocusedElement);
            Assert.False(
                IsInsideScrollBar(focused),
                $"Tab press {press + 1} focused {focused.GetType().Name} in a scroll bar.");
        }
    }

    // A click on the increase button of the vertical bar scrolls and leaves the keyboard focus on the focused button,
    // as in WPF where the RepeatButtons of the ScrollBar are not focusable (#700).
    [Fact]
    public void ClickOnTheVerticalIncreaseButtonKeepsTheKeyboardFocus()
    {
        (UIRoot root, Button before, ScrollViewer viewer) = CreateFocusViewer(new LayoutSize(80, 300), horizontal: false);
        ElementInputBridge bridge = new();
        ClickCenter(root, bridge, before);
        Assert.True(before.IsKeyboardFocused);
        RepeatButton increase = Assert.IsType<RepeatButton>(
            viewer.VerticalScrollBar.ComponentTemplateInstance!.Parts["PART_IncreaseButton"]);

        ClickCenter(root, bridge, increase);

        Assert.Equal(48, viewer.Presenter.VerticalOffset);
        Assert.True(before.IsKeyboardFocused);
    }

    // The same for the decrease button of the vertical bar (#700).
    [Fact]
    public void ClickOnTheVerticalDecreaseButtonKeepsTheKeyboardFocus()
    {
        (UIRoot root, Button before, ScrollViewer viewer) = CreateFocusViewer(new LayoutSize(80, 300), horizontal: false);
        ElementInputBridge bridge = new();
        viewer.Presenter.SetVerticalOffset(100);
        root.ProcessFrame();
        ClickCenter(root, bridge, before);
        Assert.True(before.IsKeyboardFocused);
        RepeatButton decrease = Assert.IsType<RepeatButton>(
            viewer.VerticalScrollBar.ComponentTemplateInstance!.Parts["PART_DecreaseButton"]);

        ClickCenter(root, bridge, decrease);

        Assert.Equal(52, viewer.Presenter.VerticalOffset);
        Assert.True(before.IsKeyboardFocused);
    }

    // The same for the increase button of the horizontal bar (#700).
    [Fact]
    public void ClickOnTheHorizontalIncreaseButtonKeepsTheKeyboardFocus()
    {
        (UIRoot root, Button before, ScrollViewer viewer) = CreateFocusViewer(new LayoutSize(600, 20), horizontal: true);
        Assert.True(viewer.IsHorizontalScrollBarVisible);
        ElementInputBridge bridge = new();
        ClickCenter(root, bridge, before);
        Assert.True(before.IsKeyboardFocused);
        RepeatButton increase = Assert.IsType<RepeatButton>(
            viewer.HorizontalScrollBar.ComponentTemplateInstance!.Parts["PART_IncreaseButton"]);

        ClickCenter(root, bridge, increase);

        Assert.Equal(48, viewer.Presenter.HorizontalOffset);
        Assert.True(before.IsKeyboardFocused);
    }

    // Guards the same contract for the page area of the Track, which is not a button: a click on it pages and leaves the
    // keyboard focus (#700).
    [Fact]
    public void ClickOnTheTrackPageAreaKeepsTheKeyboardFocus()
    {
        (UIRoot root, Button before, ScrollViewer viewer) = CreateFocusViewer(new LayoutSize(80, 300), horizontal: false);
        ElementInputBridge bridge = new();
        ClickCenter(root, bridge, before);
        Assert.True(before.IsKeyboardFocused);

        ClickTrackAfterThumb(root, bridge, viewer.VerticalScrollBar.Track);

        Assert.True(viewer.Presenter.VerticalOffset > 0);
        Assert.True(before.IsKeyboardFocused);
    }

    // Guards the same contract for the Thumb: a drag of it scrolls and leaves the keyboard focus (#700).
    [Fact]
    public void DragOfTheThumbKeepsTheKeyboardFocus()
    {
        (UIRoot root, Button before, ScrollViewer viewer) = CreateFocusViewer(new LayoutSize(80, 300), horizontal: false);
        ElementInputBridge bridge = new();
        ClickCenter(root, bridge, before);
        Assert.True(before.IsKeyboardFocused);
        LayoutRect thumb = viewer.VerticalScrollBar.Track.Thumb.ArrangedBounds;
        float x = thumb.X + (thumb.Width / 2);
        float y = thumb.Y + (thumb.Height / 2);

        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        root.ProcessFrame();
        bridge.Dispatch(root, PointerFrame(x, y, x, y + 5, previousDown: true, currentDown: true));
        root.ProcessFrame();
        bridge.Dispatch(root, PointerFrame(x, y + 5, x, y + 5, previousDown: true));
        root.ProcessFrame();

        Assert.True(viewer.Presenter.VerticalOffset > 0);
        Assert.True(before.IsKeyboardFocused);
    }

    // A button before a 150 by 60 ScrollViewer in a window; the content decides which bar is visible.
    private static (UIRoot Root, Button Before, ScrollViewer Viewer) CreateFocusViewer(LayoutSize content, bool horizontal)
    {
        Button before = new() { Content = "Before" };
        ScrollViewer viewer = new()
        {
            Width = 150,
            Height = 60,
            Content = new FixedElement(content),
            VerticalScrollBarVisibility = horizontal ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = horizontal ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled
        };
        Cerneala.UI.Layout.Panels.StackPanel panel = new();
        panel.VisualChildren.Add(before);
        panel.VisualChildren.Add(viewer);
        UIRoot root = new(360, 300);
        root.VisualChildren.Add(new Window { Content = panel, Width = 360, Height = 300 });
        root.ProcessFrame();
        return (root, before, viewer);
    }

    private static void ClickCenter(UIRoot root, ElementInputBridge bridge, UIElement element)
    {
        LayoutRect bounds = element.ArrangedBounds;
        float x = bounds.X + (bounds.Width / 2);
        float y = bounds.Y + (bounds.Height / 2);
        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        root.ProcessFrame();
        bridge.Dispatch(root, PointerFrame(x, y, x, y, previousDown: true));
        root.ProcessFrame();
    }

    private static void PressKey(UIRoot root, ElementInputBridge bridge, InputKey key)
    {
        bridge.Dispatch(root, KeyFrame([], [key]));
        bridge.Dispatch(root, KeyFrame([key], []));
        root.ProcessFrame();
    }

    private static InputFrame KeyFrame(InputKey[] previousKeys, InputKey[] currentKeys)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.FromDownKeys(previousKeys),
            KeyboardSnapshot.FromDownKeys(currentKeys),
            []);
    }

    private static bool IsInsideScrollBar(UIElement element)
    {
        for (UIElement? current = element; current is not null; current = current.VisualParent)
        {
            if (current is ScrollBar)
            {
                return true;
            }
        }

        return false;
    }

    private static void ClickTrackAfterThumb(UIRoot root, ElementInputBridge bridge, Track track)
    {
        LayoutRect thumb = track.Thumb.ArrangedBounds;
        LayoutRect bounds = track.ArrangedBounds;
        if (track.Orientation == Orientation.Horizontal)
        {
            float thumbEnd = thumb.X + thumb.Width;
            ClickAt(root, bridge, (thumbEnd + bounds.X + bounds.Width) / 2, bounds.Y + (bounds.Height / 2));
        }
        else
        {
            float thumbEnd = thumb.Y + thumb.Height;
            ClickAt(root, bridge, bounds.X + (bounds.Width / 2), (thumbEnd + bounds.Y + bounds.Height) / 2);
        }
    }

    private static void ClickTrackBeforeThumb(UIRoot root, ElementInputBridge bridge, Track track)
    {
        LayoutRect thumb = track.Thumb.ArrangedBounds;
        LayoutRect bounds = track.ArrangedBounds;
        if (track.Orientation == Orientation.Horizontal)
        {
            ClickAt(root, bridge, (bounds.X + thumb.X) / 2, bounds.Y + (bounds.Height / 2));
        }
        else
        {
            ClickAt(root, bridge, bounds.X + (bounds.Width / 2), (bounds.Y + thumb.Y) / 2);
        }
    }

    private static void ClickAt(UIRoot root, ElementInputBridge bridge, float x, float y)
    {
        bridge.Dispatch(root, PointerFrame(x, y, currentDown: true));
        bridge.Dispatch(root, PointerFrame(x, y, x, y, previousDown: true));
        root.ProcessFrame();
    }

    private static InputFrame PointerWheelFrame(float x, float y, int delta)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(x, y).WithWheelValue(delta);
        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private static InputFrame PointerFrame(
        float previousX,
        float previousY,
        float currentX,
        float currentY,
        bool previousDown = false,
        bool currentDown = false)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(previousX, previousY);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(currentX, currentY);
        if (previousDown)
        {
            previous = previous.WithButton(InputMouseButton.Left, true);
        }

        if (currentDown)
        {
            current = current.WithButton(InputMouseButton.Left, true);
        }

        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private static InputFrame PointerFrame(float x, float y, bool currentDown = false)
    {
        return PointerFrame(x, y, x, y, currentDown: currentDown);
    }

    private static ComponentTemplate<ScrollViewer> ViewerTemplate(
        string name,
        ScrollContentPresenter presenter,
        ScrollBar horizontal,
        ScrollBar vertical)
    {
        return new ComponentTemplate<ScrollViewer>(name, context =>
        {
            Cerneala.UI.Layout.Panels.Grid root = new();
            root.ColumnDefinitions.Add(new Cerneala.UI.Layout.Panels.ColumnDefinition(Cerneala.UI.Layout.Panels.GridLength.Star));
            root.ColumnDefinitions.Add(new Cerneala.UI.Layout.Panels.ColumnDefinition(Cerneala.UI.Layout.Panels.GridLength.Auto));
            root.RowDefinitions.Add(new Cerneala.UI.Layout.Panels.RowDefinition(Cerneala.UI.Layout.Panels.GridLength.Star));
            root.RowDefinitions.Add(new Cerneala.UI.Layout.Panels.RowDefinition(Cerneala.UI.Layout.Panels.GridLength.Auto));
            Cerneala.UI.Layout.Panels.Grid.SetColumn(vertical, 1);
            Cerneala.UI.Layout.Panels.Grid.SetRow(horizontal, 1);
            root.VisualChildren.Add(presenter);
            root.VisualChildren.Add(vertical);
            root.VisualChildren.Add(horizontal);
            context.RequirePart("PART_ScrollContentPresenter", presenter);
            context.RequirePart("PART_HorizontalScrollBar", horizontal);
            context.RequirePart("PART_VerticalScrollBar", vertical);
            return root;
        });
    }

    private static IEnumerable<UIElement> Descendants(UIElement element)
    {
        foreach (UIElement child in element.VisualChildren)
        {
            yield return child;
            foreach (UIElement descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }

    private sealed class MutableElement(LayoutSize size) : UIElement
    {
        private LayoutSize size = size;

        public void Resize(LayoutSize newSize)
        {
            size = newSize;
            Invalidate(InvalidationFlags.Measure | InvalidationFlags.Render, "Test content resized");
        }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }
}

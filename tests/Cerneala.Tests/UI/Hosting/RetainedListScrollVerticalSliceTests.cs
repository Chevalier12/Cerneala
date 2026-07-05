using Cerneala.Playground.Samples;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.UI.Hosting;

public sealed class RetainedListScrollVerticalSliceTests
{
    [Fact]
    public void ScrollViewerWheelOffsetInvalidatesArrangeRenderHitTestWithoutMeasure()
    {
        UIRoot root = new(100, 100);
        ScrollViewer viewer = new()
        {
            Content = new FixedElement(new LayoutSize(80, 300)),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        root.VisualChildren.Add(viewer);
        root.ProcessFrame();
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerWheelFrame(10, 10, -120));
        FrameStats stats = root.ProcessFrame();

        Assert.Equal(48, viewer.Presenter.VerticalOffset);
        Assert.Equal(0, stats.MeasureCalls);
        Assert.Equal(0, stats.MeasuredElements);
        Assert.True(stats.ArrangedElements > 0);
        Assert.True(stats.RenderedElements > 0);
        Assert.True(stats.HitTestElements > 0);
    }

    [Fact]
    public void RetainedAppSampleUsesItemsControlOrListBoxForListSection()
    {
        RetainedAppSample sample = new();

        UIElement root = sample.Build();

        Assert.Contains(Walk(root), element => element is ListBox or ItemsControl);
    }

    [Fact]
    public void RetainedListScrollSecondUnchangedFrameDoesNoRetainedWork()
    {
        UIRoot root = new(120, 80);
        ListBox listBox = new();
        listBox.SetItems(Enumerable.Range(0, 16).Select(index => $"Row {index}"));
        ScrollViewer viewer = new()
        {
            Content = listBox,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        root.VisualChildren.Add(viewer);
        UiHost host = new(new UiHostOptions { Root = root });

        host.Update(InputFrame(), new UiViewport(120, 80), TimeSpan.Zero);
        UiFrame second = host.Update(InputFrame(), new UiViewport(120, 80), TimeSpan.Zero);

        Assert.Equal(0, second.Stats.MeasuredElements);
        Assert.Equal(0, second.Stats.ArrangedElements);
        Assert.Equal(0, second.Stats.RenderedElements);
        Assert.Equal(0, second.Stats.HitTestElements);
        Assert.Equal(1, second.Stats.NoWorkFrames);
    }

    private static IEnumerable<UIElement> Walk(UIElement element)
    {
        yield return element;
        foreach (UIElement child in element.VisualChildren)
        {
            foreach (UIElement descendant in Walk(child))
            {
                yield return descendant;
            }
        }
    }

    private static InputFrame PointerWheelFrame(float x, float y, int delta)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(x, y).WithWheelValue(delta);
        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private static InputFrame InputFrame()
    {
        return new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }
}

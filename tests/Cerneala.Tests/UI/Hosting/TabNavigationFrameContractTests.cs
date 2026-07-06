using Cerneala.Drawing;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Hosting;

public sealed class TabNavigationFrameContractTests
{
    [Fact]
    public void TabFocusChangeInvalidatesRenderStyleAndSemanticsWithoutMeasure()
    {
        UiHost host = HostWithTabStops(out UIRoot root, out Button first, out _);
        host.Update(EmptyFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        SemanticsTree before = root.GetSemanticsTree();

        UiFrame tabFrame = host.Update(KeyPressFrame(InputKey.Tab), new UiViewport(100, 100), TimeSpan.Zero);
        SemanticsTree after = root.GetSemanticsTree();

        Assert.Same(first, host.InputBridge.FocusManager.FocusedElement);
        Assert.True(tabFrame.Stats.RenderedElements > 0);
        Assert.True(tabFrame.Stats.StyledElements > 0);
        Assert.NotSame(before, after);
        Assert.Equal(0, tabFrame.Stats.MeasuredElements);
        Assert.Equal(0, tabFrame.Stats.ArrangedElements);
    }

    [Fact]
    public void SecondUnchangedFrameAfterTabNavigationDoesNoRetainedWork()
    {
        UiHost host = HostWithTabStops(out _, out _, out _);
        host.Update(EmptyFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        host.Update(KeyPressFrame(InputKey.Tab), new UiViewport(100, 100), TimeSpan.Zero);

        UiFrame second = host.Update(EmptyFrame(), new UiViewport(100, 100), TimeSpan.Zero);

        Assert.Equal(0, second.Stats.MeasuredElements);
        Assert.Equal(0, second.Stats.ArrangedElements);
        Assert.Equal(0, second.Stats.RenderedElements);
        Assert.Equal(0, second.Stats.HitTestElements);
        Assert.Equal(1, second.Stats.NoWorkFrames);
    }

    [Fact]
    public void TabNavigationUsesPreInputCommittedHitTestAndRouteMap()
    {
        UIRoot root = new();
        UiHost host = new(new UiHostOptions { Root = root });
        host.Update(EmptyFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        Button addedBeforeInput = Arranged(new Button());
        root.VisualChildren.Add(addedBeforeInput);

        host.Update(KeyPressFrame(InputKey.Tab), new UiViewport(100, 100), TimeSpan.Zero);

        Assert.Same(addedBeforeInput, host.InputBridge.FocusManager.FocusedElement);
    }

    private static UiHost HostWithTabStops(out UIRoot root, out Button first, out Button second)
    {
        root = new UIRoot(100, 100);
        RenderCountingPanel panel = Arranged(new RenderCountingPanel());
        first = Arranged(new Button { Content = "First" });
        second = Arranged(new Button { Content = "Second" });
        panel.VisualChildren.Add(first);
        panel.VisualChildren.Add(second);
        root.VisualChildren.Add(panel);
        return new UiHost(new UiHostOptions { Root = root });
    }

    private static TElement Arranged<TElement>(TElement element)
        where TElement : UIElement
    {
        element.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 40)));
        return element;
    }

    private static InputFrame EmptyFrame()
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.Empty,
            []);
    }

    private static InputFrame KeyPressFrame(params InputKey[] currentKeys)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.Empty,
            KeyboardSnapshot.FromDownKeys(currentKeys),
            []);
    }

    private sealed class RenderCountingPanel : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            foreach (UIElement child in VisualChildren)
            {
                child.Measure(context);
            }

            return new LayoutSize(80, 40);
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            float x = context.FinalRect.X;
            foreach (UIElement child in VisualChildren)
            {
                child.Arrange(new ArrangeContext(new LayoutRect(x, context.FinalRect.Y, 40, 40), context.Rounding));
                x += 40;
            }

            return context.FinalRect;
        }

        protected override void OnRender(RenderContext context)
        {
            context.DrawingContext.FillRectangle(new DrawRect(context.Bounds.X, context.Bounds.Y, 1, 1), DrawColor.White);
        }
    }
}

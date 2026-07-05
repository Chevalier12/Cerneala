using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Hosting;

public sealed class UiHostViewportFrameContractTests
{
    [Fact]
    public void ViewportChangeInvalidatesMeasureArrangeRenderAndHitTest()
    {
        UiHost host = HostWithWidthSensitiveRoot(out _, out _);
        host.Update(EmptyFrame(), new UiViewport(100, 100), TimeSpan.Zero);

        UiFrame resized = host.Update(EmptyFrame(), new UiViewport(200, 120), TimeSpan.Zero);

        Assert.True(resized.Stats.MeasuredElements > 0);
        Assert.True(resized.Stats.ArrangedElements > 0);
        Assert.True(resized.Stats.RenderedElements > 0);
        Assert.True(resized.Stats.HitTestElements > 0);
    }

    [Fact]
    public void ViewportChangeRemeasuresWidthSensitiveElement()
    {
        UiHost host = HostWithWidthSensitiveRoot(out _, out WidthSensitiveElement element);
        host.Update(EmptyFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        float firstWidth = element.LastAvailableWidth;

        host.Update(EmptyFrame(), new UiViewport(220, 100), TimeSpan.Zero);

        Assert.Equal(100, firstWidth);
        Assert.Equal(220, element.LastAvailableWidth);
    }

    [Fact]
    public void UnchangedFrameStillReportsNoRetainedWorkAfterPreInputGate()
    {
        UiHost host = HostWithWidthSensitiveRoot(out _, out _);
        host.Update(EmptyFrame(), new UiViewport(100, 100), TimeSpan.Zero);

        UiFrame unchanged = host.Update(EmptyFrame(), new UiViewport(100, 100), TimeSpan.Zero);

        Assert.Equal(0, unchanged.Stats.MeasuredElements);
        Assert.Equal(0, unchanged.Stats.ArrangedElements);
        Assert.Equal(0, unchanged.Stats.RenderedElements);
        Assert.Equal(0, unchanged.Stats.HitTestElements);
        Assert.Equal(1, unchanged.Stats.NoWorkFrames);
    }

    [Fact]
    public void InputAfterViewportChangeUsesCommittedHitTestBounds()
    {
        UiHost host = HostWithButton(out _, out Button button);
        host.Update(EmptyFrame(), new UiViewport(100, 100), TimeSpan.Zero);
        host.Update(EmptyFrame(), new UiViewport(200, 100), TimeSpan.Zero);

        host.Update(PointerFrame(150, 10, pressed: true), new UiViewport(200, 100), TimeSpan.Zero);

        Assert.Same(button, host.InputBridge.FocusManager.FocusedElement);
        Assert.True(button.IsKeyboardFocused);
    }

    [Fact]
    public void InitialFrameCommitsLayoutBeforePointerHitTestingWhenNeeded()
    {
        UiHost host = HostWithButton(out _, out Button button);

        host.Update(PointerFrame(10, 10, pressed: true), new UiViewport(100, 100), TimeSpan.Zero);

        Assert.Same(button, host.InputBridge.FocusManager.FocusedElement);
        Assert.True(button.IsKeyboardFocused);
    }

    private static UiHost HostWithWidthSensitiveRoot(out UIRoot root, out WidthSensitiveElement element)
    {
        root = new UIRoot();
        element = new WidthSensitiveElement();
        root.VisualChildren.Add(element);
        return new UiHost(new UiHostOptions { Root = root });
    }

    private static UiHost HostWithButton(out UIRoot root, out Button button)
    {
        root = new UIRoot();
        button = new Button
        {
            Content = new FixedContentElement(180, 40)
        };
        root.VisualChildren.Add(button);
        return new UiHost(new UiHostOptions { Root = root });
    }

    private static InputFrame EmptyFrame()
    {
        return new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private static InputFrame PointerFrame(float x, float y, bool pressed)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(x, y);
        if (pressed)
        {
            current = current.WithButton(InputMouseButton.Left, true);
        }

        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private sealed class WidthSensitiveElement : UIElement
    {
        public float LastAvailableWidth { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            LastAvailableWidth = context.AvailableSize.Width;
            return new LayoutSize(context.AvailableSize.Width, 10);
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            return context.FinalRect;
        }

        protected override void OnRender(RenderContext context)
        {
            context.DrawingContext.FillRectangle(new DrawRect(context.Bounds.X, context.Bounds.Y, 1, 1), DrawColor.White);
        }
    }

    private sealed class FixedContentElement(float width, float height) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return new LayoutSize(width, height);
        }
    }
}

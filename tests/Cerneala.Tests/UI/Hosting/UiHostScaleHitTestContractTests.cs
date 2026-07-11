using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Hosting;

public sealed class UiHostScaleHitTestContractTests
{
    [Fact]
    public void ViewportScaleChangeInvalidatesMeasureArrangeRenderAndHitTest()
    {
        UiHost host = HostWithWidthSensitiveRoot(out _, out _);
        host.Update(EmptyFrame(), new UiViewport(100, 100, 1), TimeSpan.Zero);

        UiFrame scaled = host.Update(EmptyFrame(), new UiViewport(100, 100, 2), TimeSpan.Zero);

        Assert.True(scaled.Stats.MeasuredElements > 0);
        Assert.True(scaled.Stats.ArrangedElements > 0);
        Assert.True(scaled.Stats.RenderedElements > 0);
        Assert.True(scaled.Stats.HitTestElements > 0);
    }

    [Fact]
    public void ViewportScaleChangeRebuildsHitTestBeforeNextInput()
    {
        UiHost host = HostWithOffsetButton(out _, out Button button, left: 100, top: 10, width: 40, height: 30);
        host.Update(EmptyFrame(), new UiViewport(200, 100, 1), TimeSpan.Zero);
        host.Update(EmptyFrame(), new UiViewport(200, 100, 2), TimeSpan.Zero);

        host.Update(PointerFrame(120, 20, pressed: true), new UiViewport(200, 100, 2), TimeSpan.Zero);

        Assert.Same(button, host.InputBridge.FocusManager.FocusedElement);
        Assert.True(button.IsKeyboardFocused);
    }

    [Fact]
    public void ExplicitInputFrameCoordinatesAreNotScaledByUiHost()
    {
        UiHost host = HostWithOffsetButton(out _, out Button button, left: 100, top: 10, width: 40, height: 30);

        host.Update(PointerFrame(120, 20, pressed: true), new UiViewport(200, 100, 2), TimeSpan.Zero);

        Assert.Same(button, host.InputBridge.FocusManager.FocusedElement);
        Assert.True(button.IsKeyboardFocused);
    }

    [Fact]
    public void UnchangedScaleSecondFrameDoesNoRetainedWork()
    {
        UiHost host = HostWithWidthSensitiveRoot(out _, out _);
        host.Update(EmptyFrame(), new UiViewport(100, 100, 2), TimeSpan.Zero);

        UiFrame unchanged = host.Update(EmptyFrame(), new UiViewport(100, 100, 2), TimeSpan.Zero);

        Assert.Equal(0, unchanged.Stats.MeasuredElements);
        Assert.Equal(0, unchanged.Stats.ArrangedElements);
        Assert.Equal(0, unchanged.Stats.RenderedElements);
        Assert.Equal(0, unchanged.Stats.HitTestElements);
        Assert.Equal(1, unchanged.Stats.NoWorkFrames);
    }

    private static UiHost HostWithWidthSensitiveRoot(out UIRoot root, out WidthSensitiveElement element)
    {
        root = new UIRoot();
        element = new WidthSensitiveElement();
        root.VisualChildren.Add(element);
        return new UiHost(new UiHostOptions { Root = root });
    }

    private static UiHost HostWithOffsetButton(out UIRoot root, out Button button, float left, float top, float width, float height)
    {
        root = new UIRoot();
        Canvas canvas = new();
        button = new Button
        {
            Content = new FixedContentElement(width, height)
        };

        Canvas.SetLeft(button, left);
        Canvas.SetTop(button, top);
        canvas.VisualChildren.Add(button);
        root.VisualChildren.Add(canvas);

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
            context.DrawingContext.FillRectangle(new DrawRect(context.Bounds.X, context.Bounds.Y, 1, 1), Color.White);
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

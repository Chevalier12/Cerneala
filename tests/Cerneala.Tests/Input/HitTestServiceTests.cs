using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Media;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.Input;

public sealed class HitTestServiceTests
{
    [Fact]
    public void TopmostVisualChildWins()
    {
        UIRoot root = new(100, 100);
        UIElement bottom = Arranged(0, 0, 50, 50);
        UIElement top = Arranged(0, 0, 50, 50);
        root.VisualChildren.Add(bottom);
        root.VisualChildren.Add(top);

        HitTestResult? result = new HitTestService().HitTest(root, 10, 10);

        Assert.Same(top, result!.Element);
    }

    [Fact]
    public void LayoutPanelWithoutInputHandlersDoesNotOccludeInteractiveSibling()
    {
        UIRoot root = new(100, 100);
        UIElement target = Arranged(0, 0, 50, 50);
        target.AddHandler(UIElement.MouseUpEvent, (_, _) => { });
        Grid transparentLayoutPanel = new();
        transparentLayoutPanel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 50, 50)));
        root.VisualChildren.Add(target);
        root.VisualChildren.Add(transparentLayoutPanel);

        HitTestResult? result = new HitTestService().HitTest(root, 10, 10);

        Assert.Same(target, result!.Element);
    }

    [Fact]
    public void LayoutPanelWithInputHandlerCanBeHitDirectly()
    {
        UIRoot root = new(100, 100);
        UIElement target = Arranged(0, 0, 50, 50);
        Grid interactiveLayoutPanel = new();
        interactiveLayoutPanel.AddHandler(UIElement.MouseUpEvent, (_, _) => { });
        interactiveLayoutPanel.Arrange(new ArrangeContext(new LayoutRect(0, 0, 50, 50)));
        root.VisualChildren.Add(target);
        root.VisualChildren.Add(interactiveLayoutPanel);

        HitTestResult? result = new HitTestService().HitTest(root, 10, 10);

        Assert.Same(interactiveLayoutPanel, result!.Element);
    }

    [Fact]
    public void RootViewportBoundsAreHitWhenNoChildContainsPoint()
    {
        UIRoot root = new(100, 100);

        HitTestResult? result = new HitTestService().HitTest(root, 10, 10);

        Assert.Same(root, result!.Element);
    }

    [Fact]
    public void RootViewportBoundsRejectOverflowingDescendant()
    {
        UIRoot root = new(100, 100);
        UIElement child = Arranged(120, 10, 20, 20);
        root.VisualChildren.Add(child);

        HitTestResult? result = new HitTestService().HitTest(root, 125, 15);

        Assert.Null(result);
    }

    [Fact]
    public void DescendantOutsideParentBoundsCanBeHitWhenNotClipped()
    {
        UIRoot root = new(100, 100);
        UIElement parent = Arranged(0, 0, 20, 20);
        UIElement child = Arranged(40, 40, 10, 10);
        UIElement fallback = Arranged(40, 40, 10, 10);
        parent.VisualChildren.Add(child);
        root.VisualChildren.Add(fallback);
        root.VisualChildren.Add(parent);

        HitTestResult? result = new HitTestService().HitTest(root, 45, 45);

        Assert.Same(child, result!.Element);
    }

    [Fact]
    public void ClipBoundsRejectOverflowingDescendant()
    {
        UIRoot root = new(100, 100);
        UIElement parent = Arranged(0, 0, 20, 20);
        UIElement child = Arranged(40, 40, 10, 10);
        UIElement fallback = Arranged(40, 40, 10, 10);
        parent.VisualChildren.Add(child);
        ClipNode.SetClip(parent, parent.ArrangedBounds);
        root.VisualChildren.Add(fallback);
        root.VisualChildren.Add(parent);

        HitTestResult? result = new HitTestService().HitTest(root, 45, 45);

        Assert.Same(fallback, result!.Element);
    }

    [Fact]
    public void InvisibleCollapsedAndDisabledElementsAreSkipped()
    {
        UIRoot root = new(100, 100);
        UIElement invisible = Arranged(0, 0, 50, 50);
        UIElement collapsed = Arranged(0, 0, 50, 50);
        UIElement disabled = Arranged(0, 0, 50, 50);
        UIElement target = Arranged(0, 0, 50, 50);
        invisible.IsVisible = false;
        collapsed.Visibility = Visibility.Collapsed;
        disabled.IsEnabled = false;
        root.VisualChildren.Add(target);
        root.VisualChildren.Add(disabled);
        root.VisualChildren.Add(collapsed);
        root.VisualChildren.Add(invisible);

        HitTestResult? result = new HitTestService().HitTest(root, 10, 10);

        Assert.Same(target, result!.Element);
    }

    [Fact]
    public void DisabledScrollBarSubtreeIsSkipped()
    {
        UIRoot root = new(100, 100);
        UIElement fallback = Arranged(0, 0, 50, 80);
        ScrollBar disabledScrollBar = new()
        {
            IsEnabled = false,
            Maximum = 100,
            ViewportSize = 10
        };
        disabledScrollBar.Measure(new MeasureContext(new LayoutSize(12, 80)));
        disabledScrollBar.Arrange(new ArrangeContext(new LayoutRect(0, 0, 12, 80)));
        root.VisualChildren.Add(fallback);
        root.VisualChildren.Add(disabledScrollBar);

        HitTestResult? result = new HitTestService().HitTest(root, 6, 20);

        Assert.Same(fallback, result!.Element);
    }

    [Fact]
    public void FilterCanRejectSubtree()
    {
        UIRoot root = new(100, 100);
        UIElement parent = Arranged(0, 0, 50, 50);
        UIElement child = Arranged(0, 0, 50, 50);
        UIElement sibling = Arranged(0, 0, 50, 50);
        parent.VisualChildren.Add(child);
        root.VisualChildren.Add(sibling);
        root.VisualChildren.Add(parent);
        HitTestFilter filter = new(element => ReferenceEquals(element, parent)
            ? HitTestFilterBehavior.ExcludeSubtree
            : HitTestFilterBehavior.Include);

        HitTestResult? result = new HitTestService().HitTest(root, 10, 10, filter);

        Assert.Same(sibling, result!.Element);
    }

    [Fact]
    public void RenderTransformMovesHitTestBounds()
    {
        UIRoot root = new(200, 200);
        UIElement element = Arranged(0, 0, 20, 20);
        element.RenderTransform = new Transform(Matrix3x2.CreateTranslation(50, 0));
        root.VisualChildren.Add(element);

        HitTestResult? layoutHit = new HitTestService().HitTest(root, 10, 10);
        HitTestResult? visualOnlyHit = new HitTestService().HitTest(root, 55, 10);

        Assert.Same(root, layoutHit!.Element);
        Assert.Same(element, visualOnlyHit!.Element);
    }

    [Fact]
    public void NestedMotionTransformsMoveHitTestBounds()
    {
        UIRoot root = new(300, 300);
        UIElement parent = Arranged(0, 0, 200, 200);
        parent.RenderTransformOrigin = new LayoutPoint(0, 0);
        parent.TranslateX = 20;
        parent.TranslateY = 30;
        parent.ScaleX = 2;
        parent.ScaleY = 2;
        UIElement child = Arranged(40, 10, 20, 10);
        child.RenderTransformOrigin = new LayoutPoint(0, 0);
        child.Rotation = MathF.PI / 2;
        parent.VisualChildren.Add(child);
        root.VisualChildren.Add(parent);

        HitTestResult? staleLayoutHit = new HitTestService().HitTest(root, 50, 15);
        HitTestResult? visualHit = new HitTestService().HitTest(root, 90, 70);

        Assert.Same(root, staleLayoutHit!.Element);
        Assert.Same(child, visualHit!.Element);
    }

    [Theory]
    [InlineData(1f, false)]
    [InlineData(1e30f, true)]
    public void NestedOverflowingTransformsMakeTheOverflowingSubtreeUnhittable(float scale, bool overflows)
    {
        // Each scale is finite; with 1e30 the combined child scale 1e60 overflows float.
        UIRoot root = new(300, 300);
        UIElement parent = Arranged(0, 0, 200, 200);
        parent.RenderTransformOrigin = new LayoutPoint(0, 0);
        parent.ScaleX = scale;
        UIElement child = Arranged(0, 0, 100, 100);
        child.RenderTransformOrigin = new LayoutPoint(0, 0);
        child.ScaleX = scale;
        parent.VisualChildren.Add(child);
        root.VisualChildren.Add(parent);

        HitTestResult? result = null;
        Exception? failure = Record.Exception(() => result = new HitTestService().HitTest(root, 5, 5));

        Assert.True(failure is null, "HitTest threw " + failure?.GetType().Name + ": " + failure?.Message);
        Assert.Same(overflows ? parent : child, result?.Element);
    }

    [Theory]
    [InlineData(1f, false)]
    [InlineData(1e30f, true)]
    public void TransformWhoseInverseOverflowsMakesTheElementUnhittable(float scale, bool overflows)
    {
        // The transform is finite, but with 1e30 its determinant 1e60 overflows float during inversion.
        UIRoot root = new(300, 300);
        UIElement element = Arranged(0, 0, 200, 200);
        element.ScaleX = scale;
        element.ScaleY = scale;
        root.VisualChildren.Add(element);

        HitTestResult? result = null;
        Exception? failure = Record.Exception(() => result = new HitTestService().HitTest(root, 100, 100));

        Assert.True(failure is null, "HitTest threw " + failure?.GetType().Name + ": " + failure?.Message);
        Assert.Same(overflows ? root : element, result?.Element);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(1e-20f)]
    public void TinyNonZeroDeterminantTransformDoesNotThrowDuringHitTesting(float scale)
    {
        UIRoot root = new(300, 300);
        UIElement element = Arranged(0, 0, 100, 100);
        element.RenderTransformOrigin = new LayoutPoint(0, 0);
        element.ScaleX = scale;
        element.ScaleY = scale;
        root.VisualChildren.Add(element);

        HitTestResult? result = null;
        Exception? failure = Record.Exception(() => result = new HitTestService().HitTest(root, 5, 5));

        Assert.True(failure is null, "HitTest threw " + failure?.GetType().Name + ": " + failure?.Message);
        Assert.Same(root, result!.Element);
    }

    [Fact]
    public void OverflowingDeterminantTransformDoesNotThrowDuringHitTesting()
    {
        UIRoot root = new(300, 300);
        UIElement element = Arranged(10, 10, 100, 100);
        element.RenderTransformOrigin = new LayoutPoint(0, 0);
        element.ScaleX = 1e20f;
        element.ScaleY = 1e20f;
        root.VisualChildren.Add(element);

        Exception? failure = Record.Exception(() => new HitTestService().HitTest(root, 50, 50));

        Assert.True(failure is null, "HitTest threw " + failure?.GetType().Name + ": " + failure?.Message);
    }

    internal static UIElement Arranged(float x, float y, float width, float height)
    {
        UIElement element = new();
        element.Arrange(new ArrangeContext(new LayoutRect(x, y, width, height)));
        return element;
    }
}

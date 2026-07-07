using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
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
    public void RenderTransformDoesNotAffectHitTestBounds()
    {
        UIRoot root = new(200, 200);
        UIElement element = Arranged(0, 0, 20, 20);
        element.RenderTransform = new Transform(Matrix3x2.CreateTranslation(50, 0));
        root.VisualChildren.Add(element);

        HitTestResult? layoutHit = new HitTestService().HitTest(root, 10, 10);
        HitTestResult? visualOnlyHit = new HitTestService().HitTest(root, 55, 10);

        Assert.Same(element, layoutHit!.Element);
        Assert.NotSame(element, visualOnlyHit!.Element);
        Assert.Same(root, visualOnlyHit.Element);
    }

    internal static UIElement Arranged(float x, float y, float width, float height)
    {
        UIElement element = new();
        element.Arrange(new ArrangeContext(new LayoutRect(x, y, width, height)));
        return element;
    }
}

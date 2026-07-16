using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

namespace Cerneala.Tests.UI.Layout;

public sealed class VisibilityTests
{
    [Fact]
    public void VisibleElementParticipatesNormally()
    {
        FixedElement element = new(new LayoutSize(20, 10));

        LayoutSize desired = element.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(new LayoutSize(20, 10), desired);
    }

    [Fact]
    public void HiddenElementReservesLayoutSpace()
    {
        FixedElement element = new(new LayoutSize(20, 10))
        {
            Visibility = Visibility.Hidden
        };

        LayoutSize desired = element.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(new LayoutSize(20, 10), desired);
    }

    [Fact]
    public void CollapsedElementContributesZeroSize()
    {
        FixedElement element = new(new LayoutSize(20, 10))
        {
            Visibility = Visibility.Collapsed
        };

        LayoutSize desired = element.Measure(new MeasureContext(new LayoutSize(100, 100)));
        LayoutRect arranged = element.Arrange(new ArrangeContext(new LayoutRect(0, 0, 50, 50)));

        Assert.Equal(LayoutSize.Zero, desired);
        Assert.Equal(new LayoutRect(0, 0, 0, 0), arranged);
    }

    [Theory]
    [InlineData(Visibility.Hidden)]
    [InlineData(Visibility.Collapsed)]
    public void NonVisibleLayoutVisibilityExcludesElementFromInputRoute(Visibility visibility)
    {
        UIRoot root = new();
        UIElement child = new()
        {
            Visibility = visibility
        };

        root.VisualChildren.Add(child);

        ElementInputRouteMap routeMap = new ElementInputRouteBuilder().Build(root);

        Assert.False(routeMap.TryGetId(child, out _));
    }

    [Theory]
    [InlineData(Visibility.Hidden)]
    [InlineData(Visibility.Collapsed)]
    public void NonVisibleLayoutVisibilityExcludesDescendantsFromInputRoute(Visibility visibility)
    {
        UIRoot root = new();
        UIElement parent = new()
        {
            Visibility = visibility
        };
        UIElement child = new();

        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);

        ElementInputRouteMap routeMap = new ElementInputRouteBuilder().Build(root);

        Assert.False(routeMap.TryGetId(child, out _));
    }

    [Fact]
    public void ExpandingCollapsedElementInvalidatesDescendantLayoutCaches()
    {
        UIRoot root = new(100, 100);
        Grid host = new();
        Grid parent = new() { Visibility = Visibility.Collapsed };
        Grid child = new();
        FixedElement grandchild = new(new LayoutSize(20, 10));
        root.VisualChildren.Add(host);
        host.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        child.VisualChildren.Add(grandchild);
        root.ProcessFrame();
        int childLayoutVersion = child.LayoutVersion;
        int grandchildLayoutVersion = grandchild.LayoutVersion;

        parent.Visibility = Visibility.Visible;

        Assert.True(child.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(grandchild.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(child.LayoutVersion > childLayoutVersion);
        Assert.True(grandchild.LayoutVersion > grandchildLayoutVersion);
        root.ProcessFrame();
        Assert.True(grandchild.ArrangedBounds.Width > 0);
        Assert.True(grandchild.ArrangedBounds.Height > 0);
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }
}

using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.Controls;

public sealed class RenderSurface2DTests
{
    [Fact]
    public void ContentUsesContentControlLayoutContract()
    {
        RenderSurface2D surface = new()
        {
            Padding = new Thickness(2),
            BorderThickness = new Thickness(1)
        };
        FixedElement child = new(new LayoutSize(10, 5));
        surface.Content = child;

        LayoutSize desired = surface.Measure(
            new MeasureContext(new LayoutSize(100, 100)));
        surface.Arrange(new ArrangeContext(new LayoutRect(0, 0, 30, 20)));

        Assert.Equal(new LayoutSize(16, 11), desired);
        Assert.Equal(new LayoutRect(3, 3, 24, 14), child.ArrangedBounds);
    }

    [Fact]
    public void SurfaceRendersBetweenBackgroundAndRetainedContent()
    {
        RenderSurface2D surface = new()
        {
            Background = new SolidColorBrush(new Color(1, 2, 3)),
            BorderBrush = new SolidColorBrush(new Color(4, 5, 6)),
            BorderThickness = new Thickness(1),
            Content = new RenderingElement(new Color(7, 8, 9))
        };
        surface.DrawSurface += _ => { };
        surface.Measure(new MeasureContext(new LayoutSize(40, 30)));
        surface.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 30)));
        RetainedRenderCache cache = PreparedCache(surface);

        new DrawCommandListBuilder().Build(
            surface,
            cache,
            new RenderCounters());

        Assert.Collection(
            cache.RootCommands,
            command => Assert.Equal(DrawCommandKind.FillRectangle, command.Kind),
            command => Assert.Equal(DrawCommandKind.RenderSurface2D, command.Kind),
            command => Assert.Equal(DrawCommandKind.DrawRectangle, command.Kind),
            command =>
            {
                Assert.Equal(DrawCommandKind.FillRectangle, command.Kind);
                Assert.Equal(new Color(7, 8, 9), command.Color);
            });
    }

    [Fact]
    public void EventAndOverrideActivateManagedRenderingModes()
    {
        RenderSurface2D eventSurface = new();
        RenderSurface2DDrawEventHandler handler = _ => { };

        Assert.False(eventSurface.IsManagedModeActiveForTests);
        eventSurface.DrawSurface += handler;
        Assert.True(eventSurface.IsManagedModeActiveForTests);
        eventSurface.DrawSurface -= handler;
        Assert.False(eventSurface.IsManagedModeActiveForTests);
        Assert.True(new OverriddenSurface().IsManagedModeActiveForTests);
    }

    [Fact]
    public void ManagedSurfaceParticipatesInTheCernealaFrameLoop()
    {
        RenderSurface2D surface = new();
        surface.DrawSurface += _ => { };
        int renderVersion = surface.RenderVersion;

        bool firstFrameChanged = surface.UpdateRenderTime(TimeSpan.FromMilliseconds(16));
        int firstFrameVersion = surface.RenderVersion;
        bool secondFrameChanged = surface.UpdateRenderTime(TimeSpan.FromMilliseconds(16));

        Assert.True(firstFrameChanged);
        Assert.True(firstFrameVersion > renderVersion);
        Assert.True(secondFrameChanged);
        Assert.True(surface.RenderVersion > firstFrameVersion);
    }

    [Fact]
    public void SurfaceWithoutManagedDrawingDoesNotRequestFrames()
    {
        RenderSurface2D surface = new();
        int renderVersion = surface.RenderVersion;

        bool changed = surface.UpdateRenderTime(TimeSpan.FromMilliseconds(16));

        Assert.False(changed);
        Assert.Equal(renderVersion, surface.RenderVersion);
    }

    private static RetainedRenderCache PreparedCache(UIElement root)
    {
        RetainedRenderCache cache = new();
        RenderCounters counters = new();
        PrepareSubtree(root, cache, counters);
        return cache;
    }

    private static void PrepareSubtree(
        UIElement element,
        RetainedRenderCache cache,
        RenderCounters counters)
    {
        cache.GetElementCache(element).Ensure(
            element,
            counters,
            forceRebuild: true);
        foreach (UIElement child in element.VisualChildren)
        {
            PrepareSubtree(child, cache, counters);
        }
    }

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context) => size;
    }

    private sealed class RenderingElement(Color color) : UIElement
    {
        protected override void OnRender(RenderContext context)
        {
            context.DrawingContext.FillRectangle(
                Border.ToDrawRect(context.Bounds),
                color);
        }
    }

    private sealed class OverriddenSurface : RenderSurface2D
    {
        protected override void OnDrawSurface(
            RenderSurface2DDrawContext context)
        {
        }
    }
}

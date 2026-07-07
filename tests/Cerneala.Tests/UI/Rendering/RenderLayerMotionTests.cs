using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Rendering;

public sealed class RenderLayerMotionTests
{
    [Fact]
    public void ElementOpacityAppliesToRenderedCommands()
    {
        RenderingTestElement element = new(new DrawColor(20, 40, 60, 200))
        {
            Opacity = 0.5f
        };
        RetainedRenderCache cache = PreparedCache(element);

        new DrawCommandListBuilder().Build(element, cache, new RenderCounters());

        Assert.Equal(new DrawColor(20, 40, 60, 100), cache.RootCommands[0].Color);
    }

    [Fact]
    public void ElementRenderTransformAffectsDrawCommands()
    {
        RenderingTestElement element = new(DrawColor.White)
        {
            RenderTransform = new Transform(Matrix3x2.CreateTranslation(10, 20))
        };
        RetainedRenderCache cache = PreparedCache(element);

        new DrawCommandListBuilder().Build(element, cache, new RenderCounters());

        Assert.Equal(new DrawRect(10, 20, 1, 1), cache.RootCommands[0].Rect);
    }

    [Fact]
    public void RenderTransformOriginChangesPivotDeterministically()
    {
        RenderingTestElement element = new(DrawColor.White)
        {
            Scale = 2,
            RenderTransformOrigin = new LayoutPoint(0.5f, 0.5f)
        };
        RetainedRenderCache cache = PreparedCache(element);

        new DrawCommandListBuilder().Build(element, cache, new RenderCounters());

        Assert.Equal(new DrawRect(-5, -5, 2, 2), cache.RootCommands[0].Rect);
    }

    [Fact]
    public void RenderOnlyMotionInvalidatesRootWithoutRebuildingLocalCommands()
    {
        UIRoot root = new();
        RenderingTestElement element = new(DrawColor.White);
        root.VisualChildren.Add(element);
        root.Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Render | Cerneala.UI.Invalidation.InvalidationFlags.Subtree, "initial");
        root.ProcessFrame();
        root.RetainedRenderer.Commit(root);
        int renderCount = element.RenderCount;

        element.Opacity = 0.5f;
        root.ProcessFrame();
        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Equal(renderCount, element.RenderCount);
        Assert.Equal(128, commands[0].Color.A);
    }

    private static RetainedRenderCache PreparedCache(UIElement root)
    {
        RetainedRenderCache cache = new();
        RenderCounters counters = new();
        PrepareSubtree(root, cache, counters);
        return cache;
    }

    private static void PrepareSubtree(UIElement element, RetainedRenderCache cache, RenderCounters counters)
    {
        cache.GetElementCache(element).Ensure(element, counters, forceRebuild: true);
        foreach (UIElement child in element.VisualChildren)
        {
            PrepareSubtree(child, cache, counters);
        }
    }
}

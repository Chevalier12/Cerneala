using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Rendering;

public sealed class DrawCommandListBuilderTests
{
    [Fact]
    public void ParentLocalCommandsAppearBeforeChildCommands()
    {
        RenderingTestElement parent = new(new DrawColor(1, 0, 0));
        RenderingTestElement child = new(new DrawColor(2, 0, 0));
        parent.VisualChildren.Add(child);
        RetainedRenderCache cache = PreparedCache(parent);

        new DrawCommandListBuilder().Build(parent, cache, new RenderCounters());

        Assert.Equal(new DrawColor(1, 0, 0), cache.RootCommands[0].Color);
        Assert.Equal(new DrawColor(2, 0, 0), cache.RootCommands[1].Color);
    }

    [Fact]
    public void SiblingsRenderInVisualChildOrder()
    {
        UIElement root = new();
        RenderingTestElement first = new(new DrawColor(1, 0, 0));
        RenderingTestElement second = new(new DrawColor(2, 0, 0));
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);
        RetainedRenderCache cache = PreparedCache(root);

        new DrawCommandListBuilder().Build(root, cache, new RenderCounters());

        Assert.Equal(new DrawColor(1, 0, 0), cache.RootCommands[0].Color);
        Assert.Equal(new DrawColor(2, 0, 0), cache.RootCommands[1].Color);
    }

    [Fact]
    public void CollapsedSubtreeDoesNotEmitCommands()
    {
        UIElement root = new();
        RenderingTestElement child = new(DrawColor.White)
        {
            Visibility = Visibility.Collapsed
        };
        child.VisualChildren.Add(new RenderingTestElement(DrawColor.Black));
        root.VisualChildren.Add(child);
        RetainedRenderCache cache = PreparedCache(root);

        new DrawCommandListBuilder().Build(root, cache, new RenderCounters());

        Assert.Empty(cache.RootCommands);
    }

    [Fact]
    public void ClipCommandsAreBalancedForEmptySubtree()
    {
        UIElement root = new();
        ClipNode.SetClip(root, new LayoutRect(0, 0, 10, 10));
        RetainedRenderCache cache = PreparedCache(root);

        new DrawCommandListBuilder().Build(root, cache, new RenderCounters());

        Assert.Equal(2, cache.RootCommands.Count);
        Assert.Equal(DrawCommandKind.PushClip, cache.RootCommands[0].Kind);
        Assert.Equal(DrawCommandKind.PopClip, cache.RootCommands[1].Kind);
    }

    [Fact]
    public void ClipCommandsWrapVisibleSubtree()
    {
        RenderingTestElement root = new(DrawColor.White);
        ClipNode.SetClip(root, new LayoutRect(0, 0, 10, 10));
        RetainedRenderCache cache = PreparedCache(root);

        new DrawCommandListBuilder().Build(root, cache, new RenderCounters());

        Assert.Equal(DrawCommandKind.PushClip, cache.RootCommands[0].Kind);
        Assert.Equal(DrawCommandKind.FillRectangle, cache.RootCommands[1].Kind);
        Assert.Equal(DrawCommandKind.PopClip, cache.RootCommands[2].Kind);
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

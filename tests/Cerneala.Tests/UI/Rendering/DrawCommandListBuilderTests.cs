using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Markup;
using Cerneala.UI.Media;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Rendering;

public sealed class DrawCommandListBuilderTests
{
    [Fact]
    public void ParentLocalCommandsAppearBeforeChildCommands()
    {
        RenderingTestElement parent = new(new Color(1, 0, 0));
        RenderingTestElement child = new(new Color(2, 0, 0));
        parent.VisualChildren.Add(child);
        RetainedRenderCache cache = PreparedCache(parent);

        new DrawCommandListBuilder().Build(parent, cache, new RenderCounters());

        Assert.Equal(new Color(1, 0, 0), cache.RootCommands[0].Color);
        Assert.Equal(new Color(2, 0, 0), cache.RootCommands[1].Color);
    }

    [Fact]
    public void PrismCaptureWrapsOnlyTheAttachedElementsLocalCommands()
    {
        UIRoot root = new();
        RenderingTestElement parent = new(new Color(1, 0, 0));
        RenderingTestElement child = new(new Color(2, 0, 0));
        parent.VisualChildren.Add(child);
        using IDisposable prismLifetime = GeneratedMarkup.AttachPrism(
            parent,
            () => new PrismInstance(
                new PrismCompositionDefinition(
                    "local-visual-only",
                    [
                        new PrismLayerDefinition(
                            new PrismNodeId(1),
                            "Content",
                            filters: [new PrismFilterDefinition(PrismFilterId.Blur)])
                    ])));
        ElementLifecycle.AttachSubtree(root, parent);
        RetainedRenderCache cache = PreparedCache(parent);

        new DrawCommandListBuilder().Build(parent, cache, new RenderCounters());

        Assert.Collection(
            cache.RootCommands,
            command => Assert.Equal(DrawCommandKind.BeginPrism, command.Kind),
            command =>
            {
                Assert.Equal(DrawCommandKind.FillRectangle, command.Kind);
                Assert.Equal(new Color(1, 0, 0), command.Color);
            },
            command => Assert.Equal(DrawCommandKind.EndPrism, command.Kind),
            command =>
            {
                Assert.Equal(DrawCommandKind.FillRectangle, command.Kind);
                Assert.Equal(new Color(2, 0, 0), command.Color);
            });
    }

    [Fact]
    public void SiblingsRenderInVisualChildOrder()
    {
        UIElement root = new();
        RenderingTestElement first = new(new Color(1, 0, 0));
        RenderingTestElement second = new(new Color(2, 0, 0));
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);
        RetainedRenderCache cache = PreparedCache(root);

        new DrawCommandListBuilder().Build(root, cache, new RenderCounters());

        Assert.Equal(new Color(1, 0, 0), cache.RootCommands[0].Color);
        Assert.Equal(new Color(2, 0, 0), cache.RootCommands[1].Color);
    }

    [Fact]
    public void CollapsedSubtreeDoesNotEmitCommands()
    {
        UIElement root = new();
        RenderingTestElement child = new(Color.White)
        {
            Visibility = Visibility.Collapsed
        };
        child.VisualChildren.Add(new RenderingTestElement(Color.Black));
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
        RenderingTestElement root = new(Color.White);
        ClipNode.SetClip(root, new LayoutRect(0, 0, 10, 10));
        RetainedRenderCache cache = PreparedCache(root);

        new DrawCommandListBuilder().Build(root, cache, new RenderCounters());

        Assert.Equal(DrawCommandKind.PushClip, cache.RootCommands[0].Kind);
        Assert.Equal(DrawCommandKind.FillRectangle, cache.RootCommands[1].Kind);
        Assert.Equal(DrawCommandKind.PopClip, cache.RootCommands[2].Kind);
    }

    [Fact]
    public void BrushTextCommandPreservesBrushDuringRootComposition()
    {
        SolidColorBrush foreground = new(Color.White);
        BrushTextRenderingElement root = new(foreground);
        RetainedRenderCache cache = PreparedCache(root);

        new DrawCommandListBuilder().Build(root, cache, new RenderCounters());

        DrawCommand command = Assert.Single(cache.RootCommands);
        Assert.Equal(DrawCommandKind.DrawText, command.Kind);
        Assert.Same(foreground, command.Brush);
        Assert.Equal(1, command.BrushOpacity);
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

    private sealed class BrushTextRenderingElement(IDrawBrush foreground) : UIElement
    {
        protected override void OnRender(RenderContext context)
        {
            DrawTextRun textRun = new(new TestFont(), "Visible text", 12);
            context.DrawingContext.DrawText(textRun, new DrawPoint(3, 4), foreground);
        }
    }

    private sealed class TestFont : IDrawFont
    {
        public string FamilyName => "Test";

        public float Size => 12;
    }
}

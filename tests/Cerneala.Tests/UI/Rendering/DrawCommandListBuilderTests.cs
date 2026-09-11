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
using Cerneala.UI.Resources;

namespace Cerneala.Tests.UI.Rendering;

public sealed class DrawCommandListBuilderTests
{
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    public void ResourceFreePrismResolutionDoesNotAllocateUnusedCollections(
        bool grouped,
        bool visible)
    {
        UIElement element = new();
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "Content",
            filters: [new PrismFilterDefinition(PrismFilterId.Blur)],
            styles: [new PrismStyleDefinition(PrismStyleId.OuterGlow)],
            mask: visible ? null : new PrismMaskDefinition(new PrismResourceId("Hidden")));
        PrismInstance instance = new(new PrismCompositionDefinition(
            "resource-free",
            [grouped
                ? new PrismGroupDefinition(new PrismNodeId(2), "Group", [layer], visible: visible)
                : layer]));
        for (int index = 0; index < 128; index++)
        {
            Assert.Same(PrismDrawResources.Empty,
                DrawCommandListBuilder.ResolvePrismResources(element, instance));
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < 256; index++)
        {
            DrawCommandListBuilder.ResolvePrismResources(element, instance);
        }
        long bytesPerResolution = (GC.GetAllocatedBytesForCurrentThread() - before) / 256;

        Assert.True(bytesPerResolution <= 128,
            $"Resource-free resolution allocated {bytesPerResolution} bytes/call after warmup.");
    }

    [Fact]
    public void PrismResolutionRechecksMissingReplacedAndHiddenResources()
    {
        UIElement element = new();
        UIRoot root = new();
        root.VisualChildren.Add(element);
        ElementLifecycle.AttachSubtree(root, element);
        root.ResourceDependencyTracker.Track(element.Resources);
        PrismResourceId id = new("Mask");
        ResourceId<ImageResource> resourceId = new("Mask");
        PrismLayerDefinition layer = new(new PrismNodeId(1), "Content",
            filters: [new PrismFilterDefinition(PrismFilterId.Blur)],
            mask: new PrismMaskDefinition(id));
        PrismInstance instance = new(new PrismCompositionDefinition("resources",
            [new PrismGroupDefinition(new PrismNodeId(2), "Group", [layer],
                mask: new PrismMaskDefinition(id))]));
        PrismGroupState group = (PrismGroupState)instance.GetNodeState(new PrismNodeId(2));

        Assert.Same(PrismDrawResources.Empty, Resolve());
        TestImage first = new();
        element.Resources.SetResource(resourceId, new ImageResource(first));
        PrismDrawResources firstSnapshot = Resolve();
        Assert.Same(first, Assert.Single(firstSnapshot.Images));
        Assert.True(firstSnapshot.TryGetDependency(id, out long firstIdentity, out long firstVersion));
        Assert.True(firstSnapshot.HasStableVersions);

        TestImage replacement = new();
        element.Resources.SetResource(resourceId, new ImageResource(replacement));
        PrismDrawResources secondSnapshot = Resolve();
        Assert.Same(replacement, Assert.Single(secondSnapshot.Images));
        Assert.True(secondSnapshot.TryGetDependency(id, out long secondIdentity, out long secondVersion));
        Assert.NotEqual(firstIdentity, secondIdentity);
        Assert.True(secondVersion > firstVersion);
        Assert.Same(first, Assert.Single(firstSnapshot.Images));

        group.Visible = false;
        Assert.Same(PrismDrawResources.Empty, Resolve());
        group.Visible = true;
        group.Opacity = 0;
        Assert.Same(PrismDrawResources.Empty, Resolve());
        group.Opacity = 1;
        Assert.Same(replacement, Assert.Single(Resolve().Images));
        element.Resources.Remove(resourceId.Key);
        Assert.Same(PrismDrawResources.Empty, Resolve());

        PrismDrawResources Resolve() => DrawCommandListBuilder.ResolvePrismResources(element, instance);
    }

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
    public void PrismCaptureWrapsTheAttachedElementsVisualSubtree()
    {
        UIRoot root = new();
        RenderingTestElement parent = new(new Color(1, 0, 0));
        RenderingTestElement child = new(new Color(2, 0, 0));
        parent.VisualChildren.Add(child);
        using IDisposable prismLifetime = GeneratedMarkup.AttachPrism(
            parent,
            () => new PrismInstance(
                new PrismCompositionDefinition(
                    "visual-subtree",
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
            command =>
            {
                Assert.Equal(DrawCommandKind.FillRectangle, command.Kind);
                Assert.Equal(new Color(2, 0, 0), command.Color);
            },
            command => Assert.Equal(DrawCommandKind.EndPrism, command.Kind));
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

    private sealed class TestImage : IDrawImage
    {
        public int Width => 1;

        public int Height => 1;
    }
}

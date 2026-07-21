using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.UI.Motion.Core;
using Cerneala.Tests.UI.Rendering;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Markup;
using Cerneala.UI.Motion.Presence;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.Drawing.MonoGame.Prism.Cache;

public sealed class PrismRetainedVisualVersionTests
{
    [Fact]
    public void PixelVersionsPropagateOnlyToNearestPrismBoundary()
    {
        UIRoot root = new(100, 100);
        RenderingTestElement outer = new(new Color(1, 0, 0));
        RenderingTestElement branch = new(new Color(2, 0, 0));
        RenderingTestElement leaf = new(new Color(3, 0, 0));
        RenderingTestElement nested = new(new Color(4, 0, 0));
        RenderingTestElement nestedLeaf = new(new Color(5, 0, 0));
        branch.VisualChildren.Add(leaf);
        nested.VisualChildren.Add(nestedLeaf);
        outer.VisualChildren.Add(branch);
        outer.VisualChildren.Add(nested);
        root.VisualChildren.Add(outer);
        using IDisposable outerPrism = AttachPrism(outer, "Outer");
        using IDisposable nestedPrism = AttachPrism(nested, "Nested");
        root.ProcessFrame();

        long rootVersion = root.PrismVisualVersion;
        long outerVersion = outer.PrismVisualVersion;
        long branchVersion = branch.PrismVisualVersion;
        long leafVersion = leaf.PrismVisualVersion;
        long leafLocalVersion = leaf.PrismLocalVisualVersion;
        int leafLayoutVersion = leaf.LayoutVersion;

        leaf.Opacity = 0.5f;

        Assert.Equal(rootVersion, root.PrismVisualVersion);
        Assert.Equal(outerVersion + 1, outer.PrismVisualVersion);
        Assert.Equal(branchVersion + 1, branch.PrismVisualVersion);
        Assert.Equal(leafVersion + 1, leaf.PrismVisualVersion);
        Assert.Equal(leafLocalVersion + 1, leaf.PrismLocalVisualVersion);
        Assert.Equal(leafLayoutVersion, leaf.LayoutVersion);
        Assert.False(leaf.DirtyState.Has(InvalidationFlags.Measure));
        Assert.False(leaf.DirtyState.Has(InvalidationFlags.Arrange));
        Assert.False(leaf.DirtyState.Has(InvalidationFlags.HitTest));

        leaf.Opacity = 0.5f;
        Assert.Equal(outerVersion + 1, outer.PrismVisualVersion);
        Assert.Equal(leafLocalVersion + 1, leaf.PrismLocalVisualVersion);

        long outerAfterLeaf = outer.PrismVisualVersion;
        long nestedVersion = nested.PrismVisualVersion;
        long nestedLeafVersion = nestedLeaf.PrismVisualVersion;
        nestedLeaf.Opacity = 0.5f;

        Assert.Equal(outerAfterLeaf, outer.PrismVisualVersion);
        Assert.Equal(nestedVersion + 1, nested.PrismVisualVersion);
        Assert.Equal(nestedLeafVersion + 1, nestedLeaf.PrismVisualVersion);

        long visualBeforeNonPixelInvalidation = leaf.PrismVisualVersion;
        int layoutBeforeNonPixelInvalidation = leaf.LayoutVersion;
        leaf.Focusable = true;
        leaf.Width = 11;

        Assert.Equal(
            visualBeforeNonPixelInvalidation,
            leaf.PrismVisualVersion);
        Assert.True(leaf.LayoutVersion > layoutBeforeNonPixelInvalidation);
    }

    [Fact]
    public void ContentChildResourceAndPresenceMutationsAdvanceOneGeneration()
    {
        UIRoot root = new(100, 100);
        RenderingTestElement owner = new(new Color(1, 0, 0));
        RenderingTestElement child = new(new Color(2, 0, 0));
        owner.VisualChildren.Add(child);
        root.VisualChildren.Add(owner);
        using IDisposable prism = AttachPrism(owner, "Mutation owner");
        root.ProcessFrame();

        long ownerVersion = owner.PrismVisualVersion;
        child.IncrementRenderVersion();
        Assert.Equal(ownerVersion + 1, owner.PrismVisualVersion);

        ownerVersion = owner.PrismVisualVersion;
        owner.VisualChildren.Add(new RenderingTestElement(new Color(3, 0, 0)));
        Assert.Equal(ownerVersion + 1, owner.PrismVisualVersion);

        ownerVersion = owner.PrismVisualVersion;
        child.Resources.SetResource(
            new ResourceId<object>("PixelResource"),
            new object());
        Assert.Equal(ownerVersion + 1, owner.PrismVisualVersion);

        ownerVersion = owner.PrismVisualVersion;
        child.SetPresenceVisual(0.6f, 0.8f);
        Assert.Equal(ownerVersion + 1, owner.PrismVisualVersion);

        child.SetPresenceVisual(0.6f, 0.8f);
        Assert.Equal(ownerVersion + 1, owner.PrismVisualVersion);
    }

    [Fact]
    public void PresenceExitKeepsPropagationToOwningPrism()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        RenderingTestElement owner = new(new Color(1, 0, 0));
        RenderingTestElement child = new(new Color(2, 0, 0))
        {
            Presence = PresenceOptions.FadeAndScale(
                MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(1)),
                MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)))
        };
        owner.VisualChildren.Add(child);
        root.VisualChildren.Add(owner);
        using IDisposable prism = AttachPrism(owner, "Presence owner");
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(2));
        root.ProcessFrame();

        Assert.True(owner.VisualChildren.Remove(child));
        Assert.Null(child.VisualParent);
        Assert.Contains(
            child,
            root.Motion.Presence.GetExitingVisualChildren(owner));
        long ownerVersion = owner.PrismVisualVersion;

        child.SetPresenceVisual(0.37f, 0.81f);

        Assert.Equal(ownerVersion + 1, owner.PrismVisualVersion);
    }

    [Fact]
    public void PrismValueAndStructuralVersionsStaySeparateFromControlContent()
    {
        UIRoot root = new(100, 100);
        RenderingTestElement owner = new(new Color(1, 0, 0));
        root.VisualChildren.Add(owner);
        PrismInstance instance = new(
            PrismTestData.Composition(
                "Versioned",
                PrismTestData.Layer(1, "Content")));
        using IDisposable prism = GeneratedMarkup.AttachPrism(
            owner,
            () => instance);
        root.ProcessFrame();
        PrismStructuralVersion structuralVersion =
            instance.StructuralVersion;
        PrismValueVersion valueVersion = instance.ValueVersion;
        long visualVersion = owner.PrismVisualVersion;

        PrismLayerState layer =
            instance.GetLayerState(new PrismNodeId(1));
        layer.Opacity = 0.5f;

        Assert.Equal(structuralVersion, instance.StructuralVersion);
        Assert.NotEqual(valueVersion, instance.ValueVersion);
        Assert.Equal(visualVersion, owner.PrismVisualVersion);

        PrismValueVersion changedValueVersion = instance.ValueVersion;
        layer.Opacity = 0.5f;
        Assert.Equal(changedValueVersion, instance.ValueVersion);

        instance.ReplaceDefinition(
            PrismTestData.Composition(
                "Versioned",
                PrismTestData.Layer(1, "Content"),
                PrismTestData.Layer(2, "Overlay")));

        Assert.NotEqual(structuralVersion, instance.StructuralVersion);
        Assert.Equal(changedValueVersion, instance.ValueVersion);
        Assert.Equal(visualVersion, owner.PrismVisualVersion);
    }

    [Fact]
    public void ResourcesRequireStableVersionsWithoutIdentityFallback()
    {
        PrismResourceId unversionedId = new("Unversioned");
        PrismResourceId versionedId = new("Versioned");
        TestImage image = new(8, 6);
        PrismDrawResources mixed = PrismDrawResources.Create(
            [
                new PrismDrawImageResource(unversionedId, image),
                new PrismDrawImageResource(versionedId, image, 7)
            ]);

        Assert.False(mixed.HasStableVersions);
        Assert.True(mixed.TryGetVersion(unversionedId, out long zeroVersion));
        Assert.Equal(0, zeroVersion);
        Assert.True(mixed.TryGetVersion(versionedId, out long stableVersion));
        Assert.Equal(7, stableVersion);

        PrismDrawResources stable = PrismDrawResources.Create(
            [new PrismDrawImageResource(versionedId, image, 8)]);
        Assert.True(stable.HasStableVersions);
        Assert.True(PrismDrawResources.Empty.HasStableVersions);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PrismDrawResources.Create(
                [new PrismDrawImageResource(versionedId, image, -1)]));
    }

    [Fact]
    public void BackdropDependencyUsesHostAndOnlyLowerPaintVersions()
    {
        UIRoot root = new(100, 100);
        RenderingTestElement lower = new(new Color(1, 0, 0));
        RenderingTestElement owner = new(new Color(2, 0, 0));
        RenderingTestElement upper = new(new Color(3, 0, 0));
        root.VisualChildren.Add(lower);
        root.VisualChildren.Add(owner);
        root.VisualChildren.Add(upper);
        using IDisposable prism = GeneratedMarkup.AttachPrism(
            owner,
            () => new PrismInstance(
                PrismTestData.Composition(
                    "Backdrop",
                    PrismTestData.Layer(1, "Content"),
                    PrismTestData.Backdrop(2, "Backdrop"))));
        RetainedRenderCache cache = new();
        RenderCounters counters = new();
        PrepareSubtree(root, cache, counters);

        BackdropSnapshot initial = BuildBackdropSnapshot(
            root,
            owner,
            cache,
            counters,
            contentVersion: 41);
        upper.Opacity = 0.75f;
        BackdropSnapshot upperChanged = BuildBackdropSnapshot(
            root,
            owner,
            cache,
            counters,
            contentVersion: 41);

        Assert.Equal(initial.LowerUiVersion, upperChanged.LowerUiVersion);
        Assert.Equal(
            initial.BackdropDependencyVersion,
            upperChanged.BackdropDependencyVersion);

        lower.Opacity = 0.75f;
        BackdropSnapshot lowerChanged = BuildBackdropSnapshot(
            root,
            owner,
            cache,
            counters,
            contentVersion: 41);

        Assert.NotEqual(initial.LowerUiVersion, lowerChanged.LowerUiVersion);
        Assert.NotEqual(
            initial.BackdropDependencyVersion,
            lowerChanged.BackdropDependencyVersion);
        Assert.Equal(owner.PrismVisualVersion, lowerChanged.VisualContentVersion);

        BackdropSnapshot hostChanged = BuildBackdropSnapshot(
            root,
            owner,
            cache,
            counters,
            contentVersion: 42);
        Assert.Equal(lowerChanged.LowerUiVersion, hostChanged.LowerUiVersion);
        Assert.NotEqual(
            lowerChanged.BackdropDependencyVersion,
            hostChanged.BackdropDependencyVersion);
    }

    private static BackdropSnapshot BuildBackdropSnapshot(
        UIElement root,
        UIElement owner,
        RetainedRenderCache cache,
        RenderCounters counters,
        long contentVersion)
    {
        new DrawCommandListBuilder().Build(root, cache, counters);
        PrismDrawScope scope = Assert.Single(
            cache.RootCommands
                .Where(command =>
                    command.Kind == DrawCommandKind.BeginPrism)
                .Select(command => command.PrismScope!.Value));
        PrismFrameAnalysis analysis =
            new PrismFrameAnalyzer().Analyze(cache.RootCommands);
        PrismGraph graph = new PrismGraphBuilder().Build(
            analysis,
            CreateBackdropMetadata(contentVersion));
        PrismGraphNode input = Assert.Single(
            graph.Nodes.Where(
                node =>
                    node.Kind == PrismGraphNodeKind.BackdropInput));
        PrismGraphDependency dependency = Assert.Single(
            input.Dependencies.Where(
                candidate =>
                    candidate.Kind ==
                    PrismGraphDependencyKind.BackdropFrame));

        Assert.Equal(owner.PrismVisualVersion, scope.VisualContentVersion);
        return new BackdropSnapshot(
            scope.LowerUiVersion,
            scope.VisualContentVersion,
            dependency.Version);
    }

    private static BackdropFrameMetadata CreateBackdropMetadata(
        long contentVersion)
    {
        return new BackdropFrameMetadata(
            100,
            100,
            1,
            PrismColorProfile.DisplayP3,
            BackdropPixelFormat.Rgba8Unorm,
            BackdropAlphaMode.Premultiplied,
            System.Numerics.Matrix3x2.Identity,
            contentVersion);
    }

    private static IDisposable AttachPrism(
        UIElement element,
        string compositionName)
    {
        return GeneratedMarkup.AttachPrism(
            element,
            () => new PrismInstance(
                PrismTestData.Composition(
                    compositionName,
                    PrismTestData.Layer(1, "Content"))));
    }

    private static void PrepareSubtree(
        UIElement element,
        RetainedRenderCache cache,
        RenderCounters counters)
    {
        cache.GetElementCache(element)
            .Ensure(element, counters, forceRebuild: true);
        foreach (UIElement child in element.VisualChildren)
        {
            PrepareSubtree(child, cache, counters);
        }
    }

    private readonly record struct BackdropSnapshot(
        long LowerUiVersion,
        long VisualContentVersion,
        long BackdropDependencyVersion);

    private sealed record TestImage(
        int Width,
        int Height) : IDrawImage;
}

using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Markup;
using Cerneala.UI.Media;
using Cerneala.UI.Motion.Presence;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;
using Cerneala.Tests.UI.Motion.Core;
using Cerneala.Tests.UI.Rendering;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;
using NumericsMatrix3x2 = System.Numerics.Matrix3x2;
using UiMatrix3x2 = Cerneala.UI.Media.Matrix3x2;

namespace Cerneala.Tests.Drawing.Prism;

public sealed class PrismRetainedCommandContractTests
{
    [Fact]
    public void BeginAndEndCommandsCarryTypedScopeOnlyAtBegin()
    {
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition("Card", PrismTestData.Layer(1, "Content")),
            ownerToken: 42,
            bounds: new DrawRect(3, 4, 20, 10),
            transform: NumericsMatrix3x2.CreateTranslation(7, 8),
            pixelScale: 2,
            visualContentVersion: 9);

        DrawCommand begin = DrawCommand.BeginPrism(scope);
        DrawCommand end = DrawCommand.EndPrism();

        Assert.Equal(DrawCommandKind.BeginPrism, begin.Kind);
        Assert.Equal(scope, begin.PrismScope);
        Assert.Equal(DrawCommandKind.EndPrism, end.Kind);
        Assert.Null(end.PrismScope);
        Assert.Same(scope.Instance, begin.PrismScope!.Value.Instance);
        Assert.Equal(new PrismCacheOwnerToken(42), begin.PrismScope.Value.CacheOwnerToken);
    }

    [Fact]
    public void BuilderEmitsClipAndPrismAroundTheOwnedVisualSubtree()
    {
        UIRoot root = new(100, 100);
        RenderingTestElement owner = new(new Color(1, 0, 0));
        RenderingTestElement child = new(new Color(2, 0, 0));
        owner.VisualChildren.Add(child);
        root.VisualChildren.Add(owner);
        ClipNode.SetClip(owner, new LayoutRect(0, 0, 10, 10));
        using IDisposable prism = AttachPrism(owner, "Clipped");
        RetainedRenderCache cache = PreparedCache(root);

        new DrawCommandListBuilder().Build(root, cache, new RenderCounters());

        Assert.Equal(
            [
                DrawCommandKind.PushClip,
                DrawCommandKind.BeginPrism,
                DrawCommandKind.FillRectangle,
                DrawCommandKind.FillRectangle,
                DrawCommandKind.EndPrism,
                DrawCommandKind.PopClip
            ],
            cache.RootCommands.Select(command => command.Kind));
        Assert.Equal(
            [new Color(1, 0, 0), new Color(2, 0, 0)],
            cache.RootCommands
                .Where(command => command.Kind == DrawCommandKind.FillRectangle)
                .Select(command => command.Color));
    }

    [Fact]
    public void BuilderNestsParentAndChildScopesAndPreservesRenderState()
    {
        UIRoot root = new(100, 100, scale: 2);
        RenderingTestElement parent = new(new Color(10, 0, 0))
        {
            Opacity = 0.5f,
            RenderTransform = new Transform(UiMatrix3x2.CreateTranslation(4, 5))
        };
        RenderingTestElement child = new(new Color(20, 0, 0))
        {
            Opacity = 0.5f,
            RenderTransform = new Transform(UiMatrix3x2.CreateTranslation(2, 3))
        };
        parent.VisualChildren.Add(child);
        root.VisualChildren.Add(parent);
        using IDisposable parentPrism = AttachPrism(parent, "Parent");
        using IDisposable childPrism = AttachPrism(child, "Child");
        RetainedRenderCache cache = PreparedCache(root);

        new DrawCommandListBuilder().Build(root, cache, new RenderCounters());

        Assert.Equal(
            [
                DrawCommandKind.BeginPrism,
                DrawCommandKind.FillRectangle,
                DrawCommandKind.BeginPrism,
                DrawCommandKind.FillRectangle,
                DrawCommandKind.EndPrism,
                DrawCommandKind.EndPrism
            ],
            cache.RootCommands.Select(command => command.Kind));

        PrismDrawScope parentScope = cache.RootCommands[0].PrismScope!.Value;
        PrismDrawScope childScope = cache.RootCommands[2].PrismScope!.Value;
        Assert.Equal(new DrawRect(0, 0, 10, 10), parentScope.ControlBounds);
        Assert.Equal(2, parentScope.PixelScale);
        Assert.Equal(4, parentScope.EffectiveTransform.M31);
        Assert.Equal(5, parentScope.EffectiveTransform.M32);
        Assert.Equal(6, childScope.EffectiveTransform.M31);
        Assert.Equal(8, childScope.EffectiveTransform.M32);
        Assert.Equal(parentScope.Instance.StructuralVersion, parentScope.StructuralVersion);
        Assert.Equal(parentScope.Instance.ValueVersion, parentScope.ValueVersion);
        Assert.True(parentScope.VisualContentVersion > 0);
        Assert.True(parentScope.CacheOwnerToken.Value > 0);
        Assert.Equal(128, cache.RootCommands[1].Color.A);
        Assert.Equal(64, cache.RootCommands[3].Color.A);
    }

    [Fact]
    public void BuilderSnapshotsNamedMaskImagesIntoThePrismScope()
    {
        PrismResourceId maskId = new("CardMask");
        TestImage image = new(8, 6);
        UIRoot root = new(100, 100);
        RenderingTestElement owner = new(Color.White);
        owner.Resources.SetResource(
            new ResourceId<ImageResource>(maskId.Key!),
            new ImageResource(image));
        root.VisualChildren.Add(owner);
        using IDisposable prism = GeneratedMarkup.AttachPrism(
            owner,
            () => new PrismInstance(
                PrismTestData.Composition(
                    "Masked card",
                    PrismTestData.Layer(
                        1,
                        "Content",
                        mask: new PrismMaskDefinition(maskId)))));
        RetainedRenderCache cache = PreparedCache(root);

        new DrawCommandListBuilder().Build(
            root,
            cache,
            new RenderCounters());

        PrismDrawScope scope = Assert.Single(
            cache.RootCommands.Where(
                command =>
                    command.Kind == DrawCommandKind.BeginPrism))
            .PrismScope!.Value;
        Assert.True(
            scope.Resources.TryGetImage(
                maskId,
                out IDrawImage resolved));
        Assert.Same(image, resolved);
        Assert.True(scope.Resources.HasStableVersions);
    }

    [Fact]
    public void BuilderSnapshotsMarkupImageBrushesIntoThePrismScope()
    {
        PrismResourceId maskId = new("MarkupMask");
        TestImage image = new(9, 7);
        UIRoot root = new(100, 100);
        RenderingTestElement owner = new(Color.White);
        owner.Resources.Add(maskId.Key!, new ImageBrush(image));
        root.VisualChildren.Add(owner);
        using IDisposable prism = GeneratedMarkup.AttachPrism(
            owner,
            () => new PrismInstance(
                PrismTestData.Composition(
                    "Markup mask",
                    PrismTestData.Layer(
                        1,
                        "Content",
                        mask: new PrismMaskDefinition(maskId)))));
        RetainedRenderCache cache = PreparedCache(root);

        new DrawCommandListBuilder().Build(
            root,
            cache,
            new RenderCounters());

        PrismDrawScope scope = Assert.Single(
            cache.RootCommands.Where(
                command =>
                    command.Kind == DrawCommandKind.BeginPrism))
            .PrismScope!.Value;
        Assert.True(
            scope.Resources.TryGetImage(
                maskId,
                out IDrawImage resolved));
        Assert.Same(image, resolved);
    }

    [Fact]
    public void PresenceExitingChildrenRenderInsideTheOwningPrismAfterLiveChildren()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        RenderingTestElement owner = new(new Color(1, 0, 0));
        RenderingTestElement live = new(new Color(2, 0, 0));
        RenderingTestElement exiting = new(new Color(3, 0, 0))
        {
            Presence = PresenceOptions.FadeAndScale(
                MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(1)),
                MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)))
        };
        owner.VisualChildren.Add(live);
        owner.VisualChildren.Add(exiting);
        root.VisualChildren.Add(owner);
        using IDisposable prism = AttachPrism(owner, "Presence");

        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(2));
        root.ProcessFrame();
        RetainedRenderCache cache = PreparedCache(root);
        Assert.True(owner.VisualChildren.Remove(exiting));
        Assert.Contains(exiting, root.Motion.Presence.GetExitingVisualChildren(owner));
        RenderCounters counters = new();
        PrepareSubtree(root, cache, counters);
        PrepareSubtree(exiting, cache, counters);

        new DrawCommandListBuilder().Build(root, cache, counters);

        DrawCommand[] commands = cache.RootCommands.ToArray();
        int begin = Array.FindIndex(commands, command => command.Kind == DrawCommandKind.BeginPrism);
        int end = Array.FindIndex(commands, command => command.Kind == DrawCommandKind.EndPrism);
        int ownerCommand = FindColor(commands, new Color(1, 0, 0));
        int liveCommand = FindColor(commands, new Color(2, 0, 0));
        int exitingCommand = FindColor(commands, new Color(3, 0, 0));
        Assert.True(begin < ownerCommand);
        Assert.True(ownerCommand < liveCommand);
        Assert.True(liveCommand < exitingCommand);
        Assert.True(exitingCommand < end);
    }

    [Fact]
    public void BuilderOmitsScopesForPlainAndNonRenderableOwnersButKeepsInvisiblePrismLayers()
    {
        UIRoot root = new(100, 100);
        RenderingTestElement plain = new(new Color(1, 0, 0));
        RenderingTestElement hiddenOwner = new(new Color(2, 0, 0))
        {
            Visibility = Visibility.Hidden
        };
        RenderingTestElement invisibleLayerOwner = new(new Color(3, 0, 0));
        root.VisualChildren.Add(plain);
        root.VisualChildren.Add(hiddenOwner);
        root.VisualChildren.Add(invisibleLayerOwner);
        using IDisposable hiddenPrism = AttachPrism(hiddenOwner, "HiddenOwner");
        using IDisposable invisibleLayerPrism = GeneratedMarkup.AttachPrism(
            invisibleLayerOwner,
            () => new PrismInstance(
                PrismTestData.Composition(
                    "InvisibleLayer",
                    PrismTestData.Layer(1, "Layer", visible: false))));
        RetainedRenderCache cache = PreparedCache(root);

        new DrawCommandListBuilder().Build(root, cache, new RenderCounters());

        DrawCommand begin = Assert.Single(
            cache.RootCommands.Where(command => command.Kind == DrawCommandKind.BeginPrism));
        Assert.Equal("InvisibleLayer", begin.PrismScope!.Value.Definition.Name);
        Assert.Contains(
            cache.RootCommands,
            command => command.Kind == DrawCommandKind.FillRectangle &&
                command.Color == new Color(1, 0, 0));
        Assert.DoesNotContain(
            cache.RootCommands,
            command => command.Kind == DrawCommandKind.FillRectangle &&
                command.Color == new Color(2, 0, 0));
    }

    [Fact]
    public void PrismParameterChangeReusesStructuralAndElementCommands()
    {
        UIRoot root = new(100, 100);
        RenderingTestElement owner = new(new Color(1, 0, 0));
        root.VisualChildren.Add(owner);
        using IDisposable prism = AttachPrism(owner, "Animated");
        RetainedRenderCache cache = PreparedCache(root);
        RetainedRenderer renderer = new(
            cache,
            new DrawCommandListBuilder(),
            new RenderCounters());

        DrawCommandList first = renderer.Commit(root);
        long commandListVersion = first.Version;
        int retainedCacheVersion = cache.Version;
        ElementRenderCache elementCache = cache.GetElementCache(owner);
        int elementRenderVersion = elementCache.RenderVersion;
        DrawCommandKind[] commandKinds = first.Select(command => command.Kind).ToArray();
        PrismDrawScope firstScope = Assert.Single(
            first.Where(command => command.Kind == DrawCommandKind.BeginPrism))
            .PrismScope!.Value;
        PrismValueVersion valueVersion = firstScope.ValueVersion;
        long visualContentVersion = firstScope.VisualContentVersion;

        firstScope.Instance.GetLayerState(new PrismNodeId(1)).Opacity = 0.5f;
        DrawCommandList second = renderer.Commit(root);
        PrismDrawScope secondScope = Assert.Single(
            second.Where(command => command.Kind == DrawCommandKind.BeginPrism))
            .PrismScope!.Value;

        Assert.Same(first, second);
        Assert.Equal(commandListVersion, second.Version);
        Assert.Equal(retainedCacheVersion, cache.Version);
        Assert.Equal(commandKinds, second.Select(command => command.Kind));
        Assert.Equal(elementRenderVersion, elementCache.RenderVersion);
        Assert.True(elementCache.IsValid);
        Assert.NotEqual(valueVersion, secondScope.ValueVersion);
        Assert.Equal(visualContentVersion, secondScope.VisualContentVersion);
    }

    [Fact]
    public void ThousandsOfAnimatedParameterCommitsReuseRetainedCommands()
    {
        const int frameCount = 4_096;
        UIRoot root = new(100, 100);
        RenderingTestElement owner = new(new Color(1, 0, 0));
        root.VisualChildren.Add(owner);
        using IDisposable prism = AttachPrism(owner, "AnimatedStress");
        RetainedRenderCache cache = PreparedCache(root);
        RenderCounters counters = new();
        RetainedRenderer renderer = new(
            cache,
            new DrawCommandListBuilder(),
            counters);
        DrawCommandList commands = renderer.Commit(root);
        long commandListVersion = commands.Version;
        int retainedCacheVersion = cache.Version;
        ElementRenderCache elementCache =
            cache.GetElementCache(owner);
        int elementRenderVersion = elementCache.RenderVersion;
        int missesAfterWarmup = counters.CacheMisses;
        int rebuildsAfterWarmup = counters.LocalRebuilds;
        int hitsAfterWarmup = counters.CacheHits;
        PrismInstance instance = Assert.Single(
            commands.Where(
                command =>
                    command.Kind == DrawCommandKind.BeginPrism))
            .PrismScope!.Value.Instance;
        PrismLayerState layer =
            instance.GetLayerState(new PrismNodeId(1));

        for (int frame = 0; frame < frameCount; frame++)
        {
            layer.Opacity =
                (frame & 1) == 0 ? 0.25f : 0.75f;
            Assert.Same(commands, renderer.Commit(root));
        }

        Assert.Equal(commandListVersion, commands.Version);
        Assert.Equal(retainedCacheVersion, cache.Version);
        Assert.Equal(elementRenderVersion, elementCache.RenderVersion);
        Assert.True(elementCache.IsValid);
        Assert.Equal(missesAfterWarmup, counters.CacheMisses);
        Assert.Equal(rebuildsAfterWarmup, counters.LocalRebuilds);
        Assert.Equal(hitsAfterWarmup, counters.CacheHits);
        Console.WriteLine(
            $"PRISM_RETAINED frames={frameCount} " +
            $"hits={counters.CacheHits - hitsAfterWarmup} " +
            $"misses={counters.CacheMisses - missesAfterWarmup} " +
            $"rebuilds={counters.LocalRebuilds - rebuildsAfterWarmup}");
    }

    [Fact]
    public void FallbackBackendIgnoresPrismDelimitersAndProcessesInteriorCommands()
    {
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition("Fallback", PrismTestData.Layer(1, "Content")));
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.PushClip(new DrawRect(0, 0, 10, 10)),
            DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(new DrawRect(0, 0, 4, 4), Color.White),
            DrawCommand.EndPrism(),
            DrawCommand.PopClip());
        FallbackDrawingBackend backend = new();
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        DrawingFrameContext frameContext = new(analysis);

        backend.Render(commands, in frameContext);

        Assert.Equal(
            [DrawCommandKind.PushClip, DrawCommandKind.FillRectangle, DrawCommandKind.PopClip],
            backend.ExecutedKinds);
    }

    [Fact]
    public void AnalyzerHandlesZeroOneNestedAndSiblingScopesInOnePass()
    {
        PrismFrameAnalyzer analyzer = new();
        PrismFrameAnalysis empty = analyzer.Analyze(new DrawCommandList());
        Assert.Empty(empty.Scopes);
        Assert.False(empty.RequiresBackdrop);

        PrismDrawScope outer = PrismTestData.Scope(
            PrismTestData.Composition("Outer", PrismTestData.Layer(1, "Outer")),
            ownerToken: 11,
            bounds: new DrawRect(1, 2, 30, 20),
            visualContentVersion: 7);
        PrismDrawScope inner = PrismTestData.Scope(
            PrismTestData.Composition("Inner", PrismTestData.Layer(2, "Inner")),
            ownerToken: 12);
        PrismDrawScope sibling = PrismTestData.Scope(
            PrismTestData.Composition("Sibling", PrismTestData.Layer(3, "Sibling")),
            ownerToken: 13);
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(outer),
            DrawCommand.FillRectangle(new DrawRect(0, 0, 1, 1), Color.White),
            DrawCommand.BeginPrism(inner),
            DrawCommand.FillRectangle(new DrawRect(0, 0, 1, 1), Color.White),
            DrawCommand.EndPrism(),
            DrawCommand.EndPrism(),
            DrawCommand.BeginPrism(sibling),
            DrawCommand.EndPrism());

        PrismFrameAnalysis analysis = analyzer.Analyze(commands);

        Assert.Equal(commands.Version, analysis.CommandListVersion);
        Assert.Equal(3, analysis.Scopes.Length);
        Assert.Equal([0, 2, 6], analysis.Scopes.Select(scope => scope.BeginCommandIndex));
        Assert.Equal([5, 4, 7], analysis.Scopes.Select(scope => scope.EndCommandIndex));
        Assert.Equal([0, 1, 0], analysis.Scopes.Select(scope => scope.Depth));
        Assert.Null(analysis.Scopes[0].ParentScopeIndex);
        Assert.Equal(0, analysis.Scopes[1].ParentScopeIndex);
        Assert.Null(analysis.Scopes[2].ParentScopeIndex);
        Assert.Equal(new DrawRect(1, 2, 30, 20), analysis.Scopes[0].Bounds);
        Assert.Equal(new PrismCacheOwnerToken(11), analysis.Scopes[0].DependencyStamp.CacheOwnerToken);
        Assert.Equal(7, analysis.Scopes[0].DependencyStamp.VisualContentVersion);
        Assert.True(
            analysis.Scopes[0].RequiredCapabilities.HasFlag(
                PrismGraphCapabilities.FilterProcessing));
        Assert.True(analysis.Scopes[0].RequiredSurfaceCount > 0);
    }

    [Fact]
    public void AnalyzerRejectsUnderflowDanglingBeginAndStaleSameSizedLists()
    {
        PrismFrameAnalyzer analyzer = new();
        InvalidOperationException underflow = Assert.Throws<InvalidOperationException>(
            () => analyzer.Analyze(PrismTestData.Commands(DrawCommand.EndPrism())));
        Assert.Contains("0", underflow.Message, StringComparison.Ordinal);

        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition("Invalid", PrismTestData.Layer(1, "Content")));
        InvalidOperationException dangling = Assert.Throws<InvalidOperationException>(
            () => analyzer.Analyze(PrismTestData.Commands(DrawCommand.BeginPrism(scope))));
        Assert.Contains("0", dangling.Message, StringComparison.Ordinal);

        InvalidOperationException crossedNesting = Assert.Throws<InvalidOperationException>(
            () => analyzer.Analyze(
                PrismTestData.Commands(
                    DrawCommand.BeginPrism(scope),
                    DrawCommand.PushClip(new DrawRect(0, 0, 1, 1)),
                    DrawCommand.EndPrism(),
                    DrawCommand.PopClip())));
        Assert.Contains("clip", crossedNesting.Message, StringComparison.OrdinalIgnoreCase);

        DrawCommandList reusable = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(new DrawRect(0, 0, 1, 1), Color.White),
            DrawCommand.EndPrism());
        PrismFrameAnalysis analysis = analyzer.Analyze(reusable);
        reusable.Clear();
        reusable.Add(DrawCommand.BeginPrism(scope));
        reusable.Add(DrawCommand.FillRectangle(new DrawRect(1, 1, 1, 1), Color.Black));
        reusable.Add(DrawCommand.EndPrism());

        Assert.Equal(3, reusable.Count);
        Assert.Throws<InvalidOperationException>(() => analysis.EnsureCurrent(reusable));
    }

    [Fact]
    public void AnalyzerAggregatesMultipleBackdropScopesIntoOneFrameRequirement()
    {
        PrismDrawScope first = PrismTestData.Scope(
            PrismTestData.Composition(
                "First",
                PrismTestData.Layer(1, "Content"),
                PrismTestData.Backdrop(2, "Backdrop")),
            ownerToken: 21);
        PrismDrawScope second = PrismTestData.Scope(
            PrismTestData.Composition(
                "Second",
                PrismTestData.Layer(3, "Content"),
                PrismTestData.Backdrop(4, "Backdrop")),
            ownerToken: 22);
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(first),
            DrawCommand.EndPrism(),
            DrawCommand.BeginPrism(second),
            DrawCommand.EndPrism());

        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);

        Assert.True(analysis.RequiresBackdrop);
        PrismBackdropRequirement requirement = Assert.IsType<PrismBackdropRequirement>(
            analysis.BackdropRequirement);
        Assert.Equal(2, requirement.ScopeCount);
        Assert.Equal([0, 1], requirement.ScopeIndices);
    }

    [Fact]
    public void AnalyzerAppliesTransformAndActiveClipToScopeBounds()
    {
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "Bounds",
                PrismTestData.Layer(1, "Content")),
            bounds: new DrawRect(0, 0, 10, 10),
            transform: NumericsMatrix3x2.CreateTranslation(5, 6));
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.PushClip(new DrawRect(8, 8, 4, 4)),
            DrawCommand.BeginPrism(scope),
            DrawCommand.EndPrism(),
            DrawCommand.PopClip());

        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);

        Assert.Equal(new DrawRect(8, 8, 4, 4), Assert.Single(analysis.Scopes).Bounds);
    }

    [Fact]
    public void AnalyzerTracksNestedValueDependenciesWithoutRebuildingCommands()
    {
        PrismDrawScope outer = PrismTestData.Scope(
            PrismTestData.Composition(
                "OuterDependency",
                PrismTestData.Layer(1, "Content")),
            ownerToken: 31);
        PrismDrawScope inner = PrismTestData.Scope(
            PrismTestData.Composition(
                "InnerDependency",
                PrismTestData.Layer(2, "Content")),
            ownerToken: 32);
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(outer),
            DrawCommand.BeginPrism(inner),
            DrawCommand.EndPrism(),
            DrawCommand.EndPrism());
        PrismFrameAnalyzer analyzer = new();
        PrismFrameAnalysis first = analyzer.Analyze(commands);
        long commandListVersion = commands.Version;
        long outerDescendantVersion =
            first.Scopes[0].DependencyStamp.DescendantVersion;

        inner.Instance.GetLayerState(new PrismNodeId(2)).Opacity = 0.5f;

        Assert.Throws<InvalidOperationException>(
            () => first.EnsureCurrent(commands));
        PrismFrameAnalysis second = analyzer.Analyze(commands);
        Assert.Equal(commandListVersion, commands.Version);
        Assert.NotEqual(
            outerDescendantVersion,
            second.Scopes[0].DependencyStamp.DescendantVersion);
    }

    [Fact]
    public void AnalyzerSkipsBackdropWhenScopeIsClippedOut()
    {
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "ClippedBackdrop",
                PrismTestData.Layer(1, "Content"),
                PrismTestData.Backdrop(2, "Backdrop")),
            bounds: new DrawRect(20, 20, 5, 5));
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.PushClip(new DrawRect(0, 0, 10, 10)),
            DrawCommand.BeginPrism(scope),
            DrawCommand.EndPrism(),
            DrawCommand.PopClip());

        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);

        Assert.False(analysis.RequiresBackdrop);
        Assert.Equal(0, Assert.Single(analysis.Scopes).RequiredSurfaceCount);
    }

    private static IDisposable AttachPrism(UIElement element, string compositionName)
    {
        return GeneratedMarkup.AttachPrism(
            element,
            () => new PrismInstance(
                PrismTestData.Composition(
                    compositionName,
                    PrismTestData.Layer(1, "Content"))));
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
        cache.GetElementCache(element).Ensure(element, counters, forceRebuild: true);
        foreach (UIElement child in element.VisualChildren)
        {
            PrepareSubtree(child, cache, counters);
        }
    }

    private static int FindColor(DrawCommand[] commands, Color color)
    {
        return Array.FindIndex(
            commands,
            command => command.Kind == DrawCommandKind.FillRectangle && command.Color == color);
    }

    private sealed class FallbackDrawingBackend : IDrawingBackend
    {
        public List<DrawCommandKind> ExecutedKinds { get; } = [];

        public void Render(
            DrawCommandList commands,
            in DrawingFrameContext frameContext)
        {
            frameContext.EnsureCurrent(commands);
            foreach (DrawCommand command in commands)
            {
                switch (command.Kind)
                {
                    case DrawCommandKind.BeginPrism:
                    case DrawCommandKind.EndPrism:
                        break;
                    default:
                        ExecutedKinds.Add(command.Kind);
                        break;
                }
            }
        }
    }

    private sealed record TestImage(
        int Width,
        int Height) : IDrawImage;
}

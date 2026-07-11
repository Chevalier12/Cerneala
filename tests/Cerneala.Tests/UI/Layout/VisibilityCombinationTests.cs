using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Layout;

public sealed class VisibilityCombinationTests
{
    public static TheoryData<VisibilityCase> ParticipationCases => new()
    {
        VisibleCase,
        HiddenCase,
        CollapsedCase,
        RuntimeInvisibleCase
    };

    [Theory]
    [MemberData(nameof(ParticipationCases))]
    public void VisibilityCombinationControlsLayoutParticipation(VisibilityCase visibilityCase)
    {
        VisibilityProbeElement element = Probe(visibilityCase);

        LayoutSize desired = element.Measure(new MeasureContext(new LayoutSize(100, 100)));
        LayoutRect arranged = element.Arrange(new ArrangeContext(new LayoutRect(0, 0, 50, 50)));

        Assert.Equal(visibilityCase.ParticipatesInLayout, UIElementVisibility.ParticipatesInLayout(element));
        Assert.Equal(visibilityCase.ParticipatesInLayout ? new LayoutSize(20, 10) : LayoutSize.Zero, desired);
        Assert.Equal(visibilityCase.ParticipatesInLayout ? 1 : 0, element.MeasureCount);
        Assert.Equal(visibilityCase.ParticipatesInLayout ? new LayoutRect(0, 0, 50, 50) : new LayoutRect(0, 0, 0, 0), arranged);
        Assert.Equal(visibilityCase.ParticipatesInLayout ? 1 : 0, element.ArrangeCount);
    }

    [Theory]
    [MemberData(nameof(ParticipationCases))]
    public void VisibilityCombinationControlsRenderParticipation(VisibilityCase visibilityCase)
    {
        VisibilityProbeElement element = Probe(visibilityCase);
        RetainedRenderCache cache = PreparedCache(element);

        new DrawCommandListBuilder().Build(element, cache, new RenderCounters());

        Assert.Equal(visibilityCase.ParticipatesInRendering, UIElementVisibility.ParticipatesInRendering(element));
        Assert.Equal(visibilityCase.ParticipatesInRendering ? 1 : 0, cache.RootCommands.Count);
    }

    [Theory]
    [MemberData(nameof(ParticipationCases))]
    public void VisibilityCombinationControlsInputRouteParticipation(VisibilityCase visibilityCase)
    {
        UIRoot root = new();
        VisibilityProbeElement element = Probe(visibilityCase);
        root.VisualChildren.Add(element);

        ElementInputRouteMap routeMap = new ElementInputRouteBuilder().Build(root);

        Assert.Equal(visibilityCase.ParticipatesInInput, UIElementVisibility.ParticipatesInInput(element));
        Assert.Equal(visibilityCase.ParticipatesInInput, routeMap.TryGetId(element, out _));
    }

    [Theory]
    [MemberData(nameof(ParticipationCases))]
    public void VisibilityCombinationControlsHitTestParticipation(VisibilityCase visibilityCase)
    {
        UIRoot root = new(100, 100);
        VisibilityProbeElement element = Probe(visibilityCase);
        element.Arrange(new ArrangeContext(new LayoutRect(0, 0, 50, 50)));
        root.VisualChildren.Add(element);

        HitTestResult? result = new HitTestService().HitTest(root, 10, 10);

        Assert.Equal(visibilityCase.ParticipatesInHitTest, UIElementVisibility.ParticipatesInHitTest(element));
        Assert.Same(visibilityCase.ParticipatesInHitTest ? element : root, result!.Element);
    }

    private static VisibilityProbeElement Probe(VisibilityCase visibilityCase)
    {
        return new VisibilityProbeElement(new LayoutSize(20, 10))
        {
            IsVisible = visibilityCase.IsVisible,
            Visibility = visibilityCase.Visibility
        };
    }

    private static readonly VisibilityCase VisibleCase = new(
        "visible",
        true,
        Visibility.Visible,
        ParticipatesInLayout: true,
        ParticipatesInRendering: true,
        ParticipatesInInput: true,
        ParticipatesInHitTest: true);

    private static readonly VisibilityCase HiddenCase = new(
        "hidden",
        true,
        Visibility.Hidden,
        ParticipatesInLayout: true,
        ParticipatesInRendering: false,
        ParticipatesInInput: false,
        ParticipatesInHitTest: false);

    private static readonly VisibilityCase CollapsedCase = new(
        "collapsed",
        true,
        Visibility.Collapsed,
        ParticipatesInLayout: false,
        ParticipatesInRendering: false,
        ParticipatesInInput: false,
        ParticipatesInHitTest: false);

    private static readonly VisibilityCase RuntimeInvisibleCase = new(
        "runtime-invisible",
        false,
        Visibility.Visible,
        ParticipatesInLayout: true,
        ParticipatesInRendering: false,
        ParticipatesInInput: false,
        ParticipatesInHitTest: false);

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

    private sealed class VisibilityProbeElement(LayoutSize size) : UIElement
    {
        public int MeasureCount { get; private set; }

        public int ArrangeCount { get; private set; }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            MeasureCount++;
            return size;
        }

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            ArrangeCount++;
            return context.FinalRect;
        }

        protected override void OnRender(RenderContext context)
        {
            context.DrawingContext.FillRectangle(new DrawRect(context.Bounds.X, context.Bounds.Y, 1, 1), Color.White);
        }
    }

    public sealed record VisibilityCase(
        string Name,
        bool IsVisible,
        Visibility Visibility,
        bool ParticipatesInLayout,
        bool ParticipatesInRendering,
        bool ParticipatesInInput,
        bool ParticipatesInHitTest)
    {
        public override string ToString()
        {
            return Name;
        }
    }
}

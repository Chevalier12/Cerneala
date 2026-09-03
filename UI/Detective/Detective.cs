using System.Globalization;
using Cerneala.UI.Aspect;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Input;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Detective;

public sealed class Detective
{
    private readonly UIRoot root;

    internal Detective(UIRoot root, InvalidationTrace invalidation)
    {
        this.root = root ?? throw new ArgumentNullException(nameof(root));
        Invalidation = invalidation ?? throw new ArgumentNullException(nameof(invalidation));
    }

    public InvalidationTrace Invalidation { get; }

    public MotionDiagnostics Motion => root.Motion.Diagnostics;

    public AspectEngineCounters AspectCounters => root.AspectProcessor.Engine.Counters;

    public RenderCounters RenderingCounters => root.RenderCounters;

    public DetectiveSnapshot Capture(FrameStats stats)
    {
        ArgumentNullException.ThrowIfNull(stats);

        return new DetectiveSnapshot(
            new ViewportDiagnosticsSnapshot(root.ViewportWidth, root.ViewportHeight, root.Scale),
            FrameDiagnostics.Capture(stats),
            new RootInputDiagnosticsSnapshot(
                root.InputCache.IsDirty,
                root.InputCache.RebuildCount,
                root.InputCache.LastInvalidationReason),
            new RootRenderDiagnosticsSnapshot(
                root.RetainedRenderCache.IsRootValid,
                root.RetainedRenderCache.Version,
                root.RetainedRenderCache.RootCommands.Count),
            new ResourceDiagnosticsSnapshot(
                root.ImageResourceCache is not null,
                root.ImageResourceCache?.LoadCount),
            new PlatformDiagnosticsSnapshot(
                root.PlatformServices.Clipboard is not null,
                root.PlatformServices.Cursor is not null,
                root.PlatformServices.FileDialogs is not null,
                root.PlatformServices.TextInput is not null,
                root.PlatformServices.Dpi is not null,
                root.PlatformServices.Accessibility is not null),
            root.Motion.Diagnostics.CreateSnapshot(root.Motion));
    }

    public string Format(DetectiveSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return snapshot.ToString();
    }

    public FrameDiagnosticsSnapshot CaptureFrame(FrameStats stats) =>
        FrameDiagnostics.Capture(stats);

    public InputDiagnosticsSnapshot CaptureInput(UIElement? hitTarget, RoutedEvent? routedEvent = null) =>
        InputDiagnostics.Capture(hitTarget, routedEvent);

    public LayoutDiagnosticsSnapshot CaptureLayout(UIElement element) =>
        LayoutDiagnostics.Capture(element);

    public RootRenderDiagnosticsSnapshot CaptureRendering() =>
        RenderDiagnostics.CaptureRoot(root.RetainedRenderCache);

    public ElementRenderDiagnosticsSnapshot CaptureRendering(UIElement element) =>
        RenderDiagnostics.CaptureElement(element, root.RetainedRenderCache);

    public AspectDiagnostics.Snapshot CaptureAspect(UIElement element) =>
        root.AspectProcessor.Engine.GetDiagnostics(element);

    public AspectTraceSnapshot TraceAspect(UIElement element, UiProperty property) =>
        AspectTrace.Capture(element, property, CaptureAspect(element));

    public MotionGraphSnapshot CaptureMotion() =>
        root.Motion.Diagnostics.CreateSnapshot(root.Motion);

    public RoutedEventTraceSnapshot TraceRoutedEvent(
        UIElement target,
        RoutedEvent routedEvent,
        ElementChildRole role = ElementChildRole.Visual) =>
        RoutedEventTrace.Trace(target, routedEvent, role);
}

public sealed record DetectiveSnapshot(
    ViewportDiagnosticsSnapshot Viewport,
    FrameDiagnosticsSnapshot Frame,
    RootInputDiagnosticsSnapshot Input,
    RootRenderDiagnosticsSnapshot Rendering,
    ResourceDiagnosticsSnapshot Resources,
    PlatformDiagnosticsSnapshot Platform,
    MotionGraphSnapshot Motion)
{
    public override string ToString()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"detective viewport={Viewport.LogicalWidth}x{Viewport.LogicalHeight}, scale={Viewport.Scale}, frame inherited={Frame.InheritedElements}, commandState={Frame.CommandStateElements}, aspect={Frame.AspectElements}, queuedMeasure={Frame.QueuedMeasureElements}, queuedArrange={Frame.QueuedArrangeElements}, measureCalls={Frame.MeasureCalls}, arrangeCalls={Frame.ArrangeCalls}, renderCache={Frame.RenderedElements}, hitTest={Frame.HitTestElements}, reusedCaches={Frame.ReusedCaches}, noWork={Frame.NoWorkFrames}, motion={Frame.MotionFrames}, sampled={Frame.MotionNodesSampled}, motionValues={Frame.MotionValuesChanged}, motionWrites={Frame.MotionPropertyWrites}, completed={Frame.MotionCompleted}, motionRender={Frame.MotionRenderInvalidations}, motionLayout={Frame.MotionLayoutInvalidations}, reduced={Frame.MotionSkippedByReducedMotion}, hasWork={Frame.HasWork}, input dirty={Input.IsDirty}, inputRebuilds={Input.RebuildCount}, commands={Rendering.RootCommandCount}, imageCache={Resources.ImageCacheLoadCount?.ToString(CultureInfo.InvariantCulture) ?? "none"}, platform clipboard={Platform.HasClipboard}, cursor={Platform.HasCursor}");
    }
}

public sealed record ViewportDiagnosticsSnapshot(float LogicalWidth, float LogicalHeight, float Scale);

public sealed record RootInputDiagnosticsSnapshot(bool IsDirty, int RebuildCount, string LastInvalidationReason);

public sealed record ResourceDiagnosticsSnapshot(bool HasImageCache, int? ImageCacheLoadCount);

public sealed record PlatformDiagnosticsSnapshot(
    bool HasClipboard,
    bool HasCursor,
    bool HasFileDialogs,
    bool HasTextInput,
    bool HasDpi,
    bool HasAccessibility);

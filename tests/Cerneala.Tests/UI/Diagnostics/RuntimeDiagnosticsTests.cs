using Cerneala.Drawing;
using Cerneala.Tests.UI.Rendering;
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.UI.Diagnostics;

public sealed class RuntimeDiagnosticsTests
{
    [Fact]
    public void RuntimeDiagnosticsSnapshotIncludesViewportScale()
    {
        UIRoot root = new();
        UiViewport viewport = new(320, 180, 1.5f);
        FrameStats stats = new();

        RuntimeDiagnosticsSnapshot snapshot = RuntimeDiagnostics.Capture(root, viewport, stats);

        Assert.Equal(320, snapshot.Viewport.LogicalWidth);
        Assert.Equal(180, snapshot.Viewport.LogicalHeight);
        Assert.Equal(1.5f, snapshot.Viewport.Scale);
    }

    [Fact]
    public void RuntimeDiagnosticsSnapshotIncludesAllRetainedPhaseCounts()
    {
        UIRoot root = new();
        UiViewport viewport = new(100, 50);
        FrameStats stats = StatsWithEveryRetainedPhase();

        RuntimeDiagnosticsSnapshot snapshot = RuntimeDiagnostics.Capture(root, viewport, stats);

        Assert.Equal(1, snapshot.Frame.InheritedElements);
        Assert.Equal(1, snapshot.Frame.CommandStateElements);
        Assert.Equal(1, snapshot.Frame.AspectElements);
        Assert.Equal(1, snapshot.Frame.QueuedMeasureElements);
        Assert.Equal(1, snapshot.Frame.QueuedArrangeElements);
        Assert.Equal(2, snapshot.Frame.MeasureCalls);
        Assert.Equal(3, snapshot.Frame.ArrangeCalls);
        Assert.Equal(1, snapshot.Frame.RenderedElements);
        Assert.Equal(1, snapshot.Frame.HitTestElements);
        Assert.Equal(1, snapshot.Frame.ReusedCaches);
        Assert.Equal(0, snapshot.Frame.NoWorkFrames);
        Assert.True(snapshot.Frame.HasWork);
    }

    [Fact]
    public void RuntimeDiagnosticsSnapshotIncludesMotionFrameCounts()
    {
        UIRoot root = new();
        UiViewport viewport = new(100, 50);
        FrameStats stats = new();
        stats.CountMotion(new MotionFrameResult(
            new MotionFrame(TimeSpan.FromMilliseconds(16), TimeSpan.FromMilliseconds(16), 1, MotionFrameReason.Scheduled, MotionFramePhase.BeforeRender),
            NeedsAnotherFrame: true,
            MotionFrames: 1,
            MotionNodesSampled: 2,
            MotionValuesChanged: 3,
            MotionPropertyWrites: 4,
            MotionCompleted: 5,
            MotionRenderInvalidations: 6,
            MotionLayoutInvalidations: 7,
            MotionSkippedByReducedMotion: 8));

        RuntimeDiagnosticsSnapshot snapshot = RuntimeDiagnostics.Capture(root, viewport, stats);

        Assert.Equal(1, snapshot.Frame.MotionFrames);
        Assert.Equal(2, snapshot.Frame.MotionNodesSampled);
        Assert.Equal(3, snapshot.Frame.MotionValuesChanged);
        Assert.Equal(4, snapshot.Frame.MotionPropertyWrites);
        Assert.Equal(5, snapshot.Frame.MotionCompleted);
        Assert.Equal(6, snapshot.Frame.MotionRenderInvalidations);
        Assert.Equal(7, snapshot.Frame.MotionLayoutInvalidations);
        Assert.Equal(8, snapshot.Frame.MotionSkippedByReducedMotion);
        Assert.True(snapshot.Frame.HasWork);
    }

    [Fact]
    public void RuntimeDiagnosticsSnapshotIncludesRenderCommandCountWithoutRebuild()
    {
        UIRoot root = RootWithCommittedRenderCache(out RenderingTestElement child);
        UiViewport viewport = new(root.ViewportWidth, root.ViewportHeight, root.Scale);
        int renderCountBeforeCapture = child.RenderCount;
        int cacheVersionBeforeCapture = root.RetainedRenderCache.Version;

        RuntimeDiagnosticsSnapshot snapshot = RuntimeDiagnostics.Capture(root, viewport, new FrameStats());

        Assert.True(snapshot.Render.IsRootValid);
        Assert.Equal(cacheVersionBeforeCapture, snapshot.Render.RootVersion);
        Assert.Equal(1, snapshot.Render.RootCommandCount);
        Assert.Equal(renderCountBeforeCapture, child.RenderCount);
    }

    [Fact]
    public void RuntimeDiagnosticsFormatIncludesCommandStateAspectAndHitTestCounts()
    {
        UIRoot root = RootWithCommittedRenderCache(out _);
        UiViewport viewport = new(root.ViewportWidth, root.ViewportHeight, root.Scale);
        FrameStats stats = StatsWithEveryRetainedPhase();
        RuntimeDiagnosticsSnapshot snapshot = RuntimeDiagnostics.Capture(root, viewport, stats);

        string formatted = RuntimeDiagnostics.Format(snapshot);

        Assert.Contains("commandState=1", formatted, StringComparison.Ordinal);
        Assert.Contains("aspect=1", formatted, StringComparison.Ordinal);
        Assert.Contains("hitTest=1", formatted, StringComparison.Ordinal);
        Assert.Contains("commands=1", formatted, StringComparison.Ordinal);
    }

    [Fact]
    public void RuntimeDiagnosticsFormatIncludesMotionCounters()
    {
        UIRoot root = new();
        UiViewport viewport = new(100, 50);
        FrameStats stats = new();
        stats.CountMotion(new MotionFrameResult(
            new MotionFrame(TimeSpan.FromMilliseconds(16), TimeSpan.FromMilliseconds(16), 1, MotionFrameReason.Scheduled, MotionFramePhase.BeforeRender),
            NeedsAnotherFrame: true,
            MotionFrames: 1,
            MotionNodesSampled: 2,
            MotionValuesChanged: 3,
            MotionPropertyWrites: 4,
            MotionCompleted: 5,
            MotionRenderInvalidations: 6,
            MotionLayoutInvalidations: 7,
            MotionSkippedByReducedMotion: 8));
        RuntimeDiagnosticsSnapshot snapshot = RuntimeDiagnostics.Capture(root, viewport, stats);

        string formatted = RuntimeDiagnostics.Format(snapshot);

        Assert.Contains("motion=1", formatted, StringComparison.Ordinal);
        Assert.Contains("sampled=2", formatted, StringComparison.Ordinal);
        Assert.Contains("motionWrites=4", formatted, StringComparison.Ordinal);
        Assert.Contains("motionRender=6", formatted, StringComparison.Ordinal);
        Assert.Contains("motionLayout=7", formatted, StringComparison.Ordinal);
        Assert.Contains("reduced=8", formatted, StringComparison.Ordinal);
    }

    [Fact]
    public void RuntimeDiagnosticsCaptureDoesNotInvalidateRoot()
    {
        UIRoot root = RootWithCommittedRenderCache(out _);
        UiViewport viewport = new(root.ViewportWidth, root.ViewportHeight, root.Scale);
        int cacheVersionBeforeCapture = root.RetainedRenderCache.Version;

        RuntimeDiagnostics.Capture(root, viewport, new FrameStats());

        Assert.True(root.RetainedRenderCache.IsRootValid);
        Assert.Equal(cacheVersionBeforeCapture, root.RetainedRenderCache.Version);
    }

    [Fact]
    public void RuntimeDiagnosticsIncludesInputCacheReuseWhenAvailable()
    {
        UIRoot root = new(100, 100);
        root.InputCache.EnsureCurrent(root);
        int rebuildCountAfterFirstBuild = root.InputCache.RebuildCount;

        RuntimeDiagnosticsSnapshot snapshot = RuntimeDiagnostics.Capture(root, new UiViewport(100, 100), new FrameStats());

        Assert.False(snapshot.Input.IsDirty);
        Assert.Equal(rebuildCountAfterFirstBuild, snapshot.Input.RebuildCount);
        Assert.Equal("Initial input cache", snapshot.Input.LastInvalidationReason);
    }

    [Fact]
    public void RuntimeDiagnosticsIncludesImageCacheLoadCountWhenAvailable()
    {
        RecordingImageLoader loader = new();
        loader.SetImage("logo.png", new TestImage(16, 8));
        UIRoot root = new(100, 100);
        root.SetImageLoader(loader);
        root.ImageResourceCache!.Resolve(new ImageResource("logo.png"));

        RuntimeDiagnosticsSnapshot snapshot = RuntimeDiagnostics.Capture(root, new UiViewport(100, 100), new FrameStats());

        Assert.True(snapshot.Resources.HasImageCache);
        Assert.Equal(1, snapshot.Resources.ImageCacheLoadCount);
    }

    [Fact]
    public void RuntimeDiagnosticsHandlesMissingOptionalServices()
    {
        UIRoot root = new(100, 100);

        RuntimeDiagnosticsSnapshot snapshot = RuntimeDiagnostics.Capture(root, new UiViewport(100, 100), new FrameStats());

        Assert.False(snapshot.Resources.HasImageCache);
        Assert.Null(snapshot.Resources.ImageCacheLoadCount);
        Assert.False(snapshot.Platform.HasClipboard);
        Assert.False(snapshot.Platform.HasCursor);
        Assert.False(snapshot.Platform.HasFileDialogs);
        Assert.False(snapshot.Platform.HasTextInput);
        Assert.False(snapshot.Platform.HasDpi);
        Assert.False(snapshot.Platform.HasAccessibility);
    }

    private static FrameStats StatsWithEveryRetainedPhase()
    {
        FrameStats stats = new();
        stats.Count(FramePhase.InheritedProperties);
        stats.Count(FramePhase.CommandState);
        stats.Count(FramePhase.Aspect);
        stats.Count(FramePhase.Measure);
        stats.Count(FramePhase.Arrange);
        stats.Count(FramePhase.RenderCache);
        stats.Count(FramePhase.HitTest);
        stats.CountMeasureCall();
        stats.CountMeasureCall();
        stats.CountArrangeCall();
        stats.CountArrangeCall();
        stats.CountArrangeCall();
        stats.CountReusedCache();
        return stats;
    }

    private static UIRoot RootWithCommittedRenderCache(out RenderingTestElement child)
    {
        UIRoot root = new(100, 100);
        child = new RenderingTestElement(Color.White);
        root.VisualChildren.Add(child);
        child.Invalidate(InvalidationFlags.Render, "render");

        root.ProcessFrame();
        root.RetainedRenderer.Commit(root);

        return root;
    }

    private sealed class RecordingImageLoader : IImageLoader
    {
        private readonly Dictionary<string, IDrawImage> images = new(StringComparer.Ordinal);

        public void SetImage(string path, IDrawImage image)
        {
            images[path] = image;
        }

        public IDrawImage Load(string path)
        {
            return images.TryGetValue(path, out IDrawImage? image)
                ? image
                : throw new InvalidOperationException($"No fake image registered for '{path}'.");
        }
    }

    private sealed class TestImage(int width, int height) : IDrawImage
    {
        public int Width { get; } = width;

        public int Height { get; } = height;
    }
}

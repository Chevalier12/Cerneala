using System.Globalization;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Invalidation;

namespace Cerneala.UI.Diagnostics;

public static class RuntimeDiagnostics
{
    public static RuntimeDiagnosticsSnapshot Capture(UIRoot root, UiViewport viewport, FrameStats stats)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(stats);

        return new RuntimeDiagnosticsSnapshot(
            new RuntimeViewportDiagnosticsSnapshot(viewport.Width, viewport.Height, viewport.Scale),
            FrameDiagnostics.Capture(stats),
            new RuntimeInputDiagnosticsSnapshot(
                root.InputCache.IsDirty,
                root.InputCache.RebuildCount,
                root.InputCache.LastInvalidationReason),
            new RuntimeRenderDiagnosticsSnapshot(
                root.RetainedRenderCache.IsRootValid,
                root.RetainedRenderCache.Version,
                root.RetainedRenderCache.RootCommands.Count),
            new RuntimeResourceDiagnosticsSnapshot(
                root.ImageResourceCache is not null,
                root.ImageResourceCache?.LoadCount),
            new RuntimePlatformDiagnosticsSnapshot(
                root.PlatformServices.Clipboard is not null,
                root.PlatformServices.Cursor is not null,
                root.PlatformServices.FileDialogs is not null,
                root.PlatformServices.TextInput is not null,
                root.PlatformServices.Dpi is not null,
                root.PlatformServices.Accessibility is not null));
    }

    public static string Format(RuntimeDiagnosticsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return snapshot.ToString();
    }
}

public sealed record RuntimeDiagnosticsSnapshot(
    RuntimeViewportDiagnosticsSnapshot Viewport,
    FrameDiagnosticsSnapshot Frame,
    RuntimeInputDiagnosticsSnapshot Input,
    RuntimeRenderDiagnosticsSnapshot Render,
    RuntimeResourceDiagnosticsSnapshot Resources,
    RuntimePlatformDiagnosticsSnapshot Platform)
{
    public override string ToString()
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"runtime viewport={Viewport.LogicalWidth}x{Viewport.LogicalHeight}, scale={Viewport.Scale}, frame inherited={Frame.InheritedElements}, commandState={Frame.CommandStateElements}, style={Frame.StyledElements}, queuedMeasure={Frame.QueuedMeasureElements}, queuedArrange={Frame.QueuedArrangeElements}, measureCalls={Frame.MeasureCalls}, arrangeCalls={Frame.ArrangeCalls}, renderCache={Frame.RenderedElements}, hitTest={Frame.HitTestElements}, reusedCaches={Frame.ReusedCaches}, noWork={Frame.NoWorkFrames}, hasWork={Frame.HasWork}, input dirty={Input.IsDirty}, inputRebuilds={Input.RebuildCount}, commands={Render.RootCommandCount}, imageCache={Resources.ImageCacheLoadCount?.ToString(CultureInfo.InvariantCulture) ?? "none"}, platform clipboard={Platform.HasClipboard}, cursor={Platform.HasCursor}");
    }
}

public sealed record RuntimeViewportDiagnosticsSnapshot(float LogicalWidth, float LogicalHeight, float Scale);

public sealed record RuntimeInputDiagnosticsSnapshot(bool IsDirty, int RebuildCount, string LastInvalidationReason);

public sealed record RuntimeRenderDiagnosticsSnapshot(bool IsRootValid, int RootVersion, int RootCommandCount);

public sealed record RuntimeResourceDiagnosticsSnapshot(bool HasImageCache, int? ImageCacheLoadCount);

public sealed record RuntimePlatformDiagnosticsSnapshot(
    bool HasClipboard,
    bool HasCursor,
    bool HasFileDialogs,
    bool HasTextInput,
    bool HasDpi,
    bool HasAccessibility);

using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Platform;
using Cerneala.UI.Relay;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Hosting;

public sealed class UiHost
{
    private UIRoot? root;
    private UiViewport viewport;
    private bool needsInitialFrame = true;
    private readonly IPlatformServices? platformServices;
    private readonly CursorService cursorService = new();

    public UiHost(UiHostOptions? options = null)
    {
        options ??= new UiHostOptions();
        root = options.Root;
        viewport = options.Viewport;
        InputSource = options.InputSource;
        Backend = options.Backend;
        Clock = options.Clock;
        InputBridge = options.InputBridge ?? new ElementInputBridge();
        platformServices = options.PlatformServices;

        if (root is not null)
        {
            root.Relay.VerifyAccess();
            root.SetPlatformServices(platformServices);
            ApplyViewport(root, viewport);
        }
    }

    public UIRoot? Root => root;

    public UiRelay? Relay => root?.Relay;

    public IInputSource? InputSource { get; set; }

    public IUiBackend? Backend { get; set; }

    public IUiClock? Clock { get; set; }

    public ElementInputBridge InputBridge { get; }

    public UiViewport Viewport => viewport;

    public UiFrame? LastFrame { get; private set; }

    public void SetRoot(UIRoot newRoot)
    {
        ArgumentNullException.ThrowIfNull(newRoot);
        root?.Relay.VerifyAccess();
        newRoot.Relay.VerifyAccess();
        root = newRoot;
        needsInitialFrame = true;
        root.SetPlatformServices(platformServices);
        ApplyViewport(root, viewport);
    }

    public UiFrame Update(UiViewport? viewport = null, TimeSpan? elapsedTime = null)
    {
        UIRoot currentRoot = RequireRoot();
        currentRoot.Relay.VerifyAccess();
        IInputSource inputSource = InputSource ?? Backend?.InputSource ?? throw new InvalidOperationException("UiHost requires an input source for Update without an explicit input frame.");
        return Update(inputSource.GetFrame(), viewport, elapsedTime);
    }

    public UiFrame Update(InputFrame inputFrame, UiViewport? viewport = null, TimeSpan? elapsedTime = null)
    {
        return UpdateCore(inputFrame, viewport, elapsedTime, advanceRenderTime: true);
    }

    internal void AdvanceRenderTime(TimeSpan elapsedTime)
    {
        UIRoot currentRoot = RequireRoot();
        currentRoot.Relay.VerifyAccess();
        TimeSensitiveRenderInvalidator.Invalidate(currentRoot, elapsedTime);
    }

    internal UiFrame UpdateAfterRenderTimeAdvance(InputFrame inputFrame, UiViewport viewport, TimeSpan elapsedTime)
    {
        return UpdateCore(inputFrame, viewport, elapsedTime, advanceRenderTime: false);
    }

    private UiFrame UpdateCore(
        InputFrame inputFrame,
        UiViewport? viewport,
        TimeSpan? elapsedTime,
        bool advanceRenderTime)
    {
        ArgumentNullException.ThrowIfNull(inputFrame);

        UIRoot currentRoot = RequireRoot();
        currentRoot.Relay.VerifyAccess();
        UiViewport currentViewport = viewport ?? this.viewport;
        TimeSpan frameTime = elapsedTime ?? Clock?.GetElapsedTime() ?? TimeSpan.Zero;
        FrameStats stats = new();
        using (currentRoot.BeginUpdate(stats))
        {
            ApplyViewportIfChanged(currentRoot, currentViewport);
            PrimeInitialFrame(currentRoot);
            if (advanceRenderTime)
            {
                TimeSensitiveRenderInvalidator.Invalidate(currentRoot, frameTime);
            }

            if (currentRoot.Scheduler.HasWork)
            {
                currentRoot.ProcessFrameCore(null, default, stats, MotionFrameReason.Scheduled);
            }

            InputBridge.Dispatch(currentRoot, inputFrame, frameTime);

            if (currentRoot.Scheduler.HasWork || (currentRoot.Motion.HasActiveMotion && stats.MotionFrames == 0))
            {
                currentRoot.ProcessFrameCore(null, default, stats, MotionFrameReason.Input);
            }
            else if (!stats.HasWork)
            {
                stats.CountNoWorkFrame();
            }

            currentRoot.RetainedRenderer.Commit(currentRoot);
            PublishCursor(currentRoot, inputFrame);
            LastFrame = new UiFrame(frameTime, this.viewport, inputFrame, stats);
            return LastFrame;
        }
    }

    public void Draw()
    {
        UIRoot currentRoot = RequireRoot();
        currentRoot.Relay.VerifyAccess();
        IDrawingBackend backend = Backend?.DrawingBackend ?? throw new InvalidOperationException("UiHost requires a drawing backend for Draw without an explicit backend.");
        Draw(backend);
    }

    public void Draw(IDrawingBackend backend)
    {
        ArgumentNullException.ThrowIfNull(backend);

        UIRoot currentRoot = RequireRoot();
        currentRoot.Relay.VerifyAccess();
        currentRoot.RetainedRenderer.Submit(currentRoot, backend);
    }

    private UIRoot RequireRoot()
    {
        return root ?? throw new InvalidOperationException("UiHost requires a retained root.");
    }

    private void ApplyViewportIfChanged(UIRoot currentRoot, UiViewport nextViewport)
    {
        if (nextViewport == viewport)
        {
            return;
        }

        viewport = nextViewport;
        ApplyViewport(currentRoot, viewport);
        currentRoot.Invalidate(
            InvalidationFlags.Measure |
            InvalidationFlags.Arrange |
            InvalidationFlags.Render |
            InvalidationFlags.HitTest |
            InvalidationFlags.Subtree,
            "Viewport changed");
    }

    private static void ApplyViewport(UIRoot root, UiViewport viewport)
    {
        root.SetViewport(viewport.Width, viewport.Height, viewport.Scale);
    }

    private void PrimeInitialFrame(UIRoot currentRoot)
    {
        if (!needsInitialFrame)
        {
            return;
        }

        currentRoot.Invalidate(
            InvalidationFlags.Measure |
            InvalidationFlags.Arrange |
            InvalidationFlags.Render |
            InvalidationFlags.HitTest |
            InvalidationFlags.Subtree,
            "Initial host frame");
        needsInitialFrame = false;
    }

    private void PublishCursor(UIRoot currentRoot, InputFrame inputFrame)
    {
        ICursorService? platformCursor = currentRoot.PlatformServices.Cursor;
        if (platformCursor is null)
        {
            return;
        }

        Cursor cursor = cursorService.Resolve(currentRoot, inputFrame.Pointer.X, inputFrame.Pointer.Y);
        platformCursor.SetCursor(ToCursorShape(cursor));
    }

    private static CursorShape ToCursorShape(Cursor cursor)
    {
        if (cursor == Cursor.Hand)
        {
            return CursorShape.Hand;
        }

        if (cursor == Cursor.IBeam)
        {
            return CursorShape.IBeam;
        }

        if (cursor == Cursor.Crosshair)
        {
            return CursorShape.Crosshair;
        }

        return CursorShape.Arrow;
    }
}

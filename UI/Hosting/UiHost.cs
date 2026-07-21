using System.Diagnostics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;
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
    private IUiBackend? backend;
    private UiViewport viewport;
    private bool needsInitialFrame = true;
    private readonly IPlatformServices? platformServices;
    private readonly CursorService cursorService = new();
    private readonly PrismFrameAnalyzer prismFrameAnalyzer = new();
    private readonly BackdropFrameCounters backdropFrameCounters = new();
    private WeakReference<IBackdropFrameSource>? identifiedBackdropSource;
    private PrismBackdropSourceToken backdropSourceToken;

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

    public IUiBackend? Backend
    {
        get => backend;
        set
        {
            ValidateBackdropCompatibility(value);
            backend = value;
        }
    }

    public IUiClock? Clock { get; set; }

    public ElementInputBridge InputBridge { get; }

    public UiViewport Viewport => viewport;

    public UiFrame? LastFrame { get; private set; }

    internal BackdropFrameCounters BackdropFrameCounters =>
        backdropFrameCounters;

    public void SetRoot(UIRoot newRoot)
    {
        ArgumentNullException.ThrowIfNull(newRoot);
        root?.Relay.VerifyAccess();
        newRoot.Relay.VerifyAccess();
        if (!ReferenceEquals(root, newRoot))
        {
            newRoot.PrismCacheInvalidations.EnqueueAll();
        }
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
            long updatePhaseStarted = Stopwatch.GetTimestamp();
            ApplyViewportIfChanged(currentRoot, currentViewport);
            PrimeInitialFrame(currentRoot);
            if (advanceRenderTime)
            {
                TimeSensitiveRenderInvalidator.Invalidate(currentRoot, frameTime);
            }
            TimeSpan updatePreparationTime = Stopwatch.GetElapsedTime(updatePhaseStarted);

            updatePhaseStarted = Stopwatch.GetTimestamp();
            FramePhaseTiming scheduledPhases = default;
            if (currentRoot.Scheduler.HasWork)
            {
                currentRoot.ProcessFrameCore(null, default, stats, MotionFrameReason.Scheduled);
                scheduledPhases = currentRoot.Scheduler.LastFrameTiming;
            }
            TimeSpan scheduledProcessingTime = Stopwatch.GetElapsedTime(updatePhaseStarted);

            updatePhaseStarted = Stopwatch.GetTimestamp();
            InputBridge.Dispatch(currentRoot, inputFrame, frameTime);
            TimeSpan inputDispatchTime = Stopwatch.GetElapsedTime(updatePhaseStarted);

            updatePhaseStarted = Stopwatch.GetTimestamp();
            FramePhaseTiming inputPhases = default;
            if (currentRoot.Scheduler.HasWork || (currentRoot.Motion.HasActiveMotion && stats.MotionFrames == 0))
            {
                currentRoot.ProcessFrameCore(null, default, stats, MotionFrameReason.Input);
                inputPhases = currentRoot.Scheduler.LastFrameTiming;
            }
            else if (!stats.HasWork)
            {
                stats.CountNoWorkFrame();
            }
            TimeSpan inputProcessingTime = Stopwatch.GetElapsedTime(updatePhaseStarted);

            updatePhaseStarted = Stopwatch.GetTimestamp();
            currentRoot.RetainedRenderer.Commit(currentRoot);
            TimeSpan retainedCommitTime = Stopwatch.GetElapsedTime(updatePhaseStarted);
            updatePhaseStarted = Stopwatch.GetTimestamp();
            PublishCursor(currentRoot, inputFrame);
            TimeSpan cursorPublicationTime = Stopwatch.GetElapsedTime(updatePhaseStarted);
            LastFrame = new UiFrame(frameTime, this.viewport, inputFrame, stats);
            LastFrame.DiagnosticsTiming = new UiFrameTiming(
                default,
                default,
                default,
                default,
                default,
                updatePreparationTime,
                scheduledProcessingTime,
                inputDispatchTime,
                inputProcessingTime,
                retainedCommitTime,
                cursorPublicationTime,
                scheduledPhases,
                inputPhases);
            return LastFrame;
        }
    }

    public void Draw()
    {
        IUiBackend configuredBackend = Backend ??
            throw new InvalidOperationException(
                "UiHost requires a backend for Draw without an explicit backend.");
        IDrawingBackend drawingBackend =
            configuredBackend.DrawingBackend ??
            throw new InvalidOperationException(
                "UiHost requires a drawing backend for Draw without an explicit backend.");
        DrawCore(
            drawingBackend,
            configuredBackend.BackdropFrameSource);
    }

    public void Draw(IDrawingBackend drawingBackend)
    {
        ArgumentNullException.ThrowIfNull(drawingBackend);
        IBackdropFrameSource? source =
            ReferenceEquals(Backend?.DrawingBackend, drawingBackend)
                ? Backend?.BackdropFrameSource
                : null;
        DrawCore(drawingBackend, source);
    }

    internal void Draw(
        IDrawingBackend drawingBackend,
        IBackdropFrameSource? backdropFrameSource)
    {
        ArgumentNullException.ThrowIfNull(drawingBackend);
        ValidateBackdropCompatibility(
            drawingBackend,
            backdropFrameSource);
        DrawCore(drawingBackend, backdropFrameSource);
    }

    private void DrawCore(
        IDrawingBackend drawingBackend,
        IBackdropFrameSource? backdropFrameSource)
    {
        UIRoot currentRoot = RequireRoot();
        currentRoot.Relay.VerifyAccess();
        DrawCommandList commands = currentRoot.RetainedRenderer.Render(currentRoot);
        PrismFrameAnalysis analysis = prismFrameAnalyzer.Analyze(commands);
        IBackdropFrameLease? lease = AcquireBackdropFrame(
            analysis,
            backdropFrameSource);
        try
        {
            PrismBackdropSourceToken sourceToken = lease is null
                ? default
                : GetBackdropSourceToken(backdropFrameSource!);
            DrawingFrameContext frameContext = new(
                analysis,
                lease,
                sourceToken,
                currentRoot.PrismCacheInvalidations);
            currentRoot.RetainedRenderer.Submit(
                currentRoot,
                drawingBackend,
                in frameContext);
        }
        finally
        {
            lease?.Dispose();
        }
    }

    private PrismBackdropSourceToken GetBackdropSourceToken(
        IBackdropFrameSource source)
    {
        if (identifiedBackdropSource is not null &&
            identifiedBackdropSource.TryGetTarget(
                out IBackdropFrameSource? current) &&
            ReferenceEquals(current, source))
        {
            return backdropSourceToken;
        }

        identifiedBackdropSource =
            new WeakReference<IBackdropFrameSource>(source);
        backdropSourceToken =
            PrismBackdropSourceToken.CreateUnique();
        return backdropSourceToken;
    }

    private IBackdropFrameLease? AcquireBackdropFrame(
        PrismFrameAnalysis analysis,
        IBackdropFrameSource? source)
    {
        PrismBackdropRequirement? requirement =
            analysis.BackdropRequirement;
        if (requirement is null)
        {
            backdropFrameCounters.RecordSkipped();
            return null;
        }

        backdropFrameCounters.RecordRequested();
        if (source is null ||
            !TryGetPhysicalViewportSize(
                out int pixelWidth,
                out int pixelHeight))
        {
            backdropFrameCounters.RecordFailed();
            return null;
        }

        BackdropFrameRequest request = new(
            pixelWidth,
            pixelHeight,
            viewport.Scale,
            requirement);
        try
        {
            IBackdropFrameLease lease =
                source.AcquireFrame(in request) ??
                throw new InvalidOperationException(
                    $"Backdrop frame source '{source.GetType().FullName}' returned a null lease.");
            backdropFrameCounters.RecordAcquired(
                requirement.ScopeCount);
            return lease;
        }
        catch
        {
            backdropFrameCounters.RecordFailed();
            throw;
        }
    }

    private bool TryGetPhysicalViewportSize(
        out int pixelWidth,
        out int pixelHeight)
    {
        double width = Math.Ceiling(viewport.Width * viewport.Scale);
        double height = Math.Ceiling(viewport.Height * viewport.Scale);
        if (width <= 0 ||
            height <= 0 ||
            width > int.MaxValue ||
            height > int.MaxValue)
        {
            pixelWidth = 0;
            pixelHeight = 0;
            return false;
        }

        pixelWidth = (int)width;
        pixelHeight = (int)height;
        return true;
    }

    private UIRoot RequireRoot()
    {
        return root ?? throw new InvalidOperationException("UiHost requires a retained root.");
    }

    private static void ValidateBackdropCompatibility(
        IUiBackend? candidate)
    {
        ValidateBackdropCompatibility(
            candidate?.DrawingBackend,
            candidate?.BackdropFrameSource);
    }

    private static void ValidateBackdropCompatibility(
        IDrawingBackend? drawingBackend,
        IBackdropFrameSource? source)
    {
        if (source is null)
        {
            return;
        }

        if (drawingBackend is null)
        {
            throw new InvalidOperationException(
                "A backdrop frame source requires a drawing backend.");
        }
        if (!source.IsCompatibleWith(drawingBackend))
        {
            throw new InvalidOperationException(
                $"Backdrop frame source '{source.GetType().FullName}' is not compatible " +
                $"with drawing backend '{drawingBackend.GetType().FullName}'.");
        }
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

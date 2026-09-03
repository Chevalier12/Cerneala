using System.Diagnostics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Platform;
using Cerneala.UI.Resources;
using Cerneala.UI.Servo;
using Cerneala.UI.Theming;

namespace Cerneala.UI.Hosting.Windowing;

internal sealed class WindowApplicationRuntime : IDisposable
{
    private static WindowApplicationRuntime? current;

    private readonly int ownerThreadId = Environment.CurrentManagedThreadId;
    private readonly IWindowPlatform platform;
    private readonly Dictionary<Window, WindowContext> contexts = new(ReferenceEqualityComparer.Instance);
    private readonly List<Window> windows = [];
    private readonly IResourceProvider? resourceProvider;
    private readonly ThemeProvider themeProvider;
    private readonly IPlatformServices? platformServices;
    private bool disposed;
    private Window? legacyMainWindow;
    private Application? application;

    internal WindowApplicationRuntime(
        IWindowPlatform platform,
        IResourceProvider? resourceProvider = null,
        ThemeProvider? themeProvider = null,
        IPlatformServices? platformServices = null)
    {
        this.platform = platform ?? throw new ArgumentNullException(nameof(platform));
        this.resourceProvider = resourceProvider;
        this.themeProvider = themeProvider ?? new ThemeProvider(DefaultTheme.Create());
        this.platformServices = platformServices ?? platform.PlatformServices;
    }

    public static WindowApplicationRuntime? Current => current;

    public static WindowApplicationRuntime CurrentOrDefault => current ??= CreateDefault();

    internal static WindowApplicationRuntime GetOrCreateDefault(bool useMultisampling) =>
        current ??= CreateDefault(useMultisampling);

    public IReadOnlyList<Window> Windows => windows;

    public Window? ActiveWindow { get; private set; }

    internal static void Install(WindowApplicationRuntime runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        if (current is not null && !ReferenceEquals(current, runtime))
        {
            throw new InvalidOperationException("A Window application runtime is already installed in this process.");
        }

        current = runtime;
    }

    internal static void ResetForTesting()
    {
        current?.DisposeCore(verifyAccess: false);
        current = null;
    }

    public void StartMainWindow(Window window)
    {
        VerifyAccess();
        Window? currentMainWindow = application?.MainWindow ?? legacyMainWindow;
        if (currentMainWindow is not null && !ReferenceEquals(currentMainWindow, window))
        {
            throw new InvalidOperationException("The Window runtime already has a MainWindow.");
        }

        ArgumentNullException.ThrowIfNull(window);
        if (application is not null)
        {
            application.MainWindow = window;
        }
        else
        {
            legacyMainWindow = window;
        }

        Show(window, modal: false);
    }

    internal void StartPreviewWindow(Window window)
    {
        VerifyAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(window);
        if (window.IsClosed)
        {
            throw new InvalidOperationException("A closed Window cannot be used for preview.");
        }

        if (application is not null)
        {
            application.MainWindow = window;
        }
        else
        {
            legacyMainWindow = window;
        }

        WindowContext context = GetOrCreateContext(window);
        context.IsPreview = true;
        window.SetShown(true);
        Render(context, TimeSpan.Zero);
    }

    public void Show(Window window, bool modal)
    {
        VerifyAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(window);
        if (window.IsClosed)
        {
            throw new InvalidOperationException("A closed Window cannot be shown again.");
        }

        if (window.IsShown)
        {
            if (modal)
            {
                throw new InvalidOperationException("A visible Window cannot be changed into a modal dialog.");
            }

            Activate(window);
            return;
        }

        if (modal && window.Owner is null && ActiveWindow is not null && !ReferenceEquals(ActiveWindow, window))
        {
            SetOwner(window, ActiveWindow);
        }

        WindowContext context = GetOrCreateContext(window);
        if (modal)
        {
            context.IsModal = true;
            SetOwnerEnabled(window.Owner, enabled: false);
        }

        window.SetShown(true);
        try
        {
            bool rendered = Render(context, TimeSpan.Zero);
            if (!rendered || !IsLiveContext(context))
            {
                return;
            }

            context.PlatformWindow.Show();
        }
        catch
        {
            if (!window.IsClosed)
            {
                window.SetShown(false);
            }

            if (context.IsModal)
            {
                context.IsModal = false;
                SetOwnerEnabled(window.Owner, enabled: true);
            }

            throw;
        }
    }

    public void Hide(Window window)
    {
        VerifyAccess();
        WindowContext context = RequireContext(window);
        if (window.IsClosed)
        {
            throw new InvalidOperationException("A closed Window cannot be hidden.");
        }

        if (!window.IsShown)
        {
            return;
        }

        context.PlatformWindow.Hide();
        window.SetShown(false);
        if (ReferenceEquals(ActiveWindow, window))
        {
            SetActiveWindow(null);
        }
    }

    public void Activate(Window window)
    {
        VerifyAccess();
        if (!window.IsShown || window.IsClosed)
        {
            throw new InvalidOperationException("Only a visible, open Window can be activated.");
        }

        RequireContext(window).PlatformWindow.Activate();
    }

    public bool Close(Window window, bool force)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(window);
        if (window.IsClosed)
        {
            return true;
        }

        if (!force && !window.RaiseClosing())
        {
            return false;
        }

        foreach (Window owned in window.OwnedWindows.ToArray())
        {
            Close(owned, force: true);
        }

        if (application is null && ReferenceEquals(window, legacyMainWindow))
        {
            foreach (Window remaining in windows.Where(candidate => !ReferenceEquals(candidate, window)).ToArray())
            {
                Close(remaining, force: true);
            }
        }

        if (contexts.Remove(window, out WindowContext? context))
        {
            if (context.IsModal)
            {
                SetOwnerEnabled(window.Owner, enabled: true);
            }

            context.Root.VisualChildren.Remove(window);
            context.Root.LogicalChildren.Remove(window);
            context.PlatformWindow.Destroy();
            context.Dispose();
        }

        windows.Remove(window);
        if (ReferenceEquals(ActiveWindow, window))
        {
            SetActiveWindow(null);
        }

        window.SetOwnerCore(null);
        window.SetRuntimeOwner(null);
        window.MarkClosed();
        application?.HandleWindowClosed(window);
        return true;
    }

    internal void SetApplication(Application value)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(value);
        if (application is not null && !ReferenceEquals(application, value))
        {
            throw new InvalidOperationException("The Window runtime already has an Application.");
        }

        application = value;
    }

    internal void ClearApplication(Application value)
    {
        VerifyAccess();
        if (ReferenceEquals(application, value))
        {
            application = null;
        }
    }

    internal void CloseAll()
    {
        VerifyAccess();
        foreach (Window window in windows.ToArray())
        {
            Close(window, force: true);
        }
    }

    public void SetOwner(Window window, Window? owner)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(window);
        if (ReferenceEquals(window, owner))
        {
            throw new InvalidOperationException("A Window cannot own itself.");
        }

        for (Window? currentOwner = owner; currentOwner is not null; currentOwner = currentOwner.Owner)
        {
            if (ReferenceEquals(currentOwner, window))
            {
                throw new InvalidOperationException("Window ownership cannot contain a cycle.");
            }
        }

        if (owner?.IsClosed == true)
        {
            throw new InvalidOperationException("A closed Window cannot own another Window.");
        }

        window.SetOwnerCore(owner);
        if (contexts.TryGetValue(window, out WindowContext? context))
        {
            IPlatformWindow? nativeOwner = owner is not null && contexts.TryGetValue(owner, out WindowContext? ownerContext)
                ? ownerContext.PlatformWindow
                : null;
            context.PlatformWindow.SetOwner(nativeOwner);
        }
    }

    public void ApplyProperties(Window window)
    {
        VerifyAccess();
        if (contexts.TryGetValue(window, out WindowContext? context))
        {
            context.PlatformWindow.ApplyProperties(window);
        }
    }

    public void SaveScreenshot(Window window, string path)
    {
        SaveScreenshotCore(window, path, region: null);
    }

    internal void SaveScreenshot(
        Window window,
        string path,
        WindowScreenshotRegion region)
    {
        SaveScreenshotCore(window, path, region);
    }

    internal Task SaveServoScreenshotAsync(
        Window window,
        string path,
        Func<UIRoot, WindowScreenshotRegion?>? resolveRegion,
        CancellationToken cancellationToken)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(window);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();
        WindowContext context = RequireContext(window);
        WindowScreenshotRegion? resolvedRegion = resolveRegion?.Invoke(context.Root);
        if (!context.IsRendering && context.Root.RetainedRenderCache.IsRootValid)
        {
            SaveScreenshotCore(window, path, resolvedRegion);
            return Task.CompletedTask;
        }

        ServoScreenshotRequest request = new(path, resolveRegion, cancellationToken);
        context.ServoScreenshotRequests.Enqueue(request);
        context.RenderRequested = true;
        return request.Task;
    }

    private void SaveScreenshotCore(
        Window window,
        string path,
        WindowScreenshotRegion? region)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(window);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        WindowContext context = RequireContext(window);
        IWindowGraphicsSession graphicsSession = context.PlatformWindow.GraphicsSession;
        if (graphicsSession is not IWindowScreenshotSource screenshotSource)
        {
            throw new NotSupportedException("The active Window graphics backend does not support screenshots.");
        }

        string fullPath = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using FileStream output = File.Create(fullPath);
        Action<IDrawingBackend> draw = drawingBackend => context.Host.Draw(
            drawingBackend,
            graphicsSession as IBackdropFrameSource);
        if (region is WindowScreenshotRegion pixelRegion)
        {
            screenshotSource.RenderPng(output, Color.White, pixelRegion, draw);
        }
        else
        {
            screenshotSource.RenderPng(output, Color.White, draw);
        }
    }

    internal WindowPreviewFrame CapturePreviewFrame(
        Window window,
        byte[]? reusablePixels = null)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(window);
        IWindowGraphicsSession graphicsSession = RequireContext(window).PlatformWindow.GraphicsSession;
        return graphicsSession is IWindowPresentedFrameSource frameSource
            ? frameSource.CapturePresentedFrame(reusablePixels)
            : throw new NotSupportedException("The active Window graphics backend cannot capture its presented frame.");
    }

    internal ServoInputState GetServoInputState(Window window)
    {
        VerifyAccess();
        return RequireContext(window).ServoInput;
    }

    internal void ClickPreview(Window window, float x, float y)
    {
        VerifyAccess();
        WindowContext context = RequireContext(window);
        context.PreviewInputDriver.ClickAt(x, y);
        context.RenderRequested = true;
    }

    internal void MovePreviewPointer(Window window, float x, float y)
    {
        VerifyAccess();
        WindowContext context = RequireContext(window);
        context.PreviewInputDriver.MovePointerTo(x, y);
        context.RenderRequested = true;
    }

    internal void SetPreviewPointerButton(
        Window window,
        float x,
        float y,
        InputMouseButton button,
        bool isDown)
    {
        VerifyAccess();
        WindowContext context = RequireContext(window);
        context.PreviewInputDriver.SetPointerButtonAt(x, y, button, isDown);
        context.RenderRequested = true;
    }

    internal void ScrollPreviewPointer(Window window, float x, float y, int wheelDelta)
    {
        VerifyAccess();
        WindowContext context = RequireContext(window);
        context.PreviewInputDriver.ScrollPointerAt(x, y, wheelDelta);
        context.RenderRequested = true;
    }

    internal void LeavePreviewPointer(Window window)
    {
        VerifyAccess();
        WindowContext context = RequireContext(window);
        context.PreviewInputDriver.LeavePointer();
        context.RenderRequested = true;
    }

    internal void SetPreviewKeyState(Window window, InputKey key, bool isDown)
    {
        VerifyAccess();
        WindowContext context = RequireContext(window);
        context.PreviewInputDriver.SetKeyState(key, isDown);
        context.RenderRequested = true;
    }

    internal void ResetPreviewInput(Window window)
    {
        VerifyAccess();
        WindowContext context = RequireContext(window);
        context.PreviewInputDriver.ResetInput();
        context.RenderRequested = true;
    }

    internal void SendPreviewText(Window window, string text)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(text);
        WindowContext context = RequireContext(window);
        context.PreviewInputDriver.SendText(text);
        context.RenderRequested = true;
    }

    internal void PressPreviewKey(Window window, InputKey key, ServoModifiers modifiers)
    {
        VerifyAccess();
        WindowContext context = RequireContext(window);
        context.PreviewInputDriver.PressKey(key, modifiers);
        context.RenderRequested = true;
    }

    private Task EnqueueServoInputAsync(
        WindowContext context,
        ServoInputSequence sequence,
        CancellationToken cancellationToken)
    {
        VerifyAccess();
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsLiveContext(context))
        {
            throw new ServoException("Servo input requires a visible, open Window.");
        }
        if (sequence.Steps.Count == 0)
        {
            return Task.CompletedTask;
        }

        ServoInputOperation operation = new(sequence, cancellationToken);
        context.ServoInputOperations.Enqueue(operation);
        context.RenderRequested = true;
        return operation.Task;
    }

    internal PrismOperationalDiagnostics? CapturePrismDiagnostics(Window window)
    {
        VerifyAccess();
        WindowContext context = RequireContext(window);
        IWindowGraphicsSession graphicsSession =
            context.PlatformWindow.GraphicsSession;
        PrismExecutionDiagnostics? diagnostics =
            graphicsSession.PrismExecutionDiagnostics;
        return diagnostics is null
            ? null
            : PrismOperationalDiagnostics.Capture(
                diagnostics,
                context.Host.BackdropFrameCounters.Snapshot,
                graphicsSession.ActiveBackdropLeaseCount,
                context.Root.Motion.HasActiveMotion);
    }

    public bool PumpOnce(TimeSpan elapsedTime)
    {
        VerifyAccess();
        platform.PumpEvents();
        bool frameRendered = false;
        foreach (WindowContext context in contexts.Values.ToArray())
        {
            if (!context.Window.IsShown ||
                context.PlatformWindow.Viewport.Width <= 0 ||
                context.PlatformWindow.Viewport.Height <= 0)
            {
                continue;
            }

            context.Host.AdvanceRenderTime(elapsedTime);
            if (!IsLiveContext(context))
            {
                continue;
            }

            if (context.RenderRequested ||
                context.Root.Relay.HasPendingWork ||
                context.Root.Scheduler.HasWork ||
                context.Root.Motion.HasActiveMotion ||
                context.Host.InputBridge.HasActivePointerRepeat)
            {
                bool rendered = Render(context, elapsedTime, renderTimeAlreadyAdvanced: true);
                frameRendered |= rendered;
            }
        }

        return frameRendered;
    }

    public void RunStandalone(Window window)
    {
        StartMainWindow(window);
        Stopwatch stopwatch = Stopwatch.StartNew();
        TimeSpan previous = stopwatch.Elapsed;
        while (!window.IsClosed && windows.Count > 0)
        {
            TimeSpan now = stopwatch.Elapsed;
            bool framePresented = PumpOnce(now - previous);
            previous = now;
            if (!framePresented)
            {
                Thread.Sleep(1);
            }
        }
    }

    internal void RunStandalone(Application value)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(value);
        Stopwatch stopwatch = Stopwatch.StartNew();
        TimeSpan previous = stopwatch.Elapsed;
        while (!value.IsShutdownRequested)
        {
            TimeSpan now = stopwatch.Elapsed;
            bool framePresented = PumpOnce(now - previous);
            previous = now;
            if (!framePresented)
            {
                Thread.Sleep(1);
            }
        }
    }

    public void Dispose()
    {
        DisposeCore(verifyAccess: true);
    }

    private void DisposeCore(bool verifyAccess)
    {
        if (disposed)
        {
            return;
        }

        if (verifyAccess)
        {
            VerifyAccess();
        }

        foreach (Window window in windows.ToArray())
        {
            if (verifyAccess)
            {
                Close(window, force: true);
            }
            else if (contexts.Remove(window, out WindowContext? context))
            {
                context.PlatformWindow.Destroy();
                context.Dispose();
                window.SetOwnerCore(null);
                window.SetRuntimeOwner(null);
                window.MarkClosed();
            }
        }

        windows.Clear();

        platform.Dispose();
        disposed = true;
        Application? attachedApplication = application;
        application = null;
        if (ReferenceEquals(current, this))
        {
            current = null;
        }

        if (verifyAccess)
        {
            attachedApplication?.CompleteExit();
        }
        else
        {
            attachedApplication?.ResetStateForTesting();
        }
    }

    private WindowContext GetOrCreateContext(Window window)
    {
        if (contexts.TryGetValue(window, out WindowContext? existing))
        {
            return existing;
        }

        WindowCallbacks callbacks = new(this, window);
        IPlatformWindow platformWindow = platform.CreateWindow(window, callbacks);
        UIRoot root = new(platformWindow.Viewport.Width, platformWindow.Viewport.Height, platformWindow.Viewport.Scale);
        root.Relay.VerifyAccess();
        root.SetThemeProvider(themeProvider);
        root.SetResourceProvider(application?.Resources ?? resourceProvider);
        root.SetPlatformServices(platformServices);
        root.SetImageResourceCache(
            platformWindow.GraphicsSession.ImageLoader,
            platformWindow.GraphicsSession.ImageResourceCache);
        UiHost host = new(new UiHostOptions
        {
            Root = root,
            Viewport = platformWindow.Viewport,
            InputSource = platformWindow.InputSource,
            PlatformServices = platformServices
        });
        WindowContext context = new(this, window, platformWindow, root, host);
        callbacks.Context = context;
        contexts.Add(window, context);
        windows.Add(window);
        window.SetRuntimeOwner(this);
        platformWindow.ApplyProperties(window);
        if (window.Owner is not null)
        {
            IPlatformWindow? nativeOwner = contexts.TryGetValue(window.Owner, out WindowContext? ownerContext)
                ? ownerContext.PlatformWindow
                : null;
            platformWindow.SetOwner(nativeOwner);
        }

        window.MarkSourceInitialized();
        root.LogicalChildren.Add(window);
        root.VisualChildren.Add(window);
        return context;
    }

    private bool Render(WindowContext context, TimeSpan elapsedTime, bool renderTimeAlreadyAdvanced = false)
    {
        if (context.IsRendering)
        {
            context.RenderRequested = true;
            return false;
        }

        context.IsRendering = true;
        context.RenderRequested = false;
        ServoInputFrameRequest? servoRequest = null;
        try
        {
            long processingStarted = Stopwatch.GetTimestamp();
            long inputCollectionStarted = Stopwatch.GetTimestamp();
            servoRequest = context.TryBeginServoInputFrame();
            InputFrame inputFrame = servoRequest?.Step.Frame ??
                (context.IsPreview
                    ? context.PreviewInputDriver.GetCurrentFrame()
                    : context.PlatformWindow.InputSource.GetFrame());
            TimeSpan inputCollectionTime = Stopwatch.GetElapsedTime(inputCollectionStarted);
            long retainedUpdateStarted = Stopwatch.GetTimestamp();
            UiFrame frame;
            if (renderTimeAlreadyAdvanced)
            {
                frame = context.Host.UpdateAfterRenderTimeAdvance(inputFrame, context.PlatformWindow.Viewport, elapsedTime);
            }
            else
            {
                frame = context.Host.Update(inputFrame, context.PlatformWindow.Viewport, elapsedTime);
            }
            TimeSpan retainedUpdateTime = Stopwatch.GetElapsedTime(retainedUpdateStarted);

            if (!IsLiveContext(context))
            {
                return false;
            }
            IWindowGraphicsSession graphicsSession = context.PlatformWindow.GraphicsSession;
            long beginFrameStarted = Stopwatch.GetTimestamp();
            graphicsSession.BeginFrame(Color.White);
            TimeSpan beginFrameTime = Stopwatch.GetElapsedTime(beginFrameStarted);
            TimeSpan drawingTime = default;
            DrawingBackendFrameTiming backendTiming = default;
            TimeSpan completeFrameTime;
            try
            {
                long drawingStarted = Stopwatch.GetTimestamp();
                context.Host.Draw(
                    graphicsSession.DrawingBackend,
                    graphicsSession as IBackdropFrameSource);
                drawingTime = Stopwatch.GetElapsedTime(drawingStarted);
                backendTiming =
                    graphicsSession.DrawingBackend is IDrawingBackendFrameTimingSource timingSource
                        ? timingSource.LastFrameTiming
                        : default;
            }
            finally
            {
                long completeFrameStarted = Stopwatch.GetTimestamp();
                try
                {
                    graphicsSession.CompleteFrame(present: !context.IsPreview);
                }
                finally
                {
                    completeFrameTime = Stopwatch.GetElapsedTime(completeFrameStarted);
                }
            }

            frame.DiagnosticsTiming = new UiFrameTiming(
                inputCollectionTime,
                retainedUpdateTime,
                beginFrameTime,
                drawingTime,
                completeFrameTime,
                backendTiming,
                frame.DiagnosticsTiming.UpdatePreparation,
                frame.DiagnosticsTiming.ScheduledProcessing,
                frame.DiagnosticsTiming.InputDispatch,
                frame.DiagnosticsTiming.InputProcessing,
                frame.DiagnosticsTiming.RetainedCommit,
                frame.DiagnosticsTiming.CursorPublication,
                frame.DiagnosticsTiming.ScheduledPhases,
                frame.DiagnosticsTiming.InputPhases);
            frame.ProcessingTime = Stopwatch.GetElapsedTime(processingStarted);
            CompleteServoScreenshotRequests(context);
            using (context.Root.Relay.EnterSynchronizationContext())
            {
                context.Window.MarkFrameRendered(frame);
                if (servoRequest is not null)
                {
                    context.CompleteServoInputFrame(servoRequest);
                }

                if (!context.ContentRendered)
                {
                    context.ContentRendered = true;
                    context.Window.MarkContentRendered();
                }
            }

            return true;
        }
        catch (Exception exception) when (servoRequest is not null)
        {
            context.FailServoInputFrame(servoRequest, exception);
            return false;
        }
        finally
        {
            context.IsRendering = false;
            if (IsLiveContext(context) &&
                (context.ServoInputOperations.Count > 0 ||
                 context.ServoScreenshotRequests.Count > 0))
            {
                context.RenderRequested = true;
            }
        }
    }

    private WindowContext RequireContext(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        return contexts.TryGetValue(window, out WindowContext? context)
            ? context
            : throw new InvalidOperationException("The Window has not been shown.");
    }

    private void CompleteServoScreenshotRequests(WindowContext context)
    {
        while (context.ServoScreenshotRequests.TryDequeue(out ServoScreenshotRequest? request))
        {
            request.Execute(() => SaveScreenshotCore(
                context.Window,
                request.Path,
                request.ResolveRegion?.Invoke(context.Root)));
        }
    }

    private bool IsLiveContext(WindowContext context)
    {
        return !context.Window.IsClosed &&
            contexts.TryGetValue(context.Window, out WindowContext? currentContext) &&
            ReferenceEquals(currentContext, context);
    }

    private void SetOwnerEnabled(Window? owner, bool enabled)
    {
        if (owner is not null && contexts.TryGetValue(owner, out WindowContext? ownerContext))
        {
            ownerContext.ModalDisableCount += enabled ? -1 : 1;
            ownerContext.ModalDisableCount = Math.Max(0, ownerContext.ModalDisableCount);
            ownerContext.PlatformWindow.SetEnabled(ownerContext.ModalDisableCount == 0);
        }
    }

    private void SetActiveWindow(Window? window)
    {
        if (ReferenceEquals(ActiveWindow, window))
        {
            return;
        }

        ActiveWindow?.SetActive(false);
        ActiveWindow = window;
        if (ActiveWindow is not null)
        {
            EnsureInitialKeyboardFocus(ActiveWindow);
            ActiveWindow.SetActive(true);
        }
    }

    private void EnsureInitialKeyboardFocus(Window window)
    {
        if (!window.Focusable ||
            !contexts.TryGetValue(window, out WindowContext? context) ||
            context.Host.InputBridge.FocusManager.FocusedElement is not null)
        {
            return;
        }

        ElementInputRouteMap routes = context.Root.InputCache.EnsureCurrent(context.Root);
        context.Host.InputBridge.FocusManager.Focus(window, routes);
    }

    private void VerifyAccess()
    {
        if (Environment.CurrentManagedThreadId != ownerThreadId)
        {
            throw new InvalidOperationException("Window APIs must be called on the owning UI thread.");
        }
    }

    private static WindowApplicationRuntime CreateDefault(bool useMultisampling = false)
    {
        IWindowPlatform platform = WindowingBackendRegistry.CreatePlatform(
            useMultisampling,
            coordinateScaleOverride: null);
        return new WindowApplicationRuntime(platform);
    }

    private sealed class WindowCallbacks : IWindowPlatformCallbacks
    {
        private readonly WindowApplicationRuntime runtime;
        private readonly Window window;

        public WindowCallbacks(WindowApplicationRuntime runtime, Window window)
        {
            this.runtime = runtime;
            this.window = window;
        }

        public WindowContext? Context { get; set; }

        public void RequestClose()
        {
            runtime.Close(window, force: false);
        }

        public void ActivationChanged(bool active)
        {
            if (active)
            {
                runtime.SetActiveWindow(window);
            }
            else if (ReferenceEquals(runtime.ActiveWindow, window))
            {
                runtime.SetActiveWindow(null);
            }
        }

        public void BoundsChanged(UiViewport viewport, float left, float top, WindowState state)
        {
            WindowContext context = Context ?? throw new InvalidOperationException("Window callback arrived before host initialization.");
            context.OverrideViewport = viewport;
            window.SetPlatformBounds(left, top, state);
            context.RenderRequested = true;
        }

        public void RenderRequested()
        {
            if (Context is not null)
            {
                Context.RenderRequested = true;
            }
        }

        public void RenderImmediately()
        {
            if (Context is not { } context ||
                !context.Window.IsShown ||
                context.PlatformWindow.Viewport.Width <= 0 ||
                context.PlatformWindow.Viewport.Height <= 0)
            {
                return;
            }

            runtime.Render(context, TimeSpan.Zero);
        }
    }

    private sealed class WindowContext : IDisposable
    {
        public WindowContext(
            WindowApplicationRuntime runtime,
            Window window,
            IPlatformWindow platformWindow,
            UIRoot root,
            UiHost host)
        {
            Window = window;
            PlatformWindow = platformWindow;
            Root = root;
            Host = host;
            ServoInput = new ServoInputState(
                host,
                (sequence, cancellationToken) =>
                    runtime.EnqueueServoInputAsync(this, sequence, cancellationToken));
        }

        public Window Window { get; }

        public IPlatformWindow PlatformWindow { get; }

        public UIRoot Root { get; }

        public UiHost Host { get; }

        public ServoInputState ServoInput { get; }

        public RetainedServoInputDriver PreviewInputDriver => ServoInput.Driver;

        public bool IsModal { get; set; }

        public bool ContentRendered { get; set; }

        public bool IsPreview { get; set; }

        public bool RenderRequested { get; set; } = true;

        public bool IsRendering { get; set; }

        public int ModalDisableCount { get; set; }

        public UiViewport? OverrideViewport { get; set; }

        public Queue<ServoInputOperation> ServoInputOperations { get; } = new();

        public Queue<ServoScreenshotRequest> ServoScreenshotRequests { get; } = new();

        public ServoInputFrameRequest? TryBeginServoInputFrame()
        {
            while (ServoInputOperations.TryPeek(out ServoInputOperation? operation))
            {
                ServoInputFrameRequest? request = operation.GetNextFrame();
                if (request is not null)
                {
                    return request;
                }

                ServoInputOperations.Dequeue();
            }

            return null;
        }

        public void CompleteServoInputFrame(ServoInputFrameRequest request)
        {
            request.Operation.CompleteFrame(request);
            RemoveCompletedServoOperation(request.Operation);
        }

        public void FailServoInputFrame(ServoInputFrameRequest request, Exception exception)
        {
            request.Operation.FailFrame(request, exception);
            RemoveCompletedServoOperation(request.Operation);
        }

        public void Dispose()
        {
            foreach (ServoInputOperation operation in ServoInputOperations)
            {
                operation.Fail(new ServoException(
                    "The Servo Window closed before the input operation completed."));
            }
            ServoInputOperations.Clear();
            foreach (ServoScreenshotRequest request in ServoScreenshotRequests)
            {
                request.Fail(new ServoException(
                    "The Servo Window closed before the screenshot operation completed."));
            }
            ServoScreenshotRequests.Clear();
            PlatformWindow.Dispose();
        }

        private void RemoveCompletedServoOperation(ServoInputOperation operation)
        {
            if (!operation.Task.IsCompleted)
            {
                return;
            }

            if (ServoInputOperations.TryPeek(out ServoInputOperation? current) &&
                ReferenceEquals(current, operation))
            {
                ServoInputOperations.Dequeue();
            }
        }
    }

    private sealed class ServoInputOperation
    {
        private readonly ServoInputSequence sequence;
        private readonly CancellationToken cancellationToken;
        private readonly TaskCompletionSource completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly CancellationTokenRegistration cancellationRegistration;
        private int nextIndex;
        private ServoInputStep? cleanupStep;
        private ServoInputStep? lastAttempted;
        private Exception? failure;
        private int started;

        public ServoInputOperation(
            ServoInputSequence sequence,
            CancellationToken cancellationToken)
        {
            this.sequence = sequence;
            this.cancellationToken = cancellationToken;
            cancellationRegistration = cancellationToken.Register(
                static state => ((ServoInputOperation)state!).OnCancellationRequested(),
                this);
        }

        public Task Task => completion.Task;

        public ServoInputFrameRequest? GetNextFrame()
        {
            if (Task.IsCompleted)
            {
                return null;
            }

            if (cancellationToken.IsCancellationRequested && Volatile.Read(ref started) != 0)
            {
                nextIndex = sequence.Steps.Count;
                ServoInputStep last = lastAttempted ?? sequence.Steps[0];
                cleanupStep ??= ServoInputSequence.CreateResetStep(last.Pointer, last.Keyboard);
            }

            if (cleanupStep is ServoInputStep cleanup)
            {
                cleanupStep = null;
                return new ServoInputFrameRequest(this, cleanup, IsCleanup: true);
            }

            if (nextIndex >= sequence.Steps.Count)
            {
                return null;
            }

            Interlocked.Exchange(ref started, 1);
            ServoInputStep step = sequence.Steps[nextIndex++];
            lastAttempted = step;
            return new ServoInputFrameRequest(this, step, IsCleanup: false);
        }

        public void CompleteFrame(ServoInputFrameRequest request)
        {
            if (!request.IsCleanup && cancellationToken.IsCancellationRequested)
            {
                nextIndex = sequence.Steps.Count;
                cleanupStep = ServoInputSequence.CreateResetStep(
                    request.Step.Pointer,
                    request.Step.Keyboard);
                return;
            }

            if (request.IsCleanup || nextIndex >= sequence.Steps.Count)
            {
                Complete();
            }
        }

        public void FailFrame(ServoInputFrameRequest request, Exception exception)
        {
            failure ??= exception;
            if (request.IsCleanup)
            {
                Complete();
                return;
            }

            nextIndex = sequence.Steps.Count;
            cleanupStep = ServoInputSequence.CreateResetStep(
                request.Step.Pointer,
                request.Step.Keyboard);
        }

        public void Fail(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);
            failure ??= exception;
            Complete();
        }

        private void Complete()
        {
            cancellationRegistration.Dispose();
            if (failure is not null)
            {
                completion.TrySetException(failure);
            }
            else if (cancellationToken.IsCancellationRequested)
            {
                completion.TrySetCanceled(cancellationToken);
            }
            else
            {
                completion.TrySetResult();
            }
        }

        private void OnCancellationRequested()
        {
            if (Volatile.Read(ref started) == 0)
            {
                completion.TrySetCanceled(cancellationToken);
            }
        }
    }

    private sealed class ServoScreenshotRequest
    {
        private readonly CancellationToken cancellationToken;
        private readonly TaskCompletionSource completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly CancellationTokenRegistration cancellationRegistration;

        public ServoScreenshotRequest(
            string path,
            Func<UIRoot, WindowScreenshotRegion?>? resolveRegion,
            CancellationToken cancellationToken)
        {
            Path = path;
            ResolveRegion = resolveRegion;
            this.cancellationToken = cancellationToken;
            cancellationRegistration = cancellationToken.Register(
                static state => ((ServoScreenshotRequest)state!).Cancel(),
                this);
        }

        public string Path { get; }

        public Func<UIRoot, WindowScreenshotRegion?>? ResolveRegion { get; }

        public Task Task => completion.Task;

        public void Execute(Action capture)
        {
            ArgumentNullException.ThrowIfNull(capture);
            if (Task.IsCompleted)
            {
                cancellationRegistration.Dispose();
                return;
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                capture();
                cancellationToken.ThrowIfCancellationRequested();
                completion.TrySetResult();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                completion.TrySetCanceled(cancellationToken);
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
            finally
            {
                cancellationRegistration.Dispose();
            }
        }

        public void Fail(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);
            cancellationRegistration.Dispose();
            completion.TrySetException(exception);
        }

        private void Cancel()
        {
            completion.TrySetCanceled(cancellationToken);
        }
    }

    private sealed record ServoInputFrameRequest(
        ServoInputOperation Operation,
        ServoInputStep Step,
        bool IsCleanup);

    private sealed class ReferenceEqualityComparer : IEqualityComparer<Window>
    {
        public static ReferenceEqualityComparer Instance { get; } = new();

        public bool Equals(Window? x, Window? y) => ReferenceEquals(x, y);

        public int GetHashCode(Window obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}

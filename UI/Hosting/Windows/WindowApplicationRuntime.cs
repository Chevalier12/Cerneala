using System.Diagnostics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Platform;
using Cerneala.UI.Resources;
using Cerneala.UI.Theming;

namespace Cerneala.UI.Hosting.Windows;

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
        this.platformServices = platformServices;
    }

    public static WindowApplicationRuntime? Current => current;

    public static WindowApplicationRuntime CurrentOrDefault => current ??= CreateDefault();

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
        context.PlatformWindow.Show();
        Render(context, TimeSpan.Zero);
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
        screenshotSource.RenderPng(
            output,
            Color.White,
            drawingBackend => context.Host.Draw(
                drawingBackend,
                graphicsSession as IBackdropFrameSource));
    }

    public void PumpOnce(TimeSpan elapsedTime)
    {
        VerifyAccess();
        platform.PumpEvents();
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
                Render(context, elapsedTime, renderTimeAlreadyAdvanced: true);
            }
        }
    }

    public void RunStandalone(Window window)
    {
        StartMainWindow(window);
        Stopwatch stopwatch = Stopwatch.StartNew();
        TimeSpan previous = stopwatch.Elapsed;
        while (!window.IsClosed && windows.Count > 0)
        {
            TimeSpan now = stopwatch.Elapsed;
            PumpOnce(now - previous);
            previous = now;
            Thread.Sleep(1);
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
            PumpOnce(now - previous);
            previous = now;
            Thread.Sleep(1);
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
        WindowContext context = new(window, platformWindow, root, host);
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

    private void Render(WindowContext context, TimeSpan elapsedTime, bool renderTimeAlreadyAdvanced = false)
    {
        if (context.IsRendering)
        {
            context.RenderRequested = true;
            return;
        }

        context.IsRendering = true;
        context.RenderRequested = false;
        try
        {
            long processingStarted = Stopwatch.GetTimestamp();
            long inputCollectionStarted = Stopwatch.GetTimestamp();
            InputFrame inputFrame = context.PlatformWindow.InputSource.GetFrame();
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
                return;
            }

            IWindowGraphicsSession graphicsSession = context.PlatformWindow.GraphicsSession;
            long beginFrameStarted = Stopwatch.GetTimestamp();
            graphicsSession.BeginFrame(Color.White);
            TimeSpan beginFrameTime = Stopwatch.GetElapsedTime(beginFrameStarted);
            try
            {
                long drawingStarted = Stopwatch.GetTimestamp();
                context.Host.Draw(
                    graphicsSession.DrawingBackend,
                    graphicsSession as IBackdropFrameSource);
                TimeSpan drawingTime = Stopwatch.GetElapsedTime(drawingStarted);
                DrawingBackendFrameTiming backendTiming =
                    graphicsSession.DrawingBackend is IDrawingBackendFrameTimingSource timingSource
                        ? timingSource.LastFrameTiming
                        : default;
                frame.DiagnosticsTiming = new UiFrameTiming(
                    inputCollectionTime,
                    retainedUpdateTime,
                    beginFrameTime,
                    drawingTime,
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
            }
            finally
            {
                graphicsSession.Present();
            }

            using (context.Root.Relay.EnterSynchronizationContext())
            {
                context.Window.MarkFrameRendered(frame);

                if (!context.ContentRendered)
                {
                    context.ContentRendered = true;
                    context.Window.MarkContentRendered();
                }
            }
        }
        finally
        {
            context.IsRendering = false;
        }
    }

    private WindowContext RequireContext(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        return contexts.TryGetValue(window, out WindowContext? context)
            ? context
            : throw new InvalidOperationException("The Window has not been shown.");
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
        ActiveWindow?.SetActive(true);
    }

    private void VerifyAccess()
    {
        if (Environment.CurrentManagedThreadId != ownerThreadId)
        {
            throw new InvalidOperationException("Window APIs must be called on the owning UI thread.");
        }
    }

    private static WindowApplicationRuntime CreateDefault()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Native Window hosting is currently available only on Windows.");
        }

        return new WindowApplicationRuntime(new Win32WindowPlatform());
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
        public WindowContext(Window window, IPlatformWindow platformWindow, UIRoot root, UiHost host)
        {
            Window = window;
            PlatformWindow = platformWindow;
            Root = root;
            Host = host;
        }

        public Window Window { get; }

        public IPlatformWindow PlatformWindow { get; }

        public UIRoot Root { get; }

        public UiHost Host { get; }

        public bool IsModal { get; set; }

        public bool ContentRendered { get; set; }

        public bool RenderRequested { get; set; } = true;

        public bool IsRendering { get; set; }

        public int ModalDisableCount { get; set; }

        public UiViewport? OverrideViewport { get; set; }

        public void Dispose()
        {
            PlatformWindow.Dispose();
        }
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<Window>
    {
        public static ReferenceEqualityComparer Instance { get; } = new();

        public bool Equals(Window? x, Window? y) => ReferenceEquals(x, y);

        public int GetHashCode(Window obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}

using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windows;
using Cerneala.UI.Input;
using Microsoft.Extensions.DependencyInjection;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.UI.Hosting;

public sealed class WindowRuntimeTests : IDisposable
{
    public WindowRuntimeTests()
    {
        GeneratedWindowApplication.ResetForTesting();
        WindowApplicationRuntime.ResetForTesting();
    }

    public void Dispose()
    {
        GeneratedWindowApplication.ResetForTesting();
        WindowApplicationRuntime.ResetForTesting();
    }

    [Fact]
    public void ShowBuildsHostBeforeLifecycleAndHidePreservesTheWindow()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Window window = new() { Content = new TextBlock { Text = "Hello" } };
        List<string> events = [];
        window.SourceInitialized += (_, _) => events.Add("source");
        window.Initialized += (_, _) => events.Add("initialized");
        window.Loaded += (_, _) => events.Add("loaded");
        window.ContentRendered += (_, _) => events.Add("rendered");

        window.Show();

        Assert.Equal(new[] { "source", "initialized", "loaded", "rendered" }, events);
        Assert.True(window.IsShown);
        Assert.True(window.IsLoaded);
        Assert.Single(runtime.Windows);
        Assert.Equal(1, platform.Windows[0].Backend.RenderCount);

        window.Hide();
        Assert.False(window.IsShown);
        Assert.True(window.IsLoaded);
        Assert.Equal(0, platform.Windows[0].Session.DisposeCount);

        window.Show();
        Assert.True(window.IsShown);
        Assert.Single(events, value => value == "rendered");
    }

    [Fact]
    public void RenderLifecycleCallbacksRunInsideTheWindowRelaySynchronizationContext()
    {
        FakeWindowPlatform platform = new();
        Install(platform);
        Window window = new();
        SynchronizationContext? frameContext = null;
        SynchronizationContext? contentContext = null;
        window.FrameRendered += (_, _) => frameContext = SynchronizationContext.Current;
        window.ContentRendered += (_, _) => contentContext = SynchronizationContext.Current;

        window.Show();

        Assert.NotNull(frameContext);
        Assert.NotNull(contentContext);
        Assert.Equal("UiRelaySynchronizationContext", frameContext.GetType().Name);
        Assert.Same(frameContext, contentContext);
    }

    [Fact]
    public void PresentedFrameUpdatesWindowDiagnosticsBeforeRaisingFrameRendered()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Window window = new() { Content = new TextBlock { Text = "Diagnostics" } };
        List<UiFrame> observedFrames = [];
        window.FrameRendered += (_, _) => observedFrames.Add(Assert.IsType<UiFrame>(window.LastFrame));

        window.Show();
        window.Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Render, "test frame");
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

        Assert.Equal(2, observedFrames.Count);
        Assert.Same(window.LastFrame, observedFrames[^1]);
        Assert.Equal(TimeSpan.FromMilliseconds(16), window.LastFrame!.ElapsedTime);
    }

    [Fact]
    public void EachWindowUsesAndDisposesItsOwnGraphicsSession()
    {
        FakeWindowPlatform platform = new();
        Install(platform);
        Window first = new();
        Window second = new();

        first.Show();
        second.Show();

        Assert.NotSame(platform.Windows[0].Session, platform.Windows[1].Session);
        Assert.Equal(1, platform.Windows[0].Session.BeginFrameCount);
        Assert.Equal(1, platform.Windows[0].Session.PresentCount);
        Assert.Equal(1, platform.Windows[1].Session.BeginFrameCount);
        Assert.Equal(1, platform.Windows[1].Session.PresentCount);

        first.Close();

        Assert.Equal(1, platform.Windows[0].Session.DisposeCount);
        Assert.Equal(0, platform.Windows[1].Session.DisposeCount);
        runtimePump(second);
        Assert.Equal(2, platform.Windows[1].Session.PresentCount);

        static void runtimePump(Window window)
        {
            window.Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.Render, "test");
            WindowApplicationRuntime.Current!.PumpOnce(TimeSpan.FromMilliseconds(16));
        }
    }

    [Fact]
    public void CaretBlinkSchedulesAWindowFrameWithoutAnExternalRenderRequest()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        TextBox textBox = new()
        {
            Text = "blink",
            IsKeyboardFocused = true
        };
        Window window = new() { Content = textBox };

        window.Show();
        FakeGraphicsSession session = Assert.Single(platform.Windows).Session;
        Assert.Equal(1, session.PresentCount);

        runtime.PumpOnce(TimeSpan.FromMilliseconds(500));

        Assert.Equal(2, session.PresentCount);
    }

    [Fact]
    public void HeldRepeatButtonSchedulesFramesWithoutExternalRenderRequests()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        RepeatButton repeatButton = new() { Content = "Hold" };
        int clickCount = 0;
        repeatButton.Click += (_, _) => clickCount++;
        Window window = new() { Content = repeatButton };
        window.Show();
        FakePlatformWindow nativeWindow = Assert.Single(platform.Windows);
        float x = repeatButton.ArrangedBounds.X + (repeatButton.ArrangedBounds.Width / 2);
        float y = repeatButton.ArrangedBounds.Y + (repeatButton.ArrangedBounds.Height / 2);
        nativeWindow.Input.MovePointer(x, y);
        nativeWindow.Input.SetButton(InputMouseButton.Left, true);
        nativeWindow.RequestRender();
        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        int framesAfterPress = nativeWindow.Session.PresentCount;

        Assert.Equal(1, clickCount);

        runtime.PumpOnce(TimeSpan.FromMilliseconds(repeatButton.Delay));

        Assert.Equal(2, clickCount);
        Assert.Equal(framesAfterPress + 1, nativeWindow.Session.PresentCount);
    }

    [Fact]
    public void PendingRelayWorkWakesAnOtherwiseIdleStandaloneWindow()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Window window = new();
        window.Show();
        FakeGraphicsSession session = Assert.Single(platform.Windows).Session;
        int framesBefore = session.PresentCount;
        int executions = 0;
        window.Root!.Relay.Post(() => executions++);

        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

        Assert.Equal(1, executions);
        Assert.Equal(framesBefore + 1, session.PresentCount);
        Assert.Equal(1, window.LastFrame!.Stats.RelayExecutedCallbacks);
    }

    [Fact]
    public void SaveScreenshotDrawsACompleteCurrentFrameWithoutReplacingThePresentedBackBuffer()
    {
        FakeWindowPlatform platform = new();
        Install(platform);
        Window window = new() { Content = new TextBlock { Text = "Current frame" } };
        window.Show();
        FakeGraphicsSession session = Assert.Single(platform.Windows).Session;
        string path = Path.Combine(Path.GetTempPath(), $"cerneala-screenshot-{Guid.NewGuid():N}.png");

        try
        {
            window.SaveScreenshot(path);

            Assert.Equal(1, session.BeginFrameCount);
            Assert.Equal(2, session.Backend.RenderCount);
            Assert.Equal(1, session.PresentCount);
            Assert.Equal(1, session.SavePngCount);
            Assert.Equal(2, session.RenderCountAtSave);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RelayCallbackCanCloseWindowWithoutRenderingItsDisposedGraphicsSession()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Window window = new();
        window.Show();
        FakeGraphicsSession session = Assert.Single(platform.Windows).Session;
        window.Root!.Relay.Post(window.Close);

        runtime.PumpOnce(TimeSpan.FromMilliseconds(16));

        Assert.True(window.IsClosed);
        Assert.Equal(1, session.BeginFrameCount);
        Assert.Equal(1, session.DisposeCount);
    }

    [Fact]
    public void NativeBoundsNotificationsDoNotEchoPropertiesBackToThePlatform()
    {
        FakeWindowPlatform platform = new();
        Install(platform);
        Window window = new() { Width = 800, Height = 600 };
        window.Show();
        FakePlatformWindow nativeWindow = Assert.Single(platform.Windows);
        int appliedBeforeNotification = nativeWindow.ApplyPropertiesCount;
        UiViewport nativeViewport = new(620, 440, 1.25f);
        int locationChangedCount = 0;
        int stateChangedCount = 0;
        window.LocationChanged += (_, _) => locationChangedCount++;
        window.StateChanged += (_, _) => stateChangedCount++;

        nativeWindow.ReportBounds(nativeViewport, 120, 75, WindowState.Maximized);

        Assert.Equal(120, window.Left);
        Assert.Equal(75, window.Top);
        Assert.Equal(WindowState.Maximized, window.WindowState);
        Assert.Equal(nativeViewport, nativeWindow.Viewport);
        Assert.Equal(appliedBeforeNotification, nativeWindow.ApplyPropertiesCount);
        Assert.Equal(2, locationChangedCount);
        Assert.Equal(1, stateChangedCount);

        window.Title = "Updated after native notification";
        Assert.Equal(appliedBeforeNotification + 1, nativeWindow.ApplyPropertiesCount);
    }

    [Fact]
    public void VisualPropertyChangesDoNotReapplyNativeWindowGeometry()
    {
        FakeWindowPlatform platform = new();
        Install(platform);
        Window window = new();
        window.Show();
        FakePlatformWindow nativeWindow = Assert.Single(platform.Windows);
        int appliedBeforeVisualChange = nativeWindow.ApplyPropertiesCount;

        window.Background = new Cerneala.UI.Media.SolidColorBrush(new Color(10, 20, 30));

        Assert.Equal(appliedBeforeVisualChange, nativeWindow.ApplyPropertiesCount);
    }

    [Fact]
    public async Task DialogInfersActiveOwnerAndRestoresItWhenResultClosesDialog()
    {
        FakeWindowPlatform platform = new();
        Install(platform);
        Window owner = new();
        owner.Show();
        owner.Activate();
        Window dialog = new();

        Task<bool?> result = dialog.ShowDialogAsync();

        Assert.Same(owner, dialog.Owner);
        Assert.False(platform.Windows[0].Enabled);
        dialog.DialogResult = true;

        Assert.True(await result);
        Assert.True(platform.Windows[0].Enabled);
        Assert.True(dialog.IsClosed);
        Assert.Empty(owner.OwnedWindows);
    }

    [Fact]
    public void ClosingCanCancelAndClosedWindowCannotBeShownAgain()
    {
        Install(new FakeWindowPlatform());
        Window window = new();
        bool cancel = true;
        window.Closing += (_, args) => args.Cancel = cancel;
        window.Show();

        window.Close();
        Assert.False(window.IsClosed);

        cancel = false;
        window.Close();
        Assert.True(window.IsClosed);
        Assert.Throws<InvalidOperationException>(window.Show);
    }

    [Fact]
    public void ClosingMainWindowClosesEveryWindowInTheRuntime()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        Window main = new();
        Window secondary = new();
        runtime.StartMainWindow(main);
        secondary.Show();

        main.Close();

        Assert.True(main.IsClosed);
        Assert.True(secondary.IsClosed);
        Assert.Empty(runtime.Windows);
        Assert.All(platform.Windows, window => Assert.True(window.Destroyed));
    }

    [Fact]
    public async Task WindowOperationsRejectAnotherThread()
    {
        Install(new FakeWindowPlatform());
        Window window = new();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(window.Show));

        Assert.Contains("owning UI thread", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void OwnershipRejectsSelfAndCyclesBeforeWindowsAreShown()
    {
        Window first = new();
        Window second = new() { Owner = first };

        Assert.Throws<InvalidOperationException>(() => first.Owner = first);
        Assert.Throws<InvalidOperationException>(() => first.Owner = second);
    }

    [Fact]
    public void GeneratedHostedStartupUsesDiOnceAndLeavesTheExternalHostInControl()
    {
        FakeWindowPlatform platform = new();
        WindowApplicationRuntime runtime = Install(platform);
        int factoryCalls = 0;
        GeneratedWindowStartupDescriptor descriptor = new(
            services => services.AddTransient<Window>(_ =>
            {
                factoryCalls++;
                return new Window { Title = "Generated" };
            }),
            provider => provider.GetRequiredService<Window>());
        GeneratedWindowApplication.RegisterStartup(descriptor);

        GeneratedWindowApplication.PumpHosted(TimeSpan.FromMilliseconds(16));
        GeneratedWindowApplication.PumpHosted(TimeSpan.FromMilliseconds(16));

        Assert.Equal(1, factoryCalls);
        Assert.Single(runtime.Windows);
        Assert.Equal("Generated", runtime.Windows[0].Title);
        Assert.Equal(2, platform.PumpCount);

        GeneratedWindowApplication.StopHosted();
        Assert.Empty(runtime.Windows);
    }

    private static WindowApplicationRuntime Install(FakeWindowPlatform platform)
    {
        WindowApplicationRuntime runtime = new(platform);
        WindowApplicationRuntime.Install(runtime);
        return runtime;
    }

    private sealed class FakeWindowPlatform : IWindowPlatform
    {
        public List<FakePlatformWindow> Windows { get; } = [];

        public int PumpCount { get; private set; }

        public IPlatformWindow CreateWindow(Window window, IWindowPlatformCallbacks callbacks)
        {
            FakePlatformWindow created = new(window, callbacks, Windows.Count + 1);
            Windows.Add(created);
            return created;
        }

        public void PumpEvents()
        {
            PumpCount++;
        }

        public void Dispose()
        {
            foreach (FakePlatformWindow window in Windows)
            {
                window.Dispose();
            }
        }
    }

    private sealed class FakePlatformWindow : IPlatformWindow
    {
        private readonly Window window;
        private readonly IWindowPlatformCallbacks callbacks;

        public FakePlatformWindow(Window window, IWindowPlatformCallbacks callbacks, int handle)
        {
            this.window = window;
            this.callbacks = callbacks;
            Handle = handle;
        }

        public nint Handle { get; }

        public UiViewport Viewport { get; private set; } = new(800, 600);

        public MutableInputSource Input { get; } = new();

        public IInputSource InputSource => Input;

        public FakeGraphicsSession Session { get; } = new();

        public FakeDrawingBackend Backend => Session.Backend;

        public IWindowGraphicsSession GraphicsSession => Session;

        public bool Enabled { get; private set; } = true;

        public bool Destroyed { get; private set; }

        public int ApplyPropertiesCount { get; private set; }

        public void ApplyProperties(Window source)
        {
            ApplyPropertiesCount++;
            Viewport = new UiViewport(source.Width, source.Height);
        }

        public void ReportBounds(UiViewport viewport, float left, float top, WindowState state)
        {
            Viewport = viewport;
            callbacks.BoundsChanged(viewport, left, top, state);
        }

        public void RequestRender()
        {
            callbacks.RenderRequested();
        }

        public void SetOwner(IPlatformWindow? owner)
        {
        }

        public void SetEnabled(bool enabled)
        {
            Enabled = enabled;
        }

        public void Show()
        {
        }

        public void Hide()
        {
        }

        public void Activate()
        {
            callbacks.ActivationChanged(true);
        }

        public void Destroy()
        {
            Destroyed = true;
        }

        public void Dispose()
        {
            Session.Dispose();
        }
    }

    private sealed class MutableInputSource : IInputSource
    {
        private PointerSnapshot previousPointer = PointerSnapshot.Empty;
        private PointerSnapshot currentPointer = PointerSnapshot.Empty;

        public InputFrame GetFrame()
        {
            InputFrame frame = new(
                previousPointer,
                currentPointer,
                KeyboardSnapshot.Empty,
                KeyboardSnapshot.Empty,
                []);
            previousPointer = currentPointer;
            return frame;
        }

        public void MovePointer(float x, float y)
        {
            currentPointer = currentPointer.WithPosition(x, y);
        }

        public void SetButton(InputMouseButton button, bool down)
        {
            currentPointer = currentPointer.WithButton(button, down);
        }
    }

    private sealed class FakeDrawingBackend : IDrawingBackend
    {
        public int RenderCount { get; private set; }

        public void Render(DrawCommandList commands)
        {
            RenderCount++;
        }
    }

    private sealed class FakeGraphicsSession : IWindowGraphicsSession, IWindowScreenshotSource
    {
        private bool disposed;

        public FakeDrawingBackend Backend { get; } = new();

        public IDrawingBackend DrawingBackend => Backend;

        public IImageLoader? ImageLoader => null;

        public ImageResourceCache? ImageResourceCache => null;

        public int BeginFrameCount { get; private set; }

        public int PresentCount { get; private set; }

        public int DisposeCount { get; private set; }

        public int SavePngCount { get; private set; }

        public int RenderCountAtSave { get; private set; }

        public void Resize(int pixelWidth, int pixelHeight, float coordinateScale)
        {
        }

        public void BeginFrame(Color clearColor)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            BeginFrameCount++;
        }

        public void Present()
        {
            PresentCount++;
        }

        public void RenderPng(Stream output, Color clearColor, Action<IDrawingBackend> draw)
        {
            SavePngCount++;
            draw(Backend);
            RenderCountAtSave = Backend.RenderCount;
            output.WriteByte(0);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            DisposeCount++;
            disposed = true;
        }
    }
}

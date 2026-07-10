using Cerneala.Drawing;
using Cerneala.UI.Controls;
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

        window.Show();
        Assert.True(window.IsShown);
        Assert.Single(events, value => value == "rendered");
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
        public IImageLoader? ImageLoader => null;

        public ImageResourceCache? ImageResourceCache => null;

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

        public IInputSource InputSource { get; } = new EmptyInputSource();

        public FakeDrawingBackend Backend { get; } = new();

        IDrawingBackend IPlatformWindow.DrawingBackend => Backend;

        public bool Enabled { get; private set; } = true;

        public bool Destroyed { get; private set; }

        public void ApplyProperties(Window source)
        {
            Viewport = new UiViewport(source.Width, source.Height);
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

        public void Present()
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class EmptyInputSource : IInputSource
    {
        public InputFrame GetFrame()
        {
            return new InputFrame(
                PointerSnapshot.Empty,
                PointerSnapshot.Empty,
                KeyboardSnapshot.Empty,
                KeyboardSnapshot.Empty,
                []);
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
}

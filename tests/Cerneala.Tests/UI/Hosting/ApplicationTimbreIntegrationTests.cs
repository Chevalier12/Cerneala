using Cerneala.Drawing;
using Cerneala.Timbre;
using Cerneala.UI;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.UI.Hosting;

[Collection(WindowRuntimeTestCollection.Name)]
public sealed class ApplicationTimbreIntegrationTests : IDisposable
{
    public ApplicationTimbreIntegrationTests()
    {
        Application.ResetForTesting();
        WindowApplicationRuntime.ResetForTesting();
    }

    public void Dispose()
    {
        WindowApplicationRuntime.ResetForTesting();
        Application.ResetForTesting();
    }

    [Fact]
    public void WindowRootsShareTheApplicationRuntimeAndClosingAWindowRetiresOnlyItsScopes()
    {
        using TimbreRuntime timbreRuntime = new(new TimbreRuntimeOptions { Output = new FullOutput() });
        WindowApplicationRuntime runtime = new(new FakeWindowPlatform());
        WindowApplicationRuntime.Install(runtime);
        Application app = new() { ShutdownMode = ApplicationShutdownMode.OnExplicitShutdown, TimbreRuntime = timbreRuntime };
        app.Install(runtime);
        Window first = new();
        Window second = new();
        first.Show();
        second.Show();
        TimbreSound clip = new(TimbreSource.FromReader(() => new SilentReader()));

        Assert.Same(timbreRuntime, first.Root!.TimbreRuntime);
        Assert.Same(timbreRuntime, second.Root!.TimbreRuntime);
        TimbreScope firstScope = first.Timbre;
        TimbreScope appScope = app.Timbre;
        TimbrePlayback firstPlayback = firstScope.Play(clip);
        TimbrePlayback secondPlayback = second.Timbre.Play(clip);
        TimbrePlayback appPlayback = appScope.Play(clip);

        first.Close();

        Assert.True(firstScope.IsDisposed);
        Assert.Equal(TimbrePlaybackState.Canceled, firstPlayback.State);
        Assert.Equal(TimbrePlaybackState.Pending, secondPlayback.State);
        Assert.Equal(TimbrePlaybackState.Pending, appPlayback.State);

        app.Shutdown();

        Assert.Equal(TimbrePlaybackState.Canceled, secondPlayback.State);
        Assert.Equal(TimbrePlaybackState.Canceled, appPlayback.State);
        Assert.True(appScope.IsDisposed);
        Assert.False(timbreRuntime.IsDisposed); // assigned runtimes stay caller-owned
    }

    // A device whose queue is always full: playbacks stay pending, nothing mixes.
    private sealed class FullOutput : ITimbreOutput
    {
        public int QueuedFrames => int.MaxValue;

        public void Open(ITimbreOutputClient client)
        {
        }

        public void Submit(ReadOnlySpan<float> samples) => throw new InvalidOperationException("The full output accepts no PCM.");

        public void Close()
        {
        }
    }

    private sealed class SilentReader : TimbreReader
    {
        public override long? LengthFrames => 48000;

        public override ValueTask<TimbreReadResult> ReadAsync(Memory<float> destination, CancellationToken cancellationToken)
        {
            destination.Span.Clear();
            return ValueTask.FromResult(new TimbreReadResult(destination.Length / 2, endOfSource: false));
        }

        public override ValueTask SeekAsync(long frame, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }

    private sealed class FakeWindowPlatform : IWindowPlatform
    {
        public IPlatformWindow CreateWindow(Window window, IWindowPlatformCallbacks callbacks) => new FakePlatformWindow();

        public void PumpEvents()
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakePlatformWindow : IPlatformWindow
    {
        public IWindowSurface Surface { get; } = new FakeWindowSurface();

        public UiViewport Viewport { get; } = new(800, 600);

        public IInputSource InputSource { get; } = new EmptyInputSource();

        public IWindowGraphicsSession GraphicsSession { get; } = new FakeGraphicsSession();

        public void ApplyProperties(Window source)
        {
        }

        public void SetOwner(IPlatformWindow? owner)
        {
        }

        public void SetEnabled(bool enabled)
        {
        }

        public void Show()
        {
        }

        public void Hide()
        {
        }

        public void Activate()
        {
        }

        public void Destroy()
        {
        }

        public void Dispose() => GraphicsSession.Dispose();
    }

    private sealed class FakeWindowSurface : IWindowSurface
    {
    }

    private sealed class EmptyInputSource : IInputSource
    {
        public InputFrame GetFrame() =>
            new(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private sealed class FakeGraphicsSession : IWindowGraphicsSession
    {
        public IDrawingBackend DrawingBackend { get; } = new NullDrawingBackend();

        public IImageLoader? ImageLoader => null;

        public ImageResourceCache? ImageResourceCache => null;

        public void Resize(int pixelWidth, int pixelHeight, float coordinateScale)
        {
        }

        public void BeginFrame(Color clearColor)
        {
        }

        public void Present()
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class NullDrawingBackend : IDrawingBackend
    {
        public void Render(DrawCommandList commands, in DrawingFrameContext frameContext)
        {
        }
    }
}
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.SdlGpu;

internal sealed class RecordingWindowCallbacks : IWindowPlatformCallbacks
{
    public int CloseRequests { get; private set; }
    public List<bool> Activations { get; } = [];
    public List<(UiViewport Viewport, float Left, float Top, WindowState State)> Bounds { get; } = [];
    public int RenderRequests { get; private set; }
    public int ImmediateRenderRequests { get; private set; }

    public void RequestClose() => CloseRequests++;

    public void ActivationChanged(bool active) => Activations.Add(active);

    public void BoundsChanged(UiViewport viewport, float left, float top, WindowState state) =>
        Bounds.Add((viewport, left, top, state));

    public void RenderRequested() => RenderRequests++;

    public void RenderImmediately() => ImmediateRenderRequests++;
}

internal sealed class RecordingGraphicsFactory : IWindowGraphicsSessionFactory
{
    public List<RecordingGraphicsSession> Sessions { get; } = [];
    public List<IWindowSurface> Surfaces { get; } = [];

    public IWindowGraphicsSession Create(
        IWindowSurface windowSurface,
        int pixelWidth,
        int pixelHeight,
        float coordinateScale)
    {
        RecordingGraphicsSession session = new(pixelWidth, pixelHeight, coordinateScale);
        Surfaces.Add(windowSurface);
        Sessions.Add(session);
        return session;
    }
}

internal sealed class RecordingGraphicsSession(
    int pixelWidth,
    int pixelHeight,
    float coordinateScale) : IWindowGraphicsSession
{
    public IDrawingBackend DrawingBackend { get; } = new RecordingDrawingBackend();
    public IImageLoader? ImageLoader => null;
    public ImageResourceCache? ImageResourceCache => null;
    public int PixelWidth { get; private set; } = pixelWidth;
    public int PixelHeight { get; private set; } = pixelHeight;
    public float CoordinateScale { get; private set; } = coordinateScale;
    public int ResizeCount { get; private set; }
    public int DisposeCount { get; private set; }

    public void Resize(int nextPixelWidth, int nextPixelHeight, float nextCoordinateScale)
    {
        PixelWidth = nextPixelWidth;
        PixelHeight = nextPixelHeight;
        CoordinateScale = nextCoordinateScale;
        ResizeCount++;
    }

    public void BeginFrame(Color clearColor) { }

    public void Present() { }

    public void Dispose() => DisposeCount++;

    private sealed class RecordingDrawingBackend : IDrawingBackend
    {
        public void Render(DrawCommandList commands, in DrawingFrameContext frameContext) { }
    }
}

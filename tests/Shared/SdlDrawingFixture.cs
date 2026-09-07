using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Hosting.Windowing;

namespace Cerneala.Tests.Drawing.SdlGpu;

// Shared by the drawing-contract and native SDL test projects.
internal sealed class SdlDrawingFixture : IDisposable
{
    private readonly NativeSdlApi api = new();
    private readonly SdlPlatformLifetime lifetime;
    private readonly SdlGpuWindowGraphicsSessionFactory factory;
    private readonly nint window;
    private readonly PrismBackdropSourceToken backdropSource = PrismBackdropSourceToken.CreateUnique();

    public SdlDrawingFixture(int width = 96, int height = 64, bool useMultisampling = false, float coordinateScale = 1)
    {
        lifetime = new SdlPlatformLifetime(api);
        factory = new SdlGpuWindowGraphicsSessionFactory(api, useMultisampling);
        try
        {
            window = api.CreateWindow("Cerneala drawing contract", width, height, SdlWindowOptions.Hidden);
            if (window == 0) { throw SdlApiError.Create(api, "Test window creation"); }
            Session = Assert.IsType<SdlGpuWindowGraphicsSession>(factory.Create(
                new SdlWindowSurface(window, api.GetWindowId(window)), width, height, coordinateScale));
        }
        catch
        {
            factory.Dispose();
            if (window != 0) { api.DestroyWindow(window); }
            lifetime.Dispose();
            throw;
        }
    }

    public SdlGpuWindowGraphicsSession Session { get; }

    public SdlGpuDrawingBackend Backend => Assert.IsType<SdlGpuDrawingBackend>(Session.DrawingBackend);

    public Color[] Render(DrawCommandList commands, Color? clearColor = null)
        => Render(Session, commands, clearColor, backdropSource);

    internal static Color[] Render(
        SdlGpuWindowGraphicsSession session,
        DrawCommandList commands,
        Color? clearColor = null,
        PrismBackdropSourceToken backdropSource = default)
    {
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        session.BeginFrame(clearColor ?? Color.Black);
        try
        {
            using IBackdropFrameLease? backdrop = analysis.RequiresBackdrop
                ? session.AcquireFrame(new BackdropFrameRequest(
                    session.PixelWidth, session.PixelHeight, session.CoordinateScale, analysis.BackdropRequirement))
                : null;
            DrawingFrameContext frame = new(analysis, backdrop, backdrop is null ? default : backdropSource);
            session.DrawingBackend.Render(commands, in frame);
        }
        finally
        {
            session.CompleteFrame(present: false);
        }

        WindowPreviewFrame pixels = session.CapturePresentedFrame();
        Color[] colors = new Color[pixels.PixelWidth * pixels.PixelHeight];
        for (int index = 0; index < colors.Length; index++)
        {
            int offset = index * 4;
            colors[index] = new Color(
                pixels.Pixels[offset], pixels.Pixels[offset + 1], pixels.Pixels[offset + 2], pixels.Pixels[offset + 3]);
        }
        return colors;
    }

    public Color RenderCenterPixel(DrawCommandList commands) =>
        Render(commands)[((Session.PixelHeight / 2) * Session.PixelWidth) + (Session.PixelWidth / 2)];

    public Color Sample(Color[] pixels, float x, float y) => pixels[
        (Cerneala.UI.Hosting.UiCoordinateMapper.LogicalToPhysicalPixel(y, Session.CoordinateScale) * Session.PixelWidth) +
        Cerneala.UI.Hosting.UiCoordinateMapper.LogicalToPhysicalPixel(x, Session.CoordinateScale)];

    public static SdlGpuImage SolidImage(Color color) =>
        new(1, 1, [color.R, color.G, color.B, color.A]);

    public void Dispose()
    {
        try { Session.Dispose(); }
        finally
        {
            try { factory.Dispose(); }
            finally
            {
                api.DestroyWindow(window);
                lifetime.Dispose();
            }
        }
    }
}

internal sealed class RecordedSurface(
    Action<DrawCommandList, DrawRect> recordFrame,
    Color clearColor) : IRenderSurface2DFrameSource, IDisposable
{
    private readonly Dictionary<object, IRenderSurface2DBackendState> states = new(ReferenceEqualityComparer.Instance);

    public Color ClearColor { get; } = clearColor;
    public long FrameVersion { get; set; } = 1;

    public void RecordFrame(DrawCommandList commands, DrawRect bounds) => recordFrame(commands, bounds);

    public IRenderSurface2DBackendState? GetBackendState(object owner) => states.GetValueOrDefault(owner);

    public void SetBackendState(object owner, IRenderSurface2DBackendState? state)
    {
        if (states.Remove(owner, out IRenderSurface2DBackendState? previous) && !ReferenceEquals(previous, state))
        {
            previous.Dispose();
        }
        if (state is not null) { states.Add(owner, state); }
    }

    public void Dispose()
    {
        foreach (IRenderSurface2DBackendState state in states.Values) { state.Dispose(); }
        states.Clear();
    }
}

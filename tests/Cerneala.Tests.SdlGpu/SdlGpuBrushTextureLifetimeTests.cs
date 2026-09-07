using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Text;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Media;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuBrushTextureLifetimeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BrushTextureIsRetainedUntilTheLastWindowStopsUsingIt(bool textBrush)
    {
        FakeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory factory = new(api);
        using SdlGpuWindowGraphicsSession first = CreateSession("first");
        using SdlGpuWindowGraphicsSession second = CreateSession("second");
        Assert.Same(first.DrawingResources, second.DrawingResources);
        SdlGpuDrawingResources resources = first.DrawingResources;
        int initialCount = resources.CachedTextureCount;
        LinearGradientBrush brush = new(new DrawPoint(0, 0), new DrawPoint(32, 0),
            [new GradientStop(0, Color.Black), new GradientStop(1, Color.White)]);
        DrawCommandList commands = new();
        commands.Add(textBrush
            ? DrawCommand.DrawText(new DrawTextRun(new SystemFontSource().LoadFont("Arial", 12),
                "Brush", 12), default, brush)
            : DrawCommand.FillRectangle(new DrawRect(0, 0, 32, 24), brush));

        Render(first, commands);
        Assert.Equal(initialCount + 1, resources.CachedTextureCount);
        Render(second, commands);
        Assert.Equal(initialCount + 1, resources.CachedTextureCount);
        Render(first, new DrawCommandList());
        Assert.Equal(initialCount + 1, resources.CachedTextureCount);
        Render(second, new DrawCommandList());
        Assert.Equal(initialCount, resources.CachedTextureCount);

        Render(first, commands);
        Render(second, commands);
        first.Dispose();
        Assert.Equal(initialCount + 1, resources.CachedTextureCount);
        second.Dispose();
        Assert.Equal(initialCount, resources.CachedTextureCount);

        SdlGpuWindowGraphicsSession CreateSession(string title)
        {
            nint window = api.CreateWindow(title, 32, 24, SdlWindowOptions.Hidden);
            return Assert.IsType<SdlGpuWindowGraphicsSession>(factory.Create(
                new SdlWindowSurface(window, api.GetWindowId(window)), 32, 24, 1));
        }
    }

    private static void Render(SdlGpuWindowGraphicsSession session, DrawCommandList commands)
    {
        session.BeginFrame(Color.Transparent);
        try
        {
            DrawingFrameContext context = new(new PrismFrameAnalyzer().Analyze(commands));
            session.DrawingBackend.Render(commands, in context);
        }
        finally
        {
            session.CompleteFrame(present: false);
        }
    }
}

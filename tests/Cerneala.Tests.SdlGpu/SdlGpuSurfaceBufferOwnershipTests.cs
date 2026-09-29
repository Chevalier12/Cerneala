using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Platforms.Sdl3;
using Cerneala.Tests.Drawing.SdlGpu;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class SdlGpuSurfaceBufferOwnershipTests
{
    [SdlNativeFact]
    public void ClearColorReplayBeforeSubmitPreservesPixelsAfterContentChanges()
    {
        using SdlDrawingFixture fixture = new(64, 64);
        RenderSurface2D surface = new() { ClearColor = Color.Black };
        Color content = Color.Coral;
        surface.Draw += (_, frame) => frame.FillRectangle(new DrawRect(0, 0, 8, 8), content);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.RenderSurface2D(surface, new DrawRect(0, 0, 32, 32), Color.White));
        try
        {
            fixture.Session.BeginFrame(Color.Black);
            try
            {
                DrawingFrameContext context = new(new PrismFrameAnalyzer().Analyze(commands));
                fixture.Backend.Render(commands, in context);
                surface.ClearColor = Color.White;
                fixture.Backend.Render(commands, in context);
            }
            finally { fixture.Session.CompleteFrame(present: false); }

            content = Color.CornflowerBlue;
            surface.InvalidateFrame();
            Color[] retained = fixture.Render(commands);
            Assert.Equal(content, fixture.Sample(retained, 4, 4));
            ((IRenderSurface2DFrameSource)surface).SetBackendState(fixture.Session.DrawingResources, null);
            Assert.Equal(fixture.Render(commands), retained);
        }
        finally
        {
            ((IRenderSurface2DFrameSource)surface).SetBackendState(fixture.Session.DrawingResources, null);
        }
    }

    [Fact]
    public void ClearColorReplayBeforeSubmitDoesNotHideTheNextContentChange()
    {
        FakeSdlApi api = new();
        nint window = api.CreateWindow("surface buffer ownership", 64, 64, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(new SdlWindowSurface(window, api.GetWindowId(window)), 64, 64, 1));
        RenderSurface2D surface = new() { ClearColor = Color.Black };
        Color content = Color.Coral;
        surface.Draw += (_, frame) => frame.FillRectangle(new DrawRect(0, 0, 8, 8), content);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.RenderSurface2D(surface, new DrawRect(0, 0, 32, 32), Color.White));
        try
        {
            session.BeginFrame(Color.Black);
            Render();
            surface.ClearColor = Color.White;
            Render();
            session.CompleteFrame(present: false);

            int targetEvents = api.RenderTargets.Count;
            content = Color.CornflowerBlue;
            surface.InvalidateFrame();
            session.BeginFrame(Color.Black);
            Render();
            session.CompleteFrame(present: false);

            Assert.Contains(api.RenderTargets.Skip(targetEvents), target =>
                api.GpuTextures.TryGetValue(target.Texture, out var texture) &&
                texture.CreateInfo.Width == 32 && texture.CreateInfo.Height == 32);
        }
        finally
        {
            ((IRenderSurface2DFrameSource)surface).SetBackendState(session.DrawingResources, null);
        }

        void Render()
        {
            DrawingFrameContext context = new(new PrismFrameAnalyzer().Analyze(commands));
            session.DrawingBackend.Render(commands, in context);
        }
    }
}

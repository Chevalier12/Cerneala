using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.Drawing.SdlGpu;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class RenderSurface3DStageZeroNativeCharacterizationTests
{
    [SdlNativeFact]
    public void SurfaceAndLaterOverlayPreserveCompositionOrder()
    {
        using SdlDrawingFixture fixture = new(64, 32, useMultisampling: false);
        using RecordedSurface surface = new(
            (commands, bounds) => commands.Add(DrawCommand.FillRectangle(bounds, Color.CornflowerBlue)),
            Color.Black);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.RenderSurface2D(surface, new DrawRect(0, 0, 64, 32), Color.White));
        commands.Add(DrawCommand.FillRectangle(new DrawRect(32, 0, 32, 32), Color.HotPink));

        Color[] pixels = fixture.Render(commands);

        Assert.Equal(Color.CornflowerBlue, fixture.Sample(pixels, 16, 16));
        Assert.Equal(Color.HotPink, fixture.Sample(pixels, 48, 16));
    }

    [SdlNativeFact]
    public void ClipAndOpacityApplyToAPrismSurfaceAsOneComposedImage()
    {
        using SdlDrawingFixture fixture = new(64, 32, useMultisampling: false);
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "StageZeroSurfacePrism",
                PrismTestData.Layer(1, "Content")),
            bounds: new DrawRect(0, 0, 64, 32));
        using RecordedSurface surface = new((commands, bounds) =>
        {
            commands.Add(DrawCommand.BeginPrism(scope));
            commands.Add(DrawCommand.FillRectangle(bounds, Color.LimeGreen));
            commands.Add(DrawCommand.EndPrism());
        }, Color.Black);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.PushClip(new DrawRect(0, 0, 32, 32)));
        commands.Add(DrawCommand.PushOpacity(0.5f));
        commands.Add(DrawCommand.RenderSurface2D(surface, new DrawRect(0, 0, 64, 32), Color.White));
        commands.Add(DrawCommand.PopOpacity());
        commands.Add(DrawCommand.PopClip());

        Color[] pixels = fixture.Render(commands);
        Color inside = fixture.Sample(pixels, 16, 16);
        Color outside = fixture.Sample(pixels, 48, 16);

        Assert.InRange(inside.R, 24, 26);
        Assert.InRange(inside.G, 101, 104);
        Assert.InRange(inside.B, 24, 26);
        Assert.Equal(Color.Black, outside);
        Assert.True(fixture.Backend.LastFramePrismCounters.PassCount > 0);
    }

    [SdlNativeFact]
    public void TwoSurfacesInOneWindowKeepDistinctTargetsAndPixels()
    {
        using SdlDrawingFixture fixture = new(64, 32, useMultisampling: false);
        using RecordedSurface left = new(
            (commands, bounds) => commands.Add(DrawCommand.FillRectangle(bounds, Color.CornflowerBlue)),
            Color.Black);
        using RecordedSurface right = new(
            (commands, bounds) => commands.Add(DrawCommand.FillRectangle(bounds, Color.LimeGreen)),
            Color.Black);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.RenderSurface2D(left, new DrawRect(0, 0, 32, 32), Color.White));
        commands.Add(DrawCommand.RenderSurface2D(right, new DrawRect(32, 0, 32, 32), Color.White));

        Color[] pixels = fixture.Render(commands);

        Assert.Equal(Color.CornflowerBlue, fixture.Sample(pixels, 16, 16));
        Assert.Equal(Color.LimeGreen, fixture.Sample(pixels, 48, 16));
        Assert.NotSame(
            ((IRenderSurface2DFrameSource)left).GetBackendState(fixture.Session.DrawingResources),
            ((IRenderSurface2DFrameSource)right).GetBackendState(fixture.Session.DrawingResources));
    }
}

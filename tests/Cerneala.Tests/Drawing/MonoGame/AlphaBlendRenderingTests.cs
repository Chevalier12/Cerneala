using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Media;
using Microsoft.Xna.Framework.Graphics;
using CernealaColor = Cerneala.Drawing.Color;

namespace Cerneala.Tests.Drawing.MonoGame;

public sealed class AlphaBlendRenderingTests
{
    [Fact]
    public void SemiTransparentSolidColorBlendsWithBackground()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        DrawCommandList commands = new();
        commands.Add(DrawCommand.FillRectangle(
            new DrawRect(0, 0, 96, 64),
            new CernealaColor(64, 160, 192, 128)));

        Microsoft.Xna.Framework.Color actual = RenderCenterPixel(fixture, commands);

        Assert.InRange(actual.R, 30, 34);
        Assert.InRange(actual.G, 78, 82);
        Assert.InRange(actual.B, 94, 98);
    }

    [Fact]
    public void TransparentGradientStopRevealsBackgroundInsteadOfKeepingSolidRgb()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        LinearGradientBrush alphaRamp = new(
            new DrawPoint(0, 0),
            new DrawPoint(96, 0),
            [
                new GradientStop(0, new CernealaColor(64, 160, 192, 0)),
                new GradientStop(1, new CernealaColor(64, 160, 192, 255))
            ]);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.FillRectangle(new DrawRect(0, 0, 96, 64), alphaRamp));

        Render(fixture, commands);

        PresentationParameters parameters = fixture.Session.GraphicsDevice.PresentationParameters;
        Microsoft.Xna.Framework.Color[] pixels =
            new Microsoft.Xna.Framework.Color[parameters.BackBufferWidth * parameters.BackBufferHeight];
        fixture.Session.GraphicsDevice.GetBackBufferData(pixels);
        int middleRow = parameters.BackBufferHeight / 2;
        MonoGameDrawingBackend backend = Assert.IsType<MonoGameDrawingBackend>(fixture.Session.DrawingBackend);
        int opaqueX = Math.Min(
            parameters.BackBufferWidth - 2,
            (int)MathF.Round(96 * backend.CoordinateScale) - 2);
        Microsoft.Xna.Framework.Color transparentEnd = pixels[(middleRow * parameters.BackBufferWidth) + 1];
        Microsoft.Xna.Framework.Color opaqueEnd = pixels[
            (middleRow * parameters.BackBufferWidth) + opaqueX];

        Assert.InRange(transparentEnd.R, 0, 4);
        Assert.InRange(transparentEnd.G, 0, 4);
        Assert.InRange(transparentEnd.B, 0, 4);
        Assert.InRange(opaqueEnd.R, 58, 66);
        Assert.InRange(opaqueEnd.G, 150, 162);
        Assert.InRange(opaqueEnd.B, 181, 194);
    }

    [Fact]
    public void DynamicBrushTexturesDoNotAccumulateAcrossFrames()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        MonoGameDrawingBackend backend = Assert.IsType<MonoGameDrawingBackend>(fixture.Session.DrawingBackend);
        int initialCount = backend.BrushTextureCacheCount;

        for (int frame = 0; frame < 64; frame++)
        {
            CernealaColor color = new((byte)(frame * 3), (byte)(255 - (frame * 3)), 160);
            LinearGradientBrush brush = new(
                new DrawPoint(0, 0),
                new DrawPoint(220, 0),
                [
                    new GradientStop(0, new CernealaColor(color.R, color.G, color.B, 0)),
                    new GradientStop(1, color)
                ]);
            DrawCommandList commands = new();
            commands.Add(DrawCommand.FillRectangle(new DrawRect(0, 0, 220, 16), brush));

            Render(fixture, commands);
        }

        Assert.InRange(backend.BrushTextureCacheCount, 1, initialCount + 2);
    }

    private static Microsoft.Xna.Framework.Color RenderCenterPixel(
        PrismGraphExecutorTests.WindowsDxFixture fixture,
        DrawCommandList commands)
    {
        Render(fixture, commands);
        PresentationParameters parameters = fixture.Session.GraphicsDevice.PresentationParameters;
        Microsoft.Xna.Framework.Color[] pixels =
            new Microsoft.Xna.Framework.Color[parameters.BackBufferWidth * parameters.BackBufferHeight];
        fixture.Session.GraphicsDevice.GetBackBufferData(pixels);
        return pixels[((parameters.BackBufferHeight / 2) * parameters.BackBufferWidth) +
            (parameters.BackBufferWidth / 2)];
    }

    private static void Render(
        PrismGraphExecutorTests.WindowsDxFixture fixture,
        DrawCommandList commands)
    {
        fixture.Session.BeginFrame(CernealaColor.Black);
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        DrawingFrameContext frameContext = new(analysis);
        fixture.Session.DrawingBackend.Render(commands, in frameContext);
        fixture.Session.Present();
    }
}

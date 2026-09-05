using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.UI.Media;
using Microsoft.Xna.Framework.Graphics;
using CernealaColor = Cerneala.Drawing.Color;

namespace Cerneala.Tests.Drawing.MonoGame;

public sealed class AlphaBlendRenderingTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void OpaqueStrokeOccludesEarlierStrokeAroundTranslucentContent(int content)
    {
        if (!OperatingSystem.IsWindows()) { return; }
        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        DrawPen hidden = new(new SolidColorBrush(new CernealaColor(129, 221, 151)), 0.75f);
        DrawPen covering = new(new SolidColorBrush(new CernealaColor(255, 80, 220)), 0.75f);
        IDrawFont font = new Cerneala.Drawing.Text.SystemFontSource().LoadFont("Consolas", 5);
        Microsoft.Xna.Framework.Color[] withHidden = RenderVariant(true);
        Microsoft.Xna.Framework.Color[] withoutHidden = RenderVariant(false);
        int maximumDelta = 0;
        for (int pixel = 0; pixel < withHidden.Length; pixel++)
        {
            var a = withHidden[pixel];
            var b = withoutHidden[pixel];
            maximumDelta = Math.Max(maximumDelta, Math.Max(Math.Abs(a.R - b.R),
                Math.Max(Math.Abs(a.G - b.G), Math.Abs(a.B - b.B))));
        }
        Assert.True(maximumDelta <= 1,
            $"A fully occluded stroke changed the output by {maximumDelta}/255 (interleaved content: {content}).");

        Microsoft.Xna.Framework.Color[] RenderVariant(bool drawHidden)
        {
            using MonoGameRenderSurface2DSession surface = new(fixture.Session.GraphicsDevice, 180, 90);
            surface.Render((commands, _) =>
            {
                DrawingContext drawing = new(commands);
                using DrawTransformScope transform = drawing.Transform(
                    System.Numerics.Matrix3x2.CreateRotation(0.2f) *
                    System.Numerics.Matrix3x2.CreateScale(1.5f) *
                    System.Numerics.Matrix3x2.CreateTranslation(20, 25));
                DrawRect rectangle = new(0, 0, 24, 24);
                if (drawHidden) { drawing.DrawRectangle(rectangle, hidden); }
                if (content == 1)
                {
                    drawing.DrawText(new DrawTextRun(font, "L00000001 MFFFFFFFF solid", 5),
                        default, new CernealaColor(129, 221, 151));
                }
                else if (content == 2)
                {
                    drawing.FillRectangle(new DrawRect(0, -5, 60, 10),
                        new CernealaColor(129, 221, 151, 128));
                }
                drawing.DrawRectangle(rectangle, covering);
            }, new CernealaColor(12, 20, 32), frameVersion: 1);
            fixture.Session.GraphicsDevice.SetRenderTarget(null);
            Microsoft.Xna.Framework.Color[] pixels = new Microsoft.Xna.Framework.Color[180 * 90];
            surface.Surface.GetData(pixels);
            return pixels;
        }
    }

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
    public void SemiTransparentSpriteBatchBlendsWithBackground()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        using Texture2D texture = new(fixture.Session.GraphicsDevice, 1, 1);
        texture.SetData([new Microsoft.Xna.Framework.Color(200, 100, 50, 255)]);
        using MonoGameImage image = new(texture);
        DrawSpriteBatch batch = new(
            image,
            [new DrawSprite2D(
                new DrawRect(0, 0, 96, 64),
                new DrawImageOptions(opacity: 0.5f))]);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.FillRectangle(
            new DrawRect(0, 0, 96, 64),
            new CernealaColor(20, 40, 60)));
        commands.Add(DrawCommand.DrawSpriteBatch(batch));

        Microsoft.Xna.Framework.Color actual = RenderCenterPixel(fixture, commands);

        Assert.InRange(actual.R, 108, 112);
        Assert.InRange(actual.G, 68, 72);
        Assert.InRange(actual.B, 53, 57);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    public void SpriteBatchInsideOpacityScopeBlendsWithBackground(float coordinateScale)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        MonoGameDrawingBackend backend = Assert.IsType<MonoGameDrawingBackend>(
            fixture.Session.DrawingBackend);
        backend.CoordinateScale = coordinateScale;
        using Texture2D texture = new(fixture.Session.GraphicsDevice, 1, 1);
        texture.SetData([new Microsoft.Xna.Framework.Color(200, 100, 50, 255)]);
        using MonoGameImage image = new(texture);
        DrawSpriteBatch batch = new(
            image,
            [new DrawSprite2D(new DrawRect(0, 0, 96, 64))]);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.FillRectangle(
            new DrawRect(0, 0, 96, 64),
            new CernealaColor(20, 40, 60)));
        commands.Add(DrawCommand.PushTransform(
            System.Numerics.Matrix3x2.CreateScale(1.05f)));
        commands.Add(DrawCommand.PushOpacity(0.5f));
        commands.Add(DrawCommand.DrawSpriteBatch(batch));
        commands.Add(DrawCommand.PopOpacity());
        commands.Add(DrawCommand.PopTransform());

        Microsoft.Xna.Framework.Color actual = RenderCenterPixel(fixture, commands);

        Assert.InRange(actual.R, 108, 112);
        Assert.InRange(actual.G, 68, 72);
        Assert.InRange(actual.B, 53, 57);
    }

    [Fact]
    public void SpriteBatchesAroundPrismStayInsideTheOuterOpacityScope()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        using Texture2D texture = new(fixture.Session.GraphicsDevice, 1, 1);
        texture.SetData([new Microsoft.Xna.Framework.Color(200, 100, 50, 255)]);
        using MonoGameImage image = new(texture);
        DrawSpriteBatch batch = new(
            image,
            [new DrawSprite2D(new DrawRect(0, 0, 96, 64))]);
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "SpriteBatchAlpha",
                PrismTestData.Layer(1, "Content")),
            bounds: new DrawRect(0, 0, 8, 8));
        DrawCommandList commands = new();
        commands.Add(DrawCommand.FillRectangle(
            new DrawRect(0, 0, 96, 64),
            new CernealaColor(20, 40, 60)));
        commands.Add(DrawCommand.PushTransform(
            System.Numerics.Matrix3x2.CreateScale(1.05f)));
        commands.Add(DrawCommand.PushOpacity(0.5f));
        commands.Add(DrawCommand.DrawSpriteBatch(batch));
        commands.Add(DrawCommand.BeginPrism(scope));
        commands.Add(DrawCommand.FillRectangle(
            new DrawRect(0, 0, 8, 8),
            CernealaColor.White));
        commands.Add(DrawCommand.EndPrism());
        commands.Add(DrawCommand.DrawSpriteBatch(batch));
        commands.Add(DrawCommand.PopOpacity());
        commands.Add(DrawCommand.PopTransform());

        Microsoft.Xna.Framework.Color actual = RenderCenterPixel(fixture, commands);

        Assert.InRange(actual.R, 108, 112);
        Assert.InRange(actual.G, 68, 72);
        Assert.InRange(actual.B, 53, 57);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SemiTransparentSpriteBatchBlendsInsideRetainedSurface(bool includePrismScope)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        using MonoGameRenderSurface2DSession session = new(
            fixture.Session.GraphicsDevice,
            96,
            64);
        using Texture2D texture = new(fixture.Session.GraphicsDevice, 1, 1);
        texture.SetData([new Microsoft.Xna.Framework.Color(200, 100, 50, 255)]);
        using MonoGameImage image = new(texture);
        DrawSpriteBatch batch = new(
            image,
            [new DrawSprite2D(new DrawRect(0, 0, 96, 64))]);
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "RetainedSpriteBatchAlpha",
                PrismTestData.Layer(1, "Content")),
            bounds: new DrawRect(0, 0, 8, 8));

        session.Render(
            (commands, _) =>
            {
                commands.Add(DrawCommand.PushOpacity(0.5f));
                commands.Add(DrawCommand.DrawSpriteBatch(batch));
                if (includePrismScope)
                {
                    commands.Add(DrawCommand.BeginPrism(scope));
                    commands.Add(DrawCommand.FillRectangle(
                        new DrawRect(0, 0, 8, 8),
                        CernealaColor.White));
                    commands.Add(DrawCommand.EndPrism());
                }
                commands.Add(DrawCommand.PopOpacity());
            },
            new CernealaColor(20, 40, 60),
            frameVersion: 1);
        fixture.Session.GraphicsDevice.SetRenderTarget(null);
        Microsoft.Xna.Framework.Color[] pixels =
            new Microsoft.Xna.Framework.Color[session.PixelWidth * session.PixelHeight];
        session.Surface.GetData(pixels);
        Microsoft.Xna.Framework.Color actual = pixels[
            ((session.PixelHeight / 2) * session.PixelWidth) + (session.PixelWidth / 2)];

        Assert.InRange(actual.R, 108, 112);
        Assert.InRange(actual.G, 68, 72);
        Assert.InRange(actual.B, 53, 57);
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

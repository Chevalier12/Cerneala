using Cerneala.Drawing;
using Cerneala.Backends.SdlGpu;
using Cerneala.Tests.SdlGpu;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.UI.Media;
using CernealaColor = Cerneala.Drawing.Color;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class AlphaBlendRenderingTests
{
    private const string NvidiaOcclusionSkip =
        "Disabled by user request: reported NVIDIA driver issue; also fails on clean d1e4e3ef. Driver root cause not independently established.";

    [SdlNativeTheory]
    [InlineData(0)]
    [InlineData(1, Skip = NvidiaOcclusionSkip)]
    [InlineData(2)]
    [InlineData(3, Skip = NvidiaOcclusionSkip)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6, Skip = NvidiaOcclusionSkip)]
    [InlineData(7, Skip = NvidiaOcclusionSkip)]
    public void OpaqueStrokeOccludesEarlierStrokeAroundTranslucentContent(int content)
    {
        using SdlDrawingFixture fixture = new(180, 90, useMultisampling: true);
        DrawPen hidden = new(new SolidColorBrush(new CernealaColor(129, 221, 151)), 0.75f);
        DrawPen covering = new(new SolidColorBrush(new CernealaColor(255, 80, 220)), 0.75f);
        IDrawFont font = new Cerneala.Drawing.Text.SystemFontSource().LoadFont("Consolas", 5);
        using SdlGpuImage varyingImage = new(2, 2,
            [0, 0, 0, 0, 129, 221, 151, 255, 129, 221, 151, 255, 0, 0, 0, 0]);
        using SdlGpuImage constantImage = new(1, 1, [65, 111, 76, 128]);
        CernealaColor[] withHidden = RenderVariant(true);
        CernealaColor[] withoutHidden = RenderVariant(false);
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

        CernealaColor[] RenderVariant(bool drawHidden)
        {
            using RecordedSurface surface = new((commands, _) =>
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
                else if (content == 3)
                {
                    // The brush path emits one RGBA draw instead of three channel-masked draws.
                    LinearGradientBrush brush = new(new DrawPoint(0, 0), new DrawPoint(80, 0),
                        [new GradientStop(0, new CernealaColor(129, 221, 151)),
                         new GradientStop(1, new CernealaColor(129, 221, 151))]);
                    drawing.DrawText(new DrawTextRun(font, "L00000001 MFFFFFFFF solid", 5), default, brush);
                }
                else if (content == 4)
                {
                    drawing.DrawText(new DrawTextRun(font, "L00000001 MFFFFFFFF solid", 5),
                        default, CernealaColor.Transparent);
                }
                else if (content == 5)
                {
                    drawing.DrawText(new DrawTextRun(font, "L00000001 MFFFFFFFF solid", 5),
                        new DrawPoint(0, 40), new CernealaColor(129, 221, 151));
                }
                else if (content is 6 or 7)
                {
                    drawing.DrawImage(content == 6 ? varyingImage : constantImage,
                        new DrawRect(0, -5, 60, 10), CernealaColor.White);
                }
                drawing.DrawRectangle(rectangle, covering);
            }, new CernealaColor(12, 20, 32));
            DrawCommandList frame = new();
            frame.Add(DrawCommand.RenderSurface2D(surface, new DrawRect(0, 0, 180, 90), CernealaColor.White));
            return fixture.Render(frame);
        }
    }

    [SdlNativeFact]
    public void SemiTransparentSolidColorBlendsWithBackground()
    {

        using SdlDrawingFixture fixture = new();
        DrawCommandList commands = new();
        commands.Add(DrawCommand.FillRectangle(
            new DrawRect(0, 0, 96, 64),
            new CernealaColor(64, 160, 192, 128)));

        CernealaColor actual = RenderCenterPixel(fixture, commands);

        Assert.InRange(actual.R, 30, 34);
        Assert.InRange(actual.G, 78, 82);
        Assert.InRange(actual.B, 94, 98);
    }

    [SdlNativeFact]
    public void TransparentGradientStopRevealsBackgroundInsteadOfKeepingSolidRgb()
    {

        using SdlDrawingFixture fixture = new();
        LinearGradientBrush alphaRamp = new(
            new DrawPoint(0, 0),
            new DrawPoint(96, 0),
            [
                new GradientStop(0, new CernealaColor(64, 160, 192, 0)),
                new GradientStop(1, new CernealaColor(64, 160, 192, 255))
            ]);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.FillRectangle(new DrawRect(0, 0, 96, 64), alphaRamp));

        CernealaColor[] pixels = fixture.Render(commands);
        int width = fixture.Session.PixelWidth;
        int middleRow = fixture.Session.PixelHeight / 2;
        SdlGpuDrawingBackend backend = Assert.IsType<SdlGpuDrawingBackend>(fixture.Session.DrawingBackend);
        int opaqueX = Math.Min(
            width - 2,
            (int)MathF.Round(96 * backend.CoordinateScale) - 2);
        CernealaColor transparentEnd = pixels[(middleRow * width) + 1];
        CernealaColor opaqueEnd = pixels[
            (middleRow * width) + opaqueX];

        Assert.InRange(transparentEnd.R, 0, 4);
        Assert.InRange(transparentEnd.G, 0, 4);
        Assert.InRange(transparentEnd.B, 0, 4);
        Assert.InRange(opaqueEnd.R, 58, 66);
        Assert.InRange(opaqueEnd.G, 150, 162);
        Assert.InRange(opaqueEnd.B, 181, 194);
    }

    [SdlNativeFact]
    public void SemiTransparentSpriteBatchBlendsWithBackground()
    {

        using SdlDrawingFixture fixture = new();
        using SdlGpuImage image = SdlDrawingFixture.SolidImage(new CernealaColor(200, 100, 50, 255));
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

        CernealaColor actual = RenderCenterPixel(fixture, commands);

        Assert.InRange(actual.R, 108, 112);
        Assert.InRange(actual.G, 68, 72);
        Assert.InRange(actual.B, 53, 57);
    }

    [SdlNativeTheory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    public void SpriteBatchInsideOpacityScopeBlendsWithBackground(float coordinateScale)
    {

        using SdlDrawingFixture fixture = new();
        SdlGpuDrawingBackend backend = Assert.IsType<SdlGpuDrawingBackend>(
            fixture.Session.DrawingBackend);
        backend.CoordinateScale = coordinateScale;
        using SdlGpuImage image = SdlDrawingFixture.SolidImage(new CernealaColor(200, 100, 50, 255));
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

        CernealaColor actual = RenderCenterPixel(fixture, commands);

        Assert.InRange(actual.R, 108, 112);
        Assert.InRange(actual.G, 68, 72);
        Assert.InRange(actual.B, 53, 57);
    }

    [SdlNativeFact]
    public void SpriteBatchesAroundPrismStayInsideTheOuterOpacityScope()
    {

        using SdlDrawingFixture fixture = new();
        using SdlGpuImage image = SdlDrawingFixture.SolidImage(new CernealaColor(200, 100, 50, 255));
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

        CernealaColor actual = RenderCenterPixel(fixture, commands);

        Assert.InRange(actual.R, 108, 112);
        Assert.InRange(actual.G, 68, 72);
        Assert.InRange(actual.B, 53, 57);
    }

    [SdlNativeTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void SemiTransparentSpriteBatchBlendsInsideRetainedSurface(bool includePrismScope)
    {

        using SdlDrawingFixture fixture = new();
        using SdlGpuImage image = SdlDrawingFixture.SolidImage(new CernealaColor(200, 100, 50, 255));
        DrawSpriteBatch batch = new(
            image,
            [new DrawSprite2D(new DrawRect(0, 0, 96, 64))]);
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "RetainedSpriteBatchAlpha",
                PrismTestData.Layer(1, "Content")),
            bounds: new DrawRect(0, 0, 8, 8));

        using RecordedSurface surface = new(
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
            new CernealaColor(20, 40, 60));
        DrawCommandList commands = new();
        commands.Add(DrawCommand.RenderSurface2D(surface, new DrawRect(0, 0, 96, 64), CernealaColor.White));
        CernealaColor actual = fixture.RenderCenterPixel(commands);

        Assert.InRange(actual.R, 108, 112);
        Assert.InRange(actual.G, 68, 72);
        Assert.InRange(actual.B, 53, 57);
    }

    [SdlNativeFact]
    public void DynamicBrushTexturesDoNotAccumulateAcrossFrames()
    {

        using SdlDrawingFixture fixture = new();
        SdlGpuDrawingBackend backend = Assert.IsType<SdlGpuDrawingBackend>(fixture.Session.DrawingBackend);
        int initialCount = fixture.Session.DrawingResources.CachedTextureCount;

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

        Assert.InRange(fixture.Session.DrawingResources.CachedTextureCount, 0, initialCount + 2);
    }

    private static CernealaColor RenderCenterPixel(
        SdlDrawingFixture fixture,
        DrawCommandList commands)
    {
        return fixture.RenderCenterPixel(commands);
    }

    private static void Render(
        SdlDrawingFixture fixture,
        DrawCommandList commands)
    {
        fixture.Render(commands);
    }
}

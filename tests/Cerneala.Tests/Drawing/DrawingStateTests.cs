using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.MonoGame;
using Microsoft.Xna.Framework.Graphics;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Cerneala.Tests.Drawing;

public sealed class DrawingStateTests
{
    [Fact]
    public void AnalyzerComposesParentThenChildAndReportsWorldBounds()
    {
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        drawing.PushTransform(Matrix3x2.CreateTranslation(10, 20));
        drawing.PushTransform(Matrix3x2.CreateScale(2));
        drawing.FillRectangle(new DrawRect(1, 2, 3, 4), Color.White);
        drawing.PopTransform();
        drawing.PopTransform();

        DrawCommandStateAnalysis analysis =
            new DrawCommandStateAnalyzer().Analyze(commands);

        Assert.Equal(
            Matrix3x2.Multiply(
                Matrix3x2.CreateScale(2),
                Matrix3x2.CreateTranslation(10, 20)),
            analysis.Entries[2].Transform);
        Assert.Equal(new DrawRect(12, 24, 6, 8), analysis.Entries[2].Bounds);
        Assert.Equal(4, analysis.Entries[0].MatchingCommandIndex);
        Assert.Equal(3, analysis.Entries[1].MatchingCommandIndex);
    }

    [Fact]
    public void AnalyzerRejectsMixedStackImbalanceWithPushIndex()
    {
        DrawCommandList commands = new();
        commands.Add(DrawCommand.PushTransform(Matrix3x2.Identity));
        commands.Add(DrawCommand.PushOpacity(0.5f));
        commands.Add(DrawCommand.PopTransform());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => new DrawCommandStateAnalyzer().Analyze(commands));

        Assert.Contains("command index 2", exception.Message, StringComparison.Ordinal);
        Assert.Contains("command index 1", exception.Message, StringComparison.Ordinal);
        Assert.Contains("not LIFO", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RefScopesRequireLifoAndRejectDoubleDispose()
    {
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        DrawTransformScope transform = drawing.Transform(Matrix3x2.Identity);
        DrawOpacityScope opacity = drawing.Opacity(0.5f);

        bool outOfOrderRejected = false;
        try
        {
            transform.Dispose();
        }
        catch (InvalidOperationException)
        {
            outOfOrderRejected = true;
        }
        Assert.True(outOfOrderRejected);
        opacity.Dispose();
        transform.Dispose();
        bool doubleDisposeRejected = false;
        try
        {
            transform.Dispose();
        }
        catch (ObjectDisposedException)
        {
            doubleDisposeRejected = true;
        }
        Assert.True(doubleDisposeRejected);
        Assert.Equal(
            [
                DrawCommandKind.PushTransform,
                DrawCommandKind.PushOpacity,
                DrawCommandKind.PopOpacity,
                DrawCommandKind.PopTransform
            ],
            commands.Select(command => command.Kind));
    }

    [Fact]
    public void TransformGeometricClipAndGroupOpacityRenderAndRestoreState()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        DrawPath clip = new DrawPathBuilder()
            .MoveTo(new DrawPoint(16, 8))
            .LineTo(new DrawPoint(48, 8))
            .LineTo(new DrawPoint(16, 40))
            .Close()
            .Build();
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        drawing.PushTransform(Matrix3x2.CreateTranslation(8, 0));
        drawing.PushClip(clip, DrawFillRule.NonZero);
        drawing.PushOpacity(0.5f);
        drawing.FillRectangle(new DrawRect(8, 8, 24, 24), Color.White);
        drawing.FillRectangle(new DrawRect(16, 8, 24, 24), Color.White);
        drawing.PopOpacity();
        drawing.PopClip();
        drawing.PopTransform();

        XnaColor[] pixels = Render(fixture, commands);
        int width = fixture.Session.GraphicsDevice.PresentationParameters.BackBufferWidth;
        XnaColor single = pixels[(20 * width) + 55];
        XnaColor overlap = pixels[(20 * width) + 44];
        XnaColor clipped = pixels[(36 * width) + 52];

        Assert.InRange(single.R, 125, 130);
        Assert.InRange(overlap.R, 125, 130);
        Assert.Equal(XnaColor.Black, clipped);
        MonoGameDrawingBackend backend = Assert.IsType<MonoGameDrawingBackend>(
            fixture.Session.DrawingBackend);
        Assert.Equal(0, backend.ActiveDrawingLayerCount);
        Assert.InRange(backend.DrawingLayerPoolCount, 1, 8);
        Assert.Equal(0, backend.ClipStackDepth);
    }

    [Fact]
    public void AxisAlignedRectangleClipUsesScissorWithoutAllocatingALayer()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        drawing.PushClip(new DrawRect(12, 8, 24, 20));
        drawing.FillRectangle(new DrawRect(0, 0, 64, 48), Color.White);
        drawing.PopClip();

        XnaColor[] pixels = Render(fixture, commands);
        MonoGameDrawingBackend backend = Assert.IsType<MonoGameDrawingBackend>(
            fixture.Session.DrawingBackend);

        Assert.Equal(XnaColor.White, Sample(pixels, fixture, backend, 20, 16));
        Assert.Equal(XnaColor.Black, Sample(pixels, fixture, backend, 8, 16));
        Assert.Equal(0, backend.DrawingLayerPoolCount);
        Assert.Equal(0, backend.ClipStackDepth);
    }

    [Fact]
    public void NestedGeometricClipsRenderTheirIntersection()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        DrawPath outer = RectanglePath(8, 8, 40, 32);
        DrawPath inner = RectanglePath(24, 0, 32, 32);
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        drawing.PushClip(outer, DrawFillRule.NonZero);
        drawing.PushClip(inner, DrawFillRule.NonZero);
        drawing.FillRectangle(new DrawRect(0, 0, 64, 48), Color.White);
        drawing.PopClip();
        drawing.PopClip();

        XnaColor[] pixels = Render(fixture, commands);
        MonoGameDrawingBackend backend = Assert.IsType<MonoGameDrawingBackend>(
            fixture.Session.DrawingBackend);

        Assert.Equal(XnaColor.White, Sample(pixels, fixture, backend, 30, 16));
        Assert.Equal(XnaColor.Black, Sample(pixels, fixture, backend, 16, 16));
        Assert.Equal(XnaColor.Black, Sample(pixels, fixture, backend, 52, 16));
        Assert.Equal(0, backend.ActiveDrawingLayerCount);
        Assert.InRange(backend.DrawingLayerPoolCount, 2, 8);
    }

    [Fact]
    public void BasicBlendModesUsePremultipliedComposition()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);

        drawing.FillRectangle(new DrawRect(0, 0, 16, 16), new Color(0, 0, 200));
        drawing.PushBlend(DrawBlendMode.Opaque);
        drawing.FillRectangle(new DrawRect(0, 0, 16, 16), new Color(200, 0, 0, 128));
        drawing.PopBlend();

        drawing.FillRectangle(new DrawRect(20, 0, 16, 16), new Color(100, 0, 0));
        drawing.PushBlend(DrawBlendMode.Additive);
        drawing.FillRectangle(new DrawRect(20, 0, 16, 16), new Color(0, 80, 0));
        drawing.PopBlend();

        drawing.FillRectangle(new DrawRect(40, 0, 16, 16), new Color(200, 100, 50));
        drawing.PushBlend(DrawBlendMode.Multiply);
        drawing.FillRectangle(new DrawRect(40, 0, 16, 16), new Color(128, 128, 128));
        drawing.PopBlend();

        drawing.FillRectangle(new DrawRect(60, 0, 16, 16), new Color(100, 50, 0));
        drawing.PushBlend(DrawBlendMode.Screen);
        drawing.FillRectangle(new DrawRect(60, 0, 16, 16), new Color(128, 128, 128));
        drawing.PopBlend();

        XnaColor[] pixels = Render(fixture, commands);
        MonoGameDrawingBackend backend = Assert.IsType<MonoGameDrawingBackend>(
            fixture.Session.DrawingBackend);

        AssertColorNear(new XnaColor(100, 0, 0, 128), Sample(pixels, fixture, backend, 8, 8));
        AssertColorNear(new XnaColor(100, 80, 0), Sample(pixels, fixture, backend, 28, 8));
        AssertColorNear(new XnaColor(100, 50, 25), Sample(pixels, fixture, backend, 48, 8));
        AssertColorNear(new XnaColor(178, 153, 128), Sample(pixels, fixture, backend, 68, 8));
    }

    private static XnaColor[] Render(
        PrismGraphExecutorTests.WindowsDxFixture fixture,
        DrawCommandList commands)
    {
        fixture.Session.BeginFrame(Color.Black);
        PrismFrameAnalysis prism = new PrismFrameAnalyzer().Analyze(commands);
        DrawingFrameContext context = new(prism);
        fixture.Session.DrawingBackend.Render(commands, in context);
        fixture.Session.Present();
        PresentationParameters parameters =
            fixture.Session.GraphicsDevice.PresentationParameters;
        XnaColor[] pixels =
            new XnaColor[parameters.BackBufferWidth * parameters.BackBufferHeight];
        fixture.Session.GraphicsDevice.GetBackBufferData(pixels);
        return pixels;
    }

    private static XnaColor Sample(
        XnaColor[] pixels,
        PrismGraphExecutorTests.WindowsDxFixture fixture,
        MonoGameDrawingBackend backend,
        float x,
        float y)
    {
        MonoGameDrawMapper mapper = new(backend.CoordinateScale);
        Microsoft.Xna.Framework.Rectangle sample = mapper.MapRectangle(new DrawRect(x, y, 1, 1));
        int width = fixture.Session.GraphicsDevice.PresentationParameters.BackBufferWidth;
        return pixels[(sample.Y * width) + sample.X];
    }

    private static DrawPath RectanglePath(float x, float y, float width, float height) =>
        new DrawPathBuilder()
            .MoveTo(new DrawPoint(x, y))
            .LineTo(new DrawPoint(x + width, y))
            .LineTo(new DrawPoint(x + width, y + height))
            .LineTo(new DrawPoint(x, y + height))
            .Close()
            .Build();

    private static void AssertColorNear(XnaColor expected, XnaColor actual)
    {
        Assert.InRange(Math.Abs(actual.R - expected.R), 0, 3);
        Assert.InRange(Math.Abs(actual.G - expected.G), 0, 3);
        Assert.InRange(Math.Abs(actual.B - expected.B), 0, 3);
        Assert.InRange(Math.Abs(actual.A - expected.A), 0, 3);
    }
}

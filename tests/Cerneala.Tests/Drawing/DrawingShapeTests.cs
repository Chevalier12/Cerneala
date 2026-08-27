using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.Paths;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.MonoGame;
using Cerneala.UI.Controls;
using Cerneala.UI.Media;
using NumericsMatrix3x2 = System.Numerics.Matrix3x2;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Cerneala.Tests.Drawing;

public sealed class DrawingShapeTests
{
    [Fact]
    public void EllipseRowsUseDeterministicPhysicalPixelCoverage()
    {
        DrawPixelSpan[] rows = DrawEllipseRowTessellator.Build(
            new DrawRect(0, 0, 4, 4),
            coordinateScale: 1);

        Assert.Equal(
            [
                new DrawPixelSpan(1, 0, 2),
                new DrawPixelSpan(0, 1, 4),
                new DrawPixelSpan(0, 2, 4),
                new DrawPixelSpan(1, 3, 2)
            ],
            rows);
    }

    [Fact]
    public void CornerRadiiNormalizeProportionallyAcrossEveryEdge()
    {
        DrawCornerRadius normalized = new DrawCornerRadius(8, 8, 8, 8)
            .Normalize(new DrawRect(0, 0, 10, 8));

        Assert.Equal(new DrawCornerRadius(4), normalized);
        Assert.Equal(
            new DrawCornerRadius(6, 2, 4, 8),
            new DrawCornerRadius(6, 2, 4, 8)
                .Normalize(new DrawRect(0, 0, 20, 20)));
    }

    [Fact]
    public void FactoryPreservesOpenClosedContoursAndRadianArcSweeps()
    {
        DrawPath polygon = DrawPathFactory.Polygon(
            [new DrawPoint(0, 0), new DrawPoint(10, 0), new DrawPoint(5, 8)]);
        DrawPath polyline = DrawPathFactory.Polyline(
            [new DrawPoint(0, 0), new DrawPoint(10, 0), new DrawPoint(5, 8)]);
        DrawPath clockwise = DrawPathFactory.Arc(
            new DrawPoint(10, 20),
            10,
            5,
            startAngle: 0,
            sweepAngle: MathF.PI / 2);
        DrawPath counterClockwise = DrawPathFactory.Arc(
            new DrawPoint(10, 20),
            10,
            5,
            startAngle: 0,
            sweepAngle: MathF.PI / 2,
            DrawArcDirection.CounterClockwise);
        DrawPath major = DrawPathFactory.Arc(
            new DrawPoint(10, 20),
            10,
            5,
            startAngle: 0,
            sweepAngle: MathF.PI * 1.5f);
        DrawPath fullCircle = DrawPathFactory.Arc(
            new DrawPoint(10, 20),
            10,
            5,
            startAngle: MathF.PI / 3,
            sweepAngle: MathF.Tau);

        Assert.True(Assert.Single(polygon.Contours).IsClosed);
        Assert.False(Assert.Single(polyline.Contours).IsClosed);
        AssertRectNear(new DrawRect(10, 20, 10, 5), clockwise.Bounds);
        AssertRectNear(new DrawRect(10, 15, 10, 5), counterClockwise.Bounds);
        AssertRectNear(new DrawRect(0, 15, 20, 10), major.Bounds);
        AssertRectNear(new DrawRect(0, 15, 20, 10), fullCircle.Bounds);
    }

    [Fact]
    public void ContextRecordsDedicatedRoundedRectanglesAndLowersOtherShapesToPaths()
    {
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        SolidColorBrush brush = new(Color.CornflowerBlue);
        DrawPen pen = new(brush, 2);
        DrawPoint[] triangle =
            [new DrawPoint(2, 2), new DrawPoint(12, 2), new DrawPoint(7, 10)];

        drawing.FillRoundedRectangle(new DrawRect(0, 0, 20, 12), new DrawCornerRadius(4), Color.White);
        drawing.DrawRoundedRectangle(new DrawRect(0, 0, 20, 12), new DrawCornerRadius(4), pen);
        drawing.FillPolygon(triangle, Color.White);
        drawing.DrawPolygon(triangle, pen);
        drawing.DrawPolyline(triangle, pen);
        drawing.DrawArc(new DrawPoint(20, 20), 8, 6, 0, MathF.PI, pen);
        drawing.FillPie(new DrawPoint(20, 20), 8, 6, 0, MathF.PI, brush);
        drawing.DrawPie(new DrawPoint(20, 20), 8, 6, 0, MathF.PI, pen);
        drawing.FillChord(new DrawPoint(20, 20), 8, 6, 0, MathF.PI, Color.White);
        drawing.DrawChord(new DrawPoint(20, 20), 8, 6, 0, MathF.PI, pen);
        drawing.DrawPoint(new DrawPoint(4, 4), Color.White);
        drawing.FillCircle(new DrawPoint(8, 8), 3, brush);
        drawing.FillTriangle(triangle[0], triangle[1], triangle[2], Color.White);
        drawing.DrawTriangle(triangle[0], triangle[1], triangle[2], pen);
        drawing.FillRegularPolygon(new DrawPoint(20, 20), 8, 6, Color.White);
        drawing.DrawRegularPolygon(new DrawPoint(20, 20), 8, 6, pen);
        drawing.FillStar(new DrawPoint(20, 20), 8, 4, 5, brush);
        drawing.DrawStar(new DrawPoint(20, 20), 8, 4, 5, pen);

        Assert.Equal(
            [
                DrawCommandKind.FillRoundedRectangle,
                DrawCommandKind.DrawRoundedRectangle,
                DrawCommandKind.FillPath,
                DrawCommandKind.DrawPath,
                DrawCommandKind.DrawPath,
                DrawCommandKind.DrawPath,
                DrawCommandKind.FillPath,
                DrawCommandKind.DrawPath,
                DrawCommandKind.FillPath,
                DrawCommandKind.DrawPath,
                DrawCommandKind.FillEllipse,
                DrawCommandKind.FillEllipse,
                DrawCommandKind.FillPath,
                DrawCommandKind.DrawPath,
                DrawCommandKind.FillPath,
                DrawCommandKind.DrawPath,
                DrawCommandKind.FillPath,
                DrawCommandKind.DrawPath
            ],
            commands.Select(command => command.Kind));
        Assert.Equal(new DrawCornerRadius(4), commands[0].CornerRadius);
        Assert.NotNull(commands[1].Path);
        Assert.True(commands[2].Path!.Contours[0].IsClosed);
        Assert.False(commands[4].Path!.Contours[0].IsClosed);
        Assert.Equal(DrawFillRule.EvenOdd, commands[16].FillRule);
    }

    [Fact]
    public void ShapeInputsRejectNonFiniteAndDegenerateContracts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawCornerRadius(float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawCornerRadius(-1));
        Assert.Throws<ArgumentException>(() => DrawPathFactory.Polygon(
            [new DrawPoint(0, 0), new DrawPoint(1, 1)]));
        Assert.Throws<ArgumentException>(() => DrawPathFactory.Polyline(
            [new DrawPoint(0, 0)]));
        Assert.Throws<ArgumentOutOfRangeException>(() => DrawPathFactory.Arc(
            default,
            0,
            1,
            0,
            1));
        Assert.Throws<ArgumentOutOfRangeException>(() => DrawPathFactory.Arc(
            default,
            1,
            1,
            0,
            MathF.Tau + 0.01f));
        Assert.Throws<ArgumentOutOfRangeException>(() => DrawPathFactory.RegularPolygon(
            default,
            2,
            2));
        Assert.Throws<ArgumentOutOfRangeException>(() => DrawPathFactory.Star(
            default,
            2,
            3,
            5));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DrawingContext(new DrawCommandList()).FillCircle(default, 0, Color.White));
    }

    [Fact]
    public void FrameExposesEveryShapeFamilyAndEnforcesFrameLifetime()
    {
        string[] expectedMethods =
        [
            "FillRoundedRectangle", "DrawRoundedRectangle", "FillPolygon",
            "DrawPolygon", "DrawPolyline", "DrawArc", "FillPie", "DrawPie",
            "FillChord", "DrawChord", "DrawPoint", "FillCircle",
            "FillTriangle", "DrawTriangle", "FillRegularPolygon",
            "DrawRegularPolygon", "FillStar", "DrawStar"
        ];
        string[] actualMethods = typeof(RenderSurface2DFrame)
            .GetMethods()
            .Select(method => method.Name)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        foreach (string method in expectedMethods)
        {
            Assert.Contains(method, actualMethods);
        }

        DrawCommandList commands = new();
        RenderSurface2DFrame frame = new(
            commands,
            new DrawRect(0, 0, 64, 48),
            TimeSpan.Zero);
        frame.FillRoundedRectangle(
            new DrawRect(2, 2, 20, 12),
            new DrawCornerRadius(3),
            Color.White);
        frame.FillStar(
            new DrawPoint(32, 24),
            10,
            5,
            5,
            Color.White);
        frame.Complete();

        Assert.Equal(
            [DrawCommandKind.FillRoundedRectangle, DrawCommandKind.FillPath],
            commands.Select(command => command.Kind));
        Assert.Throws<ObjectDisposedException>(() =>
            frame.FillCircle(new DrawPoint(4, 4), 2, Color.White));
    }

    [Fact]
    public void RoundedStrokeBoundsIncludeOutsideExtentAndWorldTransform()
    {
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        drawing.PushTransform(
            NumericsMatrix3x2.Multiply(
                NumericsMatrix3x2.CreateScale(2),
                NumericsMatrix3x2.CreateTranslation(10, 20)));
        drawing.DrawRoundedRectangle(
            new DrawRect(10, 20, 30, 40),
            new DrawCornerRadius(6),
            new DrawPen(
                new SolidColorBrush(Color.White),
                2,
                new DrawStrokeStyle(
                    miterLimit: 1,
                    alignment: DrawStrokeAlignment.Outside)));
        drawing.PopTransform();

        DrawCommandStateAnalysis analysis =
            new DrawCommandStateAnalyzer().Analyze(commands);

        Assert.Equal(new DrawRect(26, 56, 68, 88), analysis.Entries[1].Bounds);
    }

    [Fact]
    public void ReusingFactoryPathDoesNotAllocateGeometryWhenRecording()
    {
        DrawPath path = DrawPathFactory.RegularPolygon(
            new DrawPoint(32, 32),
            24,
            64);
        _ = DrawCommand.FillPath(path, Color.White);

        long before = GC.GetAllocatedBytesForCurrentThread();
        int checksum = 0;
        for (int index = 0; index < 1_000; index++)
        {
            DrawCommand command = DrawCommand.FillPath(path, Color.White);
            checksum ^= command.Path!.StableId.GetHashCode();
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.NotEqual(int.MinValue, checksum);
        Assert.InRange(allocated, 0, 256);
    }

    [Fact]
    public void RoundedRectangleFastPathRendersIndependentCornersWithoutStrokeMesh()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        drawing.PushTransform(NumericsMatrix3x2.CreateTranslation(4, 0));
        drawing.FillRoundedRectangle(
            new DrawRect(8, 8, 48, 32),
            new DrawCornerRadius(12, 0, 8, 0),
            Color.White);
        drawing.PopTransform();

        XnaColor[] pixels = Render(fixture, commands);
        MonoGameDrawingBackend backend = Assert.IsType<MonoGameDrawingBackend>(
            fixture.Session.DrawingBackend);

        Assert.Equal(XnaColor.Black, Sample(pixels, fixture, backend, 13, 9));
        Assert.Equal(XnaColor.White, Sample(pixels, fixture, backend, 58, 9));
        Assert.Equal(XnaColor.White, Sample(pixels, fixture, backend, 36, 24));
        Assert.Equal(0, backend.StrokeMeshCacheCount);
    }

    [Fact]
    public void RoundedRectangleStrokeReusesCachedNativeGeometry()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        drawing.DrawRoundedRectangle(
            new DrawRect(8, 8, 48, 32),
            new DrawCornerRadius(10),
            new DrawPen(new SolidColorBrush(Color.White), 2));

        _ = Render(fixture, commands);
        XnaColor[] pixels = Render(fixture, commands);
        MonoGameDrawingBackend backend = Assert.IsType<MonoGameDrawingBackend>(
            fixture.Session.DrawingBackend);

        Assert.Equal(XnaColor.White, Sample(pixels, fixture, backend, 32, 8));
        Assert.Equal(XnaColor.Black, Sample(pixels, fixture, backend, 32, 24));
        Assert.Equal(1, backend.StrokeMeshCacheCount);
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
        Microsoft.Xna.Framework.Graphics.PresentationParameters parameters =
            fixture.Session.GraphicsDevice.PresentationParameters;
        XnaColor[] pixels = new XnaColor[parameters.BackBufferWidth * parameters.BackBufferHeight];
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

    private static void AssertRectNear(DrawRect expected, DrawRect actual)
    {
        Assert.InRange(MathF.Abs(actual.X - expected.X), 0, 0.001f);
        Assert.InRange(MathF.Abs(actual.Y - expected.Y), 0, 0.001f);
        Assert.InRange(MathF.Abs(actual.Width - expected.Width), 0, 0.001f);
        Assert.InRange(MathF.Abs(actual.Height - expected.Height), 0, 0.001f);
    }
}

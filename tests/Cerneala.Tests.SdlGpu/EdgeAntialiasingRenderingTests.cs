using Cerneala.Drawing;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.Drawing.SdlGpu;
using Cerneala.UI.Controls.Shapes;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Matrix = System.Numerics.Matrix3x2;

namespace Cerneala.Tests.SdlGpu;

// Distance-based edge coverage for 2D fills and strokes (issue #696): a pixel whose
// centre is at signed distance d physical pixels outside an edge gets coverage
// clamp(0.5 - d, 0, 1), and its colour is coverage * brush + (1 - coverage) * background.
// The +-2 channel tolerance covers the 8-bit rounding of the coverage term and of the
// blended result (the tolerance BrushRenderingTests.AssertColor uses).
[Collection(SdlNativeTestCollection.Name)]
public sealed class EdgeAntialiasingRenderingTests
{
    private const int ChannelTolerance = 2;
    private const int MinimumBlendedPixels = 16;
    private static readonly Color Brush = new(30, 110, 176);
    private static readonly Color Background = Color.White;
    private static readonly DrawRect EllipseBounds = new(12.3f, 10.6f, 70.4f, 41.7f);

    [SdlNativeTheory]
    [InlineData(10.25f, 1f)]
    [InlineData(10.5f, 1f)]
    [InlineData(10.75f, 1f)]
    [InlineData(10.25f, 1.25f)]
    public void StraightFillEdgeAtFractionalPositionGetsDistanceCoverage(float physicalEdgeX, float coordinateScale)
    {
        using SdlDrawingFixture fixture = new(96, 64, coordinateScale: coordinateScale);
        float scale = coordinateScale;
        DrawPath path = DrawPathFactory.Polygon(
        [
            new DrawPoint(physicalEdgeX / scale, 8 / scale), new DrawPoint(50 / scale, 8 / scale),
            new DrawPoint(50 / scale, 56 / scale), new DrawPoint(physicalEdgeX / scale, 56 / scale)
        ]);
        Color[] pixels = fixture.Render(PrismTestData.Commands(DrawCommand.FillPath(path, Brush)), Background);

        // Pixel 10 has its centre at x = 10.5, so it is physicalEdgeX - 10.5 outside the edge.
        Color edge = Mix(Brush, Background, Coverage(physicalEdgeX - 10.5));
        for (int y = 16; y < 48; y++)
        {
            AssertPixel(Background, pixels, 96, 9, y, 0);
            AssertPixel(edge, pixels, 96, 10, y, ChannelTolerance);
            AssertPixel(Brush, pixels, 96, 11, y, 0);
        }
    }

    [SdlNativeTheory]
    [InlineData(10.25f)]
    [InlineData(10.5f)]
    public void StraightStrokeEdgesAtFractionalPositionsGetDistanceCoverage(float leftEdgeX)
    {
        using SdlDrawingFixture fixture = new(96, 64);
        // A vertical line 4 px thick covers [leftEdgeX, leftEdgeX + 4].
        Color[] pixels = fixture.Render(PrismTestData.Commands(DrawCommand.DrawLine(
            new DrawPoint(leftEdgeX + 2, 4), new DrawPoint(leftEdgeX + 2, 60), Brush, 4)), Background);
        double rightEdgeX = leftEdgeX + 4;

        for (int y = 16; y < 48; y++)
        {
            AssertPixel(Background, pixels, 96, 9, y, 0);
            AssertPixel(Mix(Brush, Background, Coverage(leftEdgeX - 10.5)), pixels, 96, 10, y, ChannelTolerance);
            for (int x = 11; x <= 13; x++)
            {
                AssertPixel(Brush, pixels, 96, x, y, 0);
            }
            AssertPixel(Mix(Brush, Background, Coverage(14.5 - rightEdgeX)), pixels, 96, 14, y, ChannelTolerance);
            AssertPixel(Background, pixels, 96, 15, y, 0);
        }
    }

    [SdlNativeTheory]
    [InlineData("FillEllipse")]
    [InlineData("DrawEllipse")]
    [InlineData("FillRoundedRectangle")]
    [InlineData("DrawRoundedRectangle")]
    [InlineData("FillPathCircle")]
    [InlineData("DrawPathCircle")]
    [InlineData("FillPathStar")]
    [InlineData("DrawLineDiagonal")]
    public void CurvedAndDiagonalEdgesGetDistanceCoverageWithoutMultisampling(string scene)
    {
        using SdlDrawingFixture fixture = new(96, 64);
        (DrawCommand command, Func<double, double, double> signedDistance, double? coverageTolerance) = CurvedScene(scene);

        Color[] pixels = fixture.Render(PrismTestData.Commands(command), Background);

        AssertEdgeCoverage(pixels, 96, 64, signedDistance, Brush, coverageTolerance, scene);
    }

    [SdlNativeFact]
    public void IssueEllipseSceneMatchesExactAreaCoverageWithoutMultisampling()
    {
        const float Scale = 1.25f;
        const int Width = 300;
        const int Height = 200;
        using SdlDrawingFixture fixture = new(Width, Height, useMultisampling: false, coordinateScale: Scale);
        Color fill = new(30, 110, 176);
        Color stroke = new(255, 140, 26);
        // <Ellipse Margin="20,20,0,0" Width="200" Height="120" Fill="#1E6EB0" Stroke="#FF8C1A" StrokeThickness="8" />
        Ellipse shape = new()
        {
            Fill = new SolidColorBrush(fill),
            Stroke = new SolidColorBrush(stroke),
            StrokeThickness = 8
        };
        UIRoot root = new();
        root.VisualChildren.Add(shape);
        root.ProcessFrame();
        shape.Arrange(new ArrangeContext(new LayoutRect(20, 20, 200, 120)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "issue-696-ellipse-scene");
        root.ProcessFrame();

        Color[] actual = fixture.Render(root.RetainedRenderer.Commit(root), Background);

        // The shape deflates its fill and its centred stroke by half the thickness:
        // ellipse centre (120, 80), semi-axes 96 x 56 logical, stroke band |distance| <= 4.
        const double CenterX = 120, CenterY = 80, SemiAxisX = 96, SemiAxisY = 56, HalfThickness = 4;
        const int Samples = 16;
        int maximumDelta = 0;
        long deltaSum = 0;
        string worst = "";
        for (int py = 0; py < Height; py++)
        {
            for (int px = 0; px < Width; px++)
            {
                double centerDistance = EllipseDistance(
                    ((px + 0.5) / Scale) - CenterX, ((py + 0.5) / Scale) - CenterY, SemiAxisX, SemiAxisY, out bool centerInside);
                double r = 0, g = 0, b = 0;
                // The distance is 1-Lipschitz and half a pixel diagonal is 0.566 logical px,
                // so pixels outside [3.4, 4.6] lie on one side of every contour.
                if (centerDistance < 3.4 || centerDistance > 4.6)
                {
                    Color solid = centerDistance < 3.4 ? stroke : centerInside ? fill : Background;
                    (r, g, b) = (solid.R, solid.G, solid.B);
                }
                else
                {
                    for (int sy = 0; sy < Samples; sy++)
                    {
                        for (int sx = 0; sx < Samples; sx++)
                        {
                            double distance = EllipseDistance(
                                ((px + ((sx + 0.5) / Samples)) / Scale) - CenterX,
                                ((py + ((sy + 0.5) / Samples)) / Scale) - CenterY,
                                SemiAxisX, SemiAxisY, out bool inside);
                            Color sample = distance <= HalfThickness ? stroke : inside ? fill : Background;
                            r += sample.R;
                            g += sample.G;
                            b += sample.B;
                        }
                    }
                    const double Count = Samples * Samples;
                    (r, g, b) = (r / Count, g / Count, b / Count);
                }

                Color expected = new((byte)Math.Round(r), (byte)Math.Round(g), (byte)Math.Round(b));
                Color pixel = actual[(py * Width) + px];
                int delta = MaxRgbDelta(expected, pixel);
                deltaSum += delta;
                if (delta > maximumDelta)
                {
                    maximumDelta = delta;
                    worst = $"physical pixel ({px},{py}) expected {expected} actual {pixel}";
                }
            }
        }

        Assert.True(maximumDelta <= 20,
            $"Maximum RGB delta against exact area coverage {maximumDelta}/255 (limit 20) at {worst}; " +
            $"mean {deltaSum / (double)(Width * Height):F3}.");
    }

    [SdlNativeFact]
    public void GradientFillEdgeBlendsTheGradientColorByCoverage()
    {
        using SdlDrawingFixture fixture = new(96, 64);
        // The gradient changes only along y, so every pixel of a row has the same brush colour.
        LinearGradientBrush gradient = new(new DrawPoint(0, 8), new DrawPoint(0, 56),
            [new GradientStop(0, new Color(255, 0, 0)), new GradientStop(1, new Color(0, 0, 255))]);
        Color[] pixels = fixture.Render(PrismTestData.Commands(
            DrawCommand.FillPath(EdgeRectangle(10.25f), gradient)), Background);

        for (int y = 16; y < 48; y++)
        {
            Color rowBrush = pixels[(y * 96) + 30];
            AssertPixel(Background, pixels, 96, 9, y, 0);
            AssertPixel(Mix(rowBrush, Background, 0.75), pixels, 96, 10, y, ChannelTolerance);
            AssertPixel(rowBrush, pixels, 96, 11, y, ChannelTolerance);
        }
    }

    [SdlNativeTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void HalfOpacityFillEdgeMultipliesOpacityAndCoverage(bool opacityLayer)
    {
        using SdlDrawingFixture fixture = new(96, 64);
        DrawCommandList commands = opacityLayer
            ? PrismTestData.Commands(
                DrawCommand.PushOpacity(0.5f),
                DrawCommand.FillPath(EdgeRectangle(10.25f), Brush),
                DrawCommand.PopOpacity())
            : PrismTestData.Commands(DrawCommand.FillPath(
                EdgeRectangle(10.25f), new SolidColorBrush(Brush), DrawFillRule.NonZero, 0.5f));

        Color[] pixels = fixture.Render(commands, Background);

        for (int y = 16; y < 48; y++)
        {
            AssertPixel(Background, pixels, 96, 9, y, 0);
            AssertPixel(Mix(Brush, Background, 0.75 * 0.5), pixels, 96, 10, y, ChannelTolerance);
            AssertPixel(Mix(Brush, Background, 0.5), pixels, 96, 11, y, ChannelTolerance);
        }
    }

    [SdlNativeTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ShapeInsideAClipKeepsItsCurvedEdgeCoverage(bool pathClip)
    {
        using SdlDrawingFixture fixture = new(96, 64);
        const int ClipRight = 48;
        DrawCommand pushClip = pathClip
            ? DrawCommand.PushClip(DrawPathFactory.Polygon(
                [new DrawPoint(0, 0), new DrawPoint(ClipRight, 0), new DrawPoint(ClipRight, 64), new DrawPoint(0, 64)]))
            : DrawCommand.PushClip(new DrawRect(0, 0, ClipRight, 64));

        Color[] pixels = fixture.Render(PrismTestData.Commands(
            pushClip, DrawCommand.FillEllipse(EllipseBounds, Brush), DrawCommand.PopClip()), Background);

        // Clip edges stay hard: everything right of the clip is untouched background.
        Func<double, double, double> ellipse = EllipseSignedDistance(EllipseBounds);
        AssertEdgeCoverage(pixels, 96, 64, (x, y) => x >= ClipRight ? double.PositiveInfinity : ellipse(x, y),
            Brush, 0.15, pathClip ? "path clip" : "rectangle clip");
    }

    [SdlNativeFact]
    public void ScaledRenderTransformKeepsTheEdgeRampOnePhysicalPixelWide()
    {
        using SdlDrawingFixture fixture = new(96, 64);
        // Under a scale of 3 the logical left edge 3.75 lands on physical x = 11.25.
        DrawPath path = DrawPathFactory.Polygon(
            [new DrawPoint(3.75f, 2), new DrawPoint(12, 2), new DrawPoint(12, 18), new DrawPoint(3.75f, 18)]);

        Color[] pixels = fixture.Render(PrismTestData.Commands(
            DrawCommand.PushTransform(Matrix.CreateScale(3)),
            DrawCommand.FillPath(path, Brush),
            DrawCommand.PopTransform()), Background);

        // A ramp scaled with the transform (3 physical px) would also tint pixels 10 and 12.
        for (int y = 12; y < 48; y++)
        {
            AssertPixel(Background, pixels, 96, 10, y, 0);
            AssertPixel(Mix(Brush, Background, 0.75), pixels, 96, 11, y, ChannelTolerance);
            AssertPixel(Brush, pixels, 96, 12, y, 0);
        }
    }

    [SdlNativeFact]
    public void TranslucentClosedStrokeBandHasOneUniformColor()
    {
        using SdlDrawingFixture fixture = new(128, 96);
        DrawRect bounds = new(16.4f, 14.7f, 95.2f, 66.5f);
        Color[] pixels = fixture.Render(PrismTestData.Commands(
            DrawCommand.DrawEllipse(bounds, new Color(255, 0, 0, 128), 8)), Background);

        // Every pixel deeper than 1 px inside the 8 px band is covered exactly once.
        Color expected = Mix(new Color(255, 0, 0), Background, 128 / 255.0);
        double centerX = bounds.X + (bounds.Width / 2.0);
        double centerY = bounds.Y + (bounds.Height / 2.0);
        int checkedPixels = 0;
        int failures = 0;
        List<string> examples = [];
        for (int y = 0; y < 96; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                double distance = EllipseDistance(
                    x + 0.5 - centerX, y + 0.5 - centerY, bounds.Width / 2.0, bounds.Height / 2.0, out _);
                if (distance > 3)
                {
                    continue;
                }
                checkedPixels++;
                Color actual = pixels[(y * 128) + x];
                if (MaxRgbaDelta(expected, actual) > ChannelTolerance)
                {
                    failures++;
                    if (examples.Count < 6)
                    {
                        examples.Add($"({x},{y}) {actual}");
                    }
                }
            }
        }

        Assert.True(checkedPixels > 500, $"Only {checkedPixels} band pixels were checked.");
        Assert.True(failures == 0,
            $"{failures} of {checkedPixels} band pixels differ from {expected} by more than {ChannelTolerance}: " +
            string.Join("; ", examples));
    }

    // Small radii flatten into short segments that turn by tens of degrees; each
    // inner join must still cover its band pixels exactly once.
    [SdlNativeTheory]
    [InlineData("RoundedRectangle", 4f)]
    [InlineData("RoundedRectangle", 8f)]
    [InlineData("PathCircle", 4f)]
    [InlineData("PathCircle", 8f)]
    public void TranslucentStrokeAroundSmallRadiiHasOneUniformColor(string scene, float thickness)
    {
        using SdlDrawingFixture fixture = new(128, 96);
        Color red = new(255, 0, 0, 128);
        DrawRect bounds = new(16.4f, 14.7f, 60.2f, 40.5f);
        const double CircleX = 96.3, CircleY = 60.4, Radius = 12;
        const string Circle = "M108.3 60.4 A12 12 0 1 1 84.3 60.4 A12 12 0 1 1 108.3 60.4 Z";
        Func<double, double, double> roundedRectangle = RoundedRectangleSignedDistance(bounds, 8);
        (DrawCommand command, Func<double, double, double> centerlineDistance) = scene == "RoundedRectangle"
            ? (DrawCommand.DrawRoundedRectangle(bounds, new DrawCornerRadius(8), red, thickness), roundedRectangle)
            : (DrawCommand.DrawPath(DrawPathParser.ParseSvg(Circle), new DrawPen(new SolidColorBrush(red), thickness)),
                (x, y) => Math.Sqrt(((x - CircleX) * (x - CircleX)) + ((y - CircleY) * (y - CircleY))) - Radius);

        Color[] pixels = fixture.Render(PrismTestData.Commands(command), Background);

        // Every pixel deeper than 1 px from both band edges is covered exactly once.
        Color expected = Mix(new Color(255, 0, 0), Background, 128 / 255.0);
        int checkedPixels = 0;
        int failures = 0;
        List<string> examples = [];
        for (int y = 0; y < 96; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                if (Math.Abs(centerlineDistance(x + 0.5, y + 0.5)) > (thickness / 2.0) - 1)
                {
                    continue;
                }
                checkedPixels++;
                Color actual = pixels[(y * 128) + x];
                if (MaxRgbaDelta(expected, actual) > ChannelTolerance)
                {
                    failures++;
                    if (examples.Count < 6)
                    {
                        examples.Add($"({x},{y}) {actual}");
                    }
                }
            }
        }

        Assert.True(checkedPixels > 100, $"Only {checkedPixels} band pixels were checked.");
        Assert.True(failures == 0,
            $"{scene} {thickness}: {failures} of {checkedPixels} band pixels differ from {expected} by more than " +
            $"{ChannelTolerance}: " + string.Join("; ", examples));
    }

    // Boundary contours that share a vertex: every contour keeps its fringe and the
    // fill has no gap at the shared point.
    [SdlNativeTheory]
    [InlineData("SquaresSharingACorner")]
    [InlineData("EvenOddBowtie")]
    [InlineData("HoleTouchingTheOuterEdge")]
    public void FillWithTouchingContoursKeepsItsEdgesAndHasNoGap(string scene)
    {
        using SdlDrawingFixture fixture = new(96, 64);
        DrawPoint[] first, second;
        string svg;
        DrawFillRule fillRule;
        DrawPoint touch;
        Func<double, double, double> signedDistance;
        switch (scene)
        {
            case "SquaresSharingACorner":
                first = [new(10.3f, 8.2f), new(40.3f, 8.2f), new(40.3f, 38.2f), new(10.3f, 38.2f)];
                second = [new(40.3f, 38.2f), new(62.3f, 38.2f), new(62.3f, 60.2f), new(40.3f, 60.2f)];
                svg = "M10.3 8.2 L40.3 8.2 L40.3 38.2 L10.3 38.2 Z M40.3 38.2 L62.3 38.2 L62.3 60.2 L40.3 60.2 Z";
                fillRule = DrawFillRule.NonZero;
                touch = new DrawPoint(40.3f, 38.2f);
                signedDistance = (x, y) => Math.Min(PolygonSignedDistance(first, x, y), PolygonSignedDistance(second, x, y));
                break;
            case "EvenOddBowtie":
                // One self-crossing contour: two triangles that touch where the diagonals cross.
                first = [new(10.3f, 8.2f), new(50.3f, 32.2f), new(10.3f, 56.2f)];
                second = [new(90.3f, 8.2f), new(90.3f, 56.2f), new(50.3f, 32.2f)];
                svg = "M10.3 8.2 L90.3 56.2 L90.3 8.2 L10.3 56.2 Z";
                fillRule = DrawFillRule.EvenOdd;
                touch = new DrawPoint(50.3f, 32.2f);
                signedDistance = (x, y) => Math.Min(PolygonSignedDistance(first, x, y), PolygonSignedDistance(second, x, y));
                break;
            default:
                // A diamond hole whose left vertex lies on the outer square's left edge.
                first = [new(10.3f, 8.2f), new(70.3f, 8.2f), new(70.3f, 56.2f), new(10.3f, 56.2f)];
                second = [new(10.3f, 32.2f), new(30.3f, 12.2f), new(50.3f, 32.2f), new(30.3f, 52.2f)];
                svg = "M10.3 8.2 L70.3 8.2 L70.3 56.2 L10.3 56.2 Z M10.3 32.2 L30.3 12.2 L50.3 32.2 L30.3 52.2 Z";
                fillRule = DrawFillRule.EvenOdd;
                touch = new DrawPoint(10.3f, 32.2f);
                signedDistance = (x, y) => Math.Max(PolygonSignedDistance(first, x, y), -PolygonSignedDistance(second, x, y));
                break;
        }

        Color[] pixels = fixture.Render(PrismTestData.Commands(
            DrawCommand.FillPath(DrawPathParser.ParseSvg(svg), new SolidColorBrush(Brush), fillRule)), Background);

        AssertEdgeCoverage(pixels, 96, 64, signedDistance, Brush, null, scene);
        int nearTouch = 0;
        for (int y = (int)touch.Y - 4; y <= (int)touch.Y + 4; y++)
        {
            for (int x = Math.Max(0, (int)touch.X - 4); x <= (int)touch.X + 4; x++)
            {
                if (signedDistance(x + 0.5, y + 0.5) < -0.5)
                {
                    nearTouch++;
                    Assert.True(MaxRgbDelta(pixels[(y * 96) + x], Background) > ChannelTolerance,
                        $"{scene}: pixel ({x},{y}) inside the fill next to the touch point is background.");
                }
            }
        }
        Assert.True(nearTouch > 0, $"{scene}: no fill pixel next to the touch point was checked.");
    }

    [SdlNativeFact]
    public void MovedEllipseUnderPartialReplayMatchesAFreshRasterization()
    {
        using SdlDrawingFixture fixture = new(64, 32);
        float positionX = 3.3f;
        using RecordedSurface surface = new((commands, _) => commands.Add(
            DrawCommand.FillEllipse(new DrawRect(positionX, 4.6f, 14.4f, 11.7f), Brush)), Background);
        DrawCommandList presentation = new();
        presentation.Add(DrawCommand.RenderSurface2D(surface, new DrawRect(0, 0, 64, 32), Color.White));
        _ = fixture.Render(presentation, Background);

        positionX = 21.7f;
        surface.FrameVersion++;
        Color[] replayed = fixture.Render(presentation, Background);
        // Drop the retained raster so the next frame rasterizes the moved ellipse from scratch.
        surface.SetBackendState(fixture.Session.DrawingResources, null);
        surface.FrameVersion++;
        Color[] fresh = fixture.Render(presentation, Background);

        Assert.Contains(fresh, pixel => MaxRgbDelta(pixel, Background) > ChannelTolerance);
        for (int index = 0; index < fresh.Length; index++)
        {
            Assert.True(fresh[index] == replayed[index],
                $"Pixel ({index % 64},{index / 64}): partial replay {replayed[index]}, fresh {fresh[index]}.");
        }
    }

    [SdlNativeFact]
    public void MultisamplingStillBlendsCurvedEdgesAndKeepsInteriorAndOutsideUnchanged()
    {
        using SdlDrawingFixture fixture = new(96, 64, useMultisampling: true);

        Color[] pixels = fixture.Render(PrismTestData.Commands(
            DrawCommand.FillEllipse(EllipseBounds, Brush)), Background);

        AssertEdgeCoverage(pixels, 96, 64, EllipseSignedDistance(EllipseBounds), Brush, null, "MSAA FillEllipse");
    }

    [SdlNativeFact]
    public void HairlineStrokeSpreadsItsWidthAsCoverage()
    {
        using SdlDrawingFixture fixture = new(96, 64);
        // A 0.5 px band centred on pixel 10 covers half of that pixel.
        Color[] centred = fixture.Render(PrismTestData.Commands(DrawCommand.DrawLine(
            new DrawPoint(10.5f, 4), new DrawPoint(10.5f, 60), Brush, 0.5f)), Background);
        // A 0.5 px band centred on the boundary between pixels 10 and 11 covers a quarter of each.
        Color[] straddling = fixture.Render(PrismTestData.Commands(DrawCommand.DrawLine(
            new DrawPoint(11, 4), new DrawPoint(11, 60), Brush, 0.5f)), Background);

        for (int y = 16; y < 48; y++)
        {
            AssertPixel(Background, centred, 96, 9, y, 0);
            AssertPixel(Mix(Brush, Background, 0.5), centred, 96, 10, y, ChannelTolerance);
            AssertPixel(Background, centred, 96, 11, y, 0);

            AssertPixel(Background, straddling, 96, 9, y, 0);
            AssertPixel(Background, straddling, 96, 12, y, 0);
            double left = MeasuredCoverage(straddling[(y * 96) + 10]);
            double right = MeasuredCoverage(straddling[(y * 96) + 11]);
            Assert.True(Math.Abs(left + right - 0.5) <= 0.05 && left is >= 0.15 and <= 0.35 && right is >= 0.15 and <= 0.35,
                $"Row {y}: coverage of pixels 10 and 11 is {left:F3} and {right:F3}; expected 0.25 each, sum 0.5.");
        }
    }

    [SdlNativeFact]
    public void RotatedFillRectangleGetsBlendedEdgesAndExactInteriorAndOutside()
    {
        using SdlDrawingFixture fixture = new(96, 64);
        DrawRect rect = new(28, 19, 40, 26);
        const float Angle = MathF.PI / 6;
        System.Numerics.Vector2 center = new(48, 32);

        Color[] pixels = fixture.Render(PrismTestData.Commands(
            DrawCommand.PushTransform(Matrix.CreateRotation(Angle, center)),
            DrawCommand.FillRectangle(rect, Brush),
            DrawCommand.PopTransform()), Background);

        double cos = Math.Cos(-Angle), sin = Math.Sin(-Angle);
        AssertEdgeCoverage(pixels, 96, 64, (x, y) =>
        {
            // Rotate the pixel centre back into the rectangle's own axes.
            double dx = x - center.X, dy = y - center.Y;
            double localX = (dx * cos) - (dy * sin), localY = (dx * sin) + (dy * cos);
            double qx = Math.Abs(localX) - (rect.Width / 2.0), qy = Math.Abs(localY) - (rect.Height / 2.0);
            double outside = Math.Sqrt((Math.Max(qx, 0) * Math.Max(qx, 0)) + (Math.Max(qy, 0) * Math.Max(qy, 0)));
            return outside + Math.Min(Math.Max(qx, qy), 0);
        }, Brush, null, "rotated FillRectangle");
    }

    [SdlNativeFact]
    public void AxisAlignedFillRectangleStaysSnappedWithoutPartialPixels()
    {
        const float Scale = 1.25f;
        using SdlDrawingFixture fixture = new(96, 64, coordinateScale: Scale);
        DrawRect rect = new(8.3f, 6.7f, 30.2f, 20.9f);

        Color[] pixels = fixture.Render(PrismTestData.Commands(DrawCommand.FillRectangle(rect, Brush)), Background);

        // FillRectangle snaps each logical edge to the nearest physical pixel boundary.
        int left = Cerneala.UI.Hosting.UiCoordinateMapper.LogicalToPhysicalPixel(rect.X, Scale);
        int top = Cerneala.UI.Hosting.UiCoordinateMapper.LogicalToPhysicalPixel(rect.Y, Scale);
        int right = Cerneala.UI.Hosting.UiCoordinateMapper.LogicalToPhysicalPixel(rect.Right, Scale);
        int bottom = Cerneala.UI.Hosting.UiCoordinateMapper.LogicalToPhysicalPixel(rect.Bottom, Scale);
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 96; x++)
            {
                bool inside = x >= left && x < right && y >= top && y < bottom;
                AssertPixel(inside ? Brush : Background, pixels, 96, x, y, 0);
            }
        }
    }

    // Each scene returns its command and the signed distance (physical px, negative inside)
    // of a pixel centre from the painted area. Smooth shapes also check the coverage value:
    // fills within 0.15 (0.05 px flattening), strokes within 0.25 (0.2 px stroke flattening).
    // Polygons with corners only check blending, interior and outside: near a convex corner
    // distance to the vertex and distance to the edge lines differ.
    private static (DrawCommand Command, Func<double, double, double> SignedDistance, double? CoverageTolerance)
        CurvedScene(string scene)
    {
        DrawRect bounds = EllipseBounds;
        Func<double, double, double> ellipse = EllipseSignedDistance(bounds);
        Func<double, double, double> roundedRectangle = RoundedRectangleSignedDistance(bounds, 12);
        const double CircleX = 40.3, CircleY = 32.4, Radius = 22.6;
        const string Circle = "M62.9 32.4 A22.6 22.6 0 1 1 17.7 32.4 A22.6 22.6 0 1 1 62.9 32.4 Z";
        double CircleDistance(double x, double y) => Math.Sqrt(((x - CircleX) * (x - CircleX)) + ((y - CircleY) * (y - CircleY))) - Radius;
        DrawPen pen = new(new SolidColorBrush(Brush), 4);
        switch (scene)
        {
            case "FillEllipse":
                return (DrawCommand.FillEllipse(bounds, Brush), ellipse, 0.15);
            case "DrawEllipse":
                return (DrawCommand.DrawEllipse(bounds, Brush, 4), (x, y) => Math.Abs(ellipse(x, y)) - 2, 0.25);
            case "FillRoundedRectangle":
                return (DrawCommand.FillRoundedRectangle(bounds, new DrawCornerRadius(12), Brush), roundedRectangle, 0.15);
            case "DrawRoundedRectangle":
                return (DrawCommand.DrawRoundedRectangle(bounds, new DrawCornerRadius(12), Brush, 4),
                    (x, y) => Math.Abs(roundedRectangle(x, y)) - 2, 0.25);
            case "FillPathCircle":
                return (DrawCommand.FillPath(DrawPathParser.ParseSvg(Circle), Brush), CircleDistance, 0.15);
            case "DrawPathCircle":
                return (DrawCommand.DrawPath(DrawPathParser.ParseSvg(Circle), pen),
                    (x, y) => Math.Abs(CircleDistance(x, y)) - 2, 0.25);
            case "FillPathStar":
                DrawPoint[] star = new DrawPoint[10];
                for (int index = 0; index < star.Length; index++)
                {
                    double radius = (index & 1) == 0 ? 27 : 15;
                    double angle = (-Math.PI / 2) + (Math.PI * index / 5);
                    star[index] = new DrawPoint((float)(48.3 + (radius * Math.Cos(angle))), (float)(33.4 + (radius * Math.Sin(angle))));
                }
                return (DrawCommand.FillPath(DrawPathFactory.Polygon(star), Brush), (x, y) => PolygonSignedDistance(star, x, y), null);
            case "DrawLineDiagonal":
                DrawPoint start = new(10.3f, 8.6f);
                DrawPoint end = new(84.2f, 55.1f);
                return (DrawCommand.DrawLine(start, end, Brush, 3), (x, y) => FlatLineSignedDistance(start, end, 1.5, x, y), null);
            default:
                throw new ArgumentOutOfRangeException(nameof(scene), scene, null);
        }
    }

    // Checks the coverage rule over the whole frame: pixels at least 1 px inside are exactly
    // the brush, pixels at least 1 px outside are exactly the background, edge pixels (when a
    // tolerance is given) have coverage clamp(0.5 - d, 0, 1), and the edge has blended pixels.
    private static void AssertEdgeCoverage(
        Color[] pixels,
        int width,
        int height,
        Func<double, double, double> signedDistance,
        Color brush,
        double? coverageTolerance,
        string context)
    {
        int blended = 0;
        int violations = 0;
        List<string> examples = [];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color actual = pixels[(y * width) + x];
                double distance = signedDistance(x + 0.5, y + 0.5);
                string? violation = null;
                if (distance <= -1)
                {
                    if (MaxRgbaDelta(actual, brush) != 0)
                    {
                        violation = $"interior ({x},{y}) d={distance:F2} expected {brush} actual {actual}";
                    }
                }
                else if (distance >= 1)
                {
                    if (MaxRgbaDelta(actual, Background) != 0)
                    {
                        violation = $"outside ({x},{y}) d={distance:F2} expected {Background} actual {actual}";
                    }
                }
                else if (coverageTolerance is double tolerance)
                {
                    double expected = Coverage(distance);
                    double measured = (Background.R - actual.R) / (double)(Background.R - brush.R);
                    if (Math.Abs(measured - expected) > tolerance)
                    {
                        violation = $"edge ({x},{y}) d={distance:F2} coverage expected {expected:F2} actual {measured:F2} {actual}";
                    }
                }

                if (MaxRgbDelta(actual, Background) > ChannelTolerance && MaxRgbDelta(actual, brush) > ChannelTolerance)
                {
                    blended++;
                }
                if (violation is not null)
                {
                    violations++;
                    if (examples.Count < 6)
                    {
                        examples.Add(violation);
                    }
                }
            }
        }

        Assert.True(blended >= MinimumBlendedPixels && violations == 0,
            $"{context}: {blended} blended edge pixels (minimum {MinimumBlendedPixels}); " +
            $"{violations} coverage-rule violations: {string.Join("; ", examples)}");
    }

    private static DrawPath EdgeRectangle(float left) => DrawPathFactory.Polygon(
        [new DrawPoint(left, 8), new DrawPoint(50, 8), new DrawPoint(50, 56), new DrawPoint(left, 56)]);

    private static double Coverage(double distanceOutside) => Math.Clamp(0.5 - distanceOutside, 0, 1);

    // Coverage of Brush over Background, measured on the red channel (the widest range).
    private static double MeasuredCoverage(Color actual) =>
        (Background.R - actual.R) / (double)(Background.R - Brush.R);

    private static Color Mix(Color top, Color bottom, double coverage) => new(
        (byte)Math.Round((top.R * coverage) + (bottom.R * (1 - coverage))),
        (byte)Math.Round((top.G * coverage) + (bottom.G * (1 - coverage))),
        (byte)Math.Round((top.B * coverage) + (bottom.B * (1 - coverage))));

    private static void AssertPixel(Color expected, Color[] pixels, int width, int x, int y, int tolerance)
    {
        Color actual = pixels[(y * width) + x];
        Assert.True(MaxRgbaDelta(expected, actual) <= tolerance,
            $"Pixel ({x},{y}) expected {expected} actual {actual} (tolerance {tolerance}).");
    }

    private static int MaxRgbDelta(Color left, Color right) => Math.Max(
        Math.Abs(left.R - right.R), Math.Max(Math.Abs(left.G - right.G), Math.Abs(left.B - right.B)));

    private static int MaxRgbaDelta(Color left, Color right) =>
        Math.Max(MaxRgbDelta(left, right), Math.Abs(left.A - right.A));

    private static Func<double, double, double> EllipseSignedDistance(DrawRect bounds)
    {
        double centerX = bounds.X + (bounds.Width / 2.0);
        double centerY = bounds.Y + (bounds.Height / 2.0);
        double semiAxisX = bounds.Width / 2.0;
        double semiAxisY = bounds.Height / 2.0;
        return (x, y) =>
        {
            double distance = EllipseDistance(x - centerX, y - centerY, semiAxisX, semiAxisY, out bool inside);
            return inside ? -distance : distance;
        };
    }

    private static Func<double, double, double> RoundedRectangleSignedDistance(DrawRect bounds, double radius)
    {
        double centerX = bounds.X + (bounds.Width / 2.0);
        double centerY = bounds.Y + (bounds.Height / 2.0);
        double innerHalfWidth = (bounds.Width / 2.0) - radius;
        double innerHalfHeight = (bounds.Height / 2.0) - radius;
        return (x, y) =>
        {
            double qx = Math.Abs(x - centerX) - innerHalfWidth;
            double qy = Math.Abs(y - centerY) - innerHalfHeight;
            double outside = Math.Sqrt((Math.Max(qx, 0) * Math.Max(qx, 0)) + (Math.Max(qy, 0) * Math.Max(qy, 0)));
            return outside + Math.Min(Math.Max(qx, qy), 0) - radius;
        };
    }

    private static double PolygonSignedDistance(DrawPoint[] points, double x, double y)
    {
        double minimum = double.PositiveInfinity;
        bool inside = false;
        for (int index = 0, previous = points.Length - 1; index < points.Length; previous = index++)
        {
            double ax = points[previous].X, ay = points[previous].Y, bx = points[index].X, by = points[index].Y;
            double ex = bx - ax, ey = by - ay;
            double t = Math.Clamp((((x - ax) * ex) + ((y - ay) * ey)) / ((ex * ex) + (ey * ey)), 0, 1);
            double dx = x - (ax + (t * ex)), dy = y - (ay + (t * ey));
            minimum = Math.Min(minimum, Math.Sqrt((dx * dx) + (dy * dy)));
            if ((ay > y) != (by > y) && x < ax + ((y - ay) * ex / ey))
            {
                inside = !inside;
            }
        }
        return inside ? -minimum : minimum;
    }

    // Signed distance to the rectangle a flat-capped line of the given half thickness covers.
    private static double FlatLineSignedDistance(DrawPoint start, DrawPoint end, double halfThickness, double x, double y)
    {
        double ex = end.X - start.X, ey = end.Y - start.Y;
        double length = Math.Sqrt((ex * ex) + (ey * ey));
        double along = (((x - start.X) * ex) + ((y - start.Y) * ey)) / length;
        double across = (((y - start.Y) * ex) - ((x - start.X) * ey)) / length;
        double qx = Math.Abs(along - (length / 2)) - (length / 2);
        double qy = Math.Abs(across) - halfThickness;
        double outside = Math.Sqrt((Math.Max(qx, 0) * Math.Max(qx, 0)) + (Math.Max(qy, 0) * Math.Max(qy, 0)));
        return outside + Math.Min(Math.Max(qx, qy), 0);
    }

    // Euclidean distance from (x, y), relative to the ellipse centre, to the ellipse curve
    // (David Eberly, "Distance from a Point to an Ellipse, an Ellipsoid, or a Hyperellipsoid").
    private static double EllipseDistance(double x, double y, double semiAxisX, double semiAxisY, out bool inside)
    {
        inside = ((x / semiAxisX) * (x / semiAxisX)) + ((y / semiAxisY) * (y / semiAxisY)) <= 1;
        if (semiAxisX < semiAxisY)
        {
            (x, y) = (y, x);
            (semiAxisX, semiAxisY) = (semiAxisY, semiAxisX);
        }
        double y0 = Math.Abs(x), y1 = Math.Abs(y), e0 = semiAxisX, e1 = semiAxisY;
        if (y1 > 0)
        {
            if (y0 > 0)
            {
                double z0 = y0 / e0, z1 = y1 / e1, g = (z0 * z0) + (z1 * z1) - 1;
                if (g == 0)
                {
                    return 0;
                }
                double r0 = (e0 / e1) * (e0 / e1);
                double root = EllipseRoot(r0, z0, z1, g);
                double x0 = r0 * y0 / (root + r0), x1 = y1 / (root + 1);
                return Math.Sqrt(((x0 - y0) * (x0 - y0)) + ((x1 - y1) * (x1 - y1)));
            }
            return Math.Abs(y1 - e1);
        }
        double numerator = e0 * y0, denominator = (e0 * e0) - (e1 * e1);
        if (numerator < denominator)
        {
            double ratio = numerator / denominator;
            double x0 = e0 * ratio, x1 = e1 * Math.Sqrt(1 - (ratio * ratio));
            return Math.Sqrt(((x0 - y0) * (x0 - y0)) + (x1 * x1));
        }
        return Math.Abs(y0 - e0);
    }

    private static double EllipseRoot(double r0, double z0, double z1, double g)
    {
        double n0 = r0 * z0, s0 = z1 - 1, s1 = g < 0 ? 0 : Math.Sqrt((n0 * n0) + (z1 * z1)) - 1, s = 0;
        for (int iteration = 0; iteration < 2000; iteration++)
        {
            s = (s0 + s1) / 2;
            if (s == s0 || s == s1)
            {
                break;
            }
            double ratio0 = n0 / (s + r0), ratio1 = z1 / (s + 1);
            g = (ratio0 * ratio0) + (ratio1 * ratio1) - 1;
            if (g > 0)
            {
                s0 = s;
            }
            else if (g < 0)
            {
                s1 = s;
            }
            else
            {
                break;
            }
        }
        return s;
    }
}

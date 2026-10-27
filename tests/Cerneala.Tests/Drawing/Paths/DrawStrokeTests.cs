using Cerneala.Drawing;
using Cerneala.Drawing.Paths;
using Cerneala.UI.Controls;
using Cerneala.UI.Media;

namespace Cerneala.Tests.Drawing.Paths;

public sealed class DrawStrokeTests
{
    private static readonly SolidColorBrush Brush = new(Color.Black);

    [Fact]
    public void PenAndStyleValidateAndSnapshotTheirInputs()
    {
        float[] dashes = [2, 3];
        DrawStrokeStyle style = new(dashPattern: dashes, dashOffset: -1);
        dashes[0] = 99;

        Assert.Equal([2, 3], style.DashPattern);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<float>)style.DashPattern)[0] = 4);
        Assert.Throws<ArgumentNullException>(() => new DrawPen(null!, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawPen(Brush, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawPen(Brush, float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawStrokeStyle(miterLimit: 0.99f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawStrokeStyle(miterLimit: float.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawStrokeStyle(dashPattern: [1, 0]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawStrokeStyle(dashPattern: [float.NaN]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawStrokeStyle(dashOffset: float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawStrokeStyle(startCap: (DrawLineCap)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawStrokeStyle(join: (DrawLineJoin)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawStrokeStyle(alignment: (DrawStrokeAlignment)99));
    }

    [Theory]
    [InlineData(DrawLineCap.Flat, 0, 10)]
    [InlineData(DrawLineCap.Square, -1, 11)]
    [InlineData(DrawLineCap.Round, -1, 11)]
    [InlineData(DrawLineCap.Triangle, -1, 11)]
    public void OpenStrokeCapsProduceExpectedLongitudinalBounds(
        DrawLineCap cap,
        float expectedMinimum,
        float expectedMaximum)
    {
        DrawStrokeMesh mesh = Tessellate(
            [new DrawPoint(0, 0), new DrawPoint(10, 0)],
            isClosed: false,
            new DrawStrokeStyle(startCap: cap, endCap: cap));

        Assert.Equal(expectedMinimum, mesh.Vertices.Min(point => point.X), 3);
        Assert.Equal(expectedMaximum, mesh.Vertices.Max(point => point.X), 3);
    }

    [Fact]
    public void JoinsHonorMiterLimitAndHaveDistinctBevelAndRoundGeometry()
    {
        DrawPoint[] points =
        [
            new DrawPoint(0, 0),
            new DrawPoint(10, 0),
            new DrawPoint(10, 10)
        ];
        DrawStrokeMesh bevel = Tessellate(
            points,
            false,
            new DrawStrokeStyle(join: DrawLineJoin.Bevel));
        DrawStrokeMesh limitedMiter = Tessellate(
            points,
            false,
            new DrawStrokeStyle(join: DrawLineJoin.Miter, miterLimit: 1));
        DrawStrokeMesh miter = Tessellate(
            points,
            false,
            new DrawStrokeStyle(join: DrawLineJoin.Miter, miterLimit: 10));
        DrawStrokeMesh round = Tessellate(
            points,
            false,
            new DrawStrokeStyle(join: DrawLineJoin.Round));

        // The 2 px stroke turns at (10,0); its outer miter corner is (11,-1).
        // A mitered corner shares one cross-section between both segments, so it
        // is identified by reaching that corner, not by having more indices.
        static bool IsOuterMiterCorner(DrawPoint point) =>
            MathF.Abs(point.X - 11) < 0.001f && MathF.Abs(point.Y + 1) < 0.001f;
        Assert.Equal(bevel.Indices.Length, limitedMiter.Indices.Length);
        Assert.Contains(miter.Vertices, IsOuterMiterCorner);
        Assert.DoesNotContain(limitedMiter.Vertices, IsOuterMiterCorner);
        Assert.DoesNotContain(bevel.Vertices, IsOuterMiterCorner);
        Assert.True(round.Indices.Length > bevel.Indices.Length);
    }

    [Fact]
    public void DashOffsetAndPhaseContinueAcrossSegmentBoundaries()
    {
        DrawStrokeContour contour = new(
            [
                new DrawPoint(0, 0),
                new DrawPoint(5, 0),
                new DrawPoint(5, 5)
            ],
            false);

        IReadOnlyList<DrawStrokeContour> dashes =
            DrawStrokeTessellator.ApplyDashesForDiagnostics(
                contour,
                new DrawStrokeStyle(dashPattern: [6, 4]));
        IReadOnlyList<DrawStrokeContour> offsetDashes =
            DrawStrokeTessellator.ApplyDashesForDiagnostics(
                contour,
                new DrawStrokeStyle(dashPattern: [4, 4], dashOffset: 2));

        DrawStrokeContour continuous = Assert.Single(dashes);
        Assert.Equal(
            [new DrawPoint(0, 0), new DrawPoint(5, 0), new DrawPoint(5, 1)],
            continuous.Points);
        Assert.Equal(new DrawPoint(2, 0), offsetDashes[0].Points[^1]);
    }

    [Fact]
    public void ClosedAlignmentUsesContourInteriorAndOpenAlignmentIsCentered()
    {
        DrawPoint[] rectangle =
        [
            new DrawPoint(0, 0),
            new DrawPoint(10, 0),
            new DrawPoint(10, 10),
            new DrawPoint(0, 10)
        ];
        DrawStrokeMesh inside = Tessellate(
            rectangle,
            true,
            new DrawStrokeStyle(alignment: DrawStrokeAlignment.Inside));
        DrawStrokeMesh center = Tessellate(
            rectangle,
            true,
            new DrawStrokeStyle(alignment: DrawStrokeAlignment.Center));
        DrawStrokeMesh outside = Tessellate(
            rectangle,
            true,
            new DrawStrokeStyle(alignment: DrawStrokeAlignment.Outside));

        AssertBounds(inside, 0, 0, 10, 10);
        AssertBounds(center, -1, -1, 11, 11);
        AssertBounds(outside, -2, -2, 12, 12);

        DrawPoint[] open = [new DrawPoint(0, 0), new DrawPoint(10, 0)];
        DrawStrokeMesh openInside = Tessellate(
            open,
            false,
            new DrawStrokeStyle(alignment: DrawStrokeAlignment.Inside));
        DrawStrokeMesh openOutside = Tessellate(
            open,
            false,
            new DrawStrokeStyle(alignment: DrawStrokeAlignment.Outside));
        Assert.Equal(openInside.Vertices, openOutside.Vertices);
        Assert.Equal(openInside.Indices, openOutside.Indices);
    }

    // Issue #877: the winding of a closed contour, and so its interior side, does not
    // depend on where the contour lies. At 1e5 the float products x * y of absolute
    // coordinates are ~1e10 and lose the square's area of 100.
    [Theory]
    [InlineData(0f, DrawStrokeAlignment.Inside)]
    [InlineData(100000f, DrawStrokeAlignment.Inside)]
    [InlineData(0f, DrawStrokeAlignment.Outside)]
    [InlineData(100000f, DrawStrokeAlignment.Outside)]
    public void AlignedStrokeOfASquareStaysOnItsSideAtAnyDistanceFromTheOrigin(
        float origin,
        DrawStrokeAlignment alignment)
    {
        DrawPoint[] square =
        [
            new DrawPoint(origin, origin),
            new DrawPoint(origin + 10, origin),
            new DrawPoint(origin + 10, origin + 10),
            new DrawPoint(origin, origin + 10)
        ];

        DrawStrokeMesh mesh = Tessellate(square, isClosed: true, new DrawStrokeStyle(alignment: alignment));

        float expectedMinimum = alignment == DrawStrokeAlignment.Inside ? origin : origin - 2;
        float expectedMaximum = alignment == DrawStrokeAlignment.Inside ? origin + 10 : origin + 12;
        float minimumX = mesh.Vertices.Min(point => point.X);
        float maximumX = mesh.Vertices.Max(point => point.X);
        float minimumY = mesh.Vertices.Min(point => point.Y);
        float maximumY = mesh.Vertices.Max(point => point.Y);
        Assert.True(
            MathF.Abs(minimumX - expectedMinimum) <= 0.02f &&
            MathF.Abs(maximumX - expectedMaximum) <= 0.02f &&
            MathF.Abs(minimumY - expectedMinimum) <= 0.02f &&
            MathF.Abs(maximumY - expectedMaximum) <= 0.02f,
            $"{alignment}: vertices span X [{minimumX}, {maximumX}] and Y [{minimumY}, {maximumY}]; expected [{expectedMinimum}, {expectedMaximum}] for the square [{origin}, {origin + 10}].");
    }

    // Contract (2026-10-20): an Inside/Outside aligned stroke behaves like a centered
    // stroke on the contour offset by T/2 toward the interior/exterior, so the dash
    // caps are built around the band's mid-line, not around the contour line.
    // Rectangle (20,20,160,80), thickness 12, dashes [30,20]: the second top-edge
    // dash runs x = 70..100 and the top edge's band is y = bandMinimum..bandMaximum.
    [Theory]
    [InlineData(DrawLineCap.Round, DrawStrokeAlignment.Inside, 20f, 32f)]
    [InlineData(DrawLineCap.Round, DrawStrokeAlignment.Outside, 8f, 20f)]
    [InlineData(DrawLineCap.Triangle, DrawStrokeAlignment.Inside, 20f, 32f)]
    [InlineData(DrawLineCap.Triangle, DrawStrokeAlignment.Outside, 8f, 20f)]
    [InlineData(DrawLineCap.Square, DrawStrokeAlignment.Inside, 20f, 32f)]
    [InlineData(DrawLineCap.Square, DrawStrokeAlignment.Outside, 8f, 20f)]
    [InlineData(DrawLineCap.Round, DrawStrokeAlignment.Center, 14f, 26f)]
    [InlineData(DrawLineCap.Triangle, DrawStrokeAlignment.Center, 14f, 26f)]
    [InlineData(DrawLineCap.Square, DrawStrokeAlignment.Center, 14f, 26f)]
    public void DashedClosedStrokeCapsAreBuiltAroundTheAlignedBand(
        DrawLineCap cap,
        DrawStrokeAlignment alignment,
        float bandMinimum,
        float bandMaximum)
    {
        const float DashStart = 70;
        const float DashEnd = 100;
        const float HalfThickness = 6;
        const float Epsilon = 0.01f;
        float middle = (bandMinimum + bandMaximum) / 2;
        DrawPoint[] rectangle =
        [
            new DrawPoint(20, 20),
            new DrawPoint(180, 20),
            new DrawPoint(180, 100),
            new DrawPoint(20, 100)
        ];

        DrawStrokeMesh mesh = DrawStrokeTessellator.Tessellate(
            [new DrawStrokeContour(rectangle, true)],
            thickness: 12,
            new DrawStrokeStyle(
                startCap: cap,
                endCap: cap,
                dashPattern: [30, 20],
                alignment: alignment));

        // Only the second top-edge dash and its caps lie in x = 60..110, y < 50.
        DrawPoint[] dash = mesh.Vertices
            .Where(point => point.X is >= 60 and <= 110 && point.Y < 50)
            .ToArray();
        Assert.NotEmpty(dash);
        foreach (DrawPoint point in dash)
        {
            float overhang = MathF.Max(MathF.Max(DashStart - point.X, point.X - DashEnd), 0);
            float alongBand = MathF.Abs(point.Y - middle);
            Assert.True(
                point.Y >= bandMinimum - Epsilon && point.Y <= bandMaximum + Epsilon,
                $"{cap} {alignment}: vertex ({point.X}, {point.Y}) is outside the band y = {bandMinimum}..{bandMaximum}.");
            switch (cap)
            {
                case DrawLineCap.Round:
                    // A semicircle of radius 6 centred on the band mid-line at the dash end.
                    float distance = MathF.Sqrt((overhang * overhang) + (alongBand * alongBand));
                    Assert.True(
                        distance <= HalfThickness + Epsilon,
                        $"{cap} {alignment}: vertex ({point.X}, {point.Y}) is {distance} from the dash end on the mid-line y = {middle}, expected at most {HalfThickness}.");
                    break;
                case DrawLineCap.Triangle:
                    // A triangle with its base on the band and its tip on the mid-line.
                    Assert.True(
                        alongBand <= HalfThickness - overhang + Epsilon,
                        $"{cap} {alignment}: vertex ({point.X}, {point.Y}) is {alongBand} from the mid-line y = {middle} at overhang {overhang}, expected at most {HalfThickness - overhang}.");
                    break;
            }
        }

        Assert.Equal(DashStart - HalfThickness, dash.Min(point => point.X), 3);
        Assert.Equal(DashEnd + HalfThickness, dash.Max(point => point.X), 3);
        if (cap == DrawLineCap.Triangle)
        {
            DrawPoint tip = dash.OrderByDescending(point => point.X).First();
            Assert.Equal(middle, tip.Y, 3);
        }
    }

    [Theory]
    [InlineData(DrawStrokeAlignment.Inside, 20f, 20f, 180f, 100f)]
    [InlineData(DrawStrokeAlignment.Center, 14f, 14f, 186f, 106f)]
    [InlineData(DrawStrokeAlignment.Outside, 8f, 8f, 192f, 112f)]
    public void SolidClosedAlignedStrokeKeepsContourBand(
        DrawStrokeAlignment alignment,
        float minimumX,
        float minimumY,
        float maximumX,
        float maximumY)
    {
        DrawPoint[] rectangle =
        [
            new DrawPoint(20, 20),
            new DrawPoint(180, 20),
            new DrawPoint(180, 100),
            new DrawPoint(20, 100)
        ];

        DrawStrokeMesh mesh = DrawStrokeTessellator.Tessellate(
            [new DrawStrokeContour(rectangle, true)],
            thickness: 12,
            new DrawStrokeStyle(alignment: alignment));

        AssertBounds(mesh, minimumX, minimumY, maximumX, maximumY);
    }

    [Fact]
    public void PenCommandsRetainGeometryStyleAndFacadePayload()
    {
        DrawPath path = new DrawPathBuilder()
            .MoveTo(new DrawPoint(0, 0))
            .LineTo(new DrawPoint(10, 0))
            .LineTo(new DrawPoint(10, 10))
            .Build();
        DrawStrokeStyle style = new(
            startCap: DrawLineCap.Round,
            join: DrawLineJoin.Bevel,
            dashPattern: [2, 1]);
        DrawPen pen = new(Brush, 3, style);
        DrawCommand first = DrawCommand.DrawPath(path, pen);
        DrawCommand same = DrawCommand.DrawPath(path, pen);
        DrawCommand changed = DrawCommand.DrawPath(
            path,
            new DrawPen(Brush, 3, new DrawStrokeStyle(join: DrawLineJoin.Round)));

        Assert.Equal(first, same);
        Assert.NotEqual(first, changed);
        Assert.Same(path, first.Path);
        Assert.Same(pen, first.Pen);
        DrawCommandList commands = new();
        RenderSurface2DFrame frame = new(
            commands,
            new DrawRect(0, 0, 100, 100),
            TimeSpan.Zero);
        frame.DrawLine(new DrawPoint(1, 2), new DrawPoint(3, 4), pen);
        frame.DrawRectangle(new DrawRect(5, 6, 7, 8), pen);
        frame.DrawEllipse(new DrawRect(9, 10, 11, 12), pen);
        frame.DrawPath(path, pen);

        Assert.Equal(
            [
                DrawCommandKind.DrawLine,
                DrawCommandKind.DrawRectangle,
                DrawCommandKind.DrawEllipse,
                DrawCommandKind.DrawPath
            ],
            commands.Select(command => command.Kind));
        Assert.All(commands, command => Assert.Same(pen, command.Pen));
    }

    [Fact]
    public void StrokeMeshScalesThicknessCapsAndDashesWithCoordinateScale()
    {
        DrawPen pen = new(
            Brush,
            2,
            new DrawStrokeStyle(
                startCap: DrawLineCap.Square,
                endCap: DrawLineCap.Square,
                dashPattern: [20, 1]));
        DrawCommand command = DrawCommand.DrawLine(
            new DrawPoint(0, 0),
            new DrawPoint(10, 0),
            pen);

        DrawStrokeRenderMesh stroke = DrawStrokeMeshBuilder.Build(
            command,
            pen.Thickness,
            pen.Style,
            coordinateScale: 2);
        float minimumX = stroke.Left + stroke.Mesh.Vertices.Min(vertex => vertex.X);
        float maximumX = stroke.Left + stroke.Mesh.Vertices.Max(vertex => vertex.X);
        float minimumY = stroke.Top + stroke.Mesh.Vertices.Min(vertex => vertex.Y);
        float maximumY = stroke.Top + stroke.Mesh.Vertices.Max(vertex => vertex.Y);

        Assert.Equal(-2, minimumX, 3);
        Assert.Equal(22, maximumX, 3);
        Assert.Equal(-2, minimumY, 3);
        Assert.Equal(2, maximumY, 3);
    }

    [Fact]
    public void MappedPathStrokeScalesGeometryWithoutScalingLogicalThickness()
    {
        DrawPath path = new DrawPathBuilder()
            .MoveTo(new DrawPoint(0, 0))
            .LineTo(new DrawPoint(10, 0))
            .LineTo(new DrawPoint(10, 10))
            .LineTo(new DrawPoint(0, 10))
            .Close()
            .Build();
        DrawCommand command = DrawCommand.DrawPath(
            path,
            new DrawRect(0, 0, 10, 10),
            new DrawRect(20, 30, 20, 40),
            new DrawPen(Brush, 2));

        DrawStrokeRenderMesh stroke = DrawStrokeMeshBuilder.Build(
            command,
            command.Pen!.Thickness,
            command.Pen.Style,
            coordinateScale: 1);
        float minimumX = stroke.Left + stroke.Mesh.Vertices.Min(vertex => vertex.X);
        float maximumX = stroke.Left + stroke.Mesh.Vertices.Max(vertex => vertex.X);
        float minimumY = stroke.Top + stroke.Mesh.Vertices.Min(vertex => vertex.Y);
        float maximumY = stroke.Top + stroke.Mesh.Vertices.Max(vertex => vertex.Y);

        Assert.Equal(19, minimumX, 3);
        Assert.Equal(41, maximumX, 3);
        Assert.Equal(29, minimumY, 3);
        Assert.Equal(71, maximumY, 3);
    }

    private static DrawStrokeMesh Tessellate(
        IReadOnlyList<DrawPoint> points,
        bool isClosed,
        DrawStrokeStyle style) =>
        DrawStrokeTessellator.Tessellate(
            [new DrawStrokeContour(points, isClosed)],
            thickness: 2,
            style);

    private static void AssertBounds(
        DrawStrokeMesh mesh,
        float minimumX,
        float minimumY,
        float maximumX,
        float maximumY)
    {
        Assert.Equal(minimumX, mesh.Vertices.Min(point => point.X), 3);
        Assert.Equal(minimumY, mesh.Vertices.Min(point => point.Y), 3);
        Assert.Equal(maximumX, mesh.Vertices.Max(point => point.X), 3);
        Assert.Equal(maximumY, mesh.Vertices.Max(point => point.Y), 3);
    }
}

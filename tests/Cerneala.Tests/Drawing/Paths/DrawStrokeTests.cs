using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.Paths;
using Cerneala.UI.Controls;
using Cerneala.UI.Media;
using XnaColor = Microsoft.Xna.Framework.Color;

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

        Assert.Equal(bevel.Indices.Length, limitedMiter.Indices.Length);
        Assert.True(miter.Indices.Length > limitedMiter.Indices.Length);
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
        Assert.NotEqual(
            MonoGameDrawingBackend.CreateStrokeMeshKeyForDiagnostics(first, 1),
            MonoGameDrawingBackend.CreateStrokeMeshKeyForDiagnostics(changed, 1));
        Assert.NotEqual(
            MonoGameDrawingBackend.CreateStrokeMeshKeyForDiagnostics(first, 1),
            MonoGameDrawingBackend.CreateStrokeMeshKeyForDiagnostics(first, 2));

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

        MonoGameStrokeMesh stroke = MonoGameStrokeMeshBuilder.Build(
            command,
            coordinateScale: 2,
            _ => XnaColor.White);
        float minimumX = stroke.Left + stroke.Mesh.Vertices.Min(vertex => vertex.Position.X);
        float maximumX = stroke.Left + stroke.Mesh.Vertices.Max(vertex => vertex.Position.X);
        float minimumY = stroke.Top + stroke.Mesh.Vertices.Min(vertex => vertex.Position.Y);
        float maximumY = stroke.Top + stroke.Mesh.Vertices.Max(vertex => vertex.Position.Y);

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

        MonoGameStrokeMesh stroke = MonoGameStrokeMeshBuilder.Build(
            command,
            coordinateScale: 1,
            _ => XnaColor.White);
        float minimumX = stroke.Left + stroke.Mesh.Vertices.Min(vertex => vertex.Position.X);
        float maximumX = stroke.Left + stroke.Mesh.Vertices.Max(vertex => vertex.Position.X);
        float minimumY = stroke.Top + stroke.Mesh.Vertices.Min(vertex => vertex.Position.Y);
        float maximumY = stroke.Top + stroke.Mesh.Vertices.Max(vertex => vertex.Position.Y);

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

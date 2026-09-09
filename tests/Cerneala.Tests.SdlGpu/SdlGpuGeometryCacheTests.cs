using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Paths;
using Cerneala.UI.Media;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuGeometryCacheTests
{
    private static readonly DrawRect Bounds = new(7.125f, 8.25f, 40, 30);
    private static DrawPath CreatePath() => new DrawPathBuilder()
        .MoveTo(new DrawPoint(0, 0)).LineTo(new DrawPoint(10, 0))
        .LineTo(new DrawPoint(8, 10)).Close().Build();

    [Fact]
    public void ReusesGeometryAcrossPaintChangesButSeparatesEveryGeometryDependency()
    {
        SdlGpuGeometryCache cache = new();
        DrawPath path = CreatePath();
        DrawCommand fill = DrawCommand.FillPath(path, path.Bounds, Bounds, Color.White);
        SdlGpuGeometry first = cache.GetFill(fill, 1.25f);
        Assert.Same(first, cache.GetFill(DrawCommand.FillPath(path, path.Bounds, Bounds, Color.Black), 1.25f));
        Assert.NotSame(first, cache.GetFill(fill, 2));
        Assert.NotSame(first, cache.GetFill(DrawCommand.FillPath(CreatePath(), path.Bounds, Bounds, Color.White), 1.25f));
        Assert.NotSame(first, cache.GetFill(DrawCommand.FillPath(path, path.Bounds, Bounds, Color.White, DrawFillRule.EvenOdd), 1.25f));
        Assert.NotSame(first, cache.GetFill(DrawCommand.FillPath(path, new DrawRect(0, 0, 20, 10), Bounds, Color.White), 1.25f));
        Assert.NotSame(first, cache.GetFill(DrawCommand.FillPath(path, path.Bounds, new DrawRect(8, 8, 40, 30), Color.White), 1.25f));

        DrawStrokeStyle style = new(dashPattern: [2, 1], startCap: DrawLineCap.Round);
        DrawPen pen = new(new SolidColorBrush(Color.White), 2, style);
        DrawCommand stroke = DrawCommand.DrawPath(path, path.Bounds, Bounds, pen);
        SdlGpuGeometry outline = cache.GetStroke(stroke, 1.25f);
        Assert.Same(outline, cache.GetStroke(DrawCommand.DrawPath(path, path.Bounds, Bounds,
            new DrawPen(new SolidColorBrush(Color.Black), 2, style)), 1.25f));
        Assert.NotSame(outline, cache.GetStroke(stroke, 2));
        Assert.NotSame(outline, cache.GetStroke(DrawCommand.DrawPath(path, path.Bounds, Bounds,
            new DrawPen(pen.Brush, 3, style)), 1.25f));
        Assert.NotSame(outline, cache.GetStroke(DrawCommand.DrawPath(path, path.Bounds, Bounds,
            new DrawPen(pen.Brush, 2, new DrawStrokeStyle(dashPattern: [1, 2]))), 1.25f));
        DrawCommand line = DrawCommand.DrawLine(new DrawPoint(1, 2), new DrawPoint(20, 30), pen);
        SdlGpuGeometry lineGeometry = cache.GetStroke(line, 1);
        Assert.NotSame(lineGeometry, cache.GetStroke(DrawCommand.DrawLine(new DrawPoint(2, 2), new DrawPoint(20, 30), pen), 1));
        Assert.NotSame(lineGeometry, cache.GetStroke(DrawCommand.DrawLine(new DrawPoint(1, 2), new DrawPoint(21, 30), pen), 1));
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(2f)]
    public void CachedArraysExactlyMatchExistingTessellationAndCoordinateRoundTrip(float scale)
    {
        SdlGpuGeometryCache cache = new();
        DrawPath path = CreatePath();
        DrawCommand[] fills = [DrawCommand.FillEllipse(Bounds, Color.White),
            DrawCommand.FillPath(path, path.Bounds, Bounds, Color.White), DrawCommand.PushClip(path)];
        foreach (DrawCommand command in fills)
        {
            DrawRect destination = command.Kind == DrawCommandKind.FillEllipse
                ? DrawEllipseCoverage.AdjustBounds(command.Rect, scale) : command.Rect;
            DrawTriangleMesh expected = DrawPathMeshBuilder.Build(command.Path!, command.SourceRect,
                destination.Width * scale, destination.Height * scale, destination.X * scale, destination.Y * scale, command.FillRule);
            SdlGpuGeometry actual = cache.GetFill(command, scale);
            Assert.Equal(expected.Vertices.Select(p => new DrawPoint(p.X / scale, p.Y / scale)), actual.Positions);
            Assert.Equal(expected.Indices, actual.Indices);
            Assert.Same(actual.Positions, actual.BrushPoints);
        }
        DrawPen pen = new(new SolidColorBrush(Color.White), 2,
            new DrawStrokeStyle(startCap: DrawLineCap.Round, join: DrawLineJoin.Bevel, dashPattern: [2, 1]));
        DrawCommand[] strokes = [DrawCommand.DrawEllipse(Bounds, pen), DrawCommand.DrawRectangle(Bounds, pen),
            DrawCommand.DrawPath(path, path.Bounds, Bounds, pen),
            DrawCommand.DrawLine(new DrawPoint(1.125f, 2.25f), new DrawPoint(20.75f, 30), pen)];
        foreach (DrawCommand command in strokes)
        {
            DrawStrokeRenderMesh expected = DrawStrokeMeshBuilder.Build(command, pen.Thickness, pen.Style, scale);
            SdlGpuGeometry actual = cache.GetStroke(command, scale);
            Assert.Equal(expected.Mesh.Vertices.Select(p => new DrawPoint((p.X + expected.Left) / scale, (p.Y + expected.Top) / scale)), actual.Positions);
            Assert.Equal(expected.Mesh.Indices, actual.Indices);
            Assert.Equal(expected.BrushPoints, actual.BrushPoints);
        }
    }

    [Fact]
    public void ChangingScenesStayBoundedAndUnusedGeometryIsReleased()
    {
        SdlGpuGeometryCache cache = new(maximumEntries: 4, maximumArrayBytes: 32_768);
        for (int frame = 0; frame < 20; frame++)
        {
            cache.BeginFrame();
            for (int shape = 0; shape < 50; shape++)
            {
                cache.GetFill(DrawCommand.FillEllipse(new DrawRect(frame, shape, 10 + shape, 10), Color.White), 1.25f);
                Assert.InRange(cache.Count, 0, 4);
                Assert.InRange(cache.RetainedArrayBytes, 0, 32_768);
            }
            cache.EndFrame();
        }
        cache.BeginFrame();
        cache.EndFrame();
        Assert.Equal(0, cache.Count);
        Assert.Equal(0, cache.RetainedArrayBytes);
        DrawCommand command = DrawCommand.FillEllipse(Bounds, Color.White);
        cache.GetFill(command, 1);
        cache.Clear();
        Assert.Equal(0, cache.Count);
        Assert.Equal(0, cache.RetainedArrayBytes);
        SdlGpuGeometryCache noAdmission = new(maximumArrayBytes: 1);
        SdlGpuGeometry first = noAdmission.GetFill(command, 1);
        Assert.False(first.IsEmpty);
        Assert.NotSame(first, noAdmission.GetFill(command, 1));
        Assert.Equal(0, noAdmission.RetainedArrayBytes);
    }
}

using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.Paths;
using Cerneala.UI.Controls;
using Cerneala.UI.Media;
using Microsoft.Xna.Framework.Graphics;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Cerneala.Tests.Drawing.Paths;

public sealed class DrawPathTests
{
    [Fact]
    public void BuilderPreservesTypedOpenAndClosedContoursInImmutableSnapshots()
    {
        DrawPathBuilder builder = new();
        builder
            .MoveTo(new DrawPoint(0, 0))
            .LineTo(new DrawPoint(10, 0))
            .QuadraticTo(new DrawPoint(12, 5), new DrawPoint(10, 10))
            .CubicTo(new DrawPoint(8, 12), new DrawPoint(2, 12), new DrawPoint(0, 10))
            .ArcTo(5, 5, 0, isLargeArc: false, sweep: true, new DrawPoint(0, 0))
            .Close()
            .MoveTo(new DrawPoint(20, 20))
            .LineTo(new DrawPoint(30, 20));

        DrawPath first = builder.Build();
        builder.LineTo(new DrawPoint(30, 30));
        DrawPath second = builder.Build();

        Assert.Equal(2, first.Contours.Count);
        Assert.True(first.Contours[0].IsClosed);
        Assert.False(first.Contours[1].IsClosed);
        Assert.Equal(
            [
                DrawPathSegmentKind.Move,
                DrawPathSegmentKind.Line,
                DrawPathSegmentKind.Quadratic,
                DrawPathSegmentKind.Cubic,
                DrawPathSegmentKind.Arc,
                DrawPathSegmentKind.Close
            ],
            first.Contours[0].Segments.Select(segment => segment.Kind));
        Assert.Equal(2, first.Contours[1].Segments.Count);
        Assert.Equal(3, second.Contours[1].Segments.Count);
        Assert.NotEqual(first.StableId, second.StableId);
    }

    [Fact]
    public void BuilderRejectsInvalidContourStateAndArcValues()
    {
        DrawPathBuilder builder = new();

        Assert.Throws<InvalidOperationException>(
            () => builder.LineTo(new DrawPoint(1, 1)));
        Assert.Throws<InvalidOperationException>(() => builder.Close());

        builder.MoveTo(new DrawPoint(0, 0)).LineTo(new DrawPoint(1, 1)).Close();
        Assert.Throws<InvalidOperationException>(
            () => builder.LineTo(new DrawPoint(2, 2)));

        DrawPathBuilder arcBuilder = new();
        arcBuilder.MoveTo(new DrawPoint(0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => arcBuilder.ArcTo(0, 1, 0, false, true, new DrawPoint(1, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => arcBuilder.ArcTo(1, 1, float.NaN, false, true, new DrawPoint(1, 1)));
    }

    [Fact]
    public void SvgParserUsesOneGrammarAndPreservesContourClosure()
    {
        DrawPath path = DrawPathParser.ParseSvg(
            "M1 2h10v8l-10 0z M20 20C25 20 25 30 30 30S40 40 45 30Q50 20 55 30T65 30A5 5 0 0 1 75 30");

        Assert.Equal(2, path.Contours.Count);
        Assert.True(path.Contours[0].IsClosed);
        Assert.False(path.Contours[1].IsClosed);
        Assert.Contains(
            path.Contours[1].Segments,
            segment => segment.Kind == DrawPathSegmentKind.Arc);

        IReadOnlyList<DrawPoint[]> compatibility = SvgPathFlattener.Flatten(
            "M1 2h10v8l-10 0z",
            0.1f);
        Assert.Equal(4, Assert.Single(compatibility).Length);
    }

    [Fact]
    public void FillRulesTessellateNestedContoursAndSelfIntersections()
    {
        DrawPath sameWinding = DrawPathParser.ParseSvg(
            "M0 0L10 0L10 10L0 10Z M2 2L8 2L8 8L2 8Z");
        DrawPath oppositeWinding = DrawPathParser.ParseSvg(
            "M0 0L10 0L10 10L0 10Z M2 2L2 8L8 8L8 2Z");
        DrawPath bowTie = DrawPathParser.ParseSvg(
            "M0 0L10 10L0 10L10 0Z");

        float nonZeroSameArea = MeshArea(Build(sameWinding, DrawFillRule.NonZero));
        float evenOddArea = MeshArea(Build(sameWinding, DrawFillRule.EvenOdd));
        float nonZeroOppositeArea = MeshArea(Build(oppositeWinding, DrawFillRule.NonZero));

        Assert.InRange(nonZeroSameArea, 99.9f, 100.1f);
        Assert.InRange(evenOddArea, 63.9f, 64.1f);
        Assert.InRange(nonZeroOppositeArea, 63.9f, 64.1f);
        Assert.False(Build(bowTie, DrawFillRule.EvenOdd).IsEmpty);
    }

    [Fact]
    public void TypedPathIdentityParticipatesInCommandsAndFrameDelegation()
    {
        DrawPath path = DrawPathParser.ParseSvg("M0 0L10 0L10 10Z");
        DrawPath equivalentButDistinct = DrawPathParser.ParseSvg("M0 0L10 0L10 10Z");
        SolidColorBrush brush = new(Color.White);
        DrawCommand first = DrawCommand.FillPath(path, brush, DrawFillRule.EvenOdd);
        DrawCommand same = DrawCommand.FillPath(path, brush, DrawFillRule.EvenOdd);
        DrawCommand changedRule = DrawCommand.FillPath(path, brush, DrawFillRule.NonZero);
        DrawCommand changedPath = DrawCommand.FillPath(
            equivalentButDistinct,
            brush,
            DrawFillRule.EvenOdd);
        DrawCommandList commands = new();
        RenderSurface2DFrame frame = new(
            commands,
            new DrawRect(0, 0, 20, 20),
            TimeSpan.Zero);

        frame.FillPath(path, brush, DrawFillRule.EvenOdd);

        Assert.Equal(first, same);
        Assert.Equal(first.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(first, changedRule);
        Assert.NotEqual(first, changedPath);
        Assert.Same(path, Assert.Single(commands).Path);
        Assert.Equal(DrawFillRule.EvenOdd, commands[0].FillRule);
        Assert.Equal(path.Bounds, commands[0].Rect);
    }

    [Fact]
    public void LegacySvgCommandKeepsCompatibilityPayloadAndLowersToTypedPath()
    {
        SolidColorBrush brush = new(Color.White);
        DrawRect source = new(0, 0, 10, 10);
        DrawRect destination = new(20, 30, 40, 50);

        DrawCommand command = DrawCommand.FillPath(
            "M0 0L10 0L10 10Z",
            source,
            destination,
            brush);
        SvgGeometry geometry = new("M0 0L10 0L10 10Z", source);
        SvgGeometry equivalentGeometry = new("M0 0L10 0L10 10Z", source);

        Assert.NotNull(command.Path);
        Assert.Equal("M0 0L10 0L10 10Z", command.PathData);
        Assert.Equal(DrawFillRule.NonZero, command.FillRule);
        Assert.Equal(source, command.SourceRect);
        Assert.Equal(destination, command.Rect);
        Assert.Same(geometry.Path, geometry.Path);
        Assert.Equal(geometry, equivalentGeometry);
        Assert.Equal(geometry.GetHashCode(), equivalentGeometry.GetHashCode());
    }

    private static MonoGamePathMesh Build(DrawPath path, DrawFillRule fillRule) =>
        MonoGamePathMeshBuilder.Build(
            path,
            new DrawRect(0, 0, 10, 10),
            10,
            10,
            0,
            0,
            XnaColor.White,
            fillRule);

    private static float MeshArea(MonoGamePathMesh mesh)
    {
        float area = 0;
        for (int index = 0; index < mesh.Indices.Length; index += 3)
        {
            VertexPositionColor first = mesh.Vertices[mesh.Indices[index]];
            VertexPositionColor second = mesh.Vertices[mesh.Indices[index + 1]];
            VertexPositionColor third = mesh.Vertices[mesh.Indices[index + 2]];
            float twiceArea =
                ((second.Position.X - first.Position.X) *
                 (third.Position.Y - first.Position.Y)) -
                ((second.Position.Y - first.Position.Y) *
                 (third.Position.X - first.Position.X));
            area += MathF.Abs(twiceArea) / 2;
        }

        return area;
    }
}

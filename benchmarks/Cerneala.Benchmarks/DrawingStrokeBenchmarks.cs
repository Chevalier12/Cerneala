using BenchmarkDotNet.Attributes;
using Cerneala.Drawing;
using Cerneala.Drawing.Paths;

namespace Cerneala.Benchmarks;

[MemoryDiagnoser]
public class DrawingStrokeBenchmarks
{
    private DrawStrokeContour largePath;
    private DrawStrokeStyle solidStyle = null!;
    private DrawStrokeStyle dashedStyle = null!;
    private DrawStrokeStyle roundJoinStyle = null!;

    [GlobalSetup]
    public void Setup()
    {
        DrawPoint[] points = new DrawPoint[4096];
        for (int index = 0; index < points.Length; index++)
        {
            points[index] = new DrawPoint(
                index * 0.5f,
                50 + (MathF.Sin(index * 0.04f) * 40));
        }

        largePath = new DrawStrokeContour(points, false);
        solidStyle = new DrawStrokeStyle(join: DrawLineJoin.Bevel);
        dashedStyle = new DrawStrokeStyle(
            join: DrawLineJoin.Bevel,
            dashPattern: [8, 4, 2, 4],
            dashOffset: 3);
        roundJoinStyle = new DrawStrokeStyle(join: DrawLineJoin.Round);
    }

    [Benchmark(Baseline = true)]
    public int LargeSolidPath() =>
        DrawStrokeTessellator.Tessellate(
            [largePath],
            thickness: 2,
            solidStyle).Indices.Length;

    [Benchmark]
    public int LargeDashedPath() =>
        DrawStrokeTessellator.Tessellate(
            [largePath],
            thickness: 2,
            dashedStyle).Indices.Length;

    [Benchmark]
    public int LargeRoundJoinPath() =>
        DrawStrokeTessellator.Tessellate(
            [largePath],
            thickness: 2,
            roundJoinStyle).Indices.Length;
}

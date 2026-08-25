using BenchmarkDotNet.Attributes;
using Cerneala.Drawing;

namespace Cerneala.Benchmarks;

[MemoryDiagnoser]
public class DrawingShapeBenchmarks
{
    private DrawPoint[] polygonPoints = null!;
    private DrawPath reusablePath = null!;

    [GlobalSetup]
    public void Setup()
    {
        polygonPoints = Enumerable.Range(0, 1_024)
            .Select(index =>
            {
                float angle = MathF.Tau * index / 1_024;
                return new DrawPoint(
                    256 + (MathF.Cos(angle) * 240),
                    256 + (MathF.Sin(angle) * 240));
            })
            .ToArray();
        reusablePath = DrawPathFactory.RegularPolygon(
            new DrawPoint(256, 256),
            240,
            128);
    }

    [Benchmark]
    public DrawCommand RoundedRectangleFastCommand() =>
        DrawCommand.FillRoundedRectangle(
            new DrawRect(0, 0, 512, 256),
            new DrawCornerRadius(32, 64, 96, 16),
            Color.CornflowerBlue);

    [Benchmark]
    public DrawPath BuildLargePolygon() =>
        DrawPathFactory.Polygon(polygonPoints);

    [Benchmark]
    public DrawCommand RecordReusablePath() =>
        DrawCommand.FillPath(reusablePath, Color.CornflowerBlue);
}

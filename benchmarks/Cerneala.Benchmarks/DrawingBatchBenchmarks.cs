using BenchmarkDotNet.Attributes;
using Cerneala.Drawing;

namespace Cerneala.Benchmarks;

[MemoryDiagnoser]
public class DrawingBatchBenchmarks
{
    private DrawPoint[] points = null!;
    private DrawPointBatch batch = null!;
    private DrawCommandList commands = null!;
    private DrawingContext drawing = null!;

    [GlobalSetup]
    public void Setup()
    {
        points = Enumerable.Range(0, 1_000)
            .Select(index => new DrawPoint(index % 100, index / 100))
            .ToArray();
        batch = new DrawPointBatch(points, Color.CornflowerBlue, 2);
        commands = new DrawCommandList();
        drawing = new DrawingContext(commands);
    }

    [IterationSetup]
    public void Reset() => commands.Clear();

    [Benchmark(Baseline = true)]
    public int IndividualPointCommands()
    {
        foreach (DrawPoint point in points)
        {
            drawing.DrawPoint(point, Color.CornflowerBlue, 2);
        }

        return commands.Count;
    }

    [Benchmark]
    public int ImmutablePointBatch()
    {
        drawing.DrawPointBatch(batch);
        return commands.Count;
    }
}

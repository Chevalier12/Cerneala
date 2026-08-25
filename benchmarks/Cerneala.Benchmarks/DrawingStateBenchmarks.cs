using System.Numerics;
using BenchmarkDotNet.Attributes;
using Cerneala.Drawing;

namespace Cerneala.Benchmarks;

[MemoryDiagnoser]
public class DrawingStateBenchmarks
{
    private DrawCommandList nestedCommands = null!;
    private DrawCommandStateAnalyzer analyzer = null!;

    [GlobalSetup]
    public void Setup()
    {
        nestedCommands = new DrawCommandList();
        DrawingContext drawing = new(nestedCommands);
        analyzer = new DrawCommandStateAnalyzer();
        for (int index = 0; index < 64; index++)
        {
            drawing.PushTransform(Matrix3x2.CreateTranslation(index, index));
            drawing.PushClip(new DrawRect(0, 0, 512, 512));
            drawing.PushLayer(new DrawLayerOptions(0.9f));
            drawing.FillRectangle(
                new DrawRect(index, index, 32, 32),
                Color.CornflowerBlue);
            drawing.PopLayer();
            drawing.PopClip();
            drawing.PopTransform();
        }
    }

    [Benchmark]
    public int AnalyzeNestedLayersAndClips() =>
        analyzer.Analyze(nestedCommands).Entries.Count;
}

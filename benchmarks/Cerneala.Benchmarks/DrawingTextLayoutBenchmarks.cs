using BenchmarkDotNet.Attributes;
using Cerneala.Drawing;
using Cerneala.UI.Media;

namespace Cerneala.Benchmarks;

[MemoryDiagnoser]
public class DrawingTextLayoutBenchmarks
{
    private readonly TestFont font = new();
    private readonly SolidColorBrush brush = new(Color.Black);
    private readonly DrawTextLayoutOptions options = new(
        maxWidth: 320,
        wrapping: DrawTextWrapping.Word,
        maxLines: 4,
        trimming: DrawTextTrimming.WordEllipsis);
    private DrawTextLayout reused = null!;
    private int generation;

    [GlobalSetup]
    public void Setup()
    {
        reused = Build("Reusable text layout with styled runs, emoji 🙂, and bidirectional שלום content.");
    }

    [Benchmark(Baseline = true)]
    public long RebuildLayout()
    {
        return Build($"Reusable text layout with styled runs, emoji 🙂, and bidirectional שלום content. {generation++}")
            .StableId;
    }

    [Benchmark]
    public long ReuseImmutableLayout() => reused.StableId;

    private DrawTextLayout Build(string text) =>
        new DrawTextLayoutBuilder()
            .AddSpan(new DrawTextSpan(text, font, 16, brush))
            .Build(options);

    private sealed class TestFont : IDrawFont
    {
        public string FamilyName => "Benchmark";

        public float Size => 16;
    }
}

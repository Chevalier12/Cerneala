using BenchmarkDotNet.Attributes;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Servo;

namespace Cerneala.Benchmarks;

// 250 chains of 20 nested elements under one root: 5,000 elements, depth 20.
// "AfterSemanticChange" variants change an accessible name before each query, so
// the Servo semantics tree cannot be reused and every query pays for a build.
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 10)]
public class ServoQueryBenchmarks
{
    private const int ChainCount = 250;
    private const int Depth = 20;
    private static readonly ServoTarget LeafTarget = ServoTarget.ById("leaf-125");
    private static readonly ServoTarget ScopedLeafTarget =
        ServoTarget.ById("leaf-125").Within(ServoTarget.ById("chain-125"));

    private UiHost host = null!;
    private Servo servo = null!;
    private UIElement changedElement = null!;
    private bool toggle;

    [GlobalSetup]
    public void Setup()
    {
        UIRoot root = new(800, 600);
        for (int chain = 0; chain < ChainCount; chain++)
        {
            UIElement parent = new();
            Servo.SetId(parent, $"chain-{chain}");
            root.VisualChildren.Add(parent);
            for (int level = 1; level < Depth; level++)
            {
                UIElement child = new();
                if (level == Depth - 1)
                {
                    Servo.SetId(child, $"leaf-{chain}");
                }

                parent.VisualChildren.Add(child);
                parent = child;
            }
        }

        changedElement = root.VisualChildren[0];
        host = new UiHost(new UiHostOptions
        {
            Root = root,
            Viewport = new UiViewport(800, 600)
        });
        host.Update(
            new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []),
            host.Viewport,
            TimeSpan.Zero);
        servo = new Servo(host);
    }

    [Benchmark]
    public ServoElement FindById() => servo.FindAsync(LeafTarget).GetAwaiter().GetResult();

    [Benchmark]
    public ServoElement FindScoped() => servo.FindAsync(ScopedLeafTarget).GetAwaiter().GetResult();

    [Benchmark]
    public ServoElement FindByIdAfterSemanticChange()
    {
        ChangeSemantics();
        return servo.FindAsync(LeafTarget).GetAwaiter().GetResult();
    }

    [Benchmark]
    public ServoElement FindScopedAfterSemanticChange()
    {
        ChangeSemantics();
        return servo.FindAsync(ScopedLeafTarget).GetAwaiter().GetResult();
    }

    private void ChangeSemantics()
    {
        toggle = !toggle;
        AccessibleName.SetName(changedElement, toggle ? "a" : "b");
    }
}

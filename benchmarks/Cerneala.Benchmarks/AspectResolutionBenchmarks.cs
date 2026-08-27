using BenchmarkDotNet.Attributes;
using System.Text.Json;
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Media;

namespace Cerneala.Benchmarks;

[MemoryDiagnoser]
public class AspectResolutionBenchmarks
{
    private readonly SolidColorBrush black = new(Color.Black);
    private readonly SolidColorBrush white = new(Color.White);
    private AspectCatalog catalog = null!;
    private AspectEngine engine = null!;
    private AspectEnvironment environment = null!;
    private Button codeFirstButton = null!;
    private UIRoot registeredRoot = null!;
    private Button registeredButton = null!;
    private UIRoot scopedRoot = null!;
    private Border scopedOwner = null!;
    private Button scopedButton = null!;
    private UIRoot localRoot = null!;
    private Button localButton = null!;
    private ElementAspect localAspect = null!;
    private bool toggle;

    [GlobalSetup]
    public void Setup()
    {
        AspectRuleSet baseRule = new(
            "benchmark.base",
            AspectLayer.App,
            new AspectTarget(typeof(Button)),
            [new AspectDeclaration(Control.BackgroundProperty, AspectValue<Brush?>.Literal(white))],
            0);
        AspectRuleSet hoverRule = new(
            "benchmark.hover",
            AspectLayer.Runtime,
            new AspectTarget(
                typeof(Button),
                conditions: [AspectCondition.Property(UIElement.IsMouseOverProperty).Is(true)]),
            [new AspectDeclaration(Control.BackgroundProperty, AspectValue<Brush?>.Literal(black))],
            1);
        AspectPackage package = AspectPackage.Create("AspectBenchmark")
            .Components(components =>
            {
                components.AddRule(baseRule);
                components.AddRule(hoverRule);
            });

        catalog = new AspectRegistry().Register(package).BuildCatalog();
        engine = new AspectEngine();
        environment = new AspectEnvironment("benchmark");
        codeFirstButton = new Button();

        registeredRoot = new UIRoot();
        registeredRoot.AspectRegistry.Register(package);
        registeredButton = new Button();
        registeredRoot.VisualChildren.Add(registeredButton);
        registeredRoot.ProcessFrame();

        scopedRoot = new UIRoot();
        scopedOwner = new Border();
        scopedOwner.Resources[typeof(Button)] = AspectPackage.Create("AspectBenchmark.Scoped")
            .Components(components => components.AddRule(new AspectRuleSet(
                "benchmark.scoped",
                AspectLayer.App,
                new AspectTarget(typeof(Button)),
                [new AspectDeclaration(Control.BackgroundProperty, AspectValue<Brush?>.Literal(white))],
                0)))
            .Build();
        scopedRoot.VisualChildren.Add(scopedOwner);
        scopedRoot.ProcessFrame();
        scopedButton = new Button();

        localRoot = new UIRoot();
        localButton = new Button();
        localAspect = new ElementAspect(
            [new ElementAspectValue(Control.BackgroundProperty, white)]);
        localButton.Aspect = localAspect;
        localRoot.VisualChildren.Add(localButton);
        localRoot.ProcessFrame();
    }

    [IterationSetup(Target = nameof(NestedScopeAttachAndFrame))]
    public void ResetScopedElement()
    {
        if (scopedOwner.Child is not null)
        {
            scopedOwner.Child = null;
        }

        scopedButton = new Button();
    }

    [Benchmark]
    public ResolvedAspect CodeFirstCatalogResolve()
    {
        codeFirstButton.IsPointerOver = !codeFirstButton.IsPointerOver;
        return engine.Resolve(codeFirstButton, catalog, environment);
    }

    [Benchmark]
    public int RootRegisteredPackageFrame()
    {
        registeredButton.IsPointerOver = !registeredButton.IsPointerOver;
        return registeredRoot.ProcessFrame().AspectElements;
    }

    [Benchmark]
    public int NestedScopeAttachAndFrame()
    {
        scopedOwner.Child = scopedButton;
        return scopedRoot.ProcessFrame().AspectElements;
    }

    [Benchmark]
    public int ElementLocalMutationAndFrame()
    {
        toggle = !toggle;
        localAspect.SetValue(Control.BackgroundProperty, toggle ? black : white);
        return localRoot.ProcessFrame().AspectElements;
    }

    public IReadOnlyList<AspectResolutionMetric> CaptureMetrics(int iterations)
    {
        if (iterations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(iterations));
        }

        List<AspectResolutionMetric> metrics = [];

        CounterSnapshot directBefore = CounterSnapshot.Capture(engine.Counters);
        for (int iteration = 0; iteration < iterations; iteration++)
        {
            CodeFirstCatalogResolve();
        }
        metrics.Add(CreateMetric("CodeFirstCatalogResolve", iterations, directBefore, engine.Counters, 0));

        CounterSnapshot registeredBefore = CounterSnapshot.Capture(registeredRoot.AspectProcessor.Engine.Counters);
        int registeredInvalidations = 0;
        for (int iteration = 0; iteration < iterations; iteration++)
        {
            registeredButton.IsPointerOver = !registeredButton.IsPointerOver;
            registeredInvalidations += registeredRoot.ProcessFrame().AspectElements;
        }
        metrics.Add(CreateMetric(
            "RootRegisteredPackageFrame",
            iterations,
            registeredBefore,
            registeredRoot.AspectProcessor.Engine.Counters,
            registeredInvalidations));

        CounterSnapshot scopedBefore = CounterSnapshot.Capture(scopedRoot.AspectProcessor.Engine.Counters);
        int scopedInvalidations = 0;
        for (int iteration = 0; iteration < iterations; iteration++)
        {
            ResetScopedElement();
            scopedInvalidations += NestedScopeAttachAndFrame();
        }
        metrics.Add(CreateMetric(
            "NestedScopeAttachAndFrame",
            iterations,
            scopedBefore,
            scopedRoot.AspectProcessor.Engine.Counters,
            scopedInvalidations));

        CounterSnapshot localBefore = CounterSnapshot.Capture(localRoot.AspectProcessor.Engine.Counters);
        int localInvalidations = 0;
        for (int iteration = 0; iteration < iterations; iteration++)
        {
            toggle = !toggle;
            localAspect.SetValue(Control.BackgroundProperty, toggle ? black : white);
            localInvalidations += localRoot.ProcessFrame().AspectElements;
        }
        metrics.Add(CreateMetric(
            "ElementLocalMutationAndFrame",
            iterations,
            localBefore,
            localRoot.AspectProcessor.Engine.Counters,
            localInvalidations));

        return metrics;
    }

    private static AspectResolutionMetric CreateMetric(
        string scenario,
        int iterations,
        CounterSnapshot before,
        AspectEngineCounters after,
        int invalidations)
    {
        return new AspectResolutionMetric(
            scenario,
            iterations,
            after.RulesConsidered - before.RulesConsidered,
            after.RulesMatched - before.RulesMatched,
            after.ConditionEvaluations - before.ConditionEvaluations,
            after.DeclarationsResolved - before.DeclarationsResolved,
            invalidations);
    }

    private readonly record struct CounterSnapshot(
        int RulesConsidered,
        int RulesMatched,
        int ConditionEvaluations,
        int DeclarationsResolved)
    {
        public static CounterSnapshot Capture(AspectEngineCounters counters)
        {
            return new CounterSnapshot(
                counters.RulesConsidered,
                counters.RulesMatched,
                counters.ConditionEvaluations,
                counters.DeclarationsResolved);
        }
    }
}

public sealed record AspectResolutionMetric(
    string Scenario,
    int Iterations,
    int RuleEvaluations,
    int RulesMatched,
    int ConditionEvaluations,
    int DeclarationsResolved,
    int InvalidationCount);

internal static class AspectResolutionBenchmarkMetricsRunner
{
    public static void Run(string reportPath)
    {
        string fullPath = Path.GetFullPath(reportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        AspectResolutionBenchmarks benchmarks = new();
        benchmarks.Setup();
        const int Iterations = 1_000;
        AspectResolutionMetricsReport report = new(
            SchemaVersion: 1,
            Iterations,
            benchmarks.CaptureMetrics(Iterations));
        File.WriteAllText(
            fullPath,
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
        Console.WriteLine($"Aspect metrics: {fullPath}");
        foreach (AspectResolutionMetric metric in report.Metrics)
        {
            Console.WriteLine(
                $"{metric.Scenario}: rules={metric.RuleEvaluations}, matched={metric.RulesMatched}, " +
                $"conditions={metric.ConditionEvaluations}, declarations={metric.DeclarationsResolved}, " +
                $"invalidations={metric.InvalidationCount}");
        }
    }

    private sealed record AspectResolutionMetricsReport(
        int SchemaVersion,
        int Iterations,
        IReadOnlyList<AspectResolutionMetric> Metrics);
}

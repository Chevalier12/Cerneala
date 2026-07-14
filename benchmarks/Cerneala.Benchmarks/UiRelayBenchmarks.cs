using System.ComponentModel;
using BenchmarkDotNet.Attributes;
using Cerneala.UI.Controls;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;
using Cerneala.UI.Relay;

namespace Cerneala.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 2, iterationCount: 5, invocationCount: 1)]
public class UiRelaySchedulingBenchmarks
{
    private static readonly Action Callback = static () => { };
    private UIRoot root = null!;

    [IterationSetup]
    public void Setup()
    {
        root = new UIRoot();
        root.ProcessFrame();
    }

    [Benchmark(Baseline = true)]
    public void Post()
    {
        root.Relay.Post(Callback);
    }

    [Benchmark]
    public bool InvokeAsync()
    {
        return root.Relay.InvokeAsync(Callback).IsCompleted;
    }
}

[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 2, iterationCount: 5, invocationCount: 1)]
public class UiRelayDrainBenchmarks
{
    private static readonly Action Callback = static () => { };
    private UIRoot root = null!;

    [Params(0, 1, 100, 1_024, 2_048)]
    public int CallbackCount { get; set; }

    [IterationSetup]
    public void Setup()
    {
        root = new UIRoot(relayOptions: new UiRelayOptions { MaxCallbacksPerUpdate = 1_024 });
        root.ProcessFrame();
        for (int index = 0; index < CallbackCount; index++)
        {
            root.Relay.Post(Callback);
        }
    }

    [Benchmark]
    public int DrainOneFrame()
    {
        root.ProcessFrame();
        return root.Relay.PendingCount;
    }
}

[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 2, iterationCount: 5, invocationCount: 1)]
public class UiRelayProducerBenchmarks
{
    private static readonly Action Callback = static () => { };
    private UIRoot root = null!;

    [Params(1, 2, 4, 8)]
    public int ProducerCount { get; set; }

    [IterationSetup]
    public void Setup()
    {
        root = new UIRoot();
        root.ProcessFrame();
    }

    [Benchmark(OperationsPerInvoke = 10_000)]
    public int EnqueueTenThousand()
    {
        int perProducer = 10_000 / ProducerCount;
        Parallel.For(0, ProducerCount, _ =>
        {
            for (int index = 0; index < perProducer; index++)
            {
                root.Relay.Post(Callback);
            }
        });
        return root.Relay.PendingCount;
    }
}

[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 2, iterationCount: 5, invocationCount: 1)]
public class UiRelayBindingBenchmarks
{
    private UIRoot root = null!;
    private ObservableValue<float> simpleSource = null!;
    private NotifySource first = null!;
    private NotifySource second = null!;

    [IterationSetup(Target = nameof(SimpleBindingBurst))]
    public void SetupSimple()
    {
        root = new UIRoot();
        UIElement target = new();
        root.VisualChildren.Add(target);
        simpleSource = new ObservableValue<float>();
        _ = BindingOperations.BindOneWay(target, UIElement.OpacityProperty, simpleSource);
        root.ProcessFrame();
    }

    [IterationSetup(Target = nameof(InterpolationBurst))]
    public void SetupInterpolation()
    {
        root = new UIRoot();
        first = new NotifySource();
        second = new NotifySource();
        TextBlock target = new();
        MarkupObservation firstObservation = GeneratedMarkup.ObserveObject(() => first);
        MarkupObservation secondObservation = GeneratedMarkup.ObserveObject(() => second);
        _ = GeneratedMarkup.AttachInterpolatedStringBinding(
            target,
            target,
            TextBlock.TextProperty,
            [firstObservation, secondObservation],
            () => $"{first.Value}:{second.Value}",
            "benchmark interpolation");
        root.VisualChildren.Add(target);
        root.ProcessFrame();
    }

    [Benchmark(OperationsPerInvoke = 10_000)]
    public int SimpleBindingBurst()
    {
        RunWorker(() =>
        {
            for (int index = 0; index < 10_000; index++)
            {
                simpleSource.Value = index % 2 == 0 ? 0.25f : 0.75f;
            }
        });
        root.ProcessFrame();
        return root.Relay.PendingCount;
    }

    [Benchmark(OperationsPerInvoke = 10_000)]
    public int InterpolationBurst()
    {
        RunWorker(() =>
        {
            for (int index = 0; index < 5_000; index++)
            {
                first.Value = index;
                second.Value = index;
            }
        });
        root.ProcessFrame();
        return root.Relay.PendingCount;
    }

    private static void RunWorker(Action action)
    {
        Thread worker = new(() => action());
        worker.Start();
        worker.Join();
    }

    private sealed class NotifySource : INotifyPropertyChanged
    {
        private int value;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Value
        {
            get => value;
            set
            {
                this.value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }
    }
}

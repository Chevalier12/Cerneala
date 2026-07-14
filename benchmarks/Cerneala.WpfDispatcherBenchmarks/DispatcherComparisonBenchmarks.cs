using BenchmarkDotNet.Attributes;

namespace Cerneala.WpfDispatcherBenchmarks;

[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 2, iterationCount: 5, invocationCount: 1)]
public class DispatcherPostComparisonBenchmarks
{
    private static readonly Action Callback = static () => { };
    private CernealaRelayHarness cerneala = null!;
    private WpfDispatcherHarness wpf = null!;

    [IterationSetup]
    public void Setup()
    {
        cerneala = new CernealaRelayHarness();
        wpf = new WpfDispatcherHarness();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        cerneala.Dispose();
        wpf.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void WpfBeginInvoke()
    {
        wpf.BeginInvoke(Callback);
    }

    [Benchmark]
    public void CernealaPost()
    {
        cerneala.Post(Callback);
    }
}

[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 2, iterationCount: 5, invocationCount: 1)]
public class DispatcherInvokeAsyncComparisonBenchmarks
{
    private static readonly Action Callback = static () => { };
    private CernealaRelayHarness cerneala = null!;
    private WpfDispatcherHarness wpf = null!;

    [IterationSetup]
    public void Setup()
    {
        cerneala = new CernealaRelayHarness();
        wpf = new WpfDispatcherHarness();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        cerneala.Dispose();
        wpf.Dispose();
    }

    [Benchmark(Baseline = true)]
    public bool WpfInvokeAsync()
    {
        return wpf.InvokeAsync(Callback).IsCompleted;
    }

    [Benchmark]
    public bool CernealaInvokeAsync()
    {
        return cerneala.InvokeAsync(Callback).IsCompleted;
    }
}

[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 2, iterationCount: 5, invocationCount: 1)]
public class DispatcherDrainComparisonBenchmarks
{
    private static readonly Action Callback = static () => { };
    private CernealaRelayHarness cerneala = null!;
    private WpfDispatcherHarness wpf = null!;

    [Params(1, 100, 1_024)]
    public int CallbackCount { get; set; }

    [IterationSetup]
    public void Setup()
    {
        cerneala = new CernealaRelayHarness();
        wpf = new WpfDispatcherHarness();
        for (int index = 0; index < CallbackCount; index++)
        {
            cerneala.Post(Callback);
        }

        wpf.EnqueueDrainBatch(CallbackCount, Callback);
    }

    [IterationCleanup]
    public void Cleanup()
    {
        cerneala.Dispose();
        wpf.Dispose();
    }

    [Benchmark(Baseline = true)]
    public void WpfDrain()
    {
        wpf.DrainPreparedBatch();
    }

    [Benchmark]
    public int CernealaDrain()
    {
        cerneala.Drain();
        return cerneala.PendingCount;
    }
}

[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 2, iterationCount: 5, invocationCount: 1)]
public class DispatcherProducerComparisonBenchmarks
{
    private static readonly Action Callback = static () => { };
    private CernealaRelayHarness cerneala = null!;
    private WpfDispatcherHarness wpf = null!;

    [Params(1, 2, 4, 8)]
    public int ProducerCount { get; set; }

    [IterationSetup]
    public void Setup()
    {
        cerneala = new CernealaRelayHarness();
        wpf = new WpfDispatcherHarness();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        cerneala.Dispose();
        wpf.Dispose();
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = 10_000)]
    public void WpfBeginInvokeTenThousand()
    {
        RunProducers(wpf.BeginInvoke);
    }

    [Benchmark(OperationsPerInvoke = 10_000)]
    public int CernealaPostTenThousand()
    {
        RunProducers(cerneala.Post);
        return cerneala.PendingCount;
    }

    private void RunProducers(Action<Action> enqueue)
    {
        int perProducer = 10_000 / ProducerCount;
        Parallel.For(0, ProducerCount, _ =>
        {
            for (int index = 0; index < perProducer; index++)
            {
                enqueue(Callback);
            }
        });
    }
}

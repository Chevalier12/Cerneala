namespace Cerneala.WpfDispatcherBenchmarks;

internal static class DispatcherComparisonSmoke
{
    public static void Run()
    {
        const int callbackCount = 10_000;
        using CernealaRelayHarness cerneala = new();
        using WpfDispatcherHarness wpf = new();
        int cernealaExecuted = 0;
        int wpfExecuted = 0;
        int cernealaThread = 0;
        int wpfThread = 0;

        Parallel.For(0, callbackCount, _ => cerneala.Post(() =>
        {
            Interlocked.Increment(ref cernealaExecuted);
            Volatile.Write(ref cernealaThread, Environment.CurrentManagedThreadId);
        }));
        wpf.EnqueueDrainBatch(callbackCount, () =>
        {
            Interlocked.Increment(ref wpfExecuted);
            Volatile.Write(ref wpfThread, Environment.CurrentManagedThreadId);
        });

        cerneala.Drain();
        wpf.DrainPreparedBatch();

        Require(cernealaExecuted == callbackCount, "Cerneala did not execute every callback exactly once.");
        Require(wpfExecuted == callbackCount, "WPF did not execute every callback exactly once.");
        Require(cerneala.PendingCount == 0, "Cerneala left callbacks pending after the smoke drain.");
        Require(cernealaThread == cerneala.OwnerThreadId, "Cerneala executed work on the wrong thread.");
        Require(wpfThread == wpf.OwnerThreadId, "WPF executed work on the wrong thread.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

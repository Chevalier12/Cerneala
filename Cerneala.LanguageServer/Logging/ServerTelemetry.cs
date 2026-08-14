using System.Collections.Concurrent;
using System.Diagnostics;

namespace Cerneala.LanguageServer.Logging;

internal sealed class ServerTelemetry(IServerLogger logger)
{
    internal const int MaximumRetainedMeasurements = 2048;

    private readonly ConcurrentQueue<ServerMeasurement> measurements = new();
    private int retainedCount;

    public T Measure<T>(string operation, Func<T> action)
    {
        long started = Stopwatch.GetTimestamp();
        long allocated = GC.GetTotalAllocatedBytes(precise: false);
        bool cancelled = false;
        try
        {
            return action();
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
            throw;
        }
        finally
        {
            Record(operation, started, allocated, cancelled);
        }
    }

    public async Task<T> MeasureAsync<T>(string operation, Func<Task<T>> action)
    {
        long started = Stopwatch.GetTimestamp();
        long allocated = GC.GetTotalAllocatedBytes(precise: false);
        bool cancelled = false;
        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
            throw;
        }
        finally
        {
            Record(operation, started, allocated, cancelled);
        }
    }

    public void Record(string operation, long started, long allocatedBefore, bool cancelled)
    {
        TimeSpan elapsed = Stopwatch.GetElapsedTime(started);
        long allocatedBytes = Math.Max(0, GC.GetTotalAllocatedBytes(precise: false) - allocatedBefore);
        Add(new ServerMeasurement(operation, elapsed, allocatedBytes, cancelled));
    }

    public void RecordElapsed(string operation, long started) =>
        Add(new ServerMeasurement(operation, Stopwatch.GetElapsedTime(started), 0, Cancelled: false));

    public void RecordCancellation(string operation) =>
        Add(new ServerMeasurement(operation, TimeSpan.Zero, 0, Cancelled: true));

    public IReadOnlyList<ServerMeasurement> Snapshot() => measurements.ToArray();

    private void Add(ServerMeasurement measurement)
    {
        measurements.Enqueue(measurement);
        int count = Interlocked.Increment(ref retainedCount);
        while (count > MaximumRetainedMeasurements && measurements.TryDequeue(out _))
        {
            count = Interlocked.Decrement(ref retainedCount);
        }

        if (logger.TraceLevel == ServerTraceLevel.Verbose)
        {
            logger.Info(
                "performance.measurement",
                ("operation", measurement.Operation),
                ("elapsedMs", measurement.Elapsed.TotalMilliseconds),
                ("allocatedBytes", measurement.AllocatedBytes),
                ("cancelled", measurement.Cancelled));
        }
    }
}

internal sealed record ServerMeasurement(
    string Operation,
    TimeSpan Elapsed,
    long AllocatedBytes,
    bool Cancelled);

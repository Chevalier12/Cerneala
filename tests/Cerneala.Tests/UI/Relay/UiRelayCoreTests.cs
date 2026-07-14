using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using Cerneala.UI.Elements;
using Cerneala.UI.Relay;

namespace Cerneala.Tests.UI.Relay;

public sealed class UiRelayCoreTests
{
    [Fact]
    public void OptionsDefaultAndInvalidValuesAreDeterministic()
    {
        Assert.Equal(1024, new UiRelayOptions().MaxCallbacksPerUpdate);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new UiRelayOptions { MaxCallbacksPerUpdate = 0 });
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new UiRelayOptions { MaxCallbacksPerUpdate = -1 });
    }

    [Fact]
    public void PublicSchedulingMethodsValidateNullSynchronously()
    {
        UiRelay relay = new();

        Assert.Throws<ArgumentNullException>(() => relay.Post(null!));
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = relay.InvokeAsync((Action)null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = relay.InvokeAsync((Func<int>)null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = relay.InvokeAsync((Func<CancellationToken, Task>)null!);
        });
    }

    [Fact]
    public void PublicSurfaceIsAsyncFirstAndHasNoBlockingOrThreadOwnedApi()
    {
        string[] declaredMethods = typeof(UiRelay)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(method => method.Name)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            ["CheckAccess", "InvokeAsync", "Post", "VerifyAccess", "get_HasPendingWork", "get_PendingCount"],
            declaredMethods);
        Assert.Empty(typeof(UiRelay).GetConstructors(BindingFlags.Public | BindingFlags.Instance));
    }

    [Fact]
    public void AccessChecksUseTheConstructingThread()
    {
        UiRelay relay = new();

        Assert.True(relay.CheckAccess());
        relay.VerifyAccess();
        (bool access, Exception? exception) result = default;
        Thread worker = new(() =>
            result = (relay.CheckAccess(), Record.Exception(relay.VerifyAccess)));
        worker.Start();
        worker.Join();

        Assert.False(result.access);
        Assert.IsType<InvalidOperationException>(result.exception);
    }

    [Fact]
    public void RootOwnsRelayCreatedOnItsConstructionThread()
    {
        UIRoot root = new(relayOptions: new UiRelayOptions { MaxCallbacksPerUpdate = 1 });
        int executions = 0;
        root.Relay.Post(() => executions++);
        root.Relay.Post(() => executions++);

        UiRelayDrainResult first = root.Relay.Drain();

        Assert.True(root.Relay.CheckAccess());
        Assert.Equal(1, executions);
        Assert.Equal(1, first.Backlog);
    }

    [Fact]
    public async Task InvokeOverloadsRunOnceAndPropagateResultsAndFaults()
    {
        UiRelay relay = new();
        int actions = 0;
        Task action = relay.InvokeAsync(() => actions++);
        Task<int> result = relay.InvokeAsync(() => 42);
        Task fault = relay.InvokeAsync(() => throw new InvalidOperationException("expected"));

        UiRelayDrainResult drain = relay.Drain();

        Assert.Equal(3, drain.Executed);
        Assert.Equal(1, actions);
        await action;
        Assert.Equal(42, await result);
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fault);
        Assert.Equal("expected", exception.Message);
    }

    [Fact]
    public void PostFaultDoesNotAbandonTheSnapshot()
    {
        UiRelay relay = new();
        int executions = 0;
        relay.Post(() => throw new InvalidOperationException("first"));
        relay.Post(() => executions++);
        relay.Post(() => throw new ArgumentException("third"));

        AggregateException exception = Assert.Throws<AggregateException>(() => relay.Drain());

        Assert.Equal(2, exception.InnerExceptions.Count);
        Assert.Equal(1, executions);
        Assert.Equal(0, relay.PendingCount);
    }

    [Fact]
    public async Task CancellationBeforeDrainPreventsExecution()
    {
        UiRelay relay = new();
        using CancellationTokenSource cancellation = new();
        int executions = 0;
        Task operation = relay.InvokeAsync(() => executions++, cancellation.Token);

        cancellation.Cancel();
        UiRelayDrainResult drain = relay.Drain();

        Assert.Equal(0, executions);
        Assert.Equal(1, drain.Canceled);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
    }

    [Fact]
    public async Task PreCanceledTokenReturnsCanceledTaskWithoutCreatingBacklog()
    {
        UiRelay relay = new();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Task operation = relay.InvokeAsync(() => throw new Xunit.Sdk.XunitException("must not run"), cancellation.Token);

        Assert.False(relay.HasPendingWork);
        Assert.Equal(0, relay.PendingCount);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
    }

    [Fact]
    public async Task CancellationAfterAsyncStartRemainsCooperative()
    {
        UiRelay relay = new();
        using CancellationTokenSource cancellation = new();
        TaskCompletionSource release = new();
        Task operation = relay.InvokeAsync(async _ => await release.Task, cancellation.Token);

        UiRelayDrainResult drain = relay.Drain();
        cancellation.Cancel();

        Assert.Equal(1, drain.Executed);
        Assert.False(operation.IsCompleted);
        Assert.Equal(0, relay.PendingCount);

        release.SetResult();
        Assert.True(relay.HasPendingWork);
        relay.Drain();
        await operation;
    }

    [Fact]
    public async Task CancellationBetweenDequeueAndRunWinsRaceSafely()
    {
        using ManualResetEventSlim ownerReady = new(false);
        using ManualResetEventSlim workReady = new(false);
        using ManualResetEventSlim dequeued = new(false);
        using ManualResetEventSlim release = new(false);
        using CancellationTokenSource cancellation = new();
        UiRelay? relay = null;
        Exception? drainException = null;
        Thread owner = new(() =>
        {
            relay = new UiRelay(beforeWorkItemStart: () =>
            {
                dequeued.Set();
                release.Wait();
            });
            ownerReady.Set();
            workReady.Wait();
            drainException = Record.Exception(() => relay.Drain());
        });
        owner.Start();
        ownerReady.Wait();

        int executions = 0;
        Task operation = relay!.InvokeAsync(() => executions++, cancellation.Token);
        workReady.Set();
        dequeued.Wait();
        cancellation.Cancel();
        release.Set();
        owner.Join();

        Assert.Null(drainException);
        Assert.Equal(0, executions);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
    }

    [Fact]
    public void SnapshotAndBudgetDeferNewOrExcessWork()
    {
        UiRelay relay = new(new UiRelayOptions { MaxCallbacksPerUpdate = 2 });
        List<int> order = [];
        relay.Post(() =>
        {
            order.Add(1);
            relay.Post(() => order.Add(4));
        });
        relay.Post(() => order.Add(2));
        relay.Post(() => order.Add(3));

        UiRelayDrainResult first = relay.Drain();
        Assert.Equal([1, 2], order);
        Assert.Equal(2, first.Executed);
        Assert.Equal(2, first.Backlog);

        relay.Drain();
        Assert.Equal([1, 2, 3, 4], order);
    }

    [Fact]
    public void DefaultBudgetProcessesExactly1024Callbacks()
    {
        UiRelay relay = new();
        int executions = 0;
        for (int i = 0; i < 1025; i++)
        {
            relay.Post(() => executions++);
        }

        UiRelayDrainResult first = relay.Drain();
        Assert.Equal(1024, executions);
        Assert.Equal(1, first.Backlog);

        relay.Drain();
        Assert.Equal(1025, executions);
    }

    [Fact]
    public void ConcurrentEnqueueAtEndOfDrainCannotLoseWakeup()
    {
        UiRelay relay = new();
        using ManualResetEventSlim callbackStarted = new(false);
        using ManualResetEventSlim producerFinished = new(false);
        List<int> order = [];
        relay.Post(() =>
        {
            order.Add(1);
            callbackStarted.Set();
            producerFinished.Wait();
        });
        Thread producer = new(() =>
        {
            callbackStarted.Wait();
            relay.Post(() => order.Add(2));
            producerFinished.Set();
        });
        producer.Start();

        relay.Drain();
        producer.Join();

        Assert.Equal([1], order);
        Assert.True(relay.HasPendingWork);
        Assert.Equal(1, relay.PendingCount);
        relay.Drain();
        Assert.Equal([1, 2], order);
    }

    [Fact]
    public void MultiProducerPreservesPerProducerFifoAndExactlyOnce()
    {
        const int producers = 8;
        const int perProducer = 250;
        UiRelay relay = new(new UiRelayOptions { MaxCallbacksPerUpdate = producers * perProducer });
        ConcurrentDictionary<int, List<int>> observed = new();

        Parallel.For(0, producers, producer =>
        {
            for (int sequence = 0; sequence < perProducer; sequence++)
            {
                int capturedProducer = producer;
                int capturedSequence = sequence;
                relay.Post(() =>
                {
                    List<int> values = observed.GetOrAdd(capturedProducer, _ => []);
                    values.Add(capturedSequence);
                });
            }
        });

        UiRelayDrainResult drain = relay.Drain();

        Assert.Equal(producers * perProducer, drain.Executed);
        Assert.Equal(0, relay.PendingCount);
        for (int producer = 0; producer < producers; producer++)
        {
            Assert.Equal(Enumerable.Range(0, perProducer), observed[producer]);
        }
    }

    [Fact]
    public void ExecutionContextFlowsFromProducer()
    {
        UiRelay relay = new();
        AsyncLocal<string?> ambient = new();
        string? observedAmbient = null;
        string? observedCulture = null;
        Thread producer = new(() =>
        {
            ambient.Value = "relay-context";
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ro-RO");
            relay.Post(() =>
            {
                observedAmbient = ambient.Value;
                observedCulture = CultureInfo.CurrentCulture.Name;
            });
        });
        producer.Start();
        producer.Join();

        relay.Drain();

        Assert.Equal("relay-context", observedAmbient);
        Assert.Equal("ro-RO", observedCulture);
    }

    [Fact]
    public void CompletedWorkDoesNotRetainCapturedCallbackState()
    {
        UiRelay relay = new();
        WeakReference captured = EnqueueCapturedObject(relay);

        relay.Drain();
        CollectGarbage();

        Assert.False(captured.IsAlive);
    }

    private static WeakReference EnqueueCapturedObject(UiRelay relay)
    {
        object captured = new();
        WeakReference reference = new(captured);
        relay.Post(() => GC.KeepAlive(captured));
        return reference;
    }

    private static void CollectGarbage()
    {
        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}

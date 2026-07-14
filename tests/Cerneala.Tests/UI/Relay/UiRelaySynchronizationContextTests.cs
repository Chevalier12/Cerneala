using System.Diagnostics;
using System.Globalization;
using Cerneala.UI.Relay;

namespace Cerneala.Tests.UI.Relay;

public sealed class UiRelaySynchronizationContextTests
{
    [Fact]
    public void DrainInstallsRelayContextAndRestoresPreviousContext()
    {
        UiRelay relay = new();
        SynchronizationContext previous = new();
        SynchronizationContext? observed = null;
        int sent = 0;
        relay.Post(() =>
        {
            observed = SynchronizationContext.Current;
            observed!.Send(_ => sent++, null);
        });

        SynchronizationContext.SetSynchronizationContext(previous);
        try
        {
            relay.Drain();

            Assert.NotNull(observed);
            Assert.NotSame(previous, observed);
            Assert.Same(previous, SynchronizationContext.Current);
            Assert.Equal(1, sent);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(null);
        }
    }

    [Fact]
    public void ContextScopeIsNestedAndIdempotent()
    {
        UiRelay relay = new();
        SynchronizationContext previous = new();
        SynchronizationContext.SetSynchronizationContext(previous);
        try
        {
            IDisposable outer = relay.EnterSynchronizationContext();
            SynchronizationContext? relayContext = SynchronizationContext.Current;
            using (relay.EnterSynchronizationContext())
            {
                Assert.Same(relayContext, SynchronizationContext.Current);
            }

            Assert.Same(relayContext, SynchronizationContext.Current);
            outer.Dispose();
            outer.Dispose();
            Assert.Same(previous, SynchronizationContext.Current);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(null);
        }
    }

    [Fact]
    public void ContextIsRestoredWhenFireAndForgetWorkFails()
    {
        UiRelay relay = new();
        SynchronizationContext previous = new();
        relay.Post(() => throw new InvalidOperationException("expected"));

        SynchronizationContext.SetSynchronizationContext(previous);
        try
        {
            Assert.Throws<AggregateException>(() => relay.Drain());
            Assert.Same(previous, SynchronizationContext.Current);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(null);
        }
    }

    [Fact]
    public void SendRejectsOffThreadDispatchAndRecommendsInvokeAsync()
    {
        UiRelay relay = new();
        SynchronizationContext? relayContext = null;
        relay.Post(() => relayContext = SynchronizationContext.Current);
        relay.Drain();
        Exception? exception = null;
        Thread worker = new(() =>
            exception = Record.Exception(() => relayContext!.Send(_ => { }, null)));

        worker.Start();
        worker.Join();

        InvalidOperationException invalidOperation = Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("InvokeAsync", invalidOperation.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task YieldContinuationReturnsThroughRelayOnLaterDrain()
    {
        UiRelay relay = new();
        SynchronizationContext? beforeYield = null;
        SynchronizationContext? afterYield = null;
        Task operation = relay.InvokeAsync(async _ =>
        {
            beforeYield = SynchronizationContext.Current;
            await Task.Yield();
            afterYield = SynchronizationContext.Current;
        });

        relay.Drain();

        Assert.False(operation.IsCompleted);
        Assert.True(relay.HasPendingWork);
        relay.Drain();
        await operation;
        Assert.NotNull(beforeYield);
        Assert.Same(beforeYield, afterYield);
    }

    [Fact]
    public async Task AmbientContextFlowsAcrossPostInvokeAndYield()
    {
        UiRelay relay = new();
        AsyncLocal<string?> ambient = new();
        List<(string? Ambient, string Culture, string? Activity)> observations = [];
        Task? operation = null;
        Thread producer = new(() =>
        {
            ambient.Value = "producer";
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ro-RO");
            using Activity activity = new("relay-test");
            activity.Start();
            relay.Post(() => observations.Add((
                ambient.Value,
                CultureInfo.CurrentCulture.Name,
                Activity.Current?.OperationName)));
            operation = relay.InvokeAsync(async _ =>
            {
                observations.Add((ambient.Value, CultureInfo.CurrentCulture.Name, Activity.Current?.OperationName));
                await Task.Yield();
                observations.Add((ambient.Value, CultureInfo.CurrentCulture.Name, Activity.Current?.OperationName));
            });
        });
        producer.Start();
        producer.Join();

        relay.Drain();
        relay.Drain();
        await operation!;

        Assert.Equal(3, observations.Count);
        Assert.All(observations, observation =>
        {
            Assert.Equal("producer", observation.Ambient);
            Assert.Equal("ro-RO", observation.Culture);
            Assert.Equal("relay-test", observation.Activity);
        });
    }

    [Fact]
    public async Task TwoRootsOnOneThreadKeepTheirContinuationsSeparate()
    {
        UiRelay first = new();
        UiRelay second = new();
        List<string> order = [];
        Task firstOperation = first.InvokeAsync(async _ =>
        {
            order.Add("first-start");
            await Task.Yield();
            order.Add("first-end");
        });
        Task secondOperation = second.InvokeAsync(async _ =>
        {
            order.Add("second-start");
            await Task.Yield();
            order.Add("second-end");
        });

        first.Drain();
        second.Drain();
        Assert.True(first.HasPendingWork);
        Assert.True(second.HasPendingWork);

        first.Drain();
        await firstOperation;
        Assert.False(secondOperation.IsCompleted);
        Assert.Equal(["first-start", "second-start", "first-end"], order);

        second.Drain();
        await secondOperation;
        Assert.Equal(["first-start", "second-start", "first-end", "second-end"], order);
    }

    [Fact]
    public async Task AsyncFaultAndCancellationCompleteWithoutBlockingDrain()
    {
        UiRelay relay = new();
        using CancellationTokenSource cancellation = new();
        Task fault = relay.InvokeAsync(async _ =>
        {
            await Task.Yield();
            throw new InvalidOperationException("async-fault");
        });
        Task canceled = relay.InvokeAsync(async token =>
        {
            await Task.Yield();
            token.ThrowIfCancellationRequested();
        }, cancellation.Token);

        UiRelayDrainResult first = relay.Drain();
        Assert.Equal(2, first.Executed);
        Assert.False(fault.IsCompleted);
        Assert.False(canceled.IsCompleted);

        cancellation.Cancel();
        relay.Drain();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fault);
        Assert.Equal("async-fault", exception.Message);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled);
    }
}

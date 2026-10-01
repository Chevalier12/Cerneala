using System.Runtime.CompilerServices;
using Cerneala.UI.Relay;

namespace Cerneala.Tests.UI.Relay;

public sealed class UiRelayInvocationCompletionTests
{
    public enum InvocationKind
    {
        Action,
        Result,
        AsyncAction,
        TokenAwareAsyncAction,
        AsyncResult
    }

    [Theory]
    [InlineData(InvocationKind.Action, true)]
    [InlineData(InvocationKind.Result, true)]
    [InlineData(InvocationKind.AsyncAction, true)]
    [InlineData(InvocationKind.TokenAwareAsyncAction, true)]
    [InlineData(InvocationKind.AsyncResult, true)]
    [InlineData(InvocationKind.Action, false)]
    [InlineData(InvocationKind.Result, false)]
    [InlineData(InvocationKind.AsyncAction, false)]
    [InlineData(InvocationKind.TokenAwareAsyncAction, false)]
    [InlineData(InvocationKind.AsyncResult, false)]
    public async Task CancellationBeforeStartPreservesSubmissionTokenAndSkipsCallback(InvocationKind kind, bool preCanceled)
    {
        UiRelay relay = new();
        using CancellationTokenSource cancellation = new();
        if (preCanceled)
        {
            cancellation.Cancel();
        }

        int calls = 0;
        Task operation = Schedule(relay, kind, () => calls++, Task.FromResult(42), cancellation.Token);
        Assert.Equal(preCanceled ? 0 : 1, relay.PendingCount);
        cancellation.Cancel();
        UiRelayDrainResult drain = relay.Drain();

        Assert.Equal(0, calls);
        Assert.Equal(0, drain.Executed);
        Assert.Equal(preCanceled ? 0 : 1, drain.Canceled);
        Assert.Equal(0, relay.PendingCount);
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        Assert.Equal(cancellation.Token, exception.CancellationToken);
    }

    [Theory]
    [InlineData(InvocationKind.Action)]
    [InlineData(InvocationKind.Result)]
    [InlineData(InvocationKind.AsyncAction)]
    [InlineData(InvocationKind.TokenAwareAsyncAction)]
    [InlineData(InvocationKind.AsyncResult)]
    public async Task SynchronousCallbackThrowFaultsTheReturnedTaskNotTheDrain(InvocationKind kind)
    {
        UiRelay relay = new();
        InvalidOperationException expected = new("callback failed before returning");
        Task operation = Schedule(relay, kind, () => throw expected, Task.FromResult(42));

        UiRelayDrainResult drain = relay.Drain(out AggregateException? postException);

        Assert.Null(postException);
        Assert.Equal(1, drain.Executed);
        Assert.Equal(1, drain.Faulted);
        Assert.Equal(0, relay.PendingCount);
        Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(() => operation));
    }

    [Theory]
    [InlineData(InvocationKind.Action)]
    [InlineData(InvocationKind.Result)]
    [InlineData(InvocationKind.AsyncAction)]
    [InlineData(InvocationKind.TokenAwareAsyncAction)]
    [InlineData(InvocationKind.AsyncResult)]
    public async Task CancellationAfterDequeueButBeforeStartWinsForEveryOverload(InvocationKind kind)
    {
        using ManualResetEventSlim ready = new();
        using ManualResetEventSlim workReady = new();
        using ManualResetEventSlim dequeued = new();
        using ManualResetEventSlim release = new();
        using CancellationTokenSource cancellation = new();
        UiRelay? relay = null;
        Exception? ownerException = null;
        Thread owner = new(() =>
        {
            try
            {
                relay = new UiRelay(beforeWorkItemStart: () =>
                {
                    dequeued.Set();
                    Assert.True(release.Wait(TimeSpan.FromSeconds(10)));
                });
                ready.Set();
                Assert.True(workReady.Wait(TimeSpan.FromSeconds(10)));
                relay.Drain();
            }
            catch (Exception exception)
            {
                ownerException = exception;
            }
        })
        { IsBackground = true };
        owner.Start();
        Task? operation = null;
        int calls = 0;
        try
        {
            Assert.True(ready.Wait(TimeSpan.FromSeconds(10)));
            operation = Schedule(relay!, kind, () => calls++, Task.FromResult(42), cancellation.Token);
            workReady.Set();
            Assert.True(dequeued.Wait(TimeSpan.FromSeconds(10)));
            cancellation.Cancel();
        }
        finally
        {
            workReady.Set();
            release.Set();
            Assert.True(owner.Join(TimeSpan.FromSeconds(10)));
        }

        Assert.Null(ownerException);
        Assert.Equal(0, calls);
        Assert.Equal(0, relay!.PendingCount);
        OperationCanceledException canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation!);
        Assert.Equal(cancellation.Token, canceled.CancellationToken);
    }

    [Theory]
    [InlineData(InvocationKind.AsyncAction)]
    [InlineData(InvocationKind.TokenAwareAsyncAction)]
    [InlineData(InvocationKind.AsyncResult)]
    public async Task NullReturnedTaskFaultsBeforeCompletionOwnershipIsTransferred(InvocationKind kind)
    {
        UiRelay relay = new();
        Task operation = Schedule(relay, kind, static () => { }, null!);

        UiRelayDrainResult drain = relay.Drain(out AggregateException? postException);

        Assert.Null(postException);
        Assert.Equal(1, drain.Faulted);
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => operation);
        Assert.Equal("The asynchronous Relay callback returned null.", exception.Message);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task AsyncOverloadsAssimilateSuccessOrAllFaultsBeforeAndAfterDrain(bool immediate, bool fault)
    {
        UiRelay relay = new();
        TaskCompletionSource<int> inner = new();
        Exception[] expected = [new InvalidOperationException("first"), new AggregateException(new ArgumentException("nested"))];
        if (immediate)
        {
            Complete();
        }

        int calls = 0;
        Task[] operations = [
            Schedule(relay, InvocationKind.AsyncAction, () => calls++, inner.Task),
            Schedule(relay, InvocationKind.TokenAwareAsyncAction, () => calls++, inner.Task),
            Schedule(relay, InvocationKind.AsyncResult, () => calls++, inner.Task)
        ];
        Assert.All(operations, operation => Assert.False(operation.IsCompleted));
        UiRelayDrainResult drain = relay.Drain(out AggregateException? postException);

        Assert.Null(postException);
        Assert.Equal(3, calls);
        Assert.Equal(3, drain.Executed);
        Assert.Equal(0, drain.Faulted);
        Assert.Equal(0, relay.PendingCount);
        if (!immediate)
        {
            Assert.All(operations, operation => Assert.False(operation.IsCompleted));
            Complete();
        }

        foreach (Task operation in operations)
        {
            if (fault)
            {
                Assert.Same(expected[0], await Assert.ThrowsAsync<InvalidOperationException>(() => operation));
                Assert.Equal(2, operation.Exception!.InnerExceptions.Count);
                Assert.Same(expected[0], operation.Exception.InnerExceptions[0]);
                Assert.Same(expected[1], operation.Exception.InnerExceptions[1]);
            }
            else
            {
                await operation;
            }
        }

        if (!fault)
        {
            Assert.Equal(42, await Assert.IsType<Task<int>>(operations[2]));
        }

        void Complete()
        {
            if (fault)
            {
                inner.SetException(expected);
            }
            else
            {
                inner.SetResult(42);
            }
        }
    }

    [Theory]
    [InlineData(InvocationKind.Action)]
    [InlineData(InvocationKind.Result)]
    [InlineData(InvocationKind.AsyncAction)]
    [InlineData(InvocationKind.TokenAwareAsyncAction)]
    [InlineData(InvocationKind.AsyncResult)]
    public async Task ReturnedTasksDoNotRunUserContinuationsInlineOnTheDrainingThread(InvocationKind kind)
    {
        UiRelay relay = new();
        int ownerThread = Environment.CurrentManagedThreadId;
        int draining = 0;
        Task operation = Schedule(relay, kind, static () => { }, Task.FromResult(42));
        Task<bool> continuation = operation.ContinueWith(
            _ => Environment.CurrentManagedThreadId == ownerThread && Volatile.Read(ref draining) != 0,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        Volatile.Write(ref draining, 1);
        relay.Drain();
        Volatile.Write(ref draining, 0);

        Assert.False(await continuation.WaitAsync(TimeSpan.FromSeconds(10)));
    }

    [Theory]
    [InlineData(InvocationKind.Action, true)]
    [InlineData(InvocationKind.Result, true)]
    [InlineData(InvocationKind.AsyncAction, true)]
    [InlineData(InvocationKind.TokenAwareAsyncAction, true)]
    [InlineData(InvocationKind.AsyncResult, true)]
    [InlineData(InvocationKind.Action, false)]
    [InlineData(InvocationKind.Result, false)]
    [InlineData(InvocationKind.AsyncAction, false)]
    [InlineData(InvocationKind.TokenAwareAsyncAction, false)]
    [InlineData(InvocationKind.AsyncResult, false)]
    public void CancellationReleasesCallbackAndContextWhileReturnedTaskRemainsAlive(InvocationKind kind, bool preCanceled)
    {
        UiRelay relay = new();
        using CancellationTokenSource cancellation = new();
        if (preCanceled)
        {
            cancellation.Cancel();
        }

        (Task operation, WeakReference callback, WeakReference context) = ScheduleCaptured(
            relay, kind, Task.FromResult(42), cancellation.Token);
        cancellation.Cancel();

        AssertCollected(callback, context);
        relay.Drain();
        AssertCollected(callback, context);
        Assert.True(operation.IsCanceled);
        GC.KeepAlive(operation);
        GC.KeepAlive(relay);
        GC.KeepAlive(cancellation);
    }

    [Theory]
    [InlineData(InvocationKind.Action)]
    [InlineData(InvocationKind.Result)]
    [InlineData(InvocationKind.AsyncAction)]
    [InlineData(InvocationKind.TokenAwareAsyncAction)]
    [InlineData(InvocationKind.AsyncResult)]
    public async Task CompletedInvocationsReleaseCallbackAndContextWhileReturnedTaskRemainsAlive(InvocationKind kind)
    {
        UiRelay relay = new();
        TaskCompletionSource<int> inner = new();
        (Task operation, WeakReference callback, WeakReference context) = ScheduleCaptured(relay, kind, inner.Task);

        relay.Drain();
        AssertCollected(callback);
        if (kind is InvocationKind.AsyncAction or InvocationKind.TokenAwareAsyncAction or InvocationKind.AsyncResult)
        {
            Assert.False(operation.IsCompleted);
        }

        inner.SetResult(42);
        await operation.WaitAsync(TimeSpan.FromSeconds(10));
        AssertCollected(callback, context);
        GC.KeepAlive(operation);
        GC.KeepAlive(inner);
        GC.KeepAlive(relay);
    }

    // Explicit delegate types make all five public overloads part of this matrix.
    private static Task Schedule(UiRelay relay, InvocationKind kind, Action onInvoke, Task<int> inner, CancellationToken token = default)
    {
        return kind switch
        {
            InvocationKind.Action => relay.InvokeAsync(onInvoke, token),
            InvocationKind.Result => relay.InvokeAsync((Func<int>)(() => { onInvoke(); return 42; }), token),
            InvocationKind.AsyncAction => relay.InvokeAsync((Func<Task>)(() => { onInvoke(); return inner; }), token),
            InvocationKind.TokenAwareAsyncAction => relay.InvokeAsync(callbackToken => { Assert.Equal(token, callbackToken); onInvoke(); return inner; }, token),
            InvocationKind.AsyncResult => relay.InvokeAsync<int>((Func<Task<int>>)(() => { onInvoke(); return inner; }), token),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (Task Operation, WeakReference Callback, WeakReference Context) ScheduleCaptured(
        UiRelay relay, InvocationKind kind, Task<int> inner, CancellationToken token = default)
    {
        object callback = new();
        object context = new();
        AsyncLocal<object?> ambient = new() { Value = context };
        Task operation = Schedule(relay, kind, () => GC.KeepAlive(callback), inner, token);
        ambient.Value = null;
        return (operation, new WeakReference(callback), new WeakReference(context));
    }

    private static void AssertCollected(params WeakReference[] references)
    {
        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        Assert.All(references, reference => Assert.False(reference.IsAlive));
    }
}

using System.Collections.Concurrent;

namespace Cerneala.UI.Relay;

public sealed class UiRelay : IUiThreadAccess
{
    private readonly ConcurrentQueue<UiRelayWorkItem> queue = new();
    private readonly int ownerThreadId;
    private readonly int maxCallbacksPerUpdate;
    private readonly Action? beforeWorkItemStart;
    private readonly UiRelaySynchronizationContext synchronizationContext;
    private int pendingCount;

    internal UiRelay(UiRelayOptions? options = null, Action? beforeWorkItemStart = null)
    {
        UiRelayOptions resolvedOptions = options ?? new UiRelayOptions();
        if (resolvedOptions.MaxCallbacksPerUpdate <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                resolvedOptions.MaxCallbacksPerUpdate,
                "MaxCallbacksPerUpdate must be greater than zero.");
        }

        ownerThreadId = Environment.CurrentManagedThreadId;
        maxCallbacksPerUpdate = resolvedOptions.MaxCallbacksPerUpdate;
        this.beforeWorkItemStart = beforeWorkItemStart;
        synchronizationContext = new UiRelaySynchronizationContext(this);
    }

    public bool HasPendingWork => Volatile.Read(ref pendingCount) > 0;

    public int PendingCount => Volatile.Read(ref pendingCount);

    public bool CheckAccess()
    {
        return Environment.CurrentManagedThreadId == ownerThreadId;
    }

    public void VerifyAccess()
    {
        if (!CheckAccess())
        {
            throw new InvalidOperationException("Relay work must be drained and UI state must be accessed on the owning UI thread.");
        }
    }

    public void Post(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        Enqueue(new PostWorkItem(callback, ExecutionContext.Capture()));
    }

    public Task InvokeAsync(Action callback, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(callback);
        ActionWorkItem item = new(callback, ExecutionContext.Capture(), cancellationToken);
        Task task = item.Task;
        Enqueue(item);
        return task;
    }

    public Task<T> InvokeAsync<T>(Func<T> callback, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(callback);
        FuncWorkItem<T> item = new(callback, ExecutionContext.Capture(), cancellationToken);
        Task<T> task = item.Task;
        Enqueue(item);
        return task;
    }

    public Task InvokeAsync(
        Func<CancellationToken, Task> callback,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(callback);
        AsyncWorkItem item = new(callback, ExecutionContext.Capture(), cancellationToken);
        Task task = item.Task;
        Enqueue(item);
        return task;
    }

    internal UiRelayDrainResult Drain()
    {
        UiRelayDrainResult result = Drain(out AggregateException? postException);
        if (postException is not null)
        {
            throw postException;
        }

        return result;
    }

    internal UiRelayDrainResult Drain(out AggregateException? postException)
    {
        VerifyAccess();
        using UiRelaySynchronizationContext.Scope contextScope = EnterSynchronizationContext();
        postException = null;
        int snapshotCount = PendingCount;
        int limit = Math.Min(snapshotCount, maxCallbacksPerUpdate);
        int dequeued = 0;
        int executed = 0;
        int canceled = 0;
        int faulted = 0;
        List<Exception>? postFaults = null;

        while (dequeued < limit && queue.TryDequeue(out UiRelayWorkItem? item))
        {
            Interlocked.Decrement(ref pendingCount);
            dequeued++;
            beforeWorkItemStart?.Invoke();
            UiRelayExecutionResult result = item.Execute();
            executed += result.Executed ? 1 : 0;
            canceled += result.Canceled ? 1 : 0;
            faulted += result.Faulted ? 1 : 0;
            if (result.PostException is not null)
            {
                (postFaults ??= []).Add(result.PostException);
            }
        }

        int backlog = PendingCount;
        UiRelayDrainResult drainResult = new(
            snapshotCount,
            dequeued,
            executed,
            canceled,
            faulted,
            backlog,
            backlog);
        if (postFaults is not null)
        {
            postException = new AggregateException(
                "One or more fire-and-forget Relay callbacks failed.",
                postFaults);
        }

        return drainResult;
    }

    internal UiRelaySynchronizationContext.Scope EnterSynchronizationContext()
    {
        VerifyAccess();
        return synchronizationContext.Enter();
    }

    private void Enqueue(UiRelayWorkItem item)
    {
        if (!item.ShouldEnqueue)
        {
            item.CompleteSkippedEnqueue();
            return;
        }

        Interlocked.Increment(ref pendingCount);
        queue.Enqueue(item);
    }

    private enum UiRelayWorkItemState
    {
        Pending,
        Running,
        Completed,
        Canceled
    }

    private readonly record struct UiRelayExecutionResult(
        bool Executed,
        bool Canceled,
        bool Faulted,
        Exception? PostException)
    {
        public static UiRelayExecutionResult CanceledResult { get; } = new(false, true, false, null);
    }

    private abstract class UiRelayWorkItem
    {
        private readonly CancellationToken cancellationToken;
        private ExecutionContext? executionContext;
        private CancellationTokenRegistration cancellationRegistration;
        private int hasCancellationRegistration;
        private int state;

        protected UiRelayWorkItem(ExecutionContext? executionContext, CancellationToken cancellationToken)
        {
            this.executionContext = executionContext;
            this.cancellationToken = cancellationToken;
        }

        protected void RegisterCancellation()
        {
            if (cancellationToken.CanBeCanceled)
            {
                cancellationRegistration = cancellationToken.UnsafeRegister(
                    static state => ((UiRelayWorkItem)state!).Cancel(),
                    this);
                Volatile.Write(ref hasCancellationRegistration, 1);
            }
        }

        internal bool ShouldEnqueue =>
            Volatile.Read(ref state) == (int)UiRelayWorkItemState.Pending;

        protected CancellationToken CancellationToken => cancellationToken;

        internal UiRelayExecutionResult Execute()
        {
            if (Interlocked.CompareExchange(
                    ref state,
                    (int)UiRelayWorkItemState.Running,
                    (int)UiRelayWorkItemState.Pending) != (int)UiRelayWorkItemState.Pending)
            {
                DisposeCancellationRegistration();
                ReleaseExecutionReferences();
                return UiRelayExecutionResult.CanceledResult;
            }

            try
            {
                if (executionContext is null)
                {
                    InvokeCore();
                }
                else
                {
                    ExecutionContext.Run(
                        executionContext,
                        static state => ((UiRelayWorkItem)state!).InvokeCore(),
                        this);
                }

                return new UiRelayExecutionResult(true, false, false, null);
            }
            catch (Exception exception)
            {
                return new UiRelayExecutionResult(true, false, true, CompleteFault(exception));
            }
            finally
            {
                Volatile.Write(ref state, (int)UiRelayWorkItemState.Completed);
                DisposeCancellationRegistration();
                ReleaseExecutionReferences();
            }
        }

        internal void CompleteSkippedEnqueue()
        {
            DisposeCancellationRegistration();
            ReleaseExecutionReferences();
        }

        protected abstract void InvokeCore();

        protected abstract void CompleteCanceled(CancellationToken token);

        protected abstract Exception? CompleteFault(Exception exception);

        protected abstract void ReleaseCallbackReferences();

        private void Cancel()
        {
            if (Interlocked.CompareExchange(
                    ref state,
                    (int)UiRelayWorkItemState.Canceled,
                    (int)UiRelayWorkItemState.Pending) != (int)UiRelayWorkItemState.Pending)
            {
                return;
            }

            CompleteCanceled(cancellationToken);
            ReleaseExecutionReferences();
        }

        private void DisposeCancellationRegistration()
        {
            if (Interlocked.Exchange(ref hasCancellationRegistration, 0) != 0)
            {
                cancellationRegistration.Dispose();
            }
        }

        private void ReleaseExecutionReferences()
        {
            executionContext = null;
            ReleaseCallbackReferences();
        }
    }

    private sealed class PostWorkItem : UiRelayWorkItem
    {
        private Action? callback;

        public PostWorkItem(Action callback, ExecutionContext? executionContext)
            : base(executionContext, CancellationToken.None)
        {
            this.callback = callback;
        }

        protected override void InvokeCore()
        {
            callback!();
        }

        protected override void CompleteCanceled(CancellationToken token)
        {
        }

        protected override Exception? CompleteFault(Exception exception)
        {
            return exception;
        }

        protected override void ReleaseCallbackReferences()
        {
            callback = null;
        }
    }

    private sealed class ActionWorkItem : UiRelayWorkItem
    {
        private readonly Task task;
        private Action? callback;
        private TaskCompletionSource? completion;

        public ActionWorkItem(
            Action callback,
            ExecutionContext? executionContext,
            CancellationToken cancellationToken)
            : base(executionContext, cancellationToken)
        {
            this.callback = callback;
            completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            task = completion.Task;
            RegisterCancellation();
        }

        public Task Task => task;

        protected override void InvokeCore()
        {
            callback!();
            completion!.TrySetResult();
        }

        protected override void CompleteCanceled(CancellationToken token)
        {
            completion!.TrySetCanceled(token);
        }

        protected override Exception? CompleteFault(Exception exception)
        {
            completion!.TrySetException(exception);
            return null;
        }

        protected override void ReleaseCallbackReferences()
        {
            callback = null;
            completion = null;
        }
    }

    private sealed class FuncWorkItem<T> : UiRelayWorkItem
    {
        private readonly Task<T> task;
        private Func<T>? callback;
        private TaskCompletionSource<T>? completion;

        public FuncWorkItem(
            Func<T> callback,
            ExecutionContext? executionContext,
            CancellationToken cancellationToken)
            : base(executionContext, cancellationToken)
        {
            this.callback = callback;
            completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            task = completion.Task;
            RegisterCancellation();
        }

        public Task<T> Task => task;

        protected override void InvokeCore()
        {
            completion!.TrySetResult(callback!());
        }

        protected override void CompleteCanceled(CancellationToken token)
        {
            completion!.TrySetCanceled(token);
        }

        protected override Exception? CompleteFault(Exception exception)
        {
            completion!.TrySetException(exception);
            return null;
        }

        protected override void ReleaseCallbackReferences()
        {
            callback = null;
            completion = null;
        }
    }

    private sealed class AsyncWorkItem : UiRelayWorkItem
    {
        private readonly Task task;
        private Func<CancellationToken, Task>? callback;
        private TaskCompletionSource? completion;

        public AsyncWorkItem(
            Func<CancellationToken, Task> callback,
            ExecutionContext? executionContext,
            CancellationToken cancellationToken)
            : base(executionContext, cancellationToken)
        {
            this.callback = callback;
            completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            task = completion.Task;
            RegisterCancellation();
        }

        public Task Task => task;

        protected override void InvokeCore()
        {
            Task operation = callback!(CancellationToken)
                ?? throw new InvalidOperationException("The asynchronous Relay callback returned null.");
            TaskCompletionSource target = completion!;
            completion = null;
            if (operation.IsCompleted)
            {
                CompleteFromTask(operation, target);
                return;
            }

            _ = operation.ContinueWith(
                static (completed, state) => CompleteFromTask(completed, (TaskCompletionSource)state!),
                target,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        protected override void CompleteCanceled(CancellationToken token)
        {
            completion!.TrySetCanceled(token);
        }

        protected override Exception? CompleteFault(Exception exception)
        {
            completion!.TrySetException(exception);
            return null;
        }

        protected override void ReleaseCallbackReferences()
        {
            callback = null;
            completion = null;
        }

        private static void CompleteFromTask(Task operation, TaskCompletionSource completion)
        {
            if (operation.IsCanceled)
            {
                completion.TrySetCanceled();
            }
            else if (operation.Exception is not null)
            {
                completion.TrySetException(operation.Exception.InnerExceptions);
            }
            else
            {
                completion.TrySetResult();
            }
        }
    }
}

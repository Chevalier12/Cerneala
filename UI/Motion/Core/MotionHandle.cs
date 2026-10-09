using System.Runtime.ExceptionServices;

namespace Cerneala.UI.Motion.Core;

public sealed class MotionHandle : IDisposable
{
    private readonly MotionCompletionSource completionSource = new();
    private Action<MotionCancelBehavior>? cancel;
    private Action? complete;
    private Action? dispose;
    private Action? verifyAccess;
    private EventHandler<MotionCompletedEventArgs>? completed;
    private bool disposed;

    internal MotionHandle(
        Action<MotionCancelBehavior> cancel,
        Action complete,
        Action dispose,
        Action? verifyAccess = null)
    {
        this.cancel = cancel ?? throw new ArgumentNullException(nameof(cancel));
        this.complete = complete ?? throw new ArgumentNullException(nameof(complete));
        this.dispose = dispose ?? throw new ArgumentNullException(nameof(dispose));
        this.verifyAccess = verifyAccess;
    }

    public bool IsActive => !IsCompleted && !IsCanceled && !disposed;

    public bool IsCompleted { get; private set; }

    public bool IsCanceled { get; private set; }

    public ValueTask Completion => new(completionSource.Task);

    public event EventHandler<MotionCompletedEventArgs>? Completed
    {
        add
        {
            if (!disposed && !IsCompleted && !IsCanceled)
            {
                completed += value;
            }
        }
        remove => completed -= value;
    }

    public void Cancel(MotionCancelBehavior behavior = MotionCancelBehavior.KeepCurrent)
    {
        if (!IsActive)
        {
            return;
        }

        verifyAccess?.Invoke();
        cancel?.Invoke(behavior);
    }

    public void Complete()
    {
        if (!IsActive)
        {
            return;
        }

        verifyAccess?.Invoke();
        complete?.Invoke();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        // Rejected access must preserve both owner state and this handle's callbacks.
        verifyAccess?.Invoke();
        disposed = true;
        try
        {
            dispose?.Invoke();
        }
        finally
        {
            completed = null;
            ClearActions();
        }
    }

    internal void FinishCompleted(bool fireEvent)
    {
        if (IsCompleted || IsCanceled)
        {
            return;
        }

        IsCompleted = true;
        completionSource.TrySetResult();
        try
        {
            if (fireEvent)
            {
                RaiseCompleted(new MotionCompletedEventArgs(MotionCompletionState.Completed, null));
            }
        }
        finally
        {
            completed = null;
            ClearActions();
        }
    }

    internal void FinishCanceled(MotionCancelBehavior behavior, bool fireEvent)
    {
        if (IsCompleted || IsCanceled)
        {
            return;
        }

        IsCanceled = true;
        completionSource.TrySetCanceled();
        try
        {
            if (fireEvent)
            {
                RaiseCompleted(new MotionCompletedEventArgs(MotionCompletionState.Canceled, behavior));
            }
        }
        finally
        {
            completed = null;
            ClearActions();
        }
    }

    private void ClearActions()
    {
        cancel = null;
        complete = null;
        dispose = null;
        verifyAccess = null;
    }

    private void RaiseCompleted(MotionCompletedEventArgs args)
    {
        if (completed is null)
        {
            return;
        }

        ExceptionDispatchInfo? firstFailure = null;
        foreach (EventHandler<MotionCompletedEventArgs> handler in completed.GetInvocationList())
        {
            try
            {
                handler(this, args);
            }
            catch (Exception exception)
            {
                firstFailure ??= ExceptionDispatchInfo.Capture(exception);
            }
        }

        firstFailure?.Throw();
    }
}

public sealed class MotionCompletedEventArgs : EventArgs
{
    public MotionCompletedEventArgs(MotionCompletionState state, MotionCancelBehavior? cancelBehavior)
    {
        State = state;
        CancelBehavior = cancelBehavior;
    }

    public MotionCompletionState State { get; }

    public MotionCancelBehavior? CancelBehavior { get; }

    public bool IsCanceled => State == MotionCompletionState.Canceled;
}

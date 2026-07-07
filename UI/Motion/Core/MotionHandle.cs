namespace Cerneala.UI.Motion.Core;

public sealed class MotionHandle : IDisposable
{
    private readonly MotionCompletionSource completionSource = new();
    private Action<MotionCancelBehavior>? cancel;
    private Action? complete;
    private Action? dispose;
    private EventHandler<MotionCompletedEventArgs>? completed;
    private bool disposed;

    internal MotionHandle(
        Action<MotionCancelBehavior> cancel,
        Action complete,
        Action dispose)
    {
        this.cancel = cancel ?? throw new ArgumentNullException(nameof(cancel));
        this.complete = complete ?? throw new ArgumentNullException(nameof(complete));
        this.dispose = dispose ?? throw new ArgumentNullException(nameof(dispose));
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

        cancel?.Invoke(behavior);
    }

    public void Complete()
    {
        if (!IsActive)
        {
            return;
        }

        complete?.Invoke();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        dispose?.Invoke();
        completed = null;
        ClearActions();
    }

    internal void FinishCompleted(bool fireEvent)
    {
        if (IsCompleted || IsCanceled)
        {
            return;
        }

        IsCompleted = true;
        completionSource.TrySetResult();
        if (fireEvent)
        {
            completed?.Invoke(this, new MotionCompletedEventArgs(MotionCompletionState.Completed, null));
        }

        completed = null;
        ClearActions();
    }

    internal void FinishCanceled(MotionCancelBehavior behavior, bool fireEvent)
    {
        if (IsCompleted || IsCanceled)
        {
            return;
        }

        IsCanceled = true;
        completionSource.TrySetCanceled();
        if (fireEvent)
        {
            completed?.Invoke(this, new MotionCompletedEventArgs(MotionCompletionState.Canceled, behavior));
        }

        completed = null;
        ClearActions();
    }

    private void ClearActions()
    {
        cancel = null;
        complete = null;
        dispose = null;
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

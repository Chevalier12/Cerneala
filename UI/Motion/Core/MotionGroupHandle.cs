namespace Cerneala.UI.Motion.Core;

public sealed class MotionGroupHandle
{
    private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Action cancel;

    internal MotionGroupHandle(Action cancel)
    {
        this.cancel = cancel ?? throw new ArgumentNullException(nameof(cancel));
    }

    public bool IsCompleted { get; private set; }

    public bool IsCanceled { get; private set; }

    public ValueTask Completion => new(completion.Task);

    // Raised synchronously on the thread that completes or cancels the group, unlike the
    // continuations of Completion, which run asynchronously.
    internal event Action? Terminated;

    public void Cancel()
    {
        if (IsCompleted || IsCanceled)
        {
            return;
        }

        IsCanceled = true;
        try
        {
            cancel();
        }
        finally
        {
            completion.TrySetCanceled();
            RaiseTerminated();
        }
    }

    internal void Complete()
    {
        if (IsCompleted || IsCanceled)
        {
            return;
        }

        IsCompleted = true;
        completion.TrySetResult();
        RaiseTerminated();
    }

    private void RaiseTerminated()
    {
        Action? handlers = Terminated;
        Terminated = null;
        handlers?.Invoke();
    }
}

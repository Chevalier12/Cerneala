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

    public void Cancel()
    {
        if (IsCompleted || IsCanceled)
        {
            return;
        }

        IsCanceled = true;
        cancel();
        completion.TrySetCanceled();
    }

    internal void Complete()
    {
        if (IsCompleted || IsCanceled)
        {
            return;
        }

        IsCompleted = true;
        completion.TrySetResult();
    }
}

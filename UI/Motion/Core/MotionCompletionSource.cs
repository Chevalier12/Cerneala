namespace Cerneala.UI.Motion.Core;

public sealed class MotionCompletionSource
{
    private readonly TaskCompletionSource<object?> source =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Task => source.Task;

    public bool TrySetResult()
    {
        return source.TrySetResult(null);
    }

    public bool TrySetCanceled()
    {
        return source.TrySetCanceled();
    }
}

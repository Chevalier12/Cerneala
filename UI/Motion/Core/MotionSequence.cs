namespace Cerneala.UI.Motion.Core;

public static class MotionSequence
{
    public static MotionGroupHandle Start(params Func<MotionHandle>[] steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        int index = 0;
        MotionHandle? active = null;
        MotionGroupHandle? group = null;

        void StartNext()
        {
            if (group?.IsCanceled == true)
            {
                return;
            }

            if (index >= steps.Length)
            {
                group!.Complete();
                return;
            }

            MotionHandle current = steps[index++]() ??
                throw new InvalidOperationException("Motion sequence steps cannot return null handles.");
            active = current;
            bool completionObserved = false;
            void OnCompleted(object? sender, MotionCompletedEventArgs args)
            {
                if (completionObserved)
                {
                    return;
                }

                completionObserved = true;
                current.Completed -= OnCompleted;
                if (args.IsCanceled)
                {
                    group!.Cancel();
                    return;
                }

                StartNext();
            }

            current.Completed += OnCompleted;
            if (current.IsCompleted)
            {
                OnCompleted(
                    current,
                    new MotionCompletedEventArgs(MotionCompletionState.Completed, null));
            }
            else if (current.IsCanceled)
            {
                OnCompleted(
                    current,
                    new MotionCompletedEventArgs(
                        MotionCompletionState.Canceled,
                        MotionCancelBehavior.KeepCurrent));
            }
        }

        group = new MotionGroupHandle(() => active?.Cancel());
        StartNext();
        return group;
    }
}

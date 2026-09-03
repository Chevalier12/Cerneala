using System.Runtime.ExceptionServices;

namespace Cerneala.UI.Motion.Core;

public static class MotionGroup
{
    public static MotionGroupHandle Parallel(params MotionHandle[] children)
    {
        ArgumentNullException.ThrowIfNull(children);
        int remaining = children.Length;
        MotionGroupHandle? group = null;
        group = new MotionGroupHandle(() =>
        {
            ExceptionDispatchInfo? firstFailure = null;
            foreach (MotionHandle child in children)
            {
                try
                {
                    child.Cancel();
                }
                catch (Exception exception)
                {
                    firstFailure ??= ExceptionDispatchInfo.Capture(exception);
                }
            }

            firstFailure?.Throw();
        });

        if (remaining == 0)
        {
            group.Complete();
            return group;
        }

        foreach (MotionHandle child in children)
        {
            int terminalStateObserved = 0;
            void ObserveTerminalState()
            {
                if (Interlocked.Exchange(ref terminalStateObserved, 1) == 0 &&
                    Interlocked.Decrement(ref remaining) == 0)
                {
                    group.Complete();
                }
            }

            child.Completed += (_, _) => ObserveTerminalState();
            if (child.IsCompleted || child.IsCanceled)
            {
                ObserveTerminalState();
            }
        }

        return group;
    }
}

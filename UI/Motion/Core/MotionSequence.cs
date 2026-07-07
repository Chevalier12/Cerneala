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

            active = steps[index++]();
            active.Completed += (_, args) =>
            {
                if (args.IsCanceled)
                {
                    return;
                }

                StartNext();
            };
        }

        group = new MotionGroupHandle(() => active?.Cancel());
        StartNext();
        return group;
    }
}

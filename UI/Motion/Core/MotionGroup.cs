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
            foreach (MotionHandle child in children)
            {
                child.Cancel();
            }
        });

        if (remaining == 0)
        {
            group.Complete();
            return group;
        }

        foreach (MotionHandle child in children)
        {
            child.Completed += (_, _) =>
            {
                remaining--;
                if (remaining == 0)
                {
                    group.Complete();
                }
            };
        }

        return group;
    }
}

using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Motion.Transactions;

public sealed class MotionTransactionOptions
{
    public MotionTransactionOptions(
        MotionSpec defaultSpec,
        bool isDisabled = false,
        MotionPriority priority = MotionPriority.Normal)
    {
        DefaultSpec = defaultSpec ?? throw new ArgumentNullException(nameof(defaultSpec));
        IsDisabled = isDisabled;
        Priority = priority;
    }

    public MotionSpec DefaultSpec { get; }

    public bool IsDisabled { get; }

    public MotionPriority Priority { get; }
}

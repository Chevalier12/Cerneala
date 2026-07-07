using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Motion.Transactions;

public sealed class MotionTransactionOptions
{
    public MotionTransactionOptions(MotionSpec defaultSpec, bool isDisabled = false)
    {
        DefaultSpec = defaultSpec ?? throw new ArgumentNullException(nameof(defaultSpec));
        IsDisabled = isDisabled;
    }

    public MotionSpec DefaultSpec { get; }

    public bool IsDisabled { get; }
}

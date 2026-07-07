namespace Cerneala.UI.Motion.Transactions;

public sealed class MotionTransaction
{
    internal MotionTransaction(MotionTransactionOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public MotionTransactionOptions Options { get; }

    public bool IsDisabled => Options.IsDisabled;
}

namespace Cerneala.UI.Motion.Transactions;

public sealed class MotionTransactionScope : IDisposable
{
    private readonly MotionTransactionContext context;
    private bool disposed;

    internal MotionTransactionScope(MotionTransactionContext context, MotionTransaction transaction)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }

    public MotionTransaction Transaction { get; }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        context.Pop(Transaction);
    }
}

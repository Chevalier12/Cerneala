namespace Cerneala.UI.Relay;

internal sealed class CapturedUiThreadAccess : IUiThreadAccess
{
    private readonly int ownerThreadId = Environment.CurrentManagedThreadId;

    public bool CheckAccess() => Environment.CurrentManagedThreadId == ownerThreadId;

    public void VerifyAccess()
    {
        if (!CheckAccess())
        {
            throw new InvalidOperationException("This operation must run on the owning UI thread.");
        }
    }
}

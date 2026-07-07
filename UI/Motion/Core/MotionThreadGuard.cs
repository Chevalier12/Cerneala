namespace Cerneala.UI.Motion.Core;

public sealed class MotionThreadGuard
{
    private readonly int ownerThreadId;

    public MotionThreadGuard(int ownerThreadId)
    {
        this.ownerThreadId = ownerThreadId;
    }

    public bool CheckAccess()
    {
        return Environment.CurrentManagedThreadId == ownerThreadId;
    }

    public void VerifyAccess()
    {
        if (!CheckAccess())
        {
            throw new InvalidOperationException("Motion APIs must be called on the owning UI thread. Marshal cross-thread animation requests through the platform UI dispatcher before mutating motion state.");
        }
    }
}

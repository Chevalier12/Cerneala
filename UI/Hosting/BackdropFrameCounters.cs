namespace Cerneala.UI.Hosting;

internal sealed class BackdropFrameCounters
{
    public long RequestedFrames { get; private set; }

    public long AcquiredFrames { get; private set; }

    public long SharedScopeUses { get; private set; }

    public long SkippedFrames { get; private set; }

    public long FailedFrames { get; private set; }

    public void RecordRequested()
    {
        RequestedFrames = checked(RequestedFrames + 1);
    }

    public void RecordAcquired(int scopeCount)
    {
        if (scopeCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scopeCount));
        }

        AcquiredFrames = checked(AcquiredFrames + 1);
        SharedScopeUses = checked(
            SharedScopeUses + scopeCount - 1);
    }

    public void RecordSkipped()
    {
        SkippedFrames = checked(SkippedFrames + 1);
    }

    public void RecordFailed()
    {
        FailedFrames = checked(FailedFrames + 1);
    }
}

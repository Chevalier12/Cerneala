namespace Cerneala.UI.Hosting;

internal enum BackdropFrameFailureReason
{
    MissingSource,
    InvalidViewport,
    NullLease,
    AcquisitionFailed,
}

internal readonly record struct BackdropFrameFailureDiagnostic(
    string Code,
    BackdropFrameFailureReason Reason,
    string Detail);

internal readonly record struct BackdropFrameDiagnosticSnapshot(
    long RequestedFrames,
    long AcquiredFrames,
    long SharedScopeUses,
    long SkippedFrames,
    long FailedFrames,
    BackdropFrameFailureDiagnostic? LastFailure);

internal sealed class BackdropFrameCounters
{
    public long RequestedFrames { get; private set; }

    public long AcquiredFrames { get; private set; }

    public long SharedScopeUses { get; private set; }

    public long SkippedFrames { get; private set; }

    public long FailedFrames { get; private set; }

    public BackdropFrameFailureDiagnostic? LastFailure { get; private set; }

    public BackdropFrameDiagnosticSnapshot Snapshot => new(
        RequestedFrames,
        AcquiredFrames,
        SharedScopeUses,
        SkippedFrames,
        FailedFrames,
        LastFailure);

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

    public void RecordFailed(
        BackdropFrameFailureReason reason,
        string detail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(detail);
        FailedFrames = checked(FailedFrames + 1);
        LastFailure = new BackdropFrameFailureDiagnostic(
            GetCode(reason),
            reason,
            detail);
    }

    private static string GetCode(BackdropFrameFailureReason reason)
    {
        return reason switch
        {
            BackdropFrameFailureReason.MissingSource => "PRISM7101",
            BackdropFrameFailureReason.InvalidViewport => "PRISM7102",
            BackdropFrameFailureReason.NullLease => "PRISM7103",
            BackdropFrameFailureReason.AcquisitionFailed => "PRISM7104",
            _ => throw new ArgumentOutOfRangeException(nameof(reason)),
        };
    }
}

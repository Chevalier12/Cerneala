using Cerneala.Timbre;

namespace Cerneala.PreviewHost;

// Output of a Live Preview whose audio is disabled. Every attempt to open it
// is refused and counted, so a sound the document starts fails explicitly
// (TimbreErrorKind.DeviceUnavailable) instead of reporting a silent success.
internal sealed class DisabledPreviewTimbreOutput : ITimbreOutput
{
    internal const string Message = "Live Preview audio is disabled; enable preview audio to hear sounds.";

    private int blockedOpens;

    public int BlockedOpens => Volatile.Read(ref blockedOpens);

    public int QueuedFrames => 0;

    public void Open(ITimbreOutputClient client)
    {
        Interlocked.Increment(ref blockedOpens);
        throw new TimbreException(TimbreErrorKind.DeviceUnavailable, Message);
    }

    public void Submit(ReadOnlySpan<float> samples) =>
        throw new InvalidOperationException("The disabled preview audio output is never open.");

    public void Close()
    {
    }
}

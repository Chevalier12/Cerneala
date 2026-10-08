using Cerneala.Timbre;

namespace Cerneala.UI.Timbre;

// Output of an Application-owned TimbreRuntime. The window platform's shared
// output is resolved when the mixer first opens it, so the runtime may be
// created before the Application is installed on a window runtime.
internal sealed class PlatformTimbreOutput(Func<ITimbreOutput?> resolve) : ITimbreOutput
{
    // Touched only by the runtime's mixer thread.
    private ITimbreOutput? target;

    public int QueuedFrames => Target.QueuedFrames;

    public void Open(ITimbreOutputClient client)
    {
        ITimbreOutput output = resolve() ?? throw new TimbreException(
            TimbreErrorKind.DeviceUnavailable,
            "The window platform provides no audio output.");
        output.Open(client);
        target = output;
    }

    public void Submit(ReadOnlySpan<float> samples) => Target.Submit(samples);

    public void Close()
    {
        ITimbreOutput? output = target;
        target = null;
        output?.Close();
    }

    private ITimbreOutput Target =>
        target ?? throw new InvalidOperationException("The platform audio output is not open.");
}

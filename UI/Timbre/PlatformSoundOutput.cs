using Cerneala.Timbre;

namespace Cerneala.UI.Timbre;

// Output of an Application-owned SoundRuntime. The window platform's shared
// output is resolved when the mixer first opens it, so the runtime may be
// created before the Application is installed on a window runtime.
internal sealed class PlatformSoundOutput(Func<ISoundOutput?> resolve) : ISoundOutput
{
    // Touched only by the runtime's mixer thread.
    private ISoundOutput? target;

    public int QueuedFrames => Target.QueuedFrames;

    public void Open(ISoundOutputClient client)
    {
        ISoundOutput output = resolve() ?? throw new SoundException(
            SoundErrorKind.DeviceUnavailable,
            "The window platform provides no audio output.");
        output.Open(client);
        target = output;
    }

    public void Submit(ReadOnlySpan<float> samples) => Target.Submit(samples);

    public void Close()
    {
        ISoundOutput? output = target;
        target = null;
        output?.Close();
    }

    private ISoundOutput Target =>
        target ?? throw new InvalidOperationException("The platform audio output is not open.");
}

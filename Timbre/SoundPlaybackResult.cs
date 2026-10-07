namespace Cerneala.Timbre;

public sealed class SoundPlaybackResult
{
    internal SoundPlaybackResult(SoundPlaybackState state, SoundException? error, bool tailTruncated)
    {
        State = state;
        Error = error;
        TailTruncated = tailTruncated;
    }

    public SoundPlaybackState State { get; }

    public SoundException? Error { get; }

    public bool TailTruncated { get; }
}

namespace Cerneala.Timbre;

public sealed class TimbrePlaybackResult
{
    internal TimbrePlaybackResult(TimbrePlaybackState state, TimbreException? error, bool tailTruncated)
    {
        State = state;
        Error = error;
        TailTruncated = tailTruncated;
    }

    public TimbrePlaybackState State { get; }

    public TimbreException? Error { get; }

    public bool TailTruncated { get; }
}

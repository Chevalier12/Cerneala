namespace Cerneala.Timbre;

public abstract class TimbreReader : IDisposable
{
    protected TimbreReader()
    {
    }

    public abstract long? LengthFrames { get; }

    public abstract ValueTask<TimbreReadResult> ReadAsync(Memory<float> destination, CancellationToken cancellationToken);

    public abstract ValueTask SeekAsync(long frame, CancellationToken cancellationToken);

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }
}

public readonly struct TimbreReadResult : IEquatable<TimbreReadResult>
{
    public TimbreReadResult(int frames, bool endOfSource)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(frames);
        Frames = frames;
        EndOfSource = endOfSource;
    }

    public int Frames { get; }

    public bool EndOfSource { get; }

    public bool Equals(TimbreReadResult other) => Frames == other.Frames && EndOfSource == other.EndOfSource;

    public override bool Equals(object? obj) => obj is TimbreReadResult other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Frames, EndOfSource);

    public static bool operator ==(TimbreReadResult left, TimbreReadResult right) => left.Equals(right);

    public static bool operator !=(TimbreReadResult left, TimbreReadResult right) => !left.Equals(right);

    public override string ToString() => $"Frames = {Frames}, EndOfSource = {EndOfSource}";
}

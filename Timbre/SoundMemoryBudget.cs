using Cerneala.Timbre.Engine;

namespace Cerneala.Timbre;

// Cooperative accounting of one source opening's live reader, decoder and I/O
// memory against the runtime's StreamingMemoryLimit. A reservation precedes
// the allocation it covers; the runtime closes the budget, releasing what is
// still reserved, only after the reader (and its stream) has been disposed.
public sealed class SoundMemoryBudget
{
    private readonly SoundMemoryPool pool;
    private readonly string sourceName;
    private readonly object gate = new();
    private List<SoundMemoryReservation>? reservations;
    private bool closed;

    internal SoundMemoryBudget(SoundMemoryPool pool, string sourceName)
    {
        this.pool = pool;
        this.sourceName = sourceName;
    }

    public long Limit => pool.Limit;

    public long Reserved => pool.Reserved;

    public SoundMemoryReservation Reserve(long bytes)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bytes);
        lock (gate)
        {
            if (closed)
            {
                throw new InvalidOperationException($"The reader of '{sourceName}' has been released; its memory budget is closed.");
            }

            if (!pool.TryReserve(bytes))
            {
                throw new SoundException(
                    SoundErrorKind.ResourceLimitExceeded,
                    $"'{sourceName}' needs {bytes} more bytes of reader memory; {pool.Reserved} of the {pool.Limit}-byte streaming memory limit are reserved.");
            }

            SoundMemoryReservation reservation = new(this, bytes);
            (reservations ??= []).Add(reservation);
            return reservation;
        }
    }

    internal void Release(SoundMemoryReservation reservation)
    {
        lock (gate)
        {
            if (reservations?.Remove(reservation) == true)
            {
                pool.Release(reservation.Bytes);
            }
        }
    }

    // Called by the runtime after the reader has been disposed.
    internal void Close()
    {
        lock (gate)
        {
            closed = true;
            if (reservations is null)
            {
                return;
            }

            foreach (SoundMemoryReservation reservation in reservations)
            {
                pool.Release(reservation.Bytes);
            }

            reservations = null;
        }
    }
}

public sealed class SoundMemoryReservation : IDisposable
{
    private readonly SoundMemoryBudget owner;

    internal SoundMemoryReservation(SoundMemoryBudget owner, long bytes)
    {
        this.owner = owner;
        Bytes = bytes;
    }

    public long Bytes { get; }

    public void Dispose() => owner.Release(this);
}

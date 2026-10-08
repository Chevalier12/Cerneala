using Cerneala.Timbre.Engine;

namespace Cerneala.Timbre;

// Cooperative accounting of one source opening's live reader, decoder and I/O
// memory against the runtime's StreamingMemoryLimit. A reservation precedes
// the allocation it covers; the runtime closes the budget, releasing what is
// still reserved, only after the reader (and its stream) has been disposed.
public sealed class TimbreMemoryBudget
{
    private readonly TimbreMemoryPool pool;
    private readonly string sourceName;
    private readonly object gate = new();
    private List<TimbreMemoryReservation>? reservations;
    private bool closed;

    internal TimbreMemoryBudget(TimbreMemoryPool pool, string sourceName)
    {
        this.pool = pool;
        this.sourceName = sourceName;
    }

    public long Limit => pool.Limit;

    public long Reserved => pool.Reserved;

    public TimbreMemoryReservation Reserve(long bytes)
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
                throw new TimbreException(
                    TimbreErrorKind.ResourceLimitExceeded,
                    $"'{sourceName}' needs {bytes} more bytes of reader memory; {pool.Reserved} of the {pool.Limit}-byte streaming memory limit are reserved.");
            }

            TimbreMemoryReservation reservation = new(this, bytes);
            (reservations ??= []).Add(reservation);
            return reservation;
        }
    }

    internal void Release(TimbreMemoryReservation reservation)
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

            foreach (TimbreMemoryReservation reservation in reservations)
            {
                pool.Release(reservation.Bytes);
            }

            reservations = null;
        }
    }
}

public sealed class TimbreMemoryReservation : IDisposable
{
    private readonly TimbreMemoryBudget owner;

    internal TimbreMemoryReservation(TimbreMemoryBudget owner, long bytes)
    {
        this.owner = owner;
        Bytes = bytes;
    }

    public long Bytes { get; }

    public void Dispose() => owner.Release(this);
}

using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Engine;

// Cooperative reader/decoder memory accounting: budget-aware source factories
// reserve live memory before allocating it, against the runtime's explicit
// StreamingMemoryLimit; the runtime retires the reader before its reservations.
public sealed class MemoryBudgetTests
{
    private const long Limit = 1_000_000;

    [Fact]
    public async Task ReaderFactoryReservationsAreForwardedAndReleasedAfterTheReader()
    {
        using TimbreRig rig = new(options => options.StreamingMemoryLimit = Limit, hold: false);
        long reservedDuringDispose = -1;
        TimbreMemoryBudget? seen = null;
        TimbreClip clip = new(TimbreSource.FromReader(budget =>
        {
            seen = budget;
            budget.Reserve(300_000);
            return new DeterministicTimbreReader(48_000, DeterministicTimbreReader.DefaultSignal)
            {
                OnDispose = () => reservedDuringDispose = budget.Reserved,
            };
        }, "budgeted"), loading: TimbreLoading.Preload);

        await rig.Runtime.PrepareAsync(clip);

        Assert.NotNull(seen);
        Assert.Equal(Limit, seen!.Limit);
        Assert.True(reservedDuringDispose >= 300_000, "The reservation was released before the reader was disposed.");
        Assert.Equal(0, seen.Reserved);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().ReaderMemoryBytes);
    }

    [Fact]
    public async Task ReservationOverTheAllowanceIsRefusedBeforeTheAllocation()
    {
        using TimbreRig rig = new(options => options.StreamingMemoryLimit = Limit, hold: false);
        int allocations = 0;
        TimbreClip clip = new(TimbreSource.FromReader(budget =>
        {
            budget.Reserve(Limit + 1);
            allocations++;
            return new DeterministicTimbreReader(48_000, DeterministicTimbreReader.DefaultSignal);
        }, "oversized"));

        TimbreException prepared = await Assert.ThrowsAsync<TimbreException>(() => rig.Runtime.PrepareAsync(clip));
        TimbrePlaybackResult played = await TimbreRig.CompletionAsync(rig.Scope.Play(clip));

        Assert.Equal(TimbreErrorKind.ResourceLimitExceeded, prepared.Kind);
        Assert.Equal(TimbrePlaybackState.Failed, played.State);
        Assert.Equal(TimbreErrorKind.ResourceLimitExceeded, played.Error!.Kind);
        Assert.Equal(0, allocations);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().ReaderMemoryBytes);
    }

    [Fact]
    public async Task PlaybacksShareTheRuntimeAllowanceAndRefusalDoesNotAffectOthers()
    {
        using TimbreRig rig = new(options => options.StreamingMemoryLimit = Limit);
        DeterministicTimbreSourceFactory pcm = new(48_000);
        TimbreClip clip = new(TimbreSource.FromReader(budget =>
        {
            budget.Reserve(600_000);
            return pcm.Open();
        }, "shared"), loading: TimbreLoading.Streaming);

        TimbrePlayback first = rig.Scope.Play(clip);
        await rig.ReadyAsync(first);
        TimbrePlaybackResult second = await TimbreRig.CompletionAsync(rig.Scope.Play(clip));

        Assert.Equal(TimbreErrorKind.ResourceLimitExceeded, second.Error!.Kind);
        Assert.False(first.State is TimbrePlaybackState.Failed or TimbrePlaybackState.Canceled);
        Assert.Equal(600_000, rig.Runtime.GetDiagnostics().ReaderMemoryBytes);

        first.Cancel();
        await TimbreRig.ReleasedAsync(first);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().ReaderMemoryBytes);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().StreamingBufferBytes);
    }

    [Fact]
    public async Task StreamingBuffersAndReaderReservationsUseTheSameAllowance()
    {
        using TimbreRig rig = new(options => options.StreamingMemoryLimit = Limit, hold: false);
        TimbreClip clip = new(TimbreSource.FromReader(budget =>
        {
            budget.Reserve(Limit - 1024);
            return new DeterministicTimbreReader(48_000, DeterministicTimbreReader.DefaultSignal);
        }, "ring"), loading: TimbreLoading.Streaming);

        TimbrePlaybackResult result = await TimbreRig.CompletionAsync(rig.Scope.Play(clip));

        Assert.Equal(TimbreErrorKind.ResourceLimitExceeded, result.Error!.Kind);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().ReaderMemoryBytes);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().StreamingBufferBytes);
    }

    [Fact]
    public async Task ReservationsCanBeReturnedEarlyAndTheBudgetClosesWithTheReader()
    {
        using TimbreRig rig = new(options => options.StreamingMemoryLimit = Limit, hold: false);
        TimbreMemoryBudget? captured = null;
        long afterEarlyRelease = -1;
        TimbreClip clip = new(TimbreSource.FromReader(budget =>
        {
            captured = budget;
            TimbreMemoryReservation scratch = budget.Reserve(400_000);
            scratch.Dispose();
            scratch.Dispose();
            afterEarlyRelease = budget.Reserved;
            Assert.Equal(400_000, scratch.Bytes);
            return new DeterministicTimbreReader(4_800, DeterministicTimbreReader.DefaultSignal);
        }, "early"), loading: TimbreLoading.Preload);

        await rig.Runtime.PrepareAsync(clip);

        Assert.Equal(0, afterEarlyRelease);
        Assert.Throws<InvalidOperationException>(() => captured!.Reserve(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => captured!.Reserve(-1));
    }

    [Fact]
    public async Task DefaultAllowanceIsAccountingOnlyWithoutANumericCap()
    {
        using TimbreRig rig = new(hold: false);
        long allocatedDuringReserve = -1;
        long limit = 0;
        TimbreClip clip = new(TimbreSource.FromReader(budget =>
        {
            limit = budget.Limit;
            long before = GC.GetAllocatedBytesForCurrentThread();
            using (budget.Reserve(1L << 50))
            {
                allocatedDuringReserve = GC.GetAllocatedBytesForCurrentThread() - before;
            }

            return new DeterministicTimbreReader(4_800, DeterministicTimbreReader.DefaultSignal);
        }, "unbounded"), loading: TimbreLoading.Preload);

        await rig.Runtime.PrepareAsync(clip);

        Assert.Equal(long.MaxValue, limit);
        Assert.InRange(allocatedDuringReserve, 0, 1024);
    }

    [Fact]
    public async Task StreamFactoryReceivesTheBudgetAndFailureReleasesItsReservations()
    {
        using TimbreRig rig = new(options => options.StreamingMemoryLimit = Limit, hold: false);
        bool streamDisposedFirst = false;
        TimbreMemoryBudget? captured = null;
        TimbreClip clip = new(TimbreSource.FromStream(budget =>
        {
            captured = budget;
            budget.Reserve(100_000);
            return new ObservedStream("not audio"u8.ToArray(), () => streamDisposedFirst = budget.Reserved >= 100_000);
        }, "stream"));

        TimbrePlaybackResult result = await TimbreRig.CompletionAsync(rig.Scope.Play(clip));

        Assert.Equal(TimbreErrorKind.UnsupportedFormat, result.Error!.Kind);
        Assert.True(streamDisposedFirst, "The stream must be disposed before its reservation is released.");
        Assert.Equal(0, captured!.Reserved);
    }

    [Fact]
    public async Task FileSourceReservesItsIoBufferBeforeOpeningTheFile()
    {
        string missing = Path.Combine(Path.GetTempPath(), $"timbre-missing-{Guid.NewGuid():N}.wav");
        using TimbreRig limited = new(options => options.StreamingMemoryLimit = 1024, hold: false);
        using TimbreRig unlimited = new(hold: false);

        TimbrePlaybackResult refused = await TimbreRig.CompletionAsync(limited.Scope.Play(new TimbreClip(missing)));
        TimbrePlaybackResult unavailable = await TimbreRig.CompletionAsync(unlimited.Scope.Play(new TimbreClip(missing)));

        // The reservation precedes any I/O: the missing file is never touched.
        Assert.Equal(TimbreErrorKind.ResourceLimitExceeded, refused.Error!.Kind);
        Assert.Equal(TimbreErrorKind.SourceUnavailable, unavailable.Error!.Kind);
        Assert.Equal(0, limited.Runtime.GetDiagnostics().ReaderMemoryBytes);
    }

    private sealed class ObservedStream(byte[] data, Action onDispose) : MemoryStream(data)
    {
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                onDispose();
            }

            base.Dispose(disposing);
        }
    }
}

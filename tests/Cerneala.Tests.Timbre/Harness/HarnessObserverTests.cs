using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Harness;

// Verifies the deterministic observers on their own, without any engine, so
// later engine tests can trust what the sink and readers report.
public sealed class HarnessObserverTests
{
    [Fact]
    public void OutputTracksSubmittedQueuedAndConsumedFramesAndNotifiesClient()
    {
        DeterministicSoundOutput output = new();
        RecordingClient client = new();
        output.Open(client);

        output.Submit(new float[480 * 2]);
        output.Submit(new float[480 * 2]);
        Assert.Equal(960, output.SubmittedFrames);
        Assert.Equal(960, output.QueuedFrames);
        Assert.Equal(960, output.MaxQueuedFrames);

        output.Consume(300);
        Assert.Equal(300, output.ConsumedFrames);
        Assert.Equal(660, output.QueuedFrames);
        Assert.Equal(1, client.CapacityNotifications);

        output.ConsumeAll();
        Assert.Equal(0, output.QueuedFrames);
        Assert.Equal(960, output.ConsumedFrames);
        Assert.Equal(2, client.CapacityNotifications);
        Assert.Equal(2, output.SubmitCount);
    }

    [Fact]
    public void HeldOutputReportsFullQueueUntilReleased()
    {
        DeterministicSoundOutput output = new();
        RecordingClient client = new();
        output.Open(client);
        output.Hold();
        Assert.Equal(DeterministicSoundOutput.HeldQueueFrames, output.QueuedFrames);
        output.Release();
        Assert.Equal(0, output.QueuedFrames);
        Assert.Equal(1, client.CapacityNotifications);
    }

    [Fact]
    public void OutputRecordsPcmInSubmissionOrder()
    {
        DeterministicSoundOutput output = new();
        output.Open(new RecordingClient());
        output.Submit([1f, -1f, 2f, -2f]);
        output.Submit([3f, -3f]);

        Assert.Equal([2f, -2f, 3f, -3f], output.Read(1, 2));
        Assert.Equal([3f, -3f, 0f, 0f], output.Read(2, 2));
        Assert.Equal(6, output.ReadAll().Length);
    }

    [Fact]
    public void OutputRejectsIncompleteFramesAndSubmissionWhileClosed()
    {
        DeterministicSoundOutput output = new();
        Assert.Throws<InvalidOperationException>(() => output.Submit(new float[2]));
        output.Open(new RecordingClient());
        Assert.Throws<ArgumentException>(() => output.Submit(new float[3]));
        output.Close();
        Assert.False(output.IsOpen);
        Assert.Equal(1, output.CloseCount);
    }

    [Fact]
    public void OutputOpenFailureIsInjectedAndCounted()
    {
        DeterministicSoundOutput output = new() { OpenFailure = new InvalidOperationException("no device") };
        Assert.Throws<InvalidOperationException>(() => output.Open(new RecordingClient()));
        Assert.Equal(1, output.OpenCount);
        Assert.False(output.IsOpen);
    }

    [Fact]
    public void OutputDeviceLossIsForwardedToClient()
    {
        DeterministicSoundOutput output = new();
        RecordingClient client = new();
        output.Open(client);
        IOException failure = new("lost");
        output.LoseDevice(failure);
        Assert.Same(failure, client.LostError);
    }

    [Fact]
    public async Task OutputSubmittedFrameWaitCompletesOnlyAfterThreshold()
    {
        DeterministicSoundOutput output = new();
        output.Open(new RecordingClient());
        Task wait = output.WaitForSubmittedFramesAsync(960);
        output.Submit(new float[480 * 2]);
        Assert.False(wait.IsCompleted);
        output.Submit(new float[480 * 2]);
        await wait;
    }

    [Fact]
    public async Task ReaderProducesIdenticalPcmForSingleAndIrregularPartitions()
    {
        float[] whole = await ReadAllAsync(new DeterministicSoundReader(1000, DeterministicSoundReader.DefaultSignal), 4096);
        float[] partitioned = await ReadAllAsync(new DeterministicSoundReader(1000, DeterministicSoundReader.DefaultSignal, maxFramesPerRead: 37), 113);

        Assert.Equal(2000, whole.Length);
        Assert.Equal(whole, partitioned);
        Assert.Equal(DeterministicSoundReader.DefaultSignal(999, 1), whole[1999]);
    }

    [Fact]
    public async Task ReaderCountsReadsSeeksAndReportsEndOfSource()
    {
        DeterministicSoundReader reader = new(100, DeterministicSoundReader.DefaultSignal, maxFramesPerRead: 60);
        float[] buffer = new float[200];

        SoundReadResult first = await reader.ReadAsync(buffer, CancellationToken.None);
        Assert.Equal(new SoundReadResult(60, false), first);
        SoundReadResult second = await reader.ReadAsync(buffer, CancellationToken.None);
        Assert.Equal(new SoundReadResult(40, true), second);
        Assert.Equal(100, reader.FramesRead);

        await reader.SeekAsync(25, CancellationToken.None);
        Assert.Equal(25, reader.Position);
        SoundReadResult afterSeek = await reader.ReadAsync(buffer.AsMemory(0, 2), CancellationToken.None);
        Assert.Equal(new SoundReadResult(1, false), afterSeek);
        Assert.Equal(DeterministicSoundReader.DefaultSignal(25, 0), buffer[0]);
        Assert.Equal(3, reader.ReadCount);
        Assert.Equal([25L], reader.SeekTargets);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await reader.SeekAsync(101, CancellationToken.None));
    }

    [Fact]
    public async Task ReaderGateHoldsReadUntilReleasedAndHonorsCancellation()
    {
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        DeterministicSoundReader reader = new(10, DeterministicSoundReader.DefaultSignal)
        {
            ReadGate = token => release.Task.WaitAsync(token)
        };

        ValueTask<SoundReadResult> pending = reader.ReadAsync(new float[20], CancellationToken.None);
        Assert.False(pending.IsCompleted);
        release.SetResult();
        Assert.Equal(new SoundReadResult(10, true), await pending);

        TaskCompletionSource never = new();
        reader.ReadGate = token => never.Task.WaitAsync(token);
        using CancellationTokenSource cancel = new();
        ValueTask<SoundReadResult> canceled = reader.ReadAsync(new float[20], cancel.Token);
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await canceled);
    }

    [Fact]
    public async Task ReaderInjectsFailureAtConfiguredFrame()
    {
        DeterministicSoundReader reader = new(100, DeterministicSoundReader.DefaultSignal) { FailAtFrame = 50 };
        await Assert.ThrowsAsync<IOException>(async () => await reader.ReadAsync(new float[200], CancellationToken.None));
    }

    [Fact]
    public void SourceFactoryCreatesIndependentReadersAndTracksLiveCount()
    {
        DeterministicSoundSourceFactory factory = new(10);
        SoundReader first = factory.Open();
        SoundReader second = factory.Open();
        Assert.NotSame(first, second);
        Assert.Equal(2, factory.OpenCount);
        Assert.Equal(2, factory.LiveReaders);

        first.Dispose();
        first.Dispose();
        Assert.Equal(1, factory.LiveReaders);
        second.Dispose();
        Assert.Equal(0, factory.LiveReaders);
    }

    private static async Task<float[]> ReadAllAsync(SoundReader reader, int framesPerCall)
    {
        List<float> result = [];
        float[] buffer = new float[framesPerCall * 2];
        while (true)
        {
            SoundReadResult read = await reader.ReadAsync(buffer, CancellationToken.None);
            result.AddRange(buffer.AsSpan(0, read.Frames * 2).ToArray());
            if (read.EndOfSource)
            {
                return result.ToArray();
            }
        }
    }

    private sealed class RecordingClient : ISoundOutputClient
    {
        public int CapacityNotifications { get; private set; }

        public Exception? LostError { get; private set; }

        public void NotifyCapacityAvailable() => CapacityNotifications++;

        public void NotifyDeviceLost(Exception? error) => LostError = error;
    }
}

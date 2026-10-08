using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Engine;

// A streaming voice starts, and restarts after a seek, only with a whole software
// queue buffered (or the rest of the source). Decoders publish packet-sized reads,
// so mixing from a partial read would pad silence into the start of the PCM.
public sealed class StreamingPrimingTests
{
    private const int Packet = 128;

    [Fact]
    public async Task AStreamDoesNotStartFromAPacketSmallerThanABlock()
    {
        using TimbreRig rig = new(hold: false);
        TrickleReader reader = new(48000);
        reader.StartTrickle();
        TimbrePlayback playback = rig.Scope.Play(Clip(reader));

        await HarnessWait.WithTimeout(reader.WaitingAtGate, null, "The reader did not reach its gate.");
        await rig.SyncAsync();

        Assert.Equal(0, rig.Runtime.GetDiagnostics().UnderrunFrames);
        Assert.Equal(0, rig.Output.SubmittedFrames);

        reader.Trickle = false;
        reader.Open();
        await rig.Output.WaitForSubmittedFramesAsync(TimbreRig.Block);

        TimbreRig.AssertPcm(TimbreRig.Expected(TimbreRig.Block, Ramp), rig.Output.Read(0, TimbreRig.Block), tolerance: 0);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().UnderrunFrames);
        playback.Cancel();
    }

    [Fact]
    public async Task AStreamDoesNotRestartAfterASeekFromAPacketSmallerThanABlock()
    {
        using TimbreRig rig = new(hold: false);
        TrickleReader reader = new(480000);
        TimbrePlayback playback = rig.Scope.Play(Clip(reader));
        await rig.Output.WaitForSubmittedFramesAsync(TimbreRig.Budget);
        await HarnessWait.WithTimeout(playback.WhenSettledAsync(), null, "The stream did not settle.");
        await rig.SyncAsync();
        long beforeSeek = rig.Output.SubmittedFrames;

        reader.StartTrickle();
        Task seek = playback.SeekAsync(TimbreRig.FramesToTime(96000));
        await HarnessWait.WithTimeout(reader.WaitingAtGate, null, "The reader did not reach its gate after the seek.");
        await HarnessWait.WithTimeout(seek, null, "The seek did not complete.");
        rig.Output.ConsumeAll();
        await rig.SyncAsync();

        Assert.Equal(0, rig.Runtime.GetDiagnostics().UnderrunFrames);
        Assert.Equal(beforeSeek, rig.Output.SubmittedFrames);

        reader.Trickle = false;
        reader.Open();
        await rig.Output.WaitForSubmittedFramesAsync(beforeSeek + TimbreRig.Block);

        TimbreRig.AssertPcm(
            TimbreRig.Expected(TimbreRig.Block, Ramp, sourceStart: 96000),
            rig.Output.Read(beforeSeek, TimbreRig.Block),
            tolerance: 0);
        Assert.Equal(0, rig.Runtime.GetDiagnostics().UnderrunFrames);
        playback.Cancel();
    }

    private static float Ramp(long frame, int channel) => (channel == 0 ? 1 : -1) * (frame % 65536) / 65536f;

    private static TimbreSound Clip(TrickleReader reader) =>
        new(TimbreSource.FromReader(() => reader, "trickle"), loading: TimbreLoading.Streaming);

    // Serves one packet per read. While trickling, every read after the first
    // packet (of the start or of a seek) waits at the gate.
    private sealed class TrickleReader(long length) : TimbreReader
    {
        private readonly TaskCompletionSource open = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private volatile TaskCompletionSource waiting = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private long position;
        private bool servedPacket;

        public volatile bool Trickle;

        public Task WaitingAtGate => waiting.Task;

        public override long? LengthFrames => length;

        public void StartTrickle()
        {
            waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Trickle = true;
        }

        public void Open() => open.TrySetResult();

        public override async ValueTask<TimbreReadResult> ReadAsync(Memory<float> destination, CancellationToken cancellationToken)
        {
            if (Trickle && servedPacket)
            {
                waiting.TrySetResult();
                await open.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            servedPacket = true;
            return Fill(destination.Span);
        }

        public override ValueTask SeekAsync(long frame, CancellationToken cancellationToken)
        {
            position = frame;
            servedPacket = false;
            return ValueTask.CompletedTask;
        }

        private TimbreReadResult Fill(Span<float> destination)
        {
            int count = (int)Math.Min(Math.Min(Packet, destination.Length / 2), length - position);
            for (int index = 0; index < count; index++)
            {
                destination[index * 2] = Ramp(position + index, 0);
                destination[(index * 2) + 1] = Ramp(position + index, 1);
            }

            position += count;
            return new TimbreReadResult(count, position == length);
        }
    }
}

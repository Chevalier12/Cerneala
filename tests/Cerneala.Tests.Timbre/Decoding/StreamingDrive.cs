using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Decoding;

internal static class StreamingDrive
{
    // Blocks every further read of `stream`, then consumes output until the
    // source pump actually needs new bytes and waits at the barrier. Each
    // step waits for an engine signal (pump idle or reader blocked); the
    // decoder may first serve frames from data it has already read.
    public static async Task UntilBlockedAsync(TimbreRig rig, TimbrePlayback playback, ObservedFileStream stream, long from = 0)
    {
        stream.BlockAt(from);
        for (int step = 0; step < 400 && !stream.WhenBlocked.IsCompleted; step++)
        {
            rig.Output.ConsumeAll();
            await rig.SyncAsync();
            await HarnessWait.WithTimeout(Task.WhenAny(stream.WhenBlocked, playback.WhenSettledAsync()), null, "The source pump neither settled nor blocked.");
        }

        await HarnessWait.WithTimeout(stream.WhenBlocked, null, "The reader did not reach the barrier.");
    }

    // Consumes one block once the source pump has filled the ring, so a test
    // that consumes faster than real time never starves a decoding stream.
    public static async Task<float[]> NextBlockAsync(TimbreRig rig, TimbrePlayback playback)
    {
        await rig.SettledAsync(playback);
        return await rig.NextBlockAsync();
    }
}

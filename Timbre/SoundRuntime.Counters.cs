namespace Cerneala.Timbre;

public sealed partial class SoundRuntime
{
    // Guarded by Sync.
    private long playbacksStarted;
    private long playbacksCompleted;
    private long playbacksCanceled;
    private long playbacksFailed;
    private long pausesApplied;
    private long resumesApplied;
    private long seeksRequested;
    private long seeksCompleted;
    private long seeksSuperseded;
    private long parameterPublications;

    // Written by the mixer or workers; read with Interlocked/Volatile.
    private long loopWraps;
    private long blocksMixed;
    private long framesSubmitted;
    private long framesConsumed;
    private long underrunFrames;
    private long clippedSamples;
    private long dspStateBytes;
    private int liveReaders;
    private int liveSourcePumps;
    private int pendingLoads;
    private int outputOpenCount;
    private volatile bool outputOpen;

    internal void CountParameterPublication() => parameterPublications++;

    internal void CountPause() => pausesApplied++;

    internal void CountResume() => resumesApplied++;

    internal void CountSeekRequested() => seeksRequested++;

    internal void CountSeekCompleted() => seeksCompleted++;

    internal void CountSeekSuperseded() => seeksSuperseded++;
}

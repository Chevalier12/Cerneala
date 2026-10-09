using Cerneala.Timbre.Dsp;

namespace Cerneala.Timbre.Engine;

// Mixer-side state of one playback: the control snapshot used for rendering,
// the DSP chain and tail progress, transport bookkeeping, and the frame index
// that must drain for completion.
internal sealed class TimbreVoice(float volume, float[] values, TimbreDspChain? chain)
{
    private const double DeClickSeconds = 0.005;
    internal static readonly int DeClickFrames = (int)Math.Round(DeClickSeconds * TimbreRuntime.SampleRate);

    internal float Volume { get; set; } = volume;

    private float gain = volume;
    private float gainStart = volume;
    private float gainTarget = volume;
    internal int GainFramesRemaining { get; private set; }
    internal bool Silent => gain == 0f && GainFramesRemaining == 0;
    internal bool HasRendered { get; set; }
    internal bool SuspendAfterFade { get; set; }

    // Mixer-owned pending transport, distinct from a seek already sent to the feed.
    internal int RequestedSeekGeneration { get; set; }
    internal long SeekTarget { get; set; }
    internal bool SeekRequested { get; set; }

    internal void SetGainTarget(float target)
    {
        if (gainTarget == target)
        {
            return;
        }

        gainStart = gain;
        gainTarget = target;
        GainFramesRemaining = gain == target ? 0 : DeClickFrames;
    }

    // Before publication for replacement, or on the mixer for a pending pause.
    internal void Silence()
    {
        gain = gainStart = gainTarget = 0f;
        GainFramesRemaining = 0;
    }

    internal float NextGain()
    {
        if (GainFramesRemaining > 0)
        {
            GainFramesRemaining--;
            gain = GainFramesRemaining == 0 ? gainTarget :
                gainStart + (gainTarget - gainStart) * (DeClickFrames - GainFramesRemaining) / DeClickFrames;
        }

        return gain;
    }

    internal float[] Values { get; } = values;

    internal TimbreDspChain? Chain { get; } = chain;

    internal int ControlVersion { get; set; }

    internal int AppliedSeekGeneration { get; set; }

    internal bool SeekPending { get; set; }

    internal bool ProductionEnded { get; set; }

    // Tail after the end of a non-looping source, measured on the pre-volume
    // chain output.
    internal bool InTail { get; set; }

    internal long TailFrames { get; set; }

    internal int SilentRun { get; set; }

    internal bool TailTruncated { get; set; }

    // Output frame index just after the last block containing this voice.
    internal long LastContributedEnd { get; set; }

    internal long EndFrame { get; set; }

    internal void ResetDsp()
    {
        Chain?.Reset();
        InTail = false;
        TailFrames = 0;
        SilentRun = 0;
        TailTruncated = false;
    }
}

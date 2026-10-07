using Cerneala.Timbre.Dsp;

namespace Cerneala.Timbre.Engine;

// Mixer-side state of one playback: the control snapshot used for rendering,
// the DSP chain and tail progress, transport bookkeeping, and the frame index
// that must drain for completion.
internal sealed class SoundVoice(float volume, float[] values, SoundDspChain? chain)
{
    internal float Volume { get; set; } = volume;

    internal float[] Values { get; } = values;

    internal SoundDspChain? Chain { get; } = chain;

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

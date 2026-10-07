using Cerneala.Timbre.Catalog;

namespace Cerneala.Timbre.Dsp;

// Per-playback processing state for a clip's modifiers, in declaration order.
// Input values come from constants or from the playback's parameter values
// published at each block; the clip itself never holds runtime buffers.
internal sealed class SoundDspChain
{
    private readonly Stage[] stages;

    private SoundDspChain(Stage[] stages)
    {
        this.stages = stages;
        StageKinds = Array.AsReadOnly(stages.Select(stage => stage.Kernel.GetType()).ToArray());
        StateBytes = stages.Sum(stage => stage.StateBytes);
    }

    internal IReadOnlyList<Type> StageKinds { get; }

    internal long StateBytes { get; }

    // Frames of continuous silence that end a tail: one echo period of the
    // longest current delay, and at least one processing block.
    internal int TailWindowFrames
    {
        get
        {
            int window = TimbreCatalog.BlockFrames;
            foreach (Stage stage in stages)
            {
                if (stage.Kernel is DelayKernel delay)
                {
                    window = Math.Max(window, delay.DelayFrames);
                }
            }

            return window;
        }
    }

    internal static SoundDspChain? Create(SoundClip clip)
    {
        if (clip.Modifiers.Count == 0)
        {
            return null;
        }

        Stage[] stages = new Stage[clip.Modifiers.Count];
        for (int index = 0; index < stages.Length; index++)
        {
            stages[index] = clip.Modifiers[index] switch
            {
                LowPass lowPass => new LowPassStage(clip, lowPass),
                Delay delay => new DelayStage(clip, delay),
                SoundModifier other => throw new NotSupportedException($"Unknown sound modifier '{other.GetType().Name}'.")
            };
        }

        return new SoundDspChain(stages);
    }

    internal void Process(Span<float> interleaved, int frames, float[] values)
    {
        foreach (Stage stage in stages)
        {
            stage.Process(interleaved, frames, values);
        }
    }

    internal void Reset()
    {
        foreach (Stage stage in stages)
        {
            stage.Reset();
        }
    }

    private abstract class Stage
    {
        internal abstract object Kernel { get; }

        internal virtual long StateBytes => 0;

        internal abstract void Process(Span<float> interleaved, int frames, float[] values);

        internal abstract void Reset();

        // Constant inputs resolve to -1 and use their own value.
        private protected static int ParameterIndex(SoundClip clip, SoundInput<float> input) =>
            input.Parameter is { } parameter ? clip.GetParameterIndex(parameter, nameof(input)) : -1;

        private protected static float Value(int index, float constant, float[] values) => index < 0 ? constant : values[index];
    }

    private sealed class LowPassStage : Stage
    {
        private readonly LowPassKernel kernel = new();
        private readonly int cutoffIndex;
        private readonly float cutoff;

        internal LowPassStage(SoundClip clip, LowPass definition)
        {
            cutoffIndex = ParameterIndex(clip, definition.Cutoff);
            cutoff = definition.Cutoff.Value;
        }

        internal override object Kernel => kernel;

        internal override void Process(Span<float> interleaved, int frames, float[] values)
        {
            kernel.SetCutoff(Value(cutoffIndex, cutoff, values));
            kernel.Process(interleaved, frames);
        }

        internal override void Reset() => kernel.Reset();
    }

    private sealed class DelayStage : Stage
    {
        private readonly DelayKernel kernel;
        private readonly int timeIndex;
        private readonly int feedbackIndex;
        private readonly int mixIndex;
        private readonly float time;
        private readonly float feedback;
        private readonly float mix;

        internal DelayStage(SoundClip clip, Delay definition)
        {
            timeIndex = ParameterIndex(clip, definition.Time);
            feedbackIndex = ParameterIndex(clip, definition.Feedback);
            mixIndex = ParameterIndex(clip, definition.Mix);
            time = definition.Time.Value;
            feedback = definition.Feedback.Value;
            mix = definition.Mix.Value;

            // A constant time needs exactly its delay; an animatable time needs
            // the catalog maximum.
            int capacity = timeIndex < 0
                ? DelayKernel.ToDelayFrames(time)
                : DelayKernel.ToDelayFrames(TimbreCatalog.DelayTime.Maximum);
            kernel = new DelayKernel(capacity);
            kernel.Set(time, feedback, mix);
        }

        internal override object Kernel => kernel;

        internal override long StateBytes => kernel.StateBytes;

        internal override void Process(Span<float> interleaved, int frames, float[] values)
        {
            kernel.Set(Value(timeIndex, time, values), Value(feedbackIndex, feedback, values), Value(mixIndex, mix, values));
            kernel.Process(interleaved, frames);
        }

        internal override void Reset() => kernel.Reset();
    }
}

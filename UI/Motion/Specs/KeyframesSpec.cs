using Cerneala.UI.Motion.Interpolation;

namespace Cerneala.UI.Motion.Specs;

public readonly record struct MotionKeyframe<T>(float Offset, T Value, IEasing? Easing = null, bool Hold = false);

public sealed class KeyframesSpec<T> : MotionSpec<T>
{
    public KeyframesSpec(IReadOnlyList<MotionKeyframe<T>> frames, TimeSpan? duration = null)
    {
        ArgumentNullException.ThrowIfNull(frames);
        Validate(frames);
        Frames = [.. frames];
        Duration = duration ?? TimeSpan.FromSeconds(1);
        if (Duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Keyframes duration must be positive.");
        }
    }

    public IReadOnlyList<MotionKeyframe<T>> Frames { get; }

    public TimeSpan Duration { get; }

    public KeyframesSpec<T> WithDuration(TimeSpan duration)
    {
        return new KeyframesSpec<T>(Frames, duration);
    }

    public override MotionSampler<T> CreateSampler(T from, T to, ValueMixer<T> mixer, MotionSpecContext context)
    {
        ArgumentNullException.ThrowIfNull(mixer);
        return new KeyframesSampler(this, mixer);
    }

    private static void Validate(IReadOnlyList<MotionKeyframe<T>> frames)
    {
        if (frames.Count < 2)
        {
            throw new ArgumentException("At least two keyframes are required.", nameof(frames));
        }

        if (frames[0].Offset != 0 || frames[^1].Offset != 1)
        {
            throw new ArgumentException("Keyframes must start at offset 0 and end at offset 1.", nameof(frames));
        }

        float previous = -1;
        for (int i = 0; i < frames.Count; i++)
        {
            float offset = frames[i].Offset;
            if (float.IsNaN(offset) || offset < 0 || offset > 1)
            {
                throw new ArgumentException("Keyframe offsets must be in [0, 1].", nameof(frames));
            }

            if (offset < previous)
            {
                throw new ArgumentException("Keyframe offsets must be sorted.", nameof(frames));
            }

            previous = offset;
        }
    }

    private sealed class KeyframesSampler : MotionSampler<T>
    {
        private readonly KeyframesSpec<T> spec;
        private readonly ValueMixer<T> mixer;
        private TimeSpan elapsed;
        private T current;
        private bool isComplete;

        public KeyframesSampler(KeyframesSpec<T> spec, ValueMixer<T> mixer)
        {
            this.spec = spec;
            this.mixer = mixer;
            current = spec.Frames[0].Value;
        }

        public override T Current => current;

        public override bool IsComplete => isComplete;

        public override void Advance(TimeSpan delta)
        {
            if (delta < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(delta), "Delta cannot be negative.");
            }

            if (isComplete)
            {
                return;
            }

            elapsed += delta;
            float progress = Math.Clamp((float)(elapsed.TotalSeconds / spec.Duration.TotalSeconds), 0, 1);
            current = Sample(progress);
            if (progress >= 1)
            {
                isComplete = true;
            }
        }

        public override void Retarget(T to, RetargetMode mode)
        {
            MotionKeyframe<T> last = spec.Frames[^1] with { Value = to };
            current = last.Value;
            isComplete = true;
        }

        private T Sample(float progress)
        {
            if (progress <= 0)
            {
                return spec.Frames[0].Value;
            }

            if (progress >= 1)
            {
                return spec.Frames[^1].Value;
            }

            for (int i = 0; i < spec.Frames.Count - 1; i++)
            {
                MotionKeyframe<T> start = spec.Frames[i];
                MotionKeyframe<T> end = spec.Frames[i + 1];
                if (progress < start.Offset || progress > end.Offset)
                {
                    continue;
                }

                if (start.Hold || end.Offset == start.Offset)
                {
                    return start.Value;
                }

                float segmentProgress = (progress - start.Offset) / (end.Offset - start.Offset);
                segmentProgress = (start.Easing ?? Easings.Linear).Transform(segmentProgress);
                return mixer.Mix(start.Value, end.Value, segmentProgress);
            }

            return spec.Frames[^1].Value;
        }
    }
}

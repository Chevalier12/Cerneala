using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;

namespace Cerneala.UI.Motion.Specs;

public sealed class RepeatSpec<T> : MotionSpec<T>
{
    private readonly TweenSpec<T> inner;
    private readonly int? repeatCount;

    public RepeatSpec(TweenSpec<T> inner, int? repeatCount = null)
    {
        this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        if (repeatCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(repeatCount), "Repeat count must be positive.");
        }

        this.repeatCount = repeatCount;
    }

    public override MotionSampler<T> CreateSampler(T from, T to, ValueMixer<T> mixer, MotionSpecContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (repeatCount is null && context.ReducedMotion.Mode != ReducedMotionMode.NoPreference)
        {
            context.Diagnostics?.RecordReducedMotionSkip(context.DebugName);
            return new StaticSampler(to);
        }

        return new Sampler(inner, repeatCount, from, to, mixer);
    }

    private sealed class StaticSampler(T current) : MotionSampler<T>
    {
        public override T Current { get; } = current;

        public override bool IsComplete => true;

        public override void Advance(TimeSpan delta)
        {
        }

        public override void Retarget(T to, RetargetMode mode)
        {
        }
    }

    private sealed class Sampler : MotionSampler<T>
    {
        private readonly TweenSpec<T> spec;
        private readonly int? repeatCount;
        private readonly T from;
        private readonly T to;
        private readonly ValueMixer<T> mixer;
        private TimeSpan elapsed;
        private T current;
        private bool isComplete;

        public Sampler(TweenSpec<T> spec, int? repeatCount, T from, T to, ValueMixer<T> mixer)
        {
            this.spec = spec;
            this.repeatCount = repeatCount;
            this.from = from;
            this.to = to;
            this.mixer = mixer;
            current = from;
        }

        public override T Current => current;

        public override bool IsComplete => isComplete;

        public override void Advance(TimeSpan delta)
        {
            elapsed += delta;
            TimeSpan duration = spec.Duration;
            if (repeatCount is int count && elapsed >= duration * count)
            {
                current = to;
                isComplete = true;
                return;
            }

            double cycleMs = elapsed.TotalMilliseconds % duration.TotalMilliseconds;
            float progress = (float)(cycleMs / duration.TotalMilliseconds);
            current = mixer.Mix(from, to, spec.Easing.Transform(progress));
        }

        public override void Retarget(T to, RetargetMode mode)
        {
        }
    }
}

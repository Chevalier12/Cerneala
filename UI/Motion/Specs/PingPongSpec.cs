using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;

namespace Cerneala.UI.Motion.Specs;

public sealed class PingPongSpec<T> : MotionSpec<T>
{
    private readonly TweenSpec<T> inner;
    private readonly int? cycles;

    public PingPongSpec(TweenSpec<T> inner, int? cycles)
    {
        this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        if (cycles is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cycles), "Cycle count must be positive.");
        }

        this.cycles = cycles;
    }

    public override MotionSampler<T> CreateSampler(T from, T to, ValueMixer<T> mixer, MotionSpecContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (cycles is null && context.ReducedMotion.Mode != ReducedMotionMode.NoPreference)
        {
            context.Diagnostics?.RecordReducedMotionSkip(context.DebugName);
            return new StaticSampler(to);
        }

        return new Sampler(inner, cycles, from, to, mixer);
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
        private readonly int? cycles;
        private readonly T from;
        private readonly T to;
        private readonly ValueMixer<T> mixer;
        private TimeSpan elapsed;
        private T current;
        private bool isComplete;

        public Sampler(TweenSpec<T> spec, int? cycles, T from, T to, ValueMixer<T> mixer)
        {
            this.spec = spec;
            this.cycles = cycles;
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
            if (cycles is int count && elapsed >= duration * count)
            {
                current = count % 2 == 0 ? from : to;
                isComplete = true;
                return;
            }

            long cycle = (long)(elapsed.TotalMilliseconds / duration.TotalMilliseconds);
            double cycleMs = elapsed.TotalMilliseconds % duration.TotalMilliseconds;
            float progress = (float)(cycleMs / duration.TotalMilliseconds);
            if (cycle % 2 == 1)
            {
                progress = 1 - progress;
            }

            current = mixer.Mix(from, to, spec.Easing.Transform(progress));
        }

        public override void Retarget(T to, RetargetMode mode)
        {
        }
    }
}

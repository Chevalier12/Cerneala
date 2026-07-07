using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;

namespace Cerneala.UI.Motion.Specs;

public sealed class TweenSpec<T> : MotionSpec<T>
{
    public TweenSpec(
        TimeSpan duration,
        IEasing? easing = null,
        TimeSpan delay = default,
        FillMode fillMode = FillMode.Both)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Tween duration must be positive.");
        }

        if (delay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(delay), "Tween delay cannot be negative.");
        }

        Duration = duration;
        Delay = delay;
        Easing = easing ?? Easings.Standard;
        FillMode = fillMode;
    }

    public TimeSpan Duration { get; }

    public TimeSpan Delay { get; }

    public IEasing Easing { get; }

    public FillMode FillMode { get; }

    public TweenSpec<T> WithDelay(TimeSpan delay)
    {
        return new TweenSpec<T>(Duration, Easing, delay, FillMode);
    }

    public TweenSpec<T> WithFillMode(FillMode fillMode)
    {
        return new TweenSpec<T>(Duration, Easing, Delay, fillMode);
    }

    public override MotionSampler<T> CreateSampler(T from, T to, ValueMixer<T> mixer, MotionSpecContext context)
    {
        ArgumentNullException.ThrowIfNull(mixer);
        ArgumentNullException.ThrowIfNull(context);

        TimeSpan effectiveDuration = context.ReducedMotion.Mode == ReducedMotionMode.Reduce
            ? TimeSpan.Zero
            : Duration;
        return new TweenSampler(this, from, to, mixer, effectiveDuration);
    }

    private sealed class TweenSampler : MotionSampler<T>
    {
        private readonly TweenSpec<T> spec;
        private readonly ValueMixer<T> mixer;
        private TimeSpan effectiveDuration;
        private TimeSpan elapsed;
        private T from;
        private T to;
        private T current;
        private bool isComplete;

        public TweenSampler(TweenSpec<T> spec, T from, T to, ValueMixer<T> mixer, TimeSpan effectiveDuration)
        {
            this.spec = spec;
            this.from = from;
            this.to = to;
            this.mixer = mixer;
            this.effectiveDuration = effectiveDuration;
            current = from;

            if (effectiveDuration == TimeSpan.Zero && spec.Delay == TimeSpan.Zero)
            {
                current = to;
                isComplete = true;
            }
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
            Sample();
        }

        public override void Retarget(T to, RetargetMode mode)
        {
            if (mode == RetargetMode.Restart)
            {
                from = current;
                elapsed = TimeSpan.Zero;
            }

            this.to = to;
            isComplete = false;
            Sample();
        }

        private void Sample()
        {
            if (elapsed < spec.Delay)
            {
                current = spec.FillMode is FillMode.Backwards or FillMode.Both ? from : current;
                return;
            }

            if (effectiveDuration == TimeSpan.Zero)
            {
                current = to;
                isComplete = true;
                return;
            }

            TimeSpan activeElapsed = elapsed - spec.Delay;
            float rawProgress = (float)(activeElapsed.TotalSeconds / effectiveDuration.TotalSeconds);
            if (rawProgress >= 1)
            {
                current = to;
                isComplete = true;
                return;
            }

            float easedProgress = spec.Easing.Transform(rawProgress);
            current = mixer.Mix(from, to, easedProgress);
        }
    }
}

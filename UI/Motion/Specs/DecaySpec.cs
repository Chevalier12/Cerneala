using Cerneala.UI.Motion.Interpolation;

namespace Cerneala.UI.Motion.Specs;

public sealed class DecaySpec<T> : MotionSpec<T>
{
    public DecaySpec(
        MotionVelocity<T> initialVelocity,
        float deceleration = 0.998f,
        T? min = default,
        T? max = default,
        MotionSpec<T>? bounce = null)
    {
        if (deceleration <= 0 || deceleration >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(deceleration), "Deceleration must be greater than 0 and less than 1.");
        }

        InitialVelocity = initialVelocity;
        Deceleration = deceleration;
        Min = min;
        Max = max;
        Bounce = bounce;
    }

    public MotionVelocity<T> InitialVelocity { get; }

    public float Deceleration { get; }

    public T? Min { get; }

    public T? Max { get; }

    public MotionSpec<T>? Bounce { get; }

    public DecaySpec<T> WithBounds(T? min, T? max)
    {
        return new DecaySpec<T>(InitialVelocity, Deceleration, min, max, Bounce);
    }

    public override MotionSampler<T> CreateSampler(T from, T to, ValueMixer<T> mixer, MotionSpecContext context)
    {
        ArgumentNullException.ThrowIfNull(mixer);
        if (!mixer.SupportsVectorOperations)
        {
            throw new InvalidOperationException($"Decay for {typeof(T).Name} requires a vector-capable mixer.");
        }

        return new DecaySampler(this, from, mixer);
    }

    private sealed class DecaySampler : MotionSampler<T>
    {
        private readonly DecaySpec<T> spec;
        private readonly ValueMixer<T> mixer;
        private T current;
        private T velocity;
        private bool isComplete;

        public DecaySampler(DecaySpec<T> spec, T from, ValueMixer<T> mixer)
        {
            this.spec = spec;
            this.mixer = mixer;
            current = from;
            velocity = spec.InitialVelocity.Value;
        }

        public override T Current => current;

        public override bool IsComplete => isComplete;

        public override MotionVelocity<T>? Velocity => new(velocity);

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

            current = mixer.Add(current, mixer.Scale(velocity, (float)delta.TotalSeconds));
            if (TryClamp(current, spec.Min, spec.Max, out T clamped))
            {
                current = clamped;
                velocity = mixer.Scale(velocity, 0);
                isComplete = true;
                return;
            }

            float decay = MathF.Pow(spec.Deceleration, (float)(delta.TotalMilliseconds / 16.6666667));
            velocity = mixer.Scale(velocity, decay);
            if (mixer.Magnitude(velocity) <= 0.01f)
            {
                isComplete = true;
            }
        }

        public override void Retarget(T to, RetargetMode mode)
        {
            current = to;
            isComplete = true;
        }

        private static bool TryClamp(T value, T? min, T? max, out T clamped)
        {
            clamped = value;
            if (min is not null && Comparer<T>.Default.Compare(value, min) < 0)
            {
                clamped = min;
                return true;
            }

            if (max is not null && Comparer<T>.Default.Compare(value, max) > 0)
            {
                clamped = max;
                return true;
            }

            return false;
        }
    }
}

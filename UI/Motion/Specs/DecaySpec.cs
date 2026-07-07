using Cerneala.UI.Motion.Interpolation;

namespace Cerneala.UI.Motion.Specs;

public sealed class DecaySpec<T> : MotionSpec<T>
{
    public DecaySpec(
        MotionVelocity<T> initialVelocity,
        float deceleration = 0.998f)
        : this(initialVelocity, deceleration, default, default, hasMin: false, hasMax: false)
    {
    }

    private DecaySpec(
        MotionVelocity<T> initialVelocity,
        float deceleration,
        T? min,
        T? max,
        bool hasMin,
        bool hasMax)
    {
        if (!float.IsFinite(deceleration) || deceleration <= 0 || deceleration >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(deceleration), "Deceleration must be greater than 0 and less than 1.");
        }

        InitialVelocity = initialVelocity;
        Deceleration = deceleration;
        Min = min;
        Max = max;
        HasMin = hasMin;
        HasMax = hasMax;
    }

    public MotionVelocity<T> InitialVelocity { get; }

    public float Deceleration { get; }

    public T? Min { get; }

    public T? Max { get; }

    public bool HasMin { get; }

    public bool HasMax { get; }

    public DecaySpec<T> WithBounds(T min, T max)
    {
        return new DecaySpec<T>(InitialVelocity, Deceleration, min, max, hasMin: true, hasMax: true);
    }

    public override MotionSampler<T> CreateSampler(T from, T to, ValueMixer<T> mixer, MotionSpecContext context)
    {
        ArgumentNullException.ThrowIfNull(mixer);
        ArgumentNullException.ThrowIfNull(context);
        if (!mixer.SupportsVectorOperations)
        {
            throw new InvalidOperationException($"Decay for {typeof(T).Name} requires a vector-capable mixer.");
        }

        if ((HasMin || HasMax) && !SupportsComparableBounds())
        {
            throw new InvalidOperationException($"Decay bounds for {typeof(T).Name} require values that implement IComparable or IComparable<{typeof(T).Name}>.");
        }

        return new DecaySampler(this, from, mixer);
    }

    private static bool SupportsComparableBounds()
    {
        Type type = typeof(T);
        return typeof(IComparable<T>).IsAssignableFrom(type) || typeof(IComparable).IsAssignableFrom(type);
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
            if (TryClamp(current, spec, out T clamped))
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

        private static bool TryClamp(T value, DecaySpec<T> spec, out T clamped)
        {
            clamped = value;
            if (spec.HasMin && Compare(value, spec.Min!) < 0)
            {
                clamped = spec.Min!;
                return true;
            }

            if (spec.HasMax && Compare(value, spec.Max!) > 0)
            {
                clamped = spec.Max!;
                return true;
            }

            return false;
        }

        private static int Compare(T left, T right)
        {
            if (left is IComparable<T> genericComparable)
            {
                return genericComparable.CompareTo(right);
            }

            if (left is IComparable comparable)
            {
                return comparable.CompareTo(right);
            }

            throw new InvalidOperationException($"Decay bounds for {typeof(T).Name} require comparable values.");
        }
    }
}

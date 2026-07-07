using Cerneala.UI.Motion.Interpolation;

namespace Cerneala.UI.Motion.Specs;

public sealed class DecaySpec<T> : MotionSpec<T>
{
    public DecaySpec(
        MotionVelocity<T> initialVelocity,
        float deceleration = 0.998f)
        : this(initialVelocity, deceleration, default, default, hasMin: false, hasMax: false, bounce: null)
    {
    }

    private DecaySpec(
        MotionVelocity<T> initialVelocity,
        float deceleration,
        T? min,
        T? max,
        bool hasMin,
        bool hasMax,
        MotionSpec<T>? bounce)
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
        Bounce = bounce;
    }

    public MotionVelocity<T> InitialVelocity { get; }

    public float Deceleration { get; }

    public T? Min { get; }

    public T? Max { get; }

    public bool HasMin { get; }

    public bool HasMax { get; }

    public MotionSpec<T>? Bounce { get; }

    public DecaySpec<T> WithBounds(T min, T max)
    {
        if (TryCompareComparable(min, max, out int comparison) && comparison > 0)
        {
            throw new ArgumentOutOfRangeException(nameof(min), "Decay minimum bound must be less than or equal to the maximum bound.");
        }

        return new DecaySpec<T>(InitialVelocity, Deceleration, min, max, hasMin: true, hasMax: true, Bounce);
    }

    public DecaySpec<T> WithBounce(MotionSpec<T> bounce)
    {
        ArgumentNullException.ThrowIfNull(bounce);
        return new DecaySpec<T>(InitialVelocity, Deceleration, Min, Max, HasMin, HasMax, bounce);
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

        if (Bounce is not null && (!HasMin || !HasMax))
        {
            throw new InvalidOperationException($"Decay bounce for {typeof(T).Name} requires both min and max bounds.");
        }

        return new DecaySampler(this, from, mixer, context);
    }

    private static bool SupportsComparableBounds()
    {
        Type type = typeof(T);
        return typeof(IComparable<T>).IsAssignableFrom(type) || typeof(IComparable).IsAssignableFrom(type);
    }

    private static bool TryCompareComparable(T left, T right, out int comparison)
    {
        if (left is IComparable<T> genericComparable)
        {
            comparison = genericComparable.CompareTo(right);
            return true;
        }

        if (left is IComparable comparable)
        {
            comparison = comparable.CompareTo(right);
            return true;
        }

        comparison = 0;
        return false;
    }

    private sealed class DecaySampler : MotionSampler<T>
    {
        private readonly DecaySpec<T> spec;
        private readonly ValueMixer<T> mixer;
        private readonly MotionSpecContext context;
        private MotionSampler<T>? bounceSampler;
        private T current;
        private T velocity;
        private bool isComplete;

        public DecaySampler(DecaySpec<T> spec, T from, ValueMixer<T> mixer, MotionSpecContext context)
        {
            this.spec = spec;
            this.mixer = mixer;
            this.context = context;
            current = from;
            velocity = spec.InitialVelocity.Value;
        }

        public override T Current => current;

        public override bool IsComplete => isComplete;

        public override MotionVelocity<T>? Velocity
            => bounceSampler is not null && TryGetVelocity(bounceSampler, out MotionVelocity<T> bounceVelocity)
                ? bounceVelocity
                : new MotionVelocity<T>(velocity);

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

            if (bounceSampler is not null)
            {
                bounceSampler.Advance(delta);
                current = bounceSampler.Current;
                if (TryGetVelocity(bounceSampler, out MotionVelocity<T> bounceVelocity))
                {
                    velocity = bounceVelocity.Value;
                }

                isComplete = bounceSampler.IsComplete;
                return;
            }

            T next = mixer.Add(current, mixer.Scale(velocity, (float)delta.TotalSeconds));
            if (TryClamp(next, spec, out T clamped))
            {
                current = clamped;
                if (spec.Bounce is not null)
                {
                    T reflected = ReflectIntoBounds(next, clamped, spec, mixer);
                    velocity = mixer.Scale(velocity, 0);
                    bounceSampler = spec.Bounce.CreateSampler(clamped, reflected, mixer, context);
                    return;
                }

                velocity = mixer.Scale(velocity, 0);
                isComplete = true;
                return;
            }

            current = next;
            float decay = MathF.Pow(spec.Deceleration, (float)(delta.TotalMilliseconds / 16.6666667));
            velocity = mixer.Scale(velocity, decay);
            if (mixer.Magnitude(velocity) <= 0.01f)
            {
                isComplete = true;
            }
        }

        private static T ReflectIntoBounds(T value, T clamped, DecaySpec<T> spec, ValueMixer<T> mixer)
        {
            T overshoot = mixer.Subtract(value, clamped);
            T reflected = mixer.Subtract(clamped, overshoot);
            return TryClamp(reflected, spec, out T bounded) ? bounded : reflected;
        }

        private static bool TryGetVelocity(MotionSampler<T> sampler, out MotionVelocity<T> velocity)
        {
            try
            {
                MotionVelocity<T>? samplerVelocity = sampler.Velocity;
                if (samplerVelocity is MotionVelocity<T> typedVelocity)
                {
                    velocity = typedVelocity;
                    return true;
                }
            }
            catch (InvalidOperationException)
            {
            }

            velocity = default;
            return false;
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
            if (TryCompareComparable(left, right, out int comparison))
            {
                return comparison;
            }

            throw new InvalidOperationException($"Decay bounds for {typeof(T).Name} require comparable values.");
        }
    }
}

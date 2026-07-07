using Cerneala.UI.Motion.Interpolation;

namespace Cerneala.UI.Motion.Specs;

public sealed class SpringSpec<T> : MotionSpec<T>
{
    private const int MaxSubsteps = 1000;
    private const double FixedStepSeconds = 1d / 120d;

    public SpringSpec(
        float stiffness = 520,
        float damping = 38,
        float mass = 1,
        float restSpeed = 0.01f,
        float restDelta = 0.01f,
        SpringVelocityMode velocityMode = SpringVelocityMode.Preserve)
    {
        if (!float.IsFinite(stiffness) || stiffness <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stiffness), "Stiffness must be positive.");
        }

        if (!float.IsFinite(damping) || damping < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(damping), "Damping cannot be negative.");
        }

        if (!float.IsFinite(mass) || mass <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mass), "Mass must be positive.");
        }

        if (!float.IsFinite(restSpeed) || restSpeed < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(restSpeed), "Rest speed cannot be negative.");
        }

        if (!float.IsFinite(restDelta) || restDelta < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(restDelta), "Rest delta cannot be negative.");
        }

        Stiffness = stiffness;
        Damping = damping;
        Mass = mass;
        RestSpeed = restSpeed;
        RestDelta = restDelta;
        VelocityMode = velocityMode;
    }

    public float Stiffness { get; }

    public float Damping { get; }

    public float Mass { get; }

    public float RestSpeed { get; }

    public float RestDelta { get; }

    public SpringVelocityMode VelocityMode { get; }

    public SpringSpec<T> WithRestThresholds(float restSpeed, float restDelta)
    {
        return new SpringSpec<T>(Stiffness, Damping, Mass, restSpeed, restDelta, VelocityMode);
    }

    public SpringSpec<T> WithVelocityMode(SpringVelocityMode velocityMode)
    {
        return new SpringSpec<T>(Stiffness, Damping, Mass, RestSpeed, RestDelta, velocityMode);
    }

    public override MotionSampler<T> CreateSampler(T from, T to, ValueMixer<T> mixer, MotionSpecContext context)
    {
        ArgumentNullException.ThrowIfNull(mixer);
        ArgumentNullException.ThrowIfNull(context);
        if (!mixer.SupportsVectorOperations)
        {
            throw new InvalidOperationException(
                $"Spring for {typeof(T).Name} requires a vector-capable mixer. Register a vector adapter/mixer or use tween/keyframes for non-vector interpolation.");
        }

        return new VectorSpringSampler(this, from, to, mixer, context);
    }

    private sealed class VectorSpringSampler : MotionSampler<T>
    {
        private readonly SpringSpec<T> spec;
        private readonly ValueMixer<T> mixer;
        private readonly MotionSpecContext context;
        private T current;
        private T target;
        private T velocity;
        private bool isComplete;

        public VectorSpringSampler(SpringSpec<T> spec, T from, T to, ValueMixer<T> mixer, MotionSpecContext context)
        {
            this.spec = spec;
            this.mixer = mixer;
            this.context = context;
            current = from;
            target = to;
            velocity = mixer.Scale(mixer.Subtract(to, from), 0);
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

            double remaining = delta.TotalSeconds;
            double maxAdvanceSeconds = MaxSubsteps * FixedStepSeconds;
            if (remaining > maxAdvanceSeconds)
            {
                if (context.Diagnostics is null)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(delta),
                        $"Spring advance delta exceeds the supported integration window of {maxAdvanceSeconds:0.###} seconds. Provide MotionDiagnostics to record an explicit clamp.");
                }

                context.Diagnostics.RecordWarning(
                    $"Spring '{context.DebugName ?? typeof(T).Name}' advance delta was clamped to {maxAdvanceSeconds:0.###} seconds.");
                remaining = maxAdvanceSeconds;
            }

            int iterations = 0;
            while (remaining > 0 && iterations < MaxSubsteps)
            {
                double step = Math.Min(remaining, FixedStepSeconds);
                Integrate((float)step);
                remaining -= step;
                iterations++;
            }

            T deltaToTarget = mixer.Subtract(target, current);
            if (mixer.Magnitude(deltaToTarget) <= spec.RestDelta && mixer.Magnitude(velocity) <= spec.RestSpeed)
            {
                current = target;
                velocity = mixer.Scale(velocity, 0);
                isComplete = true;
            }
        }

        public override void Retarget(T to, RetargetMode mode)
        {
            target = to;
            if (spec.VelocityMode == SpringVelocityMode.Reset)
            {
                velocity = mixer.Scale(velocity, 0);
            }

            isComplete = false;
        }

        // Semi-implicit Euler keeps the integrator simple and stable enough for small fixed substeps.
        private void Integrate(float seconds)
        {
            T displacement = mixer.Subtract(current, target);
            T springForce = mixer.Scale(displacement, -spec.Stiffness);
            T dampingForce = mixer.Scale(velocity, -spec.Damping);
            T acceleration = mixer.Scale(mixer.Add(springForce, dampingForce), 1 / spec.Mass);
            velocity = mixer.Add(velocity, mixer.Scale(acceleration, seconds));
            current = mixer.Add(current, mixer.Scale(velocity, seconds));
        }
    }
}

using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Media;

namespace Cerneala.UI.Motion.Specs;

public sealed class SpringSpec<T> : MotionSpec<T>
{
    private const double MaxAdvanceSeconds = 1000d / 120d;

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
        if (mixer is TransformMixer && this is SpringSpec<Transform> transformSpec)
        {
            return (MotionSampler<T>)(object)new TransformSpringSampler(
                transformSpec, (Transform)(object)from!, (Transform)(object)to!, context);
        }

        if (!mixer.SupportsVectorOperations)
        {
            throw new InvalidOperationException(
                $"Spring for {typeof(T).Name} requires a vector-capable mixer. Register a vector adapter/mixer or use tween/keyframes for non-vector interpolation.");
        }

        return new VectorSpringSampler(this, from, to, mixer, context);
    }

    internal sealed class VectorSpringSampler : MotionSampler<T>
    {
        private SpringSpec<T> spec;
        private readonly ValueMixer<T> mixer;
        private MotionSpecContext context;
        private readonly bool completeAtFixedPoint;
        private T current;
        private T target;
        private T velocity;
        private bool isComplete;

        internal VectorSpringSampler(SpringSpec<T> spec, T from, T to, ValueMixer<T> mixer, MotionSpecContext context,
            MotionVelocity<T>? initialVelocity = null, bool completeAtFixedPoint = true)
        {
            this.spec = spec;
            this.mixer = mixer;
            this.context = context;
            this.completeAtFixedPoint = completeAtFixedPoint;
            current = from;
            target = to;
            velocity = initialVelocity is { } incoming ? incoming.Value : mixer.Scale(mixer.Subtract(to, from), 0);
        }

        public override T Current => current;

        public override bool IsComplete => isComplete;

        public override MotionVelocity<T>? Velocity => new(velocity);

        public override void Advance(TimeSpan delta)
        {
            MotionSampler.ThrowIfNegativeDelta(delta);

            if (isComplete)
            {
                return;
            }

            double seconds = delta.TotalSeconds;
            if (seconds > MaxAdvanceSeconds)
            {
                if (context.Diagnostics is null)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(delta),
                        $"Spring advance delta exceeds the supported integration window of {MaxAdvanceSeconds:0.###} seconds. Provide MotionDiagnostics to record an explicit clamp.");
                }

                context.Diagnostics.RecordWarning(
                    $"Spring '{context.DebugName ?? typeof(T).Name}' advance delta was clamped to {MaxAdvanceSeconds:0.###} seconds.");
                seconds = MaxAdvanceSeconds;
            }

            T positionBefore = current;
            T velocityBefore = velocity;
            bool integrated = seconds > 0;
            if (integrated)
            {
                Integrate(seconds);
            }

            // Near a large target the value can stop a few representable steps
            // short while the velocity settles at a small constant value, too
            // small to move it and possibly above RestSpeed. A state the
            // integration leaves bitwise unchanged is a fixed point it can never
            // leave: the spring has ended, on its target.
            bool fixedPoint = completeAtFixedPoint && integrated &&
                mixer.EqualsWithinTolerance(positionBefore, current, 0) &&
                mixer.EqualsWithinTolerance(velocityBefore, velocity, 0);
            T deltaToTarget = mixer.Subtract(target, current);
            if (fixedPoint ||
                mixer.Magnitude(deltaToTarget) <= spec.RestDelta && mixer.Magnitude(velocity) <= spec.RestSpeed)
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

        internal override bool TryRetargetWithSpec(T to, MotionSpec<T> incoming, MotionSpecContext context)
        {
            if (incoming is not SpringSpec<T> spring)
            {
                return false;
            }

            spec = spring;
            this.context = context;
            // Springs retain motion state, not elapsed progress. Retarget applies
            // the incoming velocity mode without changing the current position.
            Retarget(to, RetargetMode.Restart);
            return true;
        }

        // Applies the exact solution of Mass * x'' + Damping * x' + Stiffness * x = 0, where x is the displacement
        // from the target. Unlike a numeric step, it is stable for every accepted stiffness, damping, mass and delta.
        private void Integrate(double seconds)
        {
            (float positionFromDisplacement, float positionFromVelocity, float velocityFromDisplacement, float velocityFromVelocity) =
                GetSolutionCoefficients(spec.Stiffness, spec.Damping, spec.Mass, seconds);
            T displacement = mixer.Subtract(current, target);
            T nextDisplacement = mixer.Add(
                mixer.Scale(displacement, positionFromDisplacement),
                mixer.Scale(velocity, positionFromVelocity));
            velocity = mixer.Add(
                mixer.Scale(displacement, velocityFromDisplacement),
                mixer.Scale(velocity, velocityFromVelocity));
            current = mixer.Add(target, nextDisplacement);
        }

        // With beta = Damping / (2 * Mass), omega^2 = Stiffness / Mass and s = sqrt(|beta^2 - omega^2|), the
        // solution is x(t) = e^(-beta t) * ((K + beta S) * x0 + S * v0) and v(t) = e^(-beta t) * (-omega^2 S * x0 +
        // (K - beta S) * v0), where K and S are cos(s t) and sin(s t) / s when underdamped, cosh(s t) and
        // sinh(s t) / s when overdamped.
        private static (float, float, float, float) GetSolutionCoefficients(
            float stiffness, float damping, float mass, double seconds)
        {
            double beta = damping / (2d * mass);
            double omegaSquared = stiffness / (double)mass;
            double omega = Math.Sqrt(omegaSquared);
            double discriminant = (beta - omega) * (beta + omega);
            double s = Math.Sqrt(Math.Abs(discriminant));
            double decayedK;
            double decayedS;
            if (s * seconds < 1e-3)
            {
                // Near critical damping both forms reduce to the same series, without dividing by a tiny s.
                double decay = Math.Exp(-beta * seconds);
                double square = discriminant * seconds * seconds;
                decayedK = decay * (1 + (square / 2));
                decayedS = decay * seconds * (1 + (square / 6));
            }
            else if (discriminant < 0)
            {
                double decay = Math.Exp(-beta * seconds);
                decayedK = decay * Math.Cos(s * seconds);
                decayedS = decay * Math.Sin(s * seconds) / s;
            }
            else
            {
                // e^(-beta t) cosh(s t) and e^(-beta t) sinh(s t) from the two real decay rates, so that a vanishing
                // e^(-beta t) never multiplies an overflowing cosh. s - beta is written without cancellation.
                double slow = Math.Exp(-omegaSquared / (beta + s) * seconds);
                double fast = Math.Exp(-(beta + s) * seconds);
                decayedK = (slow + fast) / 2;
                decayedS = (slow - fast) / (2 * s);
            }

            return (
                (float)(decayedK + (beta * decayedS)),
                (float)decayedS,
                (float)(-omegaSquared * decayedS),
                (float)(decayedK - (beta * decayedS)));
        }
    }
}

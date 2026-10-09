using Cerneala.UI.Media;
using Cerneala.UI.Motion.Interpolation;

namespace Cerneala.UI.Motion.Specs;

// Matrices are the output representation, never the spring's position/velocity state.
// Endpoints resolve like TransformMixer.Mix: a degenerate (zero-scale) endpoint
// borrows rotation and skew from the other endpoint or from the current state.
// When no component frame represents both endpoints, the identity frame is
// tried; failing that, a spring cannot run in matrix space and completes at its
// target immediately.
internal sealed class TransformSpringSampler : MotionSampler<Transform>
{
    private static readonly ComponentMixer Mixer = new();
    private static readonly TransformComponents IdentityFrame = new(0, 0, 1, 1, 0, 0, 0);
    private SpringSpec<Transform> spec;
    private SpringSpec<TransformComponents>.VectorSpringSampler? components;
    private Transform current;
    private Transform target;
    private MotionSpecContext context;

    internal TransformSpringSampler(SpringSpec<Transform> spec, Transform from, Transform to, MotionSpecContext context)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        this.spec = spec;
        this.context = context;
        current = from;
        target = to;
        Start(from, to);
    }

    public override Transform Current => current;
    public override bool IsComplete => components is null || components.IsComplete;

    // There is no lossless Transform representation for component velocity.
    // Keep it internally rather than exposing a composed, singular "velocity matrix".
    public override MotionVelocity<Transform>? Velocity => null;

    public override void Advance(TimeSpan delta)
    {
        if (IsComplete)
        {
            MotionSampler.ThrowIfNegativeDelta(delta);
            return;
        }

        components!.Advance(delta);
        if (IsComplete)
            current = target;
        else if (delta > TimeSpan.Zero)
            current = TransformMixer.Compose(components.Current);
    }

    public override void Retarget(Transform to, RetargetMode mode)
    {
        ArgumentNullException.ThrowIfNull(to);
        target = to;
        if (components is null)
        {
            Start(current, to);
        }
        else if (TryResolveTarget(components.Current, to, out TransformComponents next))
        {
            components.Retarget(next, mode);
        }
        else
        {
            SnapToTarget();
        }
    }

    internal override bool TryRetargetWithSpec(Transform to, MotionSpec<Transform> incoming, MotionSpecContext context)
    {
        if (incoming is not SpringSpec<Transform> spring)
            return false;

        spec = spring;
        this.context = context;
        target = to;
        if (components is null)
        {
            Start(current, to);
        }
        else if (TryResolveTarget(components.Current, to, out TransformComponents next))
        {
            MotionVelocity<TransformComponents>? velocity = spring.VelocityMode == SpringVelocityMode.Preserve
                ? components.Velocity : null;
            components = CreateComponents(components.Current, next, velocity);
        }
        else
        {
            SnapToTarget();
        }

        return true;
    }

    private void Start(Transform from, Transform to)
    {
        if (TryResolveEndpoints(from, to, out TransformComponents start, out TransformComponents end))
        {
            components = CreateComponents(start, end, initialVelocity: null);
        }
        else
        {
            SnapToTarget();
        }
    }

    private void SnapToTarget()
    {
        components = null;
        current = target;
    }

    private SpringSpec<TransformComponents>.VectorSpringSampler CreateComponents(
        TransformComponents start, TransformComponents to, MotionVelocity<TransformComponents>? initialVelocity)
    {
        SpringSpec<TransformComponents> componentSpec = new(spec.Stiffness, spec.Damping, spec.Mass,
            spec.RestSpeed, spec.RestDelta, spec.VelocityMode);
        // Max norm enforces BOTH rest thresholds for EVERY component. Do not use
        // the generic fixed-point escape to declare a component outside them at rest.
        return new(componentSpec, start, to, Mixer, context, initialVelocity, completeAtFixedPoint: false);
    }

    private static bool TryResolveEndpoints(
        Transform from, Transform to, out TransformComponents start, out TransformComponents end)
    {
        if (TransformMixer.TryResolveComponents(from, to, out start, out end) ||
            (TransformMixer.TryResolveAgainst(from, IdentityFrame, out start) &&
                TransformMixer.TryResolveAgainst(to, IdentityFrame, out end)))
        {
            end = Unwrap(start, end);
            return true;
        }

        return false;
    }

    // The current state is always a valid frame, even when its scale is near 0,
    // so a degenerate target is resolved against it rather than re-decomposed.
    private static bool TryResolveTarget(TransformComponents reference, Transform to, out TransformComponents end)
    {
        if (TransformMixer.TryResolveAgainst(to, reference, out end) ||
            TransformMixer.TryResolveAgainst(to, IdentityFrame, out end))
        {
            end = Unwrap(reference, end);
            return true;
        }

        return false;
    }

    private static TransformComponents Unwrap(TransformComponents start, TransformComponents end)
    {
        return end with
        {
            RotationRadians = start.RotationRadians + TransformMixer.ShortestAngleDelta(start.RotationRadians, end.RotationRadians)
        };
    }

    private sealed class ComponentMixer : ValueMixer<TransformComponents>
    {
        public override bool SupportsVectorOperations => true;

        public override TransformComponents Mix(TransformComponents from, TransformComponents to, float progress) =>
            Add(from, Scale(Subtract(to, from), progress));

        public override TransformComponents Add(TransformComponents a, TransformComponents b) => new(
            a.TranslationX + b.TranslationX, a.TranslationY + b.TranslationY,
            a.ScaleX + b.ScaleX, a.ScaleY + b.ScaleY, a.RotationRadians + b.RotationRadians,
            a.SkewX + b.SkewX, a.SkewY + b.SkewY);

        public override TransformComponents Subtract(TransformComponents a, TransformComponents b) => new(
            a.TranslationX - b.TranslationX, a.TranslationY - b.TranslationY,
            a.ScaleX - b.ScaleX, a.ScaleY - b.ScaleY, a.RotationRadians - b.RotationRadians,
            a.SkewX - b.SkewX, a.SkewY - b.SkewY);

        public override TransformComponents Scale(TransformComponents value, float scalar) => new(
            value.TranslationX * scalar, value.TranslationY * scalar,
            value.ScaleX * scalar, value.ScaleY * scalar, value.RotationRadians * scalar,
            value.SkewX * scalar, value.SkewY * scalar);

        public override float Magnitude(TransformComponents value) =>
            MathF.Max(MathF.Max(MathF.Abs(value.TranslationX), MathF.Abs(value.TranslationY)),
                MathF.Max(MathF.Max(MathF.Abs(value.ScaleX), MathF.Abs(value.ScaleY)),
                    MathF.Max(MathF.Abs(value.RotationRadians), MathF.Max(MathF.Abs(value.SkewX), MathF.Abs(value.SkewY)))));

        public override bool EqualsWithinTolerance(TransformComponents a, TransformComponents b, float tolerance)
        {
            ThrowIfNegativeTolerance(tolerance);
            return Magnitude(Subtract(a, b)) <= tolerance;
        }
    }
}

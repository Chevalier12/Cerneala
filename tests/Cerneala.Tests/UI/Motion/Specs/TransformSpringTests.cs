using Cerneala.UI.Media;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.UI.Motion.Specs;

public sealed class TransformSpringTests
{
    [Fact]
    public void ZeroAdvanceDoesNotRecomposeTheStartingTransform()
    {
        Transform from = new(new Matrix3x2(1, 2, 3, 4, 5, 6));
        MotionSampler<Transform> sampler = CreateSampler(from, Transform.Identity);
        sampler.Advance(TimeSpan.Zero);
        Assert.Equal(from, sampler.Current);
    }

    [Fact]
    public void TranslatedAndScaledTransformSpringSettlesToIdentity()
    {
        Transform from = TransformMixer.Compose(new(40, -20, 2, 3, 0.4f, 0.2f, 0));
        MotionSampler<Transform> sampler = CreateSampler(from, Transform.Identity);
        Assert.Equal(from, sampler.Current);

        for (int i = 0; i < 600 && !sampler.IsComplete; i++)
            sampler.Advance(TimeSpan.FromMilliseconds(16));

        Assert.True(sampler.IsComplete);
        Assert.Equal(Transform.Identity, sampler.Current);
    }

    [Theory]
    [InlineData(170, -170, 1)]
    [InlineData(-170, 170, -1)]
    public void RotationSpringFollowsTheTweenShortestPath(float fromDegrees, float toDegrees, int direction)
    {
        float fromAngle = fromDegrees * MathF.PI / 180;
        Transform from = TransformMixer.Compose(new(0, 0, 1, 1, fromAngle, 0, 0));
        Transform to = TransformMixer.Compose(new(0, 0, 1, 1, toDegrees * MathF.PI / 180, 0, 0));
        MotionSampler<Transform> sampler = CreateSampler(from, to);
        sampler.Advance(TimeSpan.FromMilliseconds(16));
        float springAngle = TransformMixer.Decompose(sampler.Current).RotationRadians;
        float tweenAngle = TransformMixer.Decompose(new TransformMixer().Mix(from, to, 0.1f)).RotationRadians;

        Assert.True(direction * (springAngle - fromAngle) > 0);
        Assert.True(direction * (tweenAngle - fromAngle) > 0);
    }

    [Theory]
    [InlineData(RetargetMode.Restart)]
    [InlineData(RetargetMode.PreserveProgress)]
    public void MotionValueRetargetCarriesComponentVelocityAndUsesIncomingParameters(RetargetMode mode)
    {
        MotionGraph graph = new();
        Transform from = TransformMixer.Compose(new(10, 20, 2, 3, 0.2f, 0.1f, 0));
        MotionValue<Transform> value = graph.CreateValue(from);
        SpringSpec<Transform> first = MotionFactory.Spring<Transform>();
        value.AnimateTo(Transform.Identity, first);
        graph.Tick(new(TimeSpan.FromMilliseconds(16), TimeSpan.FromMilliseconds(16), 1, MotionFrameReason.Manual, MotionFramePhase.BeforeRender));
        Transform before = value.Current;

        // Scalar springs are the contract oracle for each decomposed component.
        float[] initial = [10, 20, 2, 3, 0.2f, 0.1f];
        float[] identity = [0, 0, 1, 1, 0, 0];
        float[] positions = new float[6];
        float[] velocities = new float[6];
        for (int i = 0; i < initial.Length; i++)
        {
            MotionSampler<float> scalar = MotionFactory.Spring<float>().CreateSampler(initial[i], identity[i], new FloatMixer(), Context());
            scalar.Advance(TimeSpan.FromMilliseconds(16));
            positions[i] = scalar.Current;
            velocities[i] = scalar.Velocity!.Value.Value;
        }

        float[] targets = [-10, 5, 4, 2, -0.3f, -0.2f];
        Transform target = TransformMixer.Compose(new(targets[0], targets[1], targets[2], targets[3], targets[4], targets[5], 0));
        value.AnimateTo(target, MotionFactory.Spring<Transform>(stiffness: 100, damping: 7, mass: 2), new(mode));
        Assert.Equal(before, value.Current); // No elapsed-time replay or position jump.
        graph.Tick(new(TimeSpan.FromMilliseconds(32), TimeSpan.FromMilliseconds(16), 2, MotionFrameReason.Manual, MotionFramePhase.BeforeRender));

        for (int i = 0; i < positions.Length; i++)
        {
            Integrate(ref positions[i], ref velocities[i], targets[i], 0.016, 100, 7, 2);
        }
        Transform expected = TransformMixer.Compose(new(positions[0], positions[1], positions[2], positions[3], positions[4], positions[5], 0));
        Assert.True(new TransformMixer().EqualsWithinTolerance(expected, value.Current, 0.00001f));
    }

    [Fact]
    public void DirectRetargetPreservesComponentVelocity()
    {
        MotionSampler<Transform> sampler = CreateSampler(new(Matrix3x2.CreateTranslation(100, 0)), Transform.Identity);
        sampler.Advance(TimeSpan.FromMilliseconds(16));
        float position = sampler.Current.Matrix.M31;
        MotionSampler<float> scalar = MotionFactory.Spring<float>().CreateSampler(100, 0, new FloatMixer(), Context());
        scalar.Advance(TimeSpan.FromMilliseconds(16));
        scalar.Retarget(200, RetargetMode.Restart);

        sampler.Retarget(new(Matrix3x2.CreateTranslation(200, 0)), RetargetMode.Restart);
        Assert.Equal(position, sampler.Current.Matrix.M31);
        sampler.Advance(TimeSpan.FromMilliseconds(16));
        scalar.Advance(TimeSpan.FromMilliseconds(16));
        Assert.Equal(scalar.Current, sampler.Current.Matrix.M31, precision: 4);
    }

    [Fact]
    public void ComponentSpringCanPassThroughZeroScaleWithoutRedecomposition()
    {
        // Semi-implicit step: scaleY = 1 + (-20000 * 2 * .005) * .005 = 0.
        // Composing a singular intermediate value must not re-decompose it.
        SpringSpec<Transform> spec = MotionFactory.Spring<Transform>(stiffness: 20000, damping: 0);
        MotionSampler<Transform> sampler = spec.CreateSampler(Transform.Identity,
            TransformMixer.Compose(new(0, 0, 1, -1, 0, 0, 0)), new TransformMixer(), Context());
        sampler.Advance(TimeSpan.FromMilliseconds(5));
        Assert.Equal(0f, sampler.Current.Matrix.M22);
        sampler.Retarget(Transform.Identity, RetargetMode.Restart);
        sampler.Advance(TimeSpan.FromMilliseconds(5));
        Assert.True(float.IsFinite(sampler.Current.Matrix.M22));
    }

    [Fact]
    public void CompletionRequiresEveryComponentToBeAtRest()
    {
        SpringSpec<Transform> spec = MotionFactory.Spring<Transform>().WithRestThresholds(0.01f, 0.01f);
        Transform from = TransformMixer.Compose(new(0.005f, 0, 2, 1, 0, 0, 0));
        MotionSampler<Transform> sampler = spec.CreateSampler(from, Transform.Identity, new TransformMixer(), Context());
        sampler.Advance(TimeSpan.Zero);
        Assert.False(sampler.IsComplete);
        sampler.Advance(TimeSpan.FromMilliseconds(16));
        Assert.False(sampler.IsComplete);
    }

    [Fact]
    public void RestThresholdsArePerComponentRatherThanSummed()
    {
        Transform from = TransformMixer.Compose(new(0.009f, 0.009f, 1.009f, 1.009f, 0.009f, 0.009f, 0));
        MotionSampler<Transform> sampler = CreateSampler(from, Transform.Identity);
        sampler.Advance(TimeSpan.Zero);
        Assert.True(sampler.IsComplete);
        Assert.Equal(Transform.Identity, sampler.Current);
    }

    [Fact]
    public void MotionValueCanRetargetWhileItsOutputHasZeroScale()
    {
        MotionGraph graph = new();
        MotionValue<Transform> value = graph.CreateValue(Transform.Identity);
        value.AnimateTo(TransformMixer.Compose(new(0, 0, 1, -1, 0, 0, 0)),
            MotionFactory.Spring<Transform>(stiffness: 20000, damping: 0));
        graph.Tick(new(TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(5), 1, MotionFrameReason.Manual, MotionFramePhase.BeforeRender));
        Assert.Equal(0f, value.Current.Matrix.M22);

        value.AnimateTo(Transform.Identity, MotionFactory.Spring<Transform>());
        graph.Tick(new(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(5), 2, MotionFrameReason.Manual, MotionFramePhase.BeforeRender));
        Assert.True(float.IsFinite(value.Current.Matrix.M22));
        Assert.Null(value.Velocity);
    }

    [Fact]
    public void IncomingResetModeResetsComponentVelocity()
    {
        MotionGraph graph = new();
        MotionValue<Transform> value = graph.CreateValue(new Transform(Matrix3x2.CreateTranslation(100, 0)));
        value.AnimateTo(Transform.Identity, MotionFactory.Spring<Transform>());
        graph.Tick(new(TimeSpan.FromMilliseconds(16), TimeSpan.FromMilliseconds(16), 1, MotionFrameReason.Manual, MotionFramePhase.BeforeRender));
        float before = value.Current.Matrix.M31;
        MotionSampler<float> scalar = MotionFactory.Spring<float>(100, 7, 2).CreateSampler(before, 200, new FloatMixer(), Context());
        value.AnimateTo(new(Matrix3x2.CreateTranslation(200, 0)),
            MotionFactory.Spring<Transform>(100, 7, 2).WithVelocityMode(SpringVelocityMode.Reset));
        graph.Tick(new(TimeSpan.FromMilliseconds(32), TimeSpan.FromMilliseconds(16), 2, MotionFrameReason.Manual, MotionFramePhase.BeforeRender));
        scalar.Advance(TimeSpan.FromMilliseconds(16));
        Assert.Equal(scalar.Current, value.Current.Matrix.M31, precision: 4);
    }

    private static MotionSampler<Transform> CreateSampler(Transform from, Transform to) =>
        MotionFactory.Spring<Transform>().CreateSampler(from, to, new TransformMixer(), Context());

    private static MotionSpecContext Context() => new(ReducedMotionPolicy.Default, new ValueMixerRegistry(), null, TimeSpan.Zero);

    private static void Integrate(ref float position, ref float velocity, float target, double remaining,
        float stiffness, float damping, float mass)
    {
        while (remaining > 0)
        {
            float step = (float)Math.Min(remaining, 1d / 120);
            velocity += ((-stiffness * (position - target) - damping * velocity) / mass) * step;
            position += velocity * step;
            remaining -= Math.Min(remaining, 1d / 120);
        }
    }
}

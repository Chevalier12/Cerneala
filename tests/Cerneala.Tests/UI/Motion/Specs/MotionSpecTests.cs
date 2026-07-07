using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Diagnostics;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.UI.Motion.Specs;

public sealed class MotionSpecTests
{
    [Fact]
    public void TweenSamplesStartMidEnd()
    {
        TweenSpec<float> spec = MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100), Easings.Linear);
        MotionSampler<float> sampler = spec.CreateSampler(0, 10, new FloatMixer(), Context());

        Assert.Equal(0, sampler.Current);

        sampler.Advance(TimeSpan.FromMilliseconds(50));
        Assert.Equal(5, sampler.Current, precision: 3);
        Assert.False(sampler.IsComplete);

        sampler.Advance(TimeSpan.FromMilliseconds(50));
        Assert.Equal(10, sampler.Current, precision: 3);
        Assert.True(sampler.IsComplete);
    }

    [Fact]
    public void TweenAppliesDelayBeforeSamplingProgress()
    {
        TweenSpec<float> spec = MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100), Easings.Linear)
            .WithDelay(TimeSpan.FromMilliseconds(50));
        MotionSampler<float> sampler = spec.CreateSampler(0, 10, new FloatMixer(), Context());

        sampler.Advance(TimeSpan.FromMilliseconds(25));
        Assert.Equal(0, sampler.Current);

        sampler.Advance(TimeSpan.FromMilliseconds(50));
        Assert.Equal(2.5f, sampler.Current, precision: 3);
    }

    [Fact]
    public void TweenRetargetRestartStartsFromCurrentValue()
    {
        TweenSpec<float> spec = MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100), Easings.Linear);
        MotionSampler<float> sampler = spec.CreateSampler(0, 10, new FloatMixer(), Context());
        sampler.Advance(TimeSpan.FromMilliseconds(50));

        sampler.Retarget(20, RetargetMode.Restart);
        sampler.Advance(TimeSpan.FromMilliseconds(50));

        Assert.Equal(12.5f, sampler.Current, precision: 3);
    }

    [Fact]
    public void TweenRetargetPreserveProgressKeepsElapsedProgress()
    {
        TweenSpec<float> spec = MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100), Easings.Linear);
        MotionSampler<float> sampler = spec.CreateSampler(0, 10, new FloatMixer(), Context());
        sampler.Advance(TimeSpan.FromMilliseconds(50));

        sampler.Retarget(20, RetargetMode.PreserveProgress);

        Assert.Equal(10, sampler.Current, precision: 3);
    }

    [Fact]
    public void SpringApproachesTargetAndCompletesUnderRestThresholds()
    {
        SpringSpec<float> spec = MotionFactory.Spring<float>(stiffness: 520, damping: 38, mass: 1)
            .WithRestThresholds(restSpeed: 0.01f, restDelta: 0.01f);
        MotionSampler<float> sampler = spec.CreateSampler(0, 100, new FloatMixer(), Context());

        for (int i = 0; i < 240 && !sampler.IsComplete; i++)
        {
            sampler.Advance(TimeSpan.FromMilliseconds(16));
        }

        Assert.True(sampler.IsComplete);
        Assert.Equal(100, sampler.Current, precision: 2);
    }

    [Fact]
    public void SpringRetargetPreservesVelocityForVectorMixer()
    {
        SpringSpec<float> spec = MotionFactory.Spring<float>();
        MotionSampler<float> sampler = spec.CreateSampler(0, 100, new FloatMixer(), Context());
        sampler.Advance(TimeSpan.FromMilliseconds(16));
        float velocityBeforeRetarget = sampler.Velocity!.Value.Value;

        sampler.Retarget(50, RetargetMode.Restart);

        Assert.Equal(velocityBeforeRetarget, sampler.Velocity!.Value.Value, precision: 3);
    }

    [Fact]
    public void SpringRejectsNonVectorMixerClearly()
    {
        MotionDiagnostics diagnostics = new();
        SpringSpec<float> spec = MotionFactory.Spring<float>();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            spec.CreateSampler(0, 100, new NonVectorFloatMixer(), Context(diagnostics)));

        Assert.Contains("spring", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("vector", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(nameof(Single), ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SpringRejectsBuiltInNonVectorMixersClearly()
    {
        SpringSpec<Cerneala.Drawing.DrawRect> spec = MotionFactory.Spring<Cerneala.Drawing.DrawRect>();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            spec.CreateSampler(
                new Cerneala.Drawing.DrawRect(0, 0, 10, 10),
                new Cerneala.Drawing.DrawRect(10, 10, 20, 20),
                new Cerneala.UI.Motion.Interpolation.DrawRectMixer(),
                Context()));

        Assert.Contains("spring", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("vector", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(nameof(Cerneala.Drawing.DrawRect), ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SpringAdvanceWithHugeDeltaRecordsClampedDeltaDiagnostic()
    {
        MotionDiagnostics diagnostics = new();
        SpringSpec<float> spec = MotionFactory.Spring<float>();
        MotionSampler<float> sampler = spec.CreateSampler(0, 100, new FloatMixer(), Context(diagnostics));

        sampler.Advance(TimeSpan.FromSeconds(20));

        Assert.Contains(diagnostics.Warnings, warning => warning.Contains("clamped", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SpringAdvanceWithHugeDeltaWithoutDiagnosticsFailsClearly()
    {
        SpringSpec<float> spec = MotionFactory.Spring<float>();
        MotionSampler<float> sampler = spec.CreateSampler(0, 100, new FloatMixer(), Context());

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            sampler.Advance(TimeSpan.FromSeconds(20)));
        Assert.Contains("diagnostics", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(float.NaN, 38, 1, 0.01f, 0.01f)]
    [InlineData(float.PositiveInfinity, 38, 1, 0.01f, 0.01f)]
    [InlineData(520, float.NaN, 1, 0.01f, 0.01f)]
    [InlineData(520, float.PositiveInfinity, 1, 0.01f, 0.01f)]
    [InlineData(520, 38, float.NaN, 0.01f, 0.01f)]
    [InlineData(520, 38, float.PositiveInfinity, 0.01f, 0.01f)]
    [InlineData(520, 38, 1, float.NaN, 0.01f)]
    [InlineData(520, 38, 1, float.PositiveInfinity, 0.01f)]
    [InlineData(520, 38, 1, 0.01f, float.NaN)]
    [InlineData(520, 38, 1, 0.01f, float.PositiveInfinity)]
    public void SpringRejectsNaNAndInfinityParameters(float stiffness, float damping, float mass, float restSpeed, float restDelta)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SpringSpec<float>(stiffness, damping, mass, restSpeed, restDelta));
    }

    [Theory]
    [InlineData(float.NaN, 38, 1)]
    [InlineData(float.PositiveInfinity, 38, 1)]
    [InlineData(520, float.NaN, 1)]
    [InlineData(520, float.PositiveInfinity, 1)]
    [InlineData(520, 38, float.NaN)]
    [InlineData(520, 38, float.PositiveInfinity)]
    public void UntypedSpringRejectsNaNAndInfinityParameters(float stiffness, float damping, float mass)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MotionFactory.Spring(stiffness, damping, mass));
    }

    [Fact]
    public void MotionDiagnosticsClearsWarningsAtFrameStart()
    {
        MotionDiagnostics diagnostics = new();
        diagnostics.RecordWarning("temporary warning");

        diagnostics.BeginFrame();

        Assert.Empty(diagnostics.Warnings);
    }

    [Fact]
    public void DecayClampsAtBounds()
    {
        DecaySpec<float> spec = MotionFactory.Decay(new MotionVelocity<float>(1000), deceleration: 0.9f)
            .WithBounds(min: 0, max: 25);
        MotionSampler<float> sampler = spec.CreateSampler(0, 0, new FloatMixer(), Context());

        for (int i = 0; i < 60 && !sampler.IsComplete; i++)
        {
            sampler.Advance(TimeSpan.FromMilliseconds(16));
        }

        Assert.True(sampler.IsComplete);
        Assert.Equal(25, sampler.Current, precision: 3);
    }

    [Fact]
    public void DecayBoundsRejectInvertedComparableRange()
    {
        DecaySpec<float> spec = MotionFactory.Decay(new MotionVelocity<float>(1000), deceleration: 0.9f);

        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            spec.WithBounds(min: 25, max: 0));
        Assert.Contains("min", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DecayWithoutBoundsDoesNotClampToDefaultValue()
    {
        DecaySpec<float> spec = MotionFactory.Decay(new MotionVelocity<float>(100), deceleration: 0.9f);
        MotionSampler<float> sampler = spec.CreateSampler(0, 0, new FloatMixer(), Context());

        sampler.Advance(TimeSpan.FromMilliseconds(16));

        Assert.False(sampler.IsComplete);
        Assert.True(sampler.Current > 0);
    }

    [Fact]
    public void DecayBoundsRejectNonComparableVectorValuesClearly()
    {
        DecaySpec<TestVector> spec = MotionFactory.Decay(new MotionVelocity<TestVector>(new TestVector(10)), deceleration: 0.9f)
            .WithBounds(new TestVector(0), new TestVector(25));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            spec.CreateSampler(new TestVector(0), new TestVector(0), new TestVectorMixer(), Context()));
        Assert.Contains("comparable", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void DecayRejectsNaNAndInfinityDeceleration(float deceleration)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MotionFactory.Decay(new MotionVelocity<float>(100), deceleration));
    }

    [Fact]
    public void DecayRejectsNonVectorMixers()
    {
        DecaySpec<float> spec = MotionFactory.Decay(new MotionVelocity<float>(100));

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            spec.CreateSampler(0, 0, new NonVectorFloatMixer(), Context()));
        Assert.Contains("vector", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void KeyframesValidateOffsets()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            MotionFactory.Keyframes(
                new MotionKeyframe<float>(0, 0),
                new MotionKeyframe<float>(0.75f, 10),
                new MotionKeyframe<float>(0.5f, 20),
                new MotionKeyframe<float>(1, 30)));

        Assert.Contains("sorted", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void KeyframesSampleExactEndpointValues()
    {
        KeyframesSpec<float> spec = MotionFactory.Keyframes(
            new MotionKeyframe<float>(0, 5),
            new MotionKeyframe<float>(0.5f, 15),
            new MotionKeyframe<float>(1, 30));
        MotionSampler<float> sampler = spec.CreateSampler(5, 30, new FloatMixer(), Context());

        Assert.Equal(5, sampler.Current);

        sampler.Advance(spec.Duration);

        Assert.Equal(30, sampler.Current);
        Assert.True(sampler.IsComplete);
    }

    [Fact]
    public void UntypedMotionFactoryPreservesInnerExceptionStackTrace()
    {
        MotionSpec spec = MotionFactory.Tween(TimeSpan.FromMilliseconds(100));

        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            spec.CreateSamplerUntyped("bad", 10f, new FloatMixer(), Context()));

        Assert.Contains("CreateTweenSampler", ex.StackTrace);
    }

    private static MotionSpecContext Context(MotionDiagnostics? diagnostics = null)
    {
        return new MotionSpecContext(
            ReducedMotionPolicy.Default,
            new ValueMixerRegistry(),
            diagnostics,
            TimeSpan.Zero,
            DebugName: "test");
    }

    private sealed class FloatMixer : ValueMixer<float>
    {
        public override float Mix(float from, float to, float progress)
        {
            return from + ((to - from) * progress);
        }

        public override bool SupportsVectorOperations => true;

        public override float Add(float left, float right)
        {
            return left + right;
        }

        public override float Subtract(float left, float right)
        {
            return left - right;
        }

        public override float Scale(float value, float scalar)
        {
            return value * scalar;
        }

        public override float Magnitude(float value)
        {
            return MathF.Abs(value);
        }
    }

    private sealed class NonVectorFloatMixer : ValueMixer<float>
    {
        public override float Mix(float from, float to, float progress)
        {
            return from + ((to - from) * progress);
        }
    }

    private readonly record struct TestVector(float X);

    private sealed class TestVectorMixer : ValueMixer<TestVector>
    {
        public override bool SupportsVectorOperations => true;

        public override TestVector Mix(TestVector from, TestVector to, float progress)
        {
            return new TestVector(from.X + ((to.X - from.X) * progress));
        }

        public override TestVector Add(TestVector left, TestVector right)
        {
            return new TestVector(left.X + right.X);
        }

        public override TestVector Subtract(TestVector left, TestVector right)
        {
            return new TestVector(left.X - right.X);
        }

        public override TestVector Scale(TestVector value, float scalar)
        {
            return new TestVector(value.X * scalar);
        }

        public override float Magnitude(TestVector value)
        {
            return MathF.Abs(value.X);
        }
    }
}

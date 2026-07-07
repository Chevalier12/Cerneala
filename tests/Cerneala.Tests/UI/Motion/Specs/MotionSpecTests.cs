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
    public void SpringRetargetOverNonVectorMixerRecordsFallbackDiagnostic()
    {
        MotionDiagnostics diagnostics = new();
        SpringSpec<float> spec = MotionFactory.Spring<float>();
        MotionSampler<float> sampler = spec.CreateSampler(0, 100, new NonVectorFloatMixer(), Context(diagnostics));

        sampler.Retarget(50, RetargetMode.Restart);

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => sampler.Velocity);
        Assert.Contains("velocity", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(diagnostics.Warnings, warning => warning.Contains("velocity", StringComparison.OrdinalIgnoreCase));
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
}

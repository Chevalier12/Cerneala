using System.Numerics;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;
using Xunit.Abstractions;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.UI.Motion.Core;

public sealed class SpringRetargetTests(ITestOutputHelper output)
{
    [Fact]
    public void AnimateToPreservesActiveSpringVelocityByDefault()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        MotionValue<float> value = root.Motion.Graph.CreateValue(0f);
        MotionHandle oldHandle = value.AnimateTo(100f, MotionFactory.Spring<float>());
        root.Motion.Tick();
        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.Motion.Tick();
        float positionBefore = value.Current;
        float velocityBefore = value.Velocity!.Value.Value;
        Assert.True(velocityBefore > 0f);

        MotionHandle newHandle = value.AnimateTo(200f, MotionFactory.Spring<float>());
        Assert.Equal(positionBefore, value.Current);
        clock.Advance(TimeSpan.FromMilliseconds(1));
        root.Motion.Tick();
        float velocityAfter = value.Velocity!.Value.Value;

        // A fresh spring that starts with the retained position and velocity, advanced 1 ms.
        float expectedVelocity = AdvanceFreshSpring(
            MotionFactory.Spring<float>(), new FloatMixer(), positionBefore, 200f, velocityBefore).Velocity!.Value.Value;
        output.WriteLine($"Before={velocityBefore:R}, after={velocityAfter:R}, expected={expectedVelocity:R}");
        Assert.True(MathF.Abs(expectedVelocity - velocityAfter) < 0.001f,
            $"Before={velocityBefore:R}, after={velocityAfter:R}, expected={expectedVelocity:R}");
        Assert.True(oldHandle.IsCanceled);
        Assert.True(newHandle.IsActive);
        Assert.Equal(200f, value.Target);
    }

    [Theory]
    [InlineData(RetargetMode.Restart, SpringVelocityMode.Preserve)]
    [InlineData(RetargetMode.PreserveProgress, SpringVelocityMode.Preserve)]
    [InlineData(RetargetMode.Restart, SpringVelocityMode.Reset)]
    [InlineData(RetargetMode.PreserveProgress, SpringVelocityMode.Reset)]
    public void RetargetUsesIncomingParametersAndVelocityModeWithoutReplayingElapsedTime(
        RetargetMode mode, SpringVelocityMode velocityMode)
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        MotionValue<float> value = root.Motion.Graph.CreateValue(0f);
        // The incoming mode, not the outgoing mode, decides whether to retain velocity.
        SpringVelocityMode outgoingMode = velocityMode == SpringVelocityMode.Preserve
            ? SpringVelocityMode.Reset : SpringVelocityMode.Preserve;
        MotionHandle oldHandle = value.AnimateTo(100f,
            MotionFactory.Spring<float>().WithVelocityMode(outgoingMode));
        root.Motion.Tick();
        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.Motion.Tick();
        float positionBefore = value.Current;
        float velocityBefore = value.Velocity!.Value.Value;
        float initialVelocity = velocityMode == SpringVelocityMode.Preserve ? velocityBefore : 0f;

        MotionHandle newHandle = value.AnimateTo(200f,
            MotionFactory.Spring<float>(stiffness: 100, damping: 7, mass: 2)
                .WithVelocityMode(velocityMode), new(mode));

        Assert.Equal(positionBefore, value.Current);
        Assert.NotNull(value.Velocity);
        Assert.Equal(initialVelocity, value.Velocity!.Value.Value);
        Assert.True(oldHandle.IsCanceled);
        Assert.True(newHandle.IsActive);
        // Repeated retargets without a tick must also carry the actual motion state.
        newHandle = value.AnimateTo(200f,
            MotionFactory.Spring<float>(stiffness: 100, damping: 7, mass: 2)
                .WithVelocityMode(velocityMode), new(mode));
        Assert.Equal(initialVelocity, value.Velocity!.Value.Value);
        clock.Advance(TimeSpan.FromMilliseconds(1));
        root.Motion.Tick();

        MotionSampler<float> expected = AdvanceFreshSpring(
            MotionFactory.Spring<float>(stiffness: 100, damping: 7, mass: 2), new FloatMixer(),
            positionBefore, 200f, initialVelocity);
        Assert.Equal(expected.Velocity!.Value.Value, value.Velocity!.Value.Value, precision: 3);
        Assert.Equal(expected.Current, value.Current, precision: 4);
        Assert.True(newHandle.IsActive);
    }

    [Theory]
    [InlineData(RetargetMode.Restart)]
    [InlineData(RetargetMode.PreserveProgress)]
    public void RetargetPreservesVectorVelocity(RetargetMode mode)
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        MotionValue<Vector4> value = root.Motion.Graph.CreateValue(Vector4.Zero);
        value.AnimateTo(new(100f, -50f, 30f, -10f), MotionFactory.Spring<Vector4>());
        root.Motion.Tick();
        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.Motion.Tick();
        Vector4 positionBefore = value.Current;
        Vector4 velocityBefore = value.Velocity!.Value.Value;
        Vector4 target = new(-200f, 100f, -60f, 20f);

        value.AnimateTo(target, MotionFactory.Spring<Vector4>(100, 7, 2), new(mode));

        Assert.Equal(positionBefore, value.Current);
        Assert.NotNull(value.Velocity);
        Assert.Equal(velocityBefore, value.Velocity!.Value.Value);
        clock.Advance(TimeSpan.FromMilliseconds(1));
        root.Motion.Tick();
        MotionSampler<Vector4> expected = AdvanceFreshSpring(
            MotionFactory.Spring<Vector4>(100, 7, 2), new Vector4Mixer(), positionBefore, target, velocityBefore);
        Assert.True(Vector4.Distance(expected.Velocity!.Value.Value, value.Velocity!.Value.Value) < 0.001f);
        Assert.True(Vector4.Distance(expected.Current, value.Current) < 0.0001f);
    }

    [Fact]
    public void CancellationCallbackChangingPositionDoesNotResurrectOldSpringState()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        MotionValue<float> value = root.Motion.Graph.CreateValue(0f);
        MotionHandle oldHandle = value.AnimateTo(100f, MotionFactory.Spring<float>());
        root.Motion.Tick();
        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.Motion.Tick();
        oldHandle.Completed += (_, _) => value.JumpTo(-20f);

        MotionHandle newHandle = value.AnimateTo(200f, MotionFactory.Spring<float>());

        Assert.Equal(-20f, value.Current);
        clock.Advance(TimeSpan.FromMilliseconds(1));
        root.Motion.Tick();
        // The new spring starts at rest from -20, not from the old spring's position and velocity.
        MotionSampler<float> expected = AdvanceFreshSpring(
            MotionFactory.Spring<float>(), new FloatMixer(), -20f, 200f, 0f);
        Assert.Equal(expected.Velocity!.Value.Value, value.Velocity!.Value.Value, precision: 3);
        Assert.True(newHandle.IsActive);
    }

    // A fresh spring that starts with the given position and velocity is the retarget oracle.
    private static MotionSampler<T> AdvanceFreshSpring<T>(SpringSpec<T> spec, ValueMixer<T> mixer, T from, T to, T velocity)
    {
        SpringSpec<T>.VectorSpringSampler sampler = new(
            spec, from, to, mixer,
            new MotionSpecContext(ReducedMotionPolicy.Default, new ValueMixerRegistry(), null, TimeSpan.Zero),
            new MotionVelocity<T>(velocity));
        sampler.Advance(TimeSpan.FromMilliseconds(1));
        return sampler;
    }
}

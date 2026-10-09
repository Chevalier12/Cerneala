using Cerneala.Tests.UI.Motion.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Media;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.UI.Motion.Interpolation;

public sealed class TransformMixerDegenerateTests
{
    [Theory]
    [InlineData(0f, 0f, false)]
    [InlineData(0f, 0f, true)]
    [InlineData(0.0000005f, 0.0000005f, false)]
    [InlineData(0.0000005f, 0.0000005f, true)]
    [InlineData(0.0000005f, 1f, false)]
    [InlineData(0.0000005f, 1f, true)]
    [InlineData(1f, 0.0000005f, false)]
    [InlineData(1f, 0.0000005f, true)]
    public void RenderTransformAnimationCompletesFullFrameSequence(float scaleX, float scaleY, bool reverse)
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        UIElement element = new();
        root.VisualChildren.Add(element);
        TransformComponents visible = new(7, 11, 1, 1, MathF.PI / 6, 0, 0);
        TransformComponents collapsed = visible with { ScaleX = scaleX, ScaleY = scaleY };
        Transform from = TransformMixer.Compose(reverse ? collapsed : visible);
        Transform to = TransformMixer.Compose(reverse ? visible : collapsed);
        MotionValue<Transform> value = root.Motion.Graph.CreateValue(from);
        using MotionPropertyBinding<Transform> binding = new(root.Motion, element, UIElement.RenderTransformProperty, value);
        MotionHandle handle = binding.AnimateTo(
            to,
            MotionFactory.Tween<Transform>(TimeSpan.FromSeconds(1), Easings.Linear),
            new MotionPropertyStartOptions { HoldOnComplete = true });

        root.ProcessFrame();
        Assert.Same(from, element.RenderTransform);
        for (int frame = 1; frame <= 100; frame++)
        {
            clock.Advance(TimeSpan.FromMilliseconds(10));
            var stats = root.ProcessFrame();
            Assert.Equal(0, stats.MeasuredElements);
            Assert.Equal(0, stats.ArrangedElements);
            float progress = frame / 100f;
            if (reverse)
            {
                progress = 1 - progress;
            }

            Transform expected = TransformMixer.Compose(visible with
            {
                ScaleX = 1 + ((scaleX - 1) * progress),
                ScaleY = 1 + ((scaleY - 1) * progress)
            });
            AssertMatrixClose(expected, element.RenderTransform);
        }

        Assert.True(handle.IsCompleted);
        Assert.False(root.Motion.HasActiveMotion);
        Assert.Same(to, element.RenderTransform);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ZeroScaleInterpolationKeepsThirtyDegreeRotation(bool reverse)
    {
        TransformMixer mixer = new();
        Transform visible = TransformMixer.Compose(new(7, 11, 1, 1, MathF.PI / 6, 0, 0));
        Transform collapsed = TransformMixer.Compose(new(7, 11, 0, 0, 0, 0, 0));
        Transform from = reverse ? collapsed : visible;
        Transform to = reverse ? visible : collapsed;

        foreach (float progress in new[] { 0.01f, 0.25f, 0.5f, 0.75f, 0.99f })
        {
            Transform mixed = mixer.Mix(from, to, progress);
            TransformComponents components = TransformMixer.Decompose(mixed);
            float scale = reverse ? progress : 1 - progress;
            Assert.Equal(MathF.PI / 6, components.RotationRadians, precision: 5);
            AssertMatrixClose(TransformMixer.Compose(new(7, 11, scale, scale, MathF.PI / 6, 0, 0)), mixed);
        }
    }

    [Theory]
    [InlineData(0f, 2f, false)]
    [InlineData(0f, 2f, true)]
    [InlineData(2f, 0f, false)]
    [InlineData(2f, 0f, true)]
    [InlineData(-0.0000005f, 3f, false)]
    [InlineData(-0.0000005f, 3f, true)]
    [InlineData(3f, -0.0000005f, false)]
    [InlineData(3f, -0.0000005f, true)]
    public void BorrowedFrameSolvesSignedScalesAndPreservesSkew(float scaleX, float scaleY, bool reverse)
    {
        TransformMixer mixer = new();
        TransformComponents valid = new(7, 11, 2, 3, MathF.PI / 6, 0.4f, 0);
        TransformComponents degenerate = valid with
        {
            TranslationX = 13,
            TranslationY = -19,
            ScaleX = scaleX,
            ScaleY = scaleY
        };
        Transform from = TransformMixer.Compose(reverse ? degenerate : valid);
        Transform to = TransformMixer.Compose(reverse ? valid : degenerate);

        Assert.True(TransformMixer.TryResolveComponents(from, to, out TransformComponents resolvedFrom, out TransformComponents resolvedTo));
        TransformComponents resolved = reverse ? resolvedFrom : resolvedTo;
        Assert.Equal(scaleX, resolved.ScaleX, precision: 6);
        Assert.Equal(scaleY, resolved.ScaleY, precision: 6);
        Assert.Equal(valid.RotationRadians, resolved.RotationRadians, precision: 6);
        Assert.Equal(valid.SkewX, resolved.SkewX, precision: 6);
        Assert.Equal(0, resolved.SkewY);
        Assert.Equal(degenerate.TranslationX, resolved.TranslationX);
        Assert.Equal(degenerate.TranslationY, resolved.TranslationY);
        AssertMatrixClose(from, TransformMixer.Compose(resolvedFrom));
        AssertMatrixClose(to, TransformMixer.Compose(resolvedTo));

        foreach (float progress in new[] { 0.001f, 0.25f, 0.5f, 0.75f, 0.999f })
        {
            float amount = reverse ? 1 - progress : progress;
            Transform expected = TransformMixer.Compose(valid with
            {
                TranslationX = 7 + (6 * amount),
                TranslationY = 11 - (30 * amount),
                ScaleX = 2 + ((scaleX - 2) * amount),
                ScaleY = 3 + ((scaleY - 3) * amount)
            });
            AssertMatrixClose(expected, mixer.Mix(from, to, progress));
        }
    }

    [Fact]
    public void BothDegenerateEndpointsUseMatrixInterpolation()
    {
        TransformMixer mixer = new();
        Transform from = new(new Matrix3x2(0, 0, 1, 2, 7, 11));
        Transform to = new(new Matrix3x2(3, 4, 0, 0, 13, -19));

        Assert.False(TransformMixer.TryResolveComponents(from, to, out _, out _));
        AssertMatrixInterpolation(mixer, from, to);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IncompatibleCollapseAxisUsesMatrixInterpolation(bool reverse)
    {
        TransformMixer mixer = new();
        Transform valid = TransformMixer.Compose(new(7, 11, 2, 3, MathF.PI / 6, 0.4f, 0));
        Transform degenerate = TransformMixer.Compose(new(13, -19, 0, 2, MathF.PI / 2, 0, 0));

        Assert.False(TransformMixer.TryResolveComponents(valid, degenerate, out _, out _));
        Assert.False(TransformMixer.TryResolveComponents(degenerate, valid, out _, out _));
        AssertMatrixInterpolation(mixer, reverse ? degenerate : valid, reverse ? valid : degenerate);
    }

    [Fact]
    public void SingularShearOutsideBorrowedFrameUsesMatrixInterpolation()
    {
        TransformMixer mixer = new();
        Transform degenerate = new(new Matrix3x2(1, 0, 2, 0, 7, 11));

        AssertMatrixInterpolation(mixer, Transform.Identity, degenerate);
    }

    [Theory]
    [InlineData(0.000001f, 1f)]
    [InlineData(1f, 0.000001f)]
    [InlineData(1f, -0.000001f)]
    public void ExistingDecompositionThresholdIsHandledInclusively(float scaleX, float scaleY)
    {
        TransformMixer mixer = new();
        Transform degenerate = new(Matrix3x2.CreateScale(scaleX, scaleY));

        Assert.Throws<InvalidOperationException>(() => TransformMixer.Decompose(degenerate));
        AssertMatrixClose(
            new Transform(Matrix3x2.CreateScale((1 + scaleX) / 2, (1 + scaleY) / 2)),
            mixer.Mix(Transform.Identity, degenerate, 0.5f));
    }

    [Theory]
    [InlineData(TransformInterpolationMode.Components)]
    [InlineData(TransformInterpolationMode.Matrix)]
    public void DegenerateEndpointsRemainExact(TransformInterpolationMode mode)
    {
        TransformMixer mixer = new(mode);
        Transform from = new(Matrix3x2.CreateScale(0, 0));
        Transform to = new(Matrix3x2.CreateScale(0, 2));

        Assert.Same(from, mixer.Mix(from, to, -1));
        Assert.Same(from, mixer.Mix(from, to, 0));
        Assert.Same(to, mixer.Mix(from, to, 1));
        Assert.Same(to, mixer.Mix(from, to, 2));
    }

    [Fact]
    public void NonDegenerateEndpointsStillUseComponentRotation()
    {
        TransformMixer mixer = new();
        Transform to = new(Matrix3x2.CreateRotation(MathF.PI / 2));

        AssertMatrixClose(new Transform(Matrix3x2.CreateRotation(MathF.PI / 4)), mixer.Mix(Transform.Identity, to, 0.5f));
    }

    [Fact]
    public void ExplicitMatrixModeStillInterpolatesDegeneratePairsDirectly()
    {
        TransformMixer mixer = new(TransformInterpolationMode.Matrix);
        Transform from = new(Matrix3x2.CreateRotation(MathF.PI / 6));
        Transform to = new(new Matrix3x2(0, 0, 2, 0, 7, 11));

        AssertMatrixInterpolation(mixer, from, to);
    }

    private static void AssertMatrixInterpolation(TransformMixer mixer, Transform from, Transform to)
    {
        foreach (float progress in new[] { 0.001f, 0.25f, 0.5f, 0.75f, 0.999f })
        {
            Matrix3x2 a = from.Matrix;
            Matrix3x2 b = to.Matrix;
            Transform expected = new(new Matrix3x2(
                a.M11 + ((b.M11 - a.M11) * progress),
                a.M12 + ((b.M12 - a.M12) * progress),
                a.M21 + ((b.M21 - a.M21) * progress),
                a.M22 + ((b.M22 - a.M22) * progress),
                a.M31 + ((b.M31 - a.M31) * progress),
                a.M32 + ((b.M32 - a.M32) * progress)));
            AssertMatrixClose(expected, mixer.Mix(from, to, progress));
        }
    }

    private static void AssertMatrixClose(Transform expected, Transform actual)
    {
        Assert.InRange(MathF.Abs(expected.Matrix.M11 - actual.Matrix.M11), 0, 0.00001f);
        Assert.InRange(MathF.Abs(expected.Matrix.M12 - actual.Matrix.M12), 0, 0.00001f);
        Assert.InRange(MathF.Abs(expected.Matrix.M21 - actual.Matrix.M21), 0, 0.00001f);
        Assert.InRange(MathF.Abs(expected.Matrix.M22 - actual.Matrix.M22), 0, 0.00001f);
        Assert.InRange(MathF.Abs(expected.Matrix.M31 - actual.Matrix.M31), 0, 0.00001f);
        Assert.InRange(MathF.Abs(expected.Matrix.M32 - actual.Matrix.M32), 0, 0.00001f);
    }
}

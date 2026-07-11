using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Motion.Interpolation;

namespace Cerneala.Tests.UI.Motion.Interpolation;

public sealed class ValueMixerBuiltInTests
{
    [Fact]
    public void BuiltInMixersReturnExactEndpoints()
    {
        AssertEndpoints(new FloatMixer(), 1.5f, 9.5f);
        AssertEndpoints(new DoubleMixer(), 1.5d, 9.5d);
        AssertEndpoints(new ColorMixer(), new Color(1, 2, 3, 4), new Color(250, 251, 252, 253));
        AssertEndpoints(new ThicknessMixer(), new Thickness(1, 2, 3, 4), new Thickness(5, 6, 7, 8));
        AssertEndpoints(new DrawPointMixer(), new DrawPoint(1, 2), new DrawPoint(5, 6));
        AssertEndpoints(new DrawSizeMixer(), new DrawSize(1, 2), new DrawSize(5, 6));
        AssertEndpoints(new DrawRectMixer(), new DrawRect(1, 2, 3, 4), new DrawRect(5, 6, 7, 8));
        AssertEndpoints(new TransformMixer(), Transform.Identity, new Transform(Matrix3x2.CreateTranslation(10, 20)));
    }

    [Fact]
    public void NumericAndVectorMixersReturnExactEndpointsForLargeValues()
    {
        AssertExactEndpoints(new FloatMixer(), 1e20f, 1f);
        AssertExactEndpoints(new DoubleMixer(), 1e20d, 1d);
        AssertExactEndpoints(
            new ThicknessMixer(),
            new Thickness(1e20f, 2e20f, 3e20f, 4e20f),
            new Thickness(1, 2, 3, 4));
        AssertExactEndpoints(
            new DrawPointMixer(),
            new DrawPoint(1e20f, 2e20f),
            new DrawPoint(1, 2));
        AssertExactEndpoints(
            new DrawSizeMixer(),
            new DrawSize(1e20f, 2e20f),
            new DrawSize(1, 2));
    }

    [Fact]
    public void ColorInterpolationHandlesAlpha()
    {
        ColorMixer mixer = new();

        Color mixed = mixer.Mix(new Color(0, 10, 20, 0), new Color(100, 110, 120, 200), 0.5f);

        Assert.Equal(new Color(50, 60, 70, 100), mixed);
    }

    [Fact]
    public void ThicknessInterpolationHandlesEachEdge()
    {
        ThicknessMixer mixer = new();

        Thickness mixed = mixer.Mix(new Thickness(0, 10, 20, 30), new Thickness(100, 110, 120, 130), 0.25f);

        Assert.Equal(new Thickness(25, 35, 45, 55), mixed);
    }

    [Fact]
    public void RectInterpolationHandlesPositionAndSize()
    {
        DrawRectMixer mixer = new();

        DrawRect mixed = mixer.Mix(new DrawRect(0, 10, 20, 30), new DrawRect(100, 110, 120, 130), 0.25f);

        Assert.Equal(new DrawRect(25, 35, 45, 55), mixed);
    }

    [Fact]
    public void TransformInterpolationPreservesIdentityEndpoints()
    {
        TransformMixer mixer = new();
        Transform target = new(Matrix3x2.CreateTranslation(10, 20));

        AssertSameMatrix(Transform.Identity, mixer.Mix(Transform.Identity, target, 0));
        AssertSameMatrix(target, mixer.Mix(Transform.Identity, target, 1));
    }

    [Fact]
    public void TransformInterpolationUsesComponentsByDefault()
    {
        TransformMixer mixer = new();
        Transform to = new(Matrix3x2.CreateTranslation(10, 20));

        Transform mixed = mixer.Mix(Transform.Identity, to, 0.5f);

        Assert.Equal(5, mixed.Matrix.M31, precision: 3);
        Assert.Equal(10, mixed.Matrix.M32, precision: 3);
        Assert.Equal(1, mixed.Matrix.M11, precision: 3);
        Assert.Equal(1, mixed.Matrix.M22, precision: 3);
    }

    [Fact]
    public void MatrixTransformInterpolationRequiresExplicitMode()
    {
        TransformMixer mixer = new(TransformInterpolationMode.Matrix);
        Transform from = new(new Matrix3x2(1, 2, 3, 4, 5, 6));
        Transform to = new(new Matrix3x2(11, 12, 13, 14, 15, 16));

        Transform mixed = mixer.Mix(from, to, 0.5f);

        Assert.Equal(6, mixed.Matrix.M11, precision: 3);
        Assert.Equal(7, mixed.Matrix.M12, precision: 3);
        Assert.Equal(8, mixed.Matrix.M21, precision: 3);
        Assert.Equal(9, mixed.Matrix.M22, precision: 3);
        Assert.Equal(10, mixed.Matrix.M31, precision: 3);
        Assert.Equal(11, mixed.Matrix.M32, precision: 3);
    }

    [Fact]
    public void TransformComponentsRoundTripScaleAndRotation()
    {
        Transform transform = TransformMixer.Compose(new TransformComponents(
            TranslationX: 7,
            TranslationY: 11,
            ScaleX: 2,
            ScaleY: 3,
            RotationRadians: 0.75f,
            SkewX: 0,
            SkewY: 0));

        Transform roundTripped = TransformMixer.Compose(TransformMixer.Decompose(transform));

        AssertSameMatrix(transform, roundTripped);
    }

    [Fact]
    public void TransformComponentsRoundTripSkewWithNonUniformScale()
    {
        Transform transform = TransformMixer.Compose(new TransformComponents(
            TranslationX: 0,
            TranslationY: 0,
            ScaleX: 2,
            ScaleY: 3,
            RotationRadians: 0,
            SkewX: 0.5f,
            SkewY: 0));

        TransformComponents components = TransformMixer.Decompose(transform);
        Transform roundTripped = TransformMixer.Compose(components);

        Assert.Equal(0.5f, components.SkewX, precision: 3);
        Assert.Equal(0, components.SkewY, precision: 3);
        AssertSameMatrix(transform, roundTripped);
    }

    [Fact]
    public void TransformDecomposeReturnsCanonicalZeroSkewY()
    {
        Transform transform = TransformMixer.Compose(new TransformComponents(
            TranslationX: 0,
            TranslationY: 0,
            ScaleX: 2,
            ScaleY: 3,
            RotationRadians: 0.25f,
            SkewX: 0.1f,
            SkewY: 0.2f));

        TransformComponents components = TransformMixer.Decompose(transform);

        Assert.Equal(0, components.SkewY, precision: 3);
        AssertSameMatrix(transform, TransformMixer.Compose(components));
    }

    [Fact]
    public void TransformDecomposeRejectsDegenerateScale()
    {
        InvalidOperationException scaleX = Assert.Throws<InvalidOperationException>(() =>
            TransformMixer.Decompose(TransformMixer.Compose(new TransformComponents(0, 0, 0, 2, 0, 0, 0))));
        InvalidOperationException scaleY = Assert.Throws<InvalidOperationException>(() =>
            TransformMixer.Decompose(TransformMixer.Compose(new TransformComponents(0, 0, 2, 0, 0, 0, 0))));

        Assert.Contains("ScaleX", scaleX.Message, StringComparison.Ordinal);
        Assert.Contains("ScaleY", scaleY.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RegistryMissingMixerExceptionIncludesPropertyAndType()
    {
        ValueMixerRegistry registry = new();

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            registry.Resolve<CustomValue>("FancyProperty"));

        Assert.Contains(nameof(CustomValue), ex.Message, StringComparison.Ordinal);
        Assert.Contains("FancyProperty", ex.Message, StringComparison.Ordinal);
        Assert.Contains("ValueMixer", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MotionSystemRegistersBuiltInMixers()
    {
        UIRoot root = new();

        Assert.IsType<FloatMixer>(root.Motion.Mixers.Resolve<float>());
        Assert.IsType<DoubleMixer>(root.Motion.Mixers.Resolve<double>());
        Assert.IsType<ColorMixer>(root.Motion.Mixers.Resolve<Color>());
        Assert.IsType<ThicknessMixer>(root.Motion.Mixers.Resolve<Thickness>());
        Assert.IsType<DrawPointMixer>(root.Motion.Mixers.Resolve<DrawPoint>());
        Assert.IsType<DrawSizeMixer>(root.Motion.Mixers.Resolve<DrawSize>());
        Assert.IsType<DrawRectMixer>(root.Motion.Mixers.Resolve<DrawRect>());
        Assert.IsType<TransformMixer>(root.Motion.Mixers.Resolve<Transform>());
    }

    [Fact]
    public void VectorMixersCompareWithinTolerance()
    {
        DrawPointMixer mixer = new();

        Assert.True(mixer.EqualsWithinTolerance(new DrawPoint(1, 2), new DrawPoint(1.01f, 2.01f), 0.02f));
        Assert.False(mixer.EqualsWithinTolerance(new DrawPoint(1, 2), new DrawPoint(1.1f, 2), 0.02f));
    }

    [Fact]
    public void DrawSizeMixerSupportsVectorOperations()
    {
        DrawSizeMixer mixer = new();

        Assert.True(mixer.SupportsVectorOperations);
        Assert.Equal(new DrawSize(4, 6), mixer.Add(new DrawSize(1, 2), new DrawSize(3, 4)));
        Assert.Equal(new DrawSize(2, 3), mixer.Subtract(new DrawSize(5, 7), new DrawSize(3, 4)));
        Assert.Equal(new DrawSize(4, 6), mixer.Scale(new DrawSize(2, 3), 2));
        Assert.Equal(5, mixer.Magnitude(new DrawSize(3, 4)), precision: 3);
    }

    private static void AssertEndpoints<T>(ValueMixer<T> mixer, T from, T to)
    {
        Assert.Equal(from, mixer.Mix(from, to, 0));
        Assert.Equal(to, mixer.Mix(from, to, 1));
    }

    private static void AssertExactEndpoints<T>(ValueMixer<T> mixer, T from, T to)
    {
        Assert.Equal(from, mixer.Mix(from, to, 0));
        Assert.Equal(from, mixer.Mix(from, to, -0.5f));
        Assert.Equal(to, mixer.Mix(from, to, 1));
        Assert.Equal(to, mixer.Mix(from, to, 1.5f));
    }

    private static void AssertSameMatrix(Transform expected, Transform actual)
    {
        Assert.Equal(expected.Matrix.M11, actual.Matrix.M11, precision: 3);
        Assert.Equal(expected.Matrix.M12, actual.Matrix.M12, precision: 3);
        Assert.Equal(expected.Matrix.M21, actual.Matrix.M21, precision: 3);
        Assert.Equal(expected.Matrix.M22, actual.Matrix.M22, precision: 3);
        Assert.Equal(expected.Matrix.M31, actual.Matrix.M31, precision: 3);
        Assert.Equal(expected.Matrix.M32, actual.Matrix.M32, precision: 3);
    }

    private readonly record struct CustomValue(float X);
}

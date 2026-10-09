using Cerneala.Tests.UI.Motion.Core;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.Tests.UI.Motion.Properties;

public sealed class MotionOvershootPropertyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NegativeWidthSampleIsSkippedAndAnimationContinuesToTarget(bool tween)
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        root.Detective.Motion.IsEnabled = true;
        Control control = new();
        control.SetValue(UIElement.WidthProperty, tween ? 0 : 100, UiPropertyValueSource.AspectBase);
        root.VisualChildren.Add(control);
        MotionValue<float> value = root.Motion.Graph.CreateValue(control.Width);
        using MotionPropertyBinding<float> binding = new(root.Motion, control, UIElement.WidthProperty, value);
        MotionSpec<float> spec = tween
            ? new TweenSpec<float>(TimeSpan.FromSeconds(1), new CubicBezierEasing(0.36f, 0, 0.66f, -0.56f))
            : new SpringSpec<float>(stiffness: 100, damping: 10);

        MotionHandle handle = binding.AnimateTo(tween ? 100 : 0, spec, new MotionPropertyStartOptions { HoldOnComplete = true });
        root.ProcessFrame();

        int skipped = 0;
        int validAfterSkip = 0;
        for (int frame = 0; frame < 600 && !handle.IsCompleted; frame++)
        {
            float lastValidWidth = control.Width;
            clock.Advance(TimeSpan.FromMilliseconds(20));
            root.ProcessFrame();
            if (value.Current < 0)
            {
                skipped++;
                Assert.Equal(lastValidWidth, control.Width);
                Assert.True(handle.IsActive);
                Assert.Contains(root.Detective.Motion.Warnings, warning => warning.Contains("Width", StringComparison.Ordinal));
            }
            else
            {
                Assert.Equal(value.Current, control.Width);
                if (skipped > 0)
                {
                    validAfterSkip++;
                }
            }
        }

        Assert.True(skipped > 0);
        Assert.True(validAfterSkip > 0);
        Assert.True(handle.IsCompleted);
        Assert.Equal(tween ? 100 : 0, control.Width);
    }

    [Theory]
    [InlineData(RetargetMode.Restart)]
    [InlineData(RetargetMode.PreserveProgress)]
    public void InvalidTargetIsRejectedBeforeStartingOrReplacingAnAnimation(RetargetMode mode)
    {
        UIRoot root = new();
        Control control = new() { Width = 0 };
        root.VisualChildren.Add(control);
        MotionValue<float> value = root.Motion.Graph.CreateValue(0f);
        using MotionPropertyBinding<float> binding = new(root.Motion, control, UIElement.WidthProperty, value);
        TweenSpec<float> spec = new(TimeSpan.FromSeconds(1));
        MotionPropertyStartOptions options = new() { RetargetMode = mode };

        Assert.Throws<ArgumentException>(() => binding.AnimateTo(-1, spec, options));
        Assert.False(value.IsAnimating);

        MotionHandle valid = binding.AnimateTo(100, spec);
        Assert.Throws<ArgumentException>(() => binding.AnimateTo(-1, spec, options));
        Assert.True(valid.IsActive);
        Assert.Equal(100, value.Target);
    }

    [Fact]
    public void BoundValueCannotBypassTargetValidationAndDisposalRemovesValidation()
    {
        UIRoot root = new();
        Control control = new();
        root.VisualChildren.Add(control);
        MotionValue<float> value = root.Motion.Graph.CreateValue(0f);
        MotionPropertyBinding<float> binding = new(root.Motion, control, UIElement.WidthProperty, value);
        TweenSpec<float> spec = new(TimeSpan.FromSeconds(1));

        Assert.Throws<ArgumentException>(() => value.AnimateTo(-1, spec));
        Assert.Throws<ArgumentException>(() => value.JumpTo(-1));

        binding.Dispose();
        value.JumpTo(-1);
        Assert.Equal(-1, value.Current);
    }

    [Fact]
    public void AnimatedWritesCoerceBeforeValidatingExactlyOnceAndExplicitWritesStillReject()
    {
        int coercions = 0;
        int validations = 0;
        UiProperty<float> property = UiProperty<float>.Register(
            nameof(AnimatedWritesCoerceBeforeValidatingExactlyOnceAndExplicitWritesStillReject),
            typeof(Control),
            new UiPropertyMetadata<float>(0,
                coerceValue: (_, sample) => { coercions++; return MathF.Max(0, sample); },
                validateValue: sample => { validations++; return sample >= 0 && sample <= 100; }));
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        Control control = new();
        root.VisualChildren.Add(control);
        MotionValue<float> value = root.Motion.Graph.CreateValue(0f);
        using MotionPropertyBinding<float> binding = new(root.Motion, control, property, value);
        binding.AnimateTo(100, new TweenSpec<float>(TimeSpan.FromSeconds(1), new CubicBezierEasing(0.36f, 0, 0.66f, -0.56f)));
        root.ProcessFrame();
        coercions = validations = 0;

        clock.Advance(TimeSpan.FromMilliseconds(20));
        root.ProcessFrame();

        Assert.True(value.Current < 0);
        Assert.Equal(0, control.GetValue(property));
        Assert.Equal(1, coercions);
        Assert.Equal(1, validations);
        Assert.Throws<ArgumentException>(() => control.SetValue(property, 101));
        Assert.Equal(0, control.GetValue(property));
    }

    [Fact]
    public void OwnerValidationRejectionAlsoSkipsAnAnimatedWrite()
    {
        CircleCollider2D collider = new();

        Assert.False(collider.TrySetAnimationValueUntyped(CircleCollider2D.RadiusProperty, -1f));
        Assert.Equal(1, collider.Radius);
        Assert.True(collider.TrySetAnimationValueUntyped(CircleCollider2D.RadiusProperty, 2f));
        Assert.Equal(2, collider.Radius);
        Assert.Throws<ArgumentOutOfRangeException>(() => collider.Radius = -1);

        UIRoot root = new();
        using MotionPropertyBinding<float> binding = new(
            root.Motion, collider, CircleCollider2D.RadiusProperty, root.Motion.Graph.CreateValue(2f));
        Assert.Throws<ArgumentOutOfRangeException>(() => binding.AnimateTo(-1, new TweenSpec<float>(TimeSpan.FromSeconds(1))));
    }

    [Fact]
    public void AnimatedWriteDoesNotSwallowValidatorOrChangeHandlerExceptions()
    {
        UiProperty<float> property = UiProperty<float>.Register(
            nameof(AnimatedWriteDoesNotSwallowValidatorOrChangeHandlerExceptions), typeof(UiObject),
            new UiPropertyMetadata<float>(0, validateValue: sample =>
                sample == 100 ? throw new ArgumentException("validator failure") : true));
        UiObject target = new();

        ArgumentException validator = Assert.Throws<ArgumentException>(() => target.TrySetAnimationValueUntyped(property, 100f));
        Assert.Equal("validator failure", validator.Message);
        Assert.Equal(0, target.GetValue(property));

        target.PropertyChanged += (_, _) => throw new ArgumentException("handler failure");
        ArgumentException handler = Assert.Throws<ArgumentException>(() => target.TrySetAnimationValueUntyped(property, 1f));
        Assert.Equal("handler failure", handler.Message);
        Assert.Equal(1, target.GetValue(property));
    }
}

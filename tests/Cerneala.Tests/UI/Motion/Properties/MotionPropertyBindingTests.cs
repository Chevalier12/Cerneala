using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Media;
using Cerneala.Tests.UI.Motion.Core;
using Cerneala.UI.Motion.Properties;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.UI.Motion.Properties;

public sealed class MotionPropertyBindingTests
{
    [Fact]
    public void AnimatableRegistryRegistersPhase6BuiltInsAndDefersPhase7Properties()
    {
        AnimatablePropertyRegistry registry = new();

        Assert.True(registry.TryGet(Control.BackgroundProperty, out MotionPropertyOptions? background));
        Assert.True(background.IsSafeForImplicitAnimation);
        Assert.True(background.InvalidationCategory.HasFlag(MotionPropertyInvalidationCategory.Render));

        Assert.True(registry.TryGet(Control.ForegroundProperty, out MotionPropertyOptions? foreground));
        Assert.Equal(typeof(Cerneala.UI.Motion.Interpolation.BrushMixer), foreground.MixerType);
        Assert.True(foreground.IsSafeForImplicitAnimation);
        Assert.True(foreground.InvalidationCategory.HasFlag(MotionPropertyInvalidationCategory.Render));

        Assert.True(registry.TryGet(Control.BorderBrushProperty, out MotionPropertyOptions? borderBrush));
        Assert.True(borderBrush.IsSafeForImplicitAnimation);

        Assert.True(registry.TryGet(Control.BorderThicknessProperty, out MotionPropertyOptions? borderThickness));
        Assert.False(borderThickness.IsSafeForImplicitAnimation);
        Assert.True(borderThickness.InvalidationCategory.HasFlag(MotionPropertyInvalidationCategory.Layout));

        Assert.True(registry.TryGet(Control.PaddingProperty, out MotionPropertyOptions? padding));
        Assert.True(padding.InvalidationCategory.HasFlag(MotionPropertyInvalidationCategory.Layout));

        Assert.True(registry.TryGet(UIElement.MarginProperty, out MotionPropertyOptions? margin));
        Assert.True(margin.InvalidationCategory.HasFlag(MotionPropertyInvalidationCategory.Layout));

        Assert.Null(typeof(Control).GetField("BorderColorProperty"));
        Assert.True(registry.TryGet(UIElement.OpacityProperty, out MotionPropertyOptions? opacity));
        Assert.True(opacity.IsSafeForImplicitAnimation);
        Assert.True(opacity.InvalidationCategory.HasFlag(MotionPropertyInvalidationCategory.Render));

        Assert.True(registry.TryGet(UIElement.RenderTransformProperty, out MotionPropertyOptions? renderTransform));
        Assert.True(renderTransform.IsSafeForImplicitAnimation);
        Assert.True(renderTransform.InvalidationCategory.HasFlag(MotionPropertyInvalidationCategory.Render));
    }

    [Fact]
    public void BindingWritesAnimationSource()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        Control control = new();
        root.VisualChildren.Add(control);
        MotionValue<Brush?> value = root.Motion.Graph.CreateValue<Brush?>(new SolidColorBrush(Color.Black));
        using MotionPropertyBinding<Brush?> binding = new(root.Motion, control, Control.BackgroundProperty, value);

        binding.AnimateTo(new SolidColorBrush(Color.White), MotionFactory.Tween<Brush?>(TimeSpan.FromMilliseconds(100)));
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.ProcessFrame();

        Assert.Equal(UiPropertyValueSource.Animation, control.GetValueSource(Control.BackgroundProperty));
        Assert.NotEqual(new SolidColorBrush(Color.Black), control.Background);
    }

    [Fact]
    public void BindingClearsAnimationSourceOnCompletion()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        Control control = new();
        root.VisualChildren.Add(control);
        control.SetValue(Control.BackgroundProperty, new SolidColorBrush(Color.Black), UiPropertyValueSource.AspectBase);
        MotionValue<Brush?> value = root.Motion.Graph.CreateValue(control.Background);
        using MotionPropertyBinding<Brush?> binding = new(root.Motion, control, Control.BackgroundProperty, value);

        binding.AnimateTo(new SolidColorBrush(Color.White), MotionFactory.Tween<Brush?>(TimeSpan.FromMilliseconds(1)));
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(1));
        root.ProcessFrame();

        Assert.Equal(UiPropertyValueSource.AspectBase, control.GetValueSource(Control.BackgroundProperty));
        Assert.Equal(new SolidColorBrush(Color.Black), control.Background);
    }

    [Fact]
    public void BindingClearsAnimationSourceWhenSpecCompletesSynchronously()
    {
        UIRoot root = new(reducedMotion: new ReducedMotionPolicy(ReducedMotionMode.Reduce));
        Control control = new();
        root.VisualChildren.Add(control);
        control.SetValue(Control.BackgroundProperty, new SolidColorBrush(Color.Black), UiPropertyValueSource.AspectBase);
        MotionValue<Brush?> value = root.Motion.Graph.CreateValue(control.Background);
        using MotionPropertyBinding<Brush?> binding = new(root.Motion, control, Control.BackgroundProperty, value);

        MotionHandle handle = binding.AnimateTo(new SolidColorBrush(Color.White), MotionFactory.Tween<Brush?>(TimeSpan.FromMilliseconds(100)));
        root.ProcessFrame();

        Assert.True(handle.IsCompleted);
        Assert.Equal(UiPropertyValueSource.AspectBase, control.GetValueSource(Control.BackgroundProperty));
        Assert.Equal(new SolidColorBrush(Color.Black), control.Background);
    }

    [Fact]
    public void BindingRejectsMotionValueFromDifferentMotionSystem()
    {
        UIRoot targetRoot = new();
        UIRoot valueRoot = new();
        Control control = new();
        targetRoot.VisualChildren.Add(control);
        MotionValue<Brush?> foreignValue = valueRoot.Motion.Graph.CreateValue<Brush?>(new SolidColorBrush(Color.Black));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            new MotionPropertyBinding<Brush?>(targetRoot.Motion, control, Control.BackgroundProperty, foreignValue));

        Assert.Contains("same MotionSystem", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BindingClearFlushesRestoreBaseEvenAfterCancelingLastMotionNode()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        Control control = new();
        root.VisualChildren.Add(control);
        control.SetValue(Control.BackgroundProperty, new SolidColorBrush(Color.Black), UiPropertyValueSource.AspectBase);
        MotionValue<Brush?> value = root.Motion.Graph.CreateValue(control.Background);
        using MotionPropertyBinding<Brush?> binding = new(root.Motion, control, Control.BackgroundProperty, value);

        binding.AnimateTo(new SolidColorBrush(Color.White), MotionFactory.Tween<Brush?>(TimeSpan.FromMilliseconds(100)));
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.ProcessFrame();

        binding.Clear();
        root.ProcessFrame();

        Assert.Equal(UiPropertyValueSource.AspectBase, control.GetValueSource(Control.BackgroundProperty));
        Assert.Equal(new SolidColorBrush(Color.Black), control.Background);
    }

    [Fact]
    public void BindingSurvivesLocalSourceMasking()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        Control control = new();
        root.VisualChildren.Add(control);
        MotionValue<Brush?> value = root.Motion.Graph.CreateValue<Brush?>(new SolidColorBrush(Color.Black));
        using MotionPropertyBinding<Brush?> binding = new(root.Motion, control, Control.BackgroundProperty, value);

        binding.AnimateTo(new SolidColorBrush(Color.White), MotionFactory.Tween<Brush?>(TimeSpan.FromMilliseconds(100)));
        root.ProcessFrame();
        control.Background = new SolidColorBrush(Color.Black);
        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.ProcessFrame();

        Assert.Equal(UiPropertyValueSource.Local, control.GetValueSource(Control.BackgroundProperty));
        Assert.Equal(new SolidColorBrush(Color.Black), control.Background);

        control.ClearValue(Control.BackgroundProperty);

        Assert.Equal(UiPropertyValueSource.Animation, control.GetValueSource(Control.BackgroundProperty));
        Assert.NotEqual(new SolidColorBrush(Color.Black), control.Background);
    }

    [Fact]
    public void BindingDoesNotInvalidateWhenSampledValueEqualsEffectiveValue()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        Control control = new();
        root.VisualChildren.Add(control);
        control.SetValue(Control.BackgroundProperty, new SolidColorBrush(Color.Black), UiPropertyValueSource.AspectBase);
        root.ProcessFrame();
        MotionValue<Brush?> value = root.Motion.Graph.CreateValue<Brush?>(new SolidColorBrush(Color.Black));
        using MotionPropertyBinding<Brush?> binding = new(root.Motion, control, Control.BackgroundProperty, value);

        binding.AnimateTo(new SolidColorBrush(Color.Black), MotionFactory.Tween<Brush?>(TimeSpan.FromMilliseconds(100)));
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(16));
        FrameStats stats = root.ProcessFrame();

        Assert.Equal(0, stats.RenderedElements);
        Assert.Equal(0, stats.MeasuredElements);
        Assert.Equal(0, stats.ArrangedElements);
    }

    [Fact]
    public void RenderOnlyBindingDoesNotEnqueueMeasureOrArrange()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        Control control = new();
        root.VisualChildren.Add(control);
        root.ProcessFrame();
        MotionValue<Brush?> value = root.Motion.Graph.CreateValue<Brush?>(new SolidColorBrush(Color.Black));
        using MotionPropertyBinding<Brush?> binding = new(root.Motion, control, Control.BackgroundProperty, value);

        binding.AnimateTo(new SolidColorBrush(Color.White), MotionFactory.Tween<Brush?>(TimeSpan.FromMilliseconds(100)));
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(50));
        FrameStats stats = root.ProcessFrame();

        Assert.Equal(0, stats.MeasuredElements);
        Assert.Equal(0, stats.ArrangedElements);
        Assert.True(stats.RenderedElements > 0);
    }

    [Fact]
    public void LayoutAffectingBindingEnqueuesMeasureAndArrangeOnlyWhenValueChanges()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        Control control = new();
        root.VisualChildren.Add(control);
        root.ProcessFrame();
        MotionValue<Thickness> value = root.Motion.Graph.CreateValue(Thickness.Zero);
        using MotionPropertyBinding<Thickness> binding = new(root.Motion, control, Control.PaddingProperty, value);

        binding.AnimateTo(new Thickness(8), MotionFactory.Tween<Thickness>(TimeSpan.FromMilliseconds(100)));
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(50));
        FrameStats changed = root.ProcessFrame();
        clock.Advance(TimeSpan.Zero);
        FrameStats unchanged = root.ProcessFrame();

        Assert.True(changed.MeasuredElements > 0);
        Assert.True(changed.ArrangedElements > 0);
        Assert.Equal(0, unchanged.MeasuredElements);
        Assert.Equal(0, unchanged.ArrangedElements);
    }

    [Fact]
    public void DetachedTargetCancelsAndClearsAnimationSource()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        Control control = new();
        root.VisualChildren.Add(control);
        MotionValue<Brush?> value = root.Motion.Graph.CreateValue<Brush?>(new SolidColorBrush(Color.Black));
        using MotionPropertyBinding<Brush?> binding = new(root.Motion, control, Control.BackgroundProperty, value);

        MotionHandle handle = binding.AnimateTo(new SolidColorBrush(Color.White), MotionFactory.Tween<Brush?>(TimeSpan.FromMilliseconds(100)));
        root.ProcessFrame();
        root.VisualChildren.Remove(control);
        clock.Advance(TimeSpan.FromMilliseconds(16));
        root.ProcessFrame();

        Assert.True(handle.IsCanceled);
        Assert.NotEqual(UiPropertyValueSource.Animation, control.GetValueSource(Control.BackgroundProperty));
    }
}

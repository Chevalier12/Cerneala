using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Media;
using Cerneala.Tests.UI.Motion.Core;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.UI.Motion;

public sealed class MotionFacadeTests
{
    [Fact]
    public void FacadeCreatesOneBindingPerElementProperty()
    {
        UIRoot root = new();
        Control control = new();
        root.VisualChildren.Add(control);

        control.Motion()
            .Animate(Control.BackgroundProperty)
            .To(new SolidColorBrush(Color.White))
            .With(MotionFactory.Tween<Brush?>(TimeSpan.FromMilliseconds(100)));

        Assert.Equal(1, root.Motion.Properties.BindingCount);
    }

    [Fact]
    public void FacadeReusesExistingBindingOnRepeatedCalls()
    {
        UIRoot root = new();
        Control control = new();
        root.VisualChildren.Add(control);

        control.Motion().Animate(Control.BackgroundProperty).To(new SolidColorBrush(Color.White)).With(MotionFactory.Tween<Brush?>(TimeSpan.FromMilliseconds(100)));
        control.Motion().Animate(Control.BackgroundProperty).To(new SolidColorBrush(Color.Black)).With(MotionFactory.Tween<Brush?>(TimeSpan.FromMilliseconds(100)));

        Assert.Equal(1, root.Motion.Properties.BindingCount);
    }

    [Fact]
    public void FacadeThrowsClearErrorForMissingMixer()
    {
        UiProperty<UnmixedValue> property = UiProperty<UnmixedValue>.Register(
            nameof(UnmixedValue),
            typeof(MotionFacadeTests),
            new UiPropertyMetadata<UnmixedValue>(new UnmixedValue(0), UiPropertyOptions.AffectsRender));
        UIRoot root = new();
        UIElement element = new();
        root.VisualChildren.Add(element);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            element.Motion().Animate(property).To(new UnmixedValue(1)).With(MotionFactory.Tween<UnmixedValue>(TimeSpan.FromMilliseconds(100))));

        Assert.Contains(nameof(UnmixedValue), exception.Message, StringComparison.Ordinal);
        Assert.Contains(property.DiagnosticName, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DetachedElementBehaviorIsDeterministic()
    {
        UIElement element = new();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            element.Motion().Opacity.To(0.5f, MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100))));

        Assert.Contains("attached to a UIRoot", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ShortcutPropertiesAnimateElementMotionProperties()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        UIElement element = new();
        root.VisualChildren.Add(element);

        MotionHandle opacity = element.Motion().Opacity.To(0.5f, MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        MotionHandle translate = element.Motion().TranslateX.To(20f, MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.ProcessFrame();

        Assert.True(opacity.IsActive);
        Assert.True(translate.IsActive);
        Assert.InRange(element.Opacity, 0.5f, 1);
        Assert.InRange(element.TranslateX, 0, 20);
    }

    [Fact]
    public void StateBuilderAnimatesMatchingStateAndRestoresBaseline()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        UIElement element = new();
        root.VisualChildren.Add(element);
        root.ProcessFrame();
        MotionStateBuilder states = element.Motion().States();
        states.When(AspectState.Hover).Set(
            UIElement.OpacityProperty,
            0.5f,
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));

        element.IsPointerOver = true;
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(100));
        root.ProcessFrame();

        Assert.Equal(0.5f, element.Opacity);

        element.IsPointerOver = false;
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(100));
        root.ProcessFrame();

        Assert.Equal(1f, element.Opacity);
    }

    [Fact]
    public void StateBuilderIsReusedAndExplicitMotionOutranksInteractiveState()
    {
        UIRoot root = new();
        UIElement element = new();
        root.VisualChildren.Add(element);
        MotionStateBuilder states = element.Motion().States();
        states.When(AspectState.Hover).Set(
            UIElement.OpacityProperty,
            0.5f,
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        element.IsPointerOver = true;
        MotionHandle explicitHandle = element.Motion().Opacity.To(
            0.8f,
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));

        element.IsPointerOver = false;

        Assert.Same(states, element.Motion().States());
        Assert.True(explicitHandle.IsActive);
        MotionPropertyBinding<float> binding = root.Motion.Properties.GetOrCreateBinding(
            root.Motion,
            element,
            UIElement.OpacityProperty);
        Assert.Equal(0.8f, binding.Value.Target);
    }

    [Theory]
    [InlineData("Natural")]
    [InlineData("Complete")]
    [InlineData("KeepCurrent")]
    [InlineData("Revert")]
    [InlineData("CancelComplete")]
    [InlineData("Dispose")]
    public void RejectedStateTargetIsRetriedAfterExplicitMotionEnds(string ending)
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        UIElement element = new();
        root.VisualChildren.Add(element);
        root.ProcessFrame();
        element.Motion().States().When(AspectState.Hover).Set(
            UIElement.OpacityProperty,
            0.5f,
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        MotionHandle explicitHandle = element.Motion().Opacity.To(
            0.8f,
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        element.IsPointerOver = true;
        root.ProcessFrame();

        MotionPropertyBinding<float> binding = root.Motion.Properties.GetOrCreateBinding(
            root.Motion, element, UIElement.OpacityProperty);
        Assert.True(explicitHandle.IsActive);
        Assert.Equal(0.8f, binding.Value.Target);

        switch (ending)
        {
            case "Natural":
                clock.Advance(TimeSpan.FromMilliseconds(100));
                root.ProcessFrame();
                Assert.True(explicitHandle.IsCompleted);
                break;
            case "Complete":
                explicitHandle.Complete();
                break;
            case "KeepCurrent":
                explicitHandle.Cancel();
                break;
            case "Revert":
                explicitHandle.Cancel(MotionCancelBehavior.Revert);
                break;
            case "CancelComplete":
                explicitHandle.Cancel(MotionCancelBehavior.Complete);
                break;
            case "Dispose":
                explicitHandle.Dispose();
                break;
        }

        root.ProcessFrame();
        Assert.Equal(0.5f, binding.Value.Target);
        clock.Advance(TimeSpan.FromMilliseconds(100));
        root.ProcessFrame();
        // A reused binding can run before its value node; drain its final staged sample.
        root.ProcessFrame();

        Assert.Equal(0.5f, element.Opacity);
        Assert.Equal(0.5f, binding.Value.Target);
        Assert.False(root.Motion.HasActiveMotion);
    }

    [Theory]
    [InlineData(false, 1f)]
    [InlineData(true, 0.3f)]
    public void RejectedStateTargetResolvesLatestStateWhenBlockerEnds(bool disabled, float expected)
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        UIElement element = new();
        root.VisualChildren.Add(element);
        root.ProcessFrame();
        MotionStateBuilder states = element.Motion().States();
        states.When(AspectState.Hover).Set(UIElement.OpacityProperty, 0.5f,
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        states.When(AspectState.Disabled).Set(UIElement.OpacityProperty, 0.3f,
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        MotionHandle explicitHandle = element.Motion().Opacity.To(0.8f,
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        element.IsPointerOver = true;
        element.IsPointerOver = false;
        element.IsEnabled = !disabled;

        explicitHandle.Complete();
        root.ProcessFrame();
        MotionPropertyBinding<float> binding = root.Motion.Properties.GetOrCreateBinding(
            root.Motion, element, UIElement.OpacityProperty);
        Assert.Equal(expected, binding.Value.Target);
        clock.Advance(TimeSpan.FromMilliseconds(100));
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.Equal(expected, element.Opacity);
        Assert.False(root.Motion.HasActiveMotion);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RejectedStateTargetWaitsForReplacementExplicitMotion(bool fromCompletionCallback)
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        UIElement element = new();
        root.VisualChildren.Add(element);
        root.ProcessFrame();
        element.Motion().States().When(AspectState.Hover).Set(UIElement.OpacityProperty, 0.5f,
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        MotionHandle first = element.Motion().Opacity.To(0.8f,
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        element.IsPointerOver = true;
        MotionHandle? replacement = null;
        if (fromCompletionCallback)
        {
            first.Completed += (_, _) => replacement = element.Motion().Opacity.To(0.9f,
                MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
            first.Complete();
        }
        else
        {
            replacement = element.Motion().Opacity.To(0.9f,
                MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        }

        root.ProcessFrame();
        Assert.NotNull(replacement);
        Assert.True(replacement.IsActive);
        MotionPropertyBinding<float> binding = root.Motion.Properties.GetOrCreateBinding(
            root.Motion, element, UIElement.OpacityProperty);
        Assert.Equal(0.9f, binding.Value.Target);

        replacement.Complete();
        root.ProcessFrame();
        Assert.Equal(0.5f, binding.Value.Target);
        clock.Advance(TimeSpan.FromMilliseconds(100));
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.Equal(0.5f, element.Opacity);
        Assert.False(root.Motion.HasActiveMotion);
    }

    [Fact]
    public void RejectedStateTargetCanReturnToPreviouslyAcceptedTarget()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        UIElement element = new();
        root.VisualChildren.Add(element);
        root.ProcessFrame();
        element.Motion().States().When(AspectState.Hover).Set(UIElement.OpacityProperty, 0.5f,
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        element.IsPointerOver = true;
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(100));
        root.ProcessFrame();
        Assert.Equal(0.5f, element.Opacity);
        MotionHandle explicitHandle = element.Motion().Opacity.To(0.8f,
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        element.IsPointerOver = false;
        element.IsPointerOver = true;

        explicitHandle.Complete();
        root.ProcessFrame();
        MotionPropertyBinding<float> binding = root.Motion.Properties.GetOrCreateBinding(
            root.Motion, element, UIElement.OpacityProperty);
        Assert.Equal(0.5f, binding.Value.Target);
        clock.Advance(TimeSpan.FromMilliseconds(100));
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.Equal(0.5f, element.Opacity);
        Assert.False(root.Motion.HasActiveMotion);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RejectedStateTargetStopsWaitingWhenElementIsDetachedOrHidden(bool detach)
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        UIElement element = new();
        root.VisualChildren.Add(element);
        root.ProcessFrame();
        element.Motion().States().When(AspectState.Hover).Set(UIElement.OpacityProperty, 0.5f,
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        element.Motion().Opacity.To(0.8f, MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        element.IsPointerOver = true;
        if (detach)
        {
            root.VisualChildren.Remove(element);
            Assert.Equal(0, root.Motion.Graph.ActiveNodeCount);
        }
        else
        {
            element.IsVisible = false;
        }

        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(100));
        root.ProcessFrame();

        Assert.False(root.Motion.HasActiveMotion);
        Assert.Equal(1f, element.Opacity);
    }

    [Fact]
    public void RejectedStateTargetUsesUpdatedRegistrationAndOneWaiter()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        UIElement element = new();
        root.VisualChildren.Add(element);
        root.ProcessFrame();
        MotionStateBuilder states = element.Motion().States();
        states.When(AspectState.Hover).Set(UIElement.OpacityProperty, 0.5f,
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        MotionHandle explicitHandle = element.Motion().Opacity.To(0.8f,
            MotionFactory.Tween<float>(TimeSpan.FromSeconds(1)));
        element.IsPointerOver = true;
        root.ProcessFrame();

        for (int index = 0; index < 100; index++)
        {
            element.IsPointerOver = !element.IsPointerOver;
            root.ProcessFrame();
            Assert.Equal(3, root.Motion.Graph.ActiveNodeCount);
            Assert.True(explicitHandle.IsActive);
        }

        states.When(AspectState.Hover).Set(UIElement.OpacityProperty, 0.4f,
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(200)));
        explicitHandle.Complete();
        root.ProcessFrame();
        MotionPropertyBinding<float> binding = root.Motion.Properties.GetOrCreateBinding(
            root.Motion, element, UIElement.OpacityProperty);
        Assert.Equal(0.4f, binding.Value.Target);
        clock.Advance(TimeSpan.FromMilliseconds(100));
        root.ProcessFrame();
        Assert.True(binding.Value.IsAnimating);
        clock.Advance(TimeSpan.FromMilliseconds(100));
        root.ProcessFrame();
        root.ProcessFrame();

        Assert.Equal(0.4f, element.Opacity);
        Assert.False(root.Motion.HasActiveMotion);
    }

    [Fact]
    public void RejectedStateTargetCanMoveToAnotherRootWithoutRetainingOldWaiter()
    {
        ManualMotionClock clock = new();
        UIRoot oldRoot = new(motionClock: clock);
        UIRoot newRoot = new(motionClock: clock);
        UIElement element = new();
        oldRoot.VisualChildren.Add(element);
        oldRoot.ProcessFrame();
        element.Motion().States().When(AspectState.Hover).Set(UIElement.OpacityProperty, 0.5f,
            MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        element.Motion().Opacity.To(0.8f, MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        element.IsPointerOver = true;

        oldRoot.VisualChildren.Remove(element);
        Assert.Equal(0, oldRoot.Motion.Graph.ActiveNodeCount);
        newRoot.VisualChildren.Add(element);
        oldRoot.ProcessFrame();
        newRoot.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(100));
        newRoot.ProcessFrame();

        Assert.Equal(0.5f, element.Opacity);
        Assert.Equal(0, oldRoot.Motion.Properties.BindingCount);
        Assert.False(oldRoot.Motion.HasActiveMotion);
        Assert.False(newRoot.Motion.HasActiveMotion);
    }

    private readonly record struct UnmixedValue(int Value);
}

using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
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
            .To(Color.White)
            .With(MotionFactory.Tween<Color>(TimeSpan.FromMilliseconds(100)));

        Assert.Equal(1, root.Motion.Properties.BindingCount);
    }

    [Fact]
    public void FacadeReusesExistingBindingOnRepeatedCalls()
    {
        UIRoot root = new();
        Control control = new();
        root.VisualChildren.Add(control);

        control.Motion().Animate(Control.BackgroundProperty).To(Color.White).With(MotionFactory.Tween<Color>(TimeSpan.FromMilliseconds(100)));
        control.Motion().Animate(Control.BackgroundProperty).To(Color.Black).With(MotionFactory.Tween<Color>(TimeSpan.FromMilliseconds(100)));

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

    private readonly record struct UnmixedValue(int Value);
}

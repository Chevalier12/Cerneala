using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;
using Cerneala.UI.Media;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.UI.Motion.Properties;

public sealed class AnimatablePropertyRegistry
{
    private readonly Dictionary<UiProperty, MotionPropertyOptions> properties = [];

    public AnimatablePropertyRegistry()
    {
        RegisterBuiltIns();
    }

    public void Register(UiProperty property, MotionPropertyOptions options)
    {
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(options);
        properties[property] = options;
    }

    public bool TryGet(UiProperty property, out MotionPropertyOptions options)
    {
        ArgumentNullException.ThrowIfNull(property);
        return properties.TryGetValue(property, out options!);
    }

    public MotionPropertyOptions Get(UiProperty property)
    {
        if (TryGet(property, out MotionPropertyOptions? options))
        {
            return options;
        }

        throw new InvalidOperationException($"Property '{property.DiagnosticName}' is not registered as animatable.");
    }

    public IReadOnlyDictionary<UiProperty, MotionPropertyOptions> RegisteredProperties => properties;

    private void RegisterBuiltIns()
    {
        MotionSpec colorSpec = MotionFactory.Tween(TimeSpan.FromMilliseconds(160));
        MotionSpec thicknessSpec = MotionFactory.Tween(TimeSpan.FromMilliseconds(180));

        Register(Control.BackgroundProperty, Options<Color, ColorMixer>(Control.BackgroundProperty, colorSpec, isSafeForImplicitAnimation: true));
        Register(Control.BorderColorProperty, Options<Color, ColorMixer>(Control.BorderColorProperty, colorSpec, isSafeForImplicitAnimation: true));
        Register(Control.BorderThicknessProperty, Options<Thickness, ThicknessMixer>(Control.BorderThicknessProperty, thicknessSpec, isSafeForImplicitAnimation: false));
        Register(Control.PaddingProperty, Options<Thickness, ThicknessMixer>(Control.PaddingProperty, thicknessSpec, isSafeForImplicitAnimation: false));
        Register(UIElement.MarginProperty, Options<Thickness, ThicknessMixer>(UIElement.MarginProperty, thicknessSpec, isSafeForImplicitAnimation: false));
        Register(UIElement.OpacityProperty, Options<float, FloatMixer>(UIElement.OpacityProperty, MotionFactory.Tween(TimeSpan.FromMilliseconds(120)), isSafeForImplicitAnimation: true));
        Register(UIElement.RenderTransformProperty, Options<Transform, TransformMixer>(UIElement.RenderTransformProperty, MotionFactory.Tween(TimeSpan.FromMilliseconds(180)), isSafeForImplicitAnimation: true));
        Register(UIElement.TranslateXProperty, Options<float, FloatMixer>(UIElement.TranslateXProperty, MotionFactory.Tween(TimeSpan.FromMilliseconds(180)), isSafeForImplicitAnimation: true));
        Register(UIElement.TranslateYProperty, Options<float, FloatMixer>(UIElement.TranslateYProperty, MotionFactory.Tween(TimeSpan.FromMilliseconds(180)), isSafeForImplicitAnimation: true));
        Register(UIElement.ScaleProperty, Options<float, FloatMixer>(UIElement.ScaleProperty, MotionFactory.Tween(TimeSpan.FromMilliseconds(120)), isSafeForImplicitAnimation: true));
        Register(UIElement.ScaleXProperty, Options<float, FloatMixer>(UIElement.ScaleXProperty, MotionFactory.Tween(TimeSpan.FromMilliseconds(120)), isSafeForImplicitAnimation: true));
        Register(UIElement.ScaleYProperty, Options<float, FloatMixer>(UIElement.ScaleYProperty, MotionFactory.Tween(TimeSpan.FromMilliseconds(120)), isSafeForImplicitAnimation: true));
        Register(UIElement.RotationProperty, Options<float, FloatMixer>(UIElement.RotationProperty, MotionFactory.Tween(TimeSpan.FromMilliseconds(180)), isSafeForImplicitAnimation: true));
        Register(UIElement.SkewXProperty, Options<float, FloatMixer>(UIElement.SkewXProperty, MotionFactory.Tween(TimeSpan.FromMilliseconds(180)), isSafeForImplicitAnimation: true));
        Register(UIElement.SkewYProperty, Options<float, FloatMixer>(UIElement.SkewYProperty, MotionFactory.Tween(TimeSpan.FromMilliseconds(180)), isSafeForImplicitAnimation: true));
    }

    private static MotionPropertyOptions Options<TValue, TMixer>(
        UiProperty<TValue> property,
        MotionSpec defaultSpec,
        bool isSafeForImplicitAnimation)
    {
        return new MotionPropertyOptions(
            typeof(TMixer),
            defaultSpec,
            MotionPropertyInvalidationClassifier.Classify(property),
            isSafeForImplicitAnimation);
    }
}

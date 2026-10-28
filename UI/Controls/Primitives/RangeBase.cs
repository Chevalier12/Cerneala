using Cerneala.UI.Core;
using Cerneala.UI.Input;
using Cerneala.UI.Markup;

namespace Cerneala.UI.Controls.Primitives;

public class RangeBase : Control
{
    public static readonly RoutedEvent ValueChangedEvent = RoutedEventRegistry.Register(nameof(ValueChanged), typeof(RangeBase), RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventArgs<float>));

    public event EventHandler<RoutedPropertyChangedEventArgs<float>> ValueChanged
    {
        add => AddTypedHandler(ValueChangedEvent, value);
        remove => RemoveTypedHandler(ValueChangedEvent, value);
    }
    public static readonly UiProperty<float> MinimumProperty = UiProperty<float>.Register(
        nameof(Minimum),
        typeof(RangeBase),
        new UiPropertyMetadata<float>(
            0,
            UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender,
            validateValue: IsValidFloat));

    public static readonly UiProperty<float> MaximumProperty = UiProperty<float>.Register(
        nameof(Maximum),
        typeof(RangeBase),
        new UiPropertyMetadata<float>(
            1,
            UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender,
            validateValue: IsValidFloat));

    public static readonly UiProperty<float> ValueProperty = UiProperty<float>.Register(
        nameof(Value),
        typeof(RangeBase),
        new UiPropertyMetadata<float>(
            0,
            UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual,
            validateValue: IsValidFloat,
            coerceValue: CoerceValue));

    public static readonly UiProperty<float> SmallChangeProperty = UiProperty<float>.Register(
        nameof(SmallChange),
        typeof(RangeBase),
        new UiPropertyMetadata<float>(0.1f, UiPropertyOptions.AffectsInputVisual, validateValue: IsValidNonNegativeFloat));

    public static readonly UiProperty<float> LargeChangeProperty = UiProperty<float>.Register(
        nameof(LargeChange),
        typeof(RangeBase),
        new UiPropertyMetadata<float>(1, UiPropertyOptions.AffectsInputVisual, validateValue: IsValidNonNegativeFloat));

    public float Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public float Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public float Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    [MarkupValueConstraint(MarkupValueConstraint.NonNegative)]
    public float SmallChange
    {
        get => GetValue(SmallChangeProperty);
        set => SetValue(SmallChangeProperty, value);
    }

    [MarkupValueConstraint(MarkupValueConstraint.NonNegative)]
    public float LargeChange
    {
        get => GetValue(LargeChangeProperty);
        set => SetValue(LargeChangeProperty, value);
    }

    protected float CoerceToRange(float value)
    {
        return Clamp(value, Minimum, Maximum);
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, ValueProperty))
        {
            float coercedValue = CoerceToRange(Value);
            if (coercedValue != Value)
            {
                Correct(this, ValueProperty, coercedValue);
                return;
            }

            RaiseEvent(new RoutedPropertyChangedEventArgs<float>(ValueChangedEvent, this, (float)args.OldValue!, Value));
        }

        if (ReferenceEquals(args.Property, MinimumProperty) && Maximum < Minimum)
        {
            Correct(this, MaximumProperty, Minimum);
        }

        if (ReferenceEquals(args.Property, MaximumProperty) && Minimum > Maximum)
        {
            Correct(this, MinimumProperty, Maximum);
        }

        if (ReferenceEquals(args.Property, MinimumProperty) || ReferenceEquals(args.Property, MaximumProperty))
        {
            float coercedValue = CoerceToRange(Value);
            if (coercedValue != Value)
            {
                Correct(this, ValueProperty, coercedValue);
            }
        }
    }

    // Writes a range correction at the source that holds the current value, so the correction never adds a Local
    // value over a markup, Aspect, binding or animation value (#733), and a ClearValue leaves no Local value (#888).
    internal static void Correct(UiObject owner, UiProperty<float> property, float value)
    {
        UiPropertyValueSource source = owner.GetValueSource(property);
        if (source == UiPropertyValueSource.Default)
        {
            owner.SetFrameworkDefault(property, value);
        }
        else
        {
            owner.SetValue(property, value, source);
        }
    }

    internal static float Clamp(float value, float minimum, float maximum)
    {
        if (maximum < minimum)
        {
            maximum = minimum;
        }

        return MathF.Min(MathF.Max(value, minimum), maximum);
    }

    // For a value computed in double, such as a value plus a Track drag change, whose sum can exceed float's range
    // (#986): the clamp happens before the conversion to float, so the result is always a finite value of the range.
    internal static float Clamp(double value, float minimum, float maximum)
    {
        if (maximum < minimum)
        {
            maximum = minimum;
        }

        return (float)Math.Min(Math.Max(value, minimum), maximum);
    }

    // A value that is not finite stays as it is, so the validation rejects it, as WPF validates before it coerces (#770).
    private static float CoerceValue(UiObject owner, float value)
    {
        RangeBase range = (RangeBase)owner;
        return float.IsFinite(value) ? Clamp(value, range.Minimum, range.Maximum) : value;
    }

    private static bool IsValidFloat(float value)
    {
        return float.IsFinite(value);
    }

    private static bool IsValidNonNegativeFloat(float value)
    {
        return value >= 0 && float.IsFinite(value);
    }
}

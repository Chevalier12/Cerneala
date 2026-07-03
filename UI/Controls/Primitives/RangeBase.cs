using Cerneala.UI.Core;

namespace Cerneala.UI.Controls.Primitives;

public class RangeBase : Control
{
    public static readonly UiProperty<float> MinimumProperty = UiProperty<float>.Register(
        nameof(Minimum),
        typeof(RangeBase),
        new UiPropertyMetadata<float>(
            0,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender,
            validateValue: IsValidFloat));

    public static readonly UiProperty<float> MaximumProperty = UiProperty<float>.Register(
        nameof(Maximum),
        typeof(RangeBase),
        new UiPropertyMetadata<float>(
            1,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender,
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

    public float SmallChange
    {
        get => GetValue(SmallChangeProperty);
        set => SetValue(SmallChangeProperty, value);
    }

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
                Value = coercedValue;
            }
        }

        if (ReferenceEquals(args.Property, MinimumProperty) && Maximum < Minimum)
        {
            Maximum = Minimum;
        }

        if (ReferenceEquals(args.Property, MaximumProperty) && Minimum > Maximum)
        {
            Minimum = Maximum;
        }

        if (ReferenceEquals(args.Property, MinimumProperty) || ReferenceEquals(args.Property, MaximumProperty))
        {
            Value = CoerceToRange(Value);
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

    private static float CoerceValue(UiObject owner, float value)
    {
        RangeBase range = (RangeBase)owner;
        return Clamp(value, range.Minimum, range.Maximum);
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

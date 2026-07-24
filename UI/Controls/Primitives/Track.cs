using Cerneala.Drawing;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

namespace Cerneala.UI.Controls.Primitives;

[TemplatePart("PART_Thumb", typeof(Thumb))]
public class Track : Control
{
    private Thumb? thumb;
    private TrackValueChangeReason valueChangeReason;

    public Track()
    {
        SetValue(
            BackgroundProperty,
            new Cerneala.UI.Media.SolidColorBrush(new Color(225, 225, 225)),
            UiPropertyValueSource.AspectBase);
        SetValue(
            BorderBrushProperty,
            new Cerneala.UI.Media.SolidColorBrush(new Color(120, 120, 120)),
            UiPropertyValueSource.AspectBase);
        SetValue(BorderThicknessProperty, new Thickness(1), UiPropertyValueSource.AspectBase);
        SmallChange = 0.1f;
        LargeChange = 1;
        Handlers.AddHandler(InputEvents.MouseDownEvent, OnMouseDown);
        SetValue(ComponentTemplateProperty, TrackTemplates.Default, UiPropertyValueSource.AspectBase);
    }

    public static readonly UiProperty<float> MinimumProperty = UiProperty<float>.Register(
        nameof(Minimum),
        typeof(Track),
        new UiPropertyMetadata<float>(0, UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender, validateValue: float.IsFinite));

    public static readonly UiProperty<float> MaximumProperty = UiProperty<float>.Register(
        nameof(Maximum),
        typeof(Track),
        new UiPropertyMetadata<float>(1, UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender, validateValue: float.IsFinite));

    public static readonly UiProperty<float> ValueProperty = UiProperty<float>.Register(
        nameof(Value),
        typeof(Track),
        new UiPropertyMetadata<float>(
            0,
            UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender | UiPropertyOptions.AffectsInputVisual,
            validateValue: float.IsFinite,
            coerceValue: CoerceValue));

    public static readonly UiProperty<float> ViewportSizeProperty = UiProperty<float>.Register(
        nameof(ViewportSize),
        typeof(Track),
        new UiPropertyMetadata<float>(0, UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender, validateValue: IsValidNonNegativeFloat));

    public static readonly UiProperty<float> SmallChangeProperty = UiProperty<float>.Register(
        nameof(SmallChange),
        typeof(Track),
        new UiPropertyMetadata<float>(0.1f, UiPropertyOptions.AffectsInputVisual, validateValue: IsValidNonNegativeFloat));

    public static readonly UiProperty<float> LargeChangeProperty = UiProperty<float>.Register(
        nameof(LargeChange),
        typeof(Track),
        new UiPropertyMetadata<float>(1, UiPropertyOptions.AffectsInputVisual, validateValue: IsValidNonNegativeFloat));

    public static readonly UiProperty<Orientation> OrientationProperty = UiProperty<Orientation>.Register(
        nameof(Orientation),
        typeof(Track),
        new UiPropertyMetadata<Orientation>(Orientation.Horizontal, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender));

    public event EventHandler? ValueChanged;

    internal event EventHandler<TrackValueChangedEventArgs>? ValueChangedWithReason;

    internal bool MoveToPointOnClick { get; set; }

    public Thumb Thumb
    {
        get
        {
            ApplyTemplate();
            return thumb ?? throw new InvalidOperationException("Track template did not provide the required part 'PART_Thumb'.");
        }
    }

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

    public float ViewportSize
    {
        get => GetValue(ViewportSizeProperty);
        set => SetValue(ViewportSizeProperty, value);
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

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public float ValueRatio => GetValueRatio(Value);

    public void DecreaseLarge()
    {
        ChangeValue(Value - LargeChange, TrackValueChangeReason.LargeDecrement);
    }

    public void IncreaseLarge()
    {
        ChangeValue(Value + LargeChange, TrackValueChangeReason.LargeIncrement);
    }

    public void DecreaseSmall()
    {
        ChangeValue(Value - SmallChange, TrackValueChangeReason.SmallDecrement);
    }

    public void IncreaseSmall()
    {
        ChangeValue(Value + SmallChange, TrackValueChangeReason.SmallIncrement);
    }

    public float ValueFromPoint(float x, float y)
    {
        float length = GetTrackLength(ArrangedBounds);
        float thumbLength = GetThumbLength(length);
        float travel = MathF.Max(0, length - thumbLength);
        float relative = Orientation == Orientation.Horizontal
            ? x - ArrangedBounds.X - (thumbLength / 2)
            : y - ArrangedBounds.Y - (thumbLength / 2);
        float ratio = travel <= 0 ? 0 : relative / travel;
        return Minimum + (Range * MathF.Min(MathF.Max(ratio, 0), 1));
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        ApplyTemplate();
        return base.MeasureCore(context);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        LayoutRect arranged = base.ArrangeCore(context);
        if (thumb is not null)
        {
            ArrangeThumb(thumb, context);
        }

        return arranged;
    }

    protected override void OnTemplateApplied(ComponentTemplateInstance? instance)
    {
        if (thumb is not null)
        {
            thumb.DragDelta -= OnThumbDragDelta;
            thumb = null;
        }

        if (instance is null)
        {
            return;
        }

        thumb = GetRequiredTemplatePart<Thumb>("PART_Thumb");
        thumb.DragDelta += OnThumbDragDelta;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, MinimumProperty) || ReferenceEquals(args.Property, MaximumProperty))
        {
            if (ReferenceEquals(args.Property, MinimumProperty) && Maximum < Minimum)
            {
                Maximum = Minimum;
            }

            if (ReferenceEquals(args.Property, MaximumProperty) && Minimum > Maximum)
            {
                Minimum = Maximum;
            }

            Value = RangeBase.Clamp(Value, Minimum, Maximum);
        }

        if (ReferenceEquals(args.Property, ValueProperty))
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
            ValueChangedWithReason?.Invoke(
                this,
                new TrackValueChangedEventArgs((float)args.OldValue!, Value, valueChangeReason));
        }

        if (ReferenceEquals(args.Property, ComponentTemplateProperty) &&
            ComponentTemplate is null &&
            GetSourceValue(ComponentTemplateProperty, UiPropertyValueSource.AspectBase) is ComponentTemplate)
        {
            ClearValue(ComponentTemplateProperty);
        }
    }

    private float Range => MathF.Max(0, Maximum - Minimum);

    private void ArrangeThumb(Thumb activeThumb, ArrangeContext context)
    {
        float length = GetTrackLength(context.FinalRect);
        float thumbLength = GetThumbLength(length);
        float travel = MathF.Max(0, length - thumbLength);
        float offset = travel * ValueRatio;
        LayoutRect thumbRect = Orientation == Orientation.Horizontal
            ? new LayoutRect(context.FinalRect.X + offset, context.FinalRect.Y, thumbLength, context.FinalRect.Height)
            : new LayoutRect(context.FinalRect.X, context.FinalRect.Y + offset, context.FinalRect.Width, thumbLength);
        activeThumb.Arrange(new ArrangeContext(thumbRect, context.Rounding));
    }

    private float GetValueRatio(float value)
    {
        float range = Range;
        return range <= 0 ? 0 : (RangeBase.Clamp(value, Minimum, Maximum) - Minimum) / range;
    }

    private float GetTrackLength(LayoutRect rect)
    {
        return Orientation == Orientation.Horizontal ? rect.Width : rect.Height;
    }

    private float GetThumbLength(float trackLength)
    {
        if (ViewportSize <= 0)
        {
            return MathF.Min(trackLength, 10);
        }

        if (Range <= 0)
        {
            return trackLength;
        }

        float length = trackLength * (ViewportSize / (Range + ViewportSize));
        return MathF.Min(trackLength, MathF.Max(10, length));
    }

    private void OnThumbDragDelta(object? sender, DragDeltaEventArgs args)
    {
        float length = GetTrackLength(ArrangedBounds);
        float travel = MathF.Max(0, length - GetThumbLength(length));
        if (travel <= 0 || Range <= 0)
        {
            return;
        }

        float pixelDelta = Orientation == Orientation.Horizontal ? args.HorizontalChange : args.VerticalChange;
        ChangeValue(Value + ((pixelDelta / travel) * Range), TrackValueChangeReason.ThumbTrack);
    }

    private void OnMouseDown(UiElementId source, RoutedEventArgs args)
    {
        if (args is not MouseButtonEventArgs mouseArgs ||
            mouseArgs.ChangedButton != InputMouseButton.Left ||
            thumb is null ||
            Contains(thumb.ArrangedBounds, mouseArgs.X, mouseArgs.Y))
        {
            return;
        }

        float oldValue = Value;
        float pointValue = ValueFromPoint(mouseArgs.X, mouseArgs.Y);
        if (MoveToPointOnClick)
        {
            ChangeValue(pointValue, TrackValueChangeReason.ThumbTrack);
        }
        else if (pointValue < Value)
        {
            DecreaseLarge();
        }
        else
        {
            IncreaseLarge();
        }

        args.Handled = oldValue != Value;
    }

    private void ChangeValue(float newValue, TrackValueChangeReason reason)
    {
        TrackValueChangeReason previousReason = valueChangeReason;
        valueChangeReason = reason;
        try
        {
            Value = newValue;
        }
        finally
        {
            valueChangeReason = previousReason;
        }
    }

    private static float CoerceValue(UiObject owner, float value)
    {
        Track track = (Track)owner;
        return RangeBase.Clamp(value, track.Minimum, track.Maximum);
    }

    private static bool IsValidNonNegativeFloat(float value)
    {
        return value >= 0 && float.IsFinite(value);
    }

    private static bool Contains(LayoutRect rect, float x, float y)
    {
        return x >= rect.X &&
            y >= rect.Y &&
            x < rect.X + rect.Width &&
            y < rect.Y + rect.Height;
    }
}

internal enum TrackValueChangeReason
{
    Programmatic,
    SmallDecrement,
    SmallIncrement,
    LargeDecrement,
    LargeIncrement,
    ThumbTrack
}

internal sealed class TrackValueChangedEventArgs(
    float oldValue,
    float newValue,
    TrackValueChangeReason reason) : EventArgs
{
    public float OldValue { get; } = oldValue;

    public float NewValue { get; } = newValue;

    public TrackValueChangeReason Reason { get; } = reason;
}

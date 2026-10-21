using Cerneala.Drawing;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;

namespace Cerneala.UI.Controls.Primitives;

[TemplatePart("PART_Thumb", typeof(Thumb))]
[TemplatePart("PART_DecreaseRepeatButton", typeof(RepeatButton))]
[TemplatePart("PART_IncreaseRepeatButton", typeof(RepeatButton))]
public class Track : Control
{
    private Thumb? thumb;
    private RepeatButton? decreaseRepeatButton;
    private RepeatButton? increaseRepeatButton;
    private TrackValueChangeReason valueChangeReason;

    public Track()
    {
        SetFrameworkDefault(
            BackgroundProperty,
            new Cerneala.UI.Media.SolidColorBrush(new Color(225, 225, 225)));
        SetFrameworkDefault(
            BorderBrushProperty,
            new Cerneala.UI.Media.SolidColorBrush(new Color(120, 120, 120)));
        SetFrameworkDefault(BorderThicknessProperty, new Thickness(1));
        SmallChange = 0.1f;
        LargeChange = 1;
        SetFrameworkDefault(ComponentTemplateProperty, TrackTemplates.Default);
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

    // As WPF's Track.IsDirectionReversed: false puts Minimum at the left of a horizontal track and at the bottom of a
    // vertical one; true puts it at the right and at the top.
    public static readonly UiProperty<bool> IsDirectionReversedProperty = UiProperty<bool>.Register(
        nameof(IsDirectionReversed),
        typeof(Track),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender));

    public event EventHandler? ValueChanged;

    internal event EventHandler<TrackValueChangedEventArgs>? ValueChangedWithReason;

    public Thumb Thumb
    {
        get
        {
            ApplyTemplate();
            return thumb ?? throw new InvalidOperationException("Track template did not provide the required part 'PART_Thumb'.");
        }
    }

    // As WPF's Track.DecreaseRepeatButton: the optional part that fills the track between the Minimum end and the thumb.
    public RepeatButton? DecreaseRepeatButton
    {
        get
        {
            ApplyTemplate();
            return decreaseRepeatButton;
        }
    }

    // As WPF's Track.IncreaseRepeatButton: the optional part that fills the track between the thumb and the Maximum end.
    public RepeatButton? IncreaseRepeatButton
    {
        get
        {
            ApplyTemplate();
            return increaseRepeatButton;
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

    public bool IsDirectionReversed
    {
        get => GetValue(IsDirectionReversedProperty);
        set => SetValue(IsDirectionReversedProperty, value);
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
        if (travel <= 0)
        {
            return Minimum;
        }

        float ratio = MathF.Min(MathF.Max(relative / travel, 0), 1);
        return Minimum + (Range * (IsMinimumAtEnd ? 1 - ratio : ratio));
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        ApplyTemplate();
        return base.MeasureCore(context);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        LayoutRect arranged = base.ArrangeCore(context);
        ArrangeParts(context);
        return arranged;
    }

    protected override void OnTemplateApplied(ComponentTemplateInstance? instance)
    {
        if (thumb is not null)
        {
            thumb.DragDelta -= OnThumbDragDelta;
            thumb = null;
        }

        if (decreaseRepeatButton is not null)
        {
            decreaseRepeatButton.Click -= OnDecreaseRepeatButtonClick;
            decreaseRepeatButton = null;
        }

        if (increaseRepeatButton is not null)
        {
            increaseRepeatButton.Click -= OnIncreaseRepeatButtonClick;
            increaseRepeatButton = null;
        }

        if (instance is null)
        {
            return;
        }

        thumb = GetRequiredTemplatePart<Thumb>("PART_Thumb");
        thumb.DragDelta += OnThumbDragDelta;
        decreaseRepeatButton = GetOptionalTemplatePart<RepeatButton>("PART_DecreaseRepeatButton");
        increaseRepeatButton = GetOptionalTemplatePart<RepeatButton>("PART_IncreaseRepeatButton");
        if (decreaseRepeatButton is not null)
        {
            decreaseRepeatButton.Click += OnDecreaseRepeatButtonClick;
        }

        if (increaseRepeatButton is not null)
        {
            increaseRepeatButton.Click += OnIncreaseRepeatButtonClick;
        }
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
            HasFrameworkDefault(ComponentTemplateProperty))
        {
            ClearValue(ComponentTemplateProperty);
        }
    }

    private float Range => MathF.Max(0, Maximum - Minimum);

    // Whether Minimum is at the right end of a horizontal track or the bottom end of a vertical one.
    private bool IsMinimumAtEnd => Orientation == Orientation.Horizontal ? IsDirectionReversed : !IsDirectionReversed;

    // The change of value that moving the thumb by a distance in layout pixels makes, as WPF's Track.ValueFromDistance.
    internal float ValueFromDistance(float horizontal, float vertical)
    {
        float length = GetTrackLength(ArrangedBounds);
        float travel = MathF.Max(0, length - GetThumbLength(length));
        if (travel <= 0 || Range <= 0)
        {
            return 0;
        }

        float pixelDelta = Orientation == Orientation.Horizontal ? horizontal : vertical;
        return (IsMinimumAtEnd ? -pixelDelta : pixelDelta) / travel * Range;
    }

    // The thumb sits at the value's place along the track, and the repeat buttons fill the track on each side of it:
    // the decrease button toward the Minimum end and the increase button toward the Maximum end.
    private void ArrangeParts(ArrangeContext context)
    {
        float length = GetTrackLength(context.FinalRect);
        float thumbLength = GetThumbLength(length);
        float travel = MathF.Max(0, length - thumbLength);
        float offset = travel * (IsMinimumAtEnd ? 1 - ValueRatio : ValueRatio);
        float thumbEnd = offset + thumbLength;
        ArrangeAlongTrack(thumb, context, offset, thumbLength);
        ArrangeAlongTrack(IsMinimumAtEnd ? increaseRepeatButton : decreaseRepeatButton, context, 0, offset);
        ArrangeAlongTrack(IsMinimumAtEnd ? decreaseRepeatButton : increaseRepeatButton, context, thumbEnd, MathF.Max(0, length - thumbEnd));
    }

    private void ArrangeAlongTrack(UIElement? element, ArrangeContext context, float start, float length)
    {
        if (element is null)
        {
            return;
        }

        LayoutRect rect = Orientation == Orientation.Horizontal
            ? new LayoutRect(context.FinalRect.X + start, context.FinalRect.Y, length, context.FinalRect.Height)
            : new LayoutRect(context.FinalRect.X, context.FinalRect.Y + start, context.FinalRect.Width, length);
        element.Arrange(new ArrangeContext(rect, context.Rounding));
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
        float change = ValueFromDistance(args.HorizontalChange, args.VerticalChange);
        if (change != 0)
        {
            ChangeValue(Value + change, TrackValueChangeReason.ThumbTrack);
        }
    }

    private void OnDecreaseRepeatButtonClick(UiElementId source, RoutedEventArgs args)
    {
        DecreaseLarge();
        args.Handled = true;
    }

    private void OnIncreaseRepeatButtonClick(UiElementId source, RoutedEventArgs args)
    {
        IncreaseLarge();
        args.Handled = true;
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

using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Controls.Primitives;

public class Track : Control
{
    private readonly Thumb thumb;
    private bool ownsThumb;
    private bool updatingFromThumb;

    public Track()
    {
        thumb = new Thumb();
        thumb.DragDelta += OnThumbDragDelta;
        AddThumb();
        Background = new Color(225, 225, 225);
        BorderBrush = new Color(120, 120, 120);
        BorderThickness = new Thickness(1);
        SmallChange = 0.1f;
        LargeChange = 1;
        Handlers.AddHandler(InputEvents.MouseDownEvent, OnMouseDown);
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

    public Thumb Thumb => thumb;

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
        Value -= LargeChange;
    }

    public void IncreaseLarge()
    {
        Value += LargeChange;
    }

    public void DecreaseSmall()
    {
        Value -= SmallChange;
    }

    public void IncreaseSmall()
    {
        Value += SmallChange;
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
        if (TemplateChild is not null)
        {
            return base.MeasureCore(context);
        }

        thumb.Measure(new MeasureContext(context.AvailableSize, context.Rounding));
        return Orientation == Orientation.Horizontal
            ? new LayoutSize(32, MathF.Max(10, thumb.DesiredSize.Height))
            : new LayoutSize(MathF.Max(10, thumb.DesiredSize.Width), 32);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        if (TemplateChild is not null)
        {
            return base.ArrangeCore(context);
        }

        ArrangeThumb(context);
        return context.FinalRect;
    }

    protected override void OnRender(RenderContext context)
    {
        if (TemplateChild is not null)
        {
            return;
        }

        DrawRect rect = Border.ToDrawRect(context.Bounds);
        if (Background.A != 0 && rect.Width > 0 && rect.Height > 0)
        {
            context.DrawingContext.FillRectangle(rect, Background);
        }

        float thickness = MathF.Max(MathF.Max(BorderThickness.Left, BorderThickness.Top), MathF.Max(BorderThickness.Right, BorderThickness.Bottom));
        if (BorderBrush.A != 0 && thickness > 0 && rect.Width > 0 && rect.Height > 0)
        {
            context.DrawingContext.DrawRectangle(rect, BorderBrush, thickness);
        }
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        bool templateChanged = ReferenceEquals(args.Property, ComponentTemplateProperty);
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

        if (ReferenceEquals(args.Property, ValueProperty) && !updatingFromThumb)
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        if (templateChanged)
        {
            if (ComponentTemplate is null)
            {
                AddThumb();
            }
            else
            {
                RemoveThumb();
            }
        }
    }

    private float Range => MathF.Max(0, Maximum - Minimum);

    private void ArrangeThumb(ArrangeContext context)
    {
        float length = GetTrackLength(context.FinalRect);
        float thumbLength = GetThumbLength(length);
        float travel = MathF.Max(0, length - thumbLength);
        float offset = travel * ValueRatio;
        LayoutRect thumbRect = Orientation == Orientation.Horizontal
            ? new LayoutRect(context.FinalRect.X + offset, context.FinalRect.Y, thumbLength, context.FinalRect.Height)
            : new LayoutRect(context.FinalRect.X, context.FinalRect.Y + offset, context.FinalRect.Width, thumbLength);
        thumb.Arrange(new ArrangeContext(thumbRect, context.Rounding));
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
        float oldValue = Value;
        updatingFromThumb = true;
        try
        {
            Value += (pixelDelta / travel) * Range;
        }
        finally
        {
            updatingFromThumb = false;
        }

        if (oldValue != Value)
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnMouseDown(UiElementId source, RoutedEventArgs args)
    {
        if (args is not MouseButtonEventArgs mouseArgs ||
            mouseArgs.ChangedButton != InputMouseButton.Left ||
            Contains(thumb.ArrangedBounds, mouseArgs.X, mouseArgs.Y))
        {
            return;
        }

        float oldValue = Value;
        float pointValue = ValueFromPoint(mouseArgs.X, mouseArgs.Y);
        if (pointValue < Value)
        {
            DecreaseLarge();
        }
        else
        {
            IncreaseLarge();
        }

        args.Handled = oldValue != Value;
    }

    private void AddThumb()
    {
        if (ownsThumb)
        {
            return;
        }

        LogicalChildren.Add(thumb);
        VisualChildren.Add(thumb);
        ownsThumb = true;
    }

    private void RemoveThumb()
    {
        if (!ownsThumb)
        {
            return;
        }

        VisualChildren.Remove(thumb);
        LogicalChildren.Remove(thumb);
        ownsThumb = false;
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

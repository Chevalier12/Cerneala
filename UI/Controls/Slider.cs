using System.Globalization;
using System.Numerics;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Markup;

namespace Cerneala.UI.Controls;

[TemplatePart("PART_Track", typeof(Track))]
[TemplatePart("PART_SelectionRange", typeof(UIElement))]
public class Slider : RangeBase
{
    private Track? track;
    private UIElement? selectionRange;
    private ToolTip? autoToolTip;
    private bool syncingTrack;
    private float dragStartValue;

    // As WPF's Slider, which is focusable and a tab stop.
    public Slider()
    {
        SetFrameworkDefault(FocusableProperty, true);
        SetFrameworkDefault(IsTabStopProperty, true);
        SetFrameworkDefault(ComponentTemplateProperty, SliderTemplates.Default);
        AddHandler(InputEvents.MouseDownEvent, OnMouseDown, handledEventsToo: true);
        AddHandler(Thumb.DragStartedEvent, OnThumbDragStarted, handledEventsToo: true);
        AddHandler(Thumb.DragCompletedEvent, OnThumbDragCompleted, handledEventsToo: true);
        Handlers.AddHandler(InputEvents.PreviewMouseDownEvent, OnPreviewMouseDown);
        Handlers.AddHandler(InputEvents.KeyDownEvent, OnKeyDown);
    }

    public static readonly UiProperty<Orientation> OrientationProperty = UiProperty<Orientation>.Register(
        nameof(Orientation),
        typeof(Slider),
        new UiPropertyMetadata<Orientation>(Orientation.Horizontal, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<bool> IsDirectionReversedProperty = UiProperty<bool>.Register(
        nameof(IsDirectionReversed),
        typeof(Slider),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<bool> IsMoveToPointEnabledProperty = UiProperty<bool>.Register(
        nameof(IsMoveToPointEnabled),
        typeof(Slider),
        new UiPropertyMetadata<bool>(false));

    public static readonly UiProperty<int> DelayProperty = UiProperty<int>.Register(
        nameof(Delay),
        typeof(Slider),
        new UiPropertyMetadata<int>(500, validateValue: value => value >= 0));

    public static readonly UiProperty<int> IntervalProperty = UiProperty<int>.Register(
        nameof(Interval),
        typeof(Slider),
        new UiPropertyMetadata<int>(100, validateValue: value => value > 0));

    public static readonly UiProperty<float> TickFrequencyProperty = UiProperty<float>.Register(
        nameof(TickFrequency),
        typeof(Slider),
        new UiPropertyMetadata<float>(1, validateValue: value => value >= 0 && float.IsFinite(value)));

    public static readonly UiProperty<IReadOnlyList<float>?> TicksProperty = UiProperty<IReadOnlyList<float>?>.Register(
        nameof(Ticks),
        typeof(Slider),
        new UiPropertyMetadata<IReadOnlyList<float>?>(null, validateValue: TickBar.IsValidTicks));

    public static readonly UiProperty<bool> IsSnapToTickEnabledProperty = UiProperty<bool>.Register(
        nameof(IsSnapToTickEnabled),
        typeof(Slider),
        new UiPropertyMetadata<bool>(false));

    public static readonly UiProperty<TickPlacement> TickPlacementProperty = UiProperty<TickPlacement>.Register(
        nameof(TickPlacement),
        typeof(Slider),
        new UiPropertyMetadata<TickPlacement>(
            TickPlacement.None,
            UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange | UiPropertyOptions.AffectsRender,
            validateValue: value => Enum.IsDefined(value)));

    public static readonly UiProperty<float> SelectionStartProperty = UiProperty<float>.Register(
        nameof(SelectionStart),
        typeof(Slider),
        new UiPropertyMetadata<float>(0, validateValue: float.IsFinite, coerceValue: CoerceSelectionStart));

    public static readonly UiProperty<float> SelectionEndProperty = UiProperty<float>.Register(
        nameof(SelectionEnd),
        typeof(Slider),
        new UiPropertyMetadata<float>(0, validateValue: float.IsFinite, coerceValue: CoerceSelectionEnd));

    public static readonly UiProperty<bool> IsSelectionRangeEnabledProperty = UiProperty<bool>.Register(
        nameof(IsSelectionRangeEnabled),
        typeof(Slider),
        new UiPropertyMetadata<bool>(false));

    public static readonly UiProperty<AutoToolTipPlacement> AutoToolTipPlacementProperty = UiProperty<AutoToolTipPlacement>.Register(
        nameof(AutoToolTipPlacement),
        typeof(Slider),
        new UiPropertyMetadata<AutoToolTipPlacement>(AutoToolTipPlacement.None, validateValue: value => Enum.IsDefined(value)));

    public static readonly UiProperty<int> AutoToolTipPrecisionProperty = UiProperty<int>.Register(
        nameof(AutoToolTipPrecision),
        typeof(Slider),
        new UiPropertyMetadata<int>(0, validateValue: value => value is >= 0 and <= 99));

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    // As WPF's Slider.IsDirectionReversed: true puts Minimum at the right of a horizontal slider and at the top of a
    // vertical one, and swaps what the arrow keys do.
    public bool IsDirectionReversed
    {
        get => GetValue(IsDirectionReversedProperty);
        set => SetValue(IsDirectionReversedProperty, value);
    }

    // As WPF's Slider.IsMoveToPointEnabled: true makes a press on the track beside the thumb move the value to the
    // pressed point; false, the default, makes it move the value by LargeChange toward the press, and repeat while held.
    // The press may be anywhere on the slider outside the thumb, also on a TickBar beside the track.
    public bool IsMoveToPointEnabled
    {
        get => GetValue(IsMoveToPointEnabledProperty);
        set => SetValue(IsMoveToPointEnabledProperty, value);
    }

    // The milliseconds that a press held on the track waits before it repeats, as WPF's Slider.Delay.
    [MarkupValueConstraint(MarkupValueConstraint.NonNegative)]
    public int Delay
    {
        get => GetValue(DelayProperty);
        set => SetValue(DelayProperty, value);
    }

    // The milliseconds between the repeats of a press held on the track, as WPF's Slider.Interval.
    [MarkupValueConstraint(MarkupValueConstraint.Positive)]
    public int Interval
    {
        get => GetValue(IntervalProperty);
        set => SetValue(IntervalProperty, value);
    }

    // The distance between ticks from Minimum, used when Ticks is null or empty, as WPF's Slider.TickFrequency.
    [MarkupValueConstraint(MarkupValueConstraint.NonNegative)]
    public float TickFrequency
    {
        get => GetValue(TickFrequencyProperty);
        set => SetValue(TickFrequencyProperty, value);
    }

    // The tick values, in any order, as WPF's Slider.Ticks. When it has values, they replace TickFrequency.
    public IReadOnlyList<float>? Ticks
    {
        get => GetValue(TicksProperty);
        set => SetValue(TicksProperty, value);
    }

    // As WPF's Slider.IsSnapToTickEnabled: true makes thumb drags and track presses snap the value to the nearest tick,
    // and makes the keys and the track's repeat buttons move it to the next tick.
    public bool IsSnapToTickEnabled
    {
        get => GetValue(IsSnapToTickEnabledProperty);
        set => SetValue(IsSnapToTickEnabledProperty, value);
    }

    // As WPF's Slider.TickPlacement: where the default template shows a TickBar beside the track, TopLeft above a
    // horizontal slider and left of a vertical one, BottomRight below and right, Both on both sides; None shows none.
    public TickPlacement TickPlacement
    {
        get => GetValue(TickPlacementProperty);
        set => SetValue(TickPlacementProperty, value);
    }

    // As WPF's Slider.SelectionStart: where the selection range starts, kept between Minimum and Maximum.
    public float SelectionStart
    {
        get => GetValue(SelectionStartProperty);
        set => SetValue(SelectionStartProperty, value);
    }

    // As WPF's Slider.SelectionEnd: where the selection range ends, kept between SelectionStart and Maximum.
    public float SelectionEnd
    {
        get => GetValue(SelectionEndProperty);
        set => SetValue(SelectionEndProperty, value);
    }

    // As WPF's Slider.IsSelectionRangeEnabled: true shows the template's PART_SelectionRange, false collapses it.
    public bool IsSelectionRangeEnabled
    {
        get => GetValue(IsSelectionRangeEnabledProperty);
        set => SetValue(IsSelectionRangeEnabledProperty, value);
    }

    // As WPF's Slider.AutoToolTipPlacement: where a thumb drag shows a ToolTip with the value, TopLeft above a
    // horizontal thumb and left of a vertical one, BottomRight below and right; None shows none.
    public AutoToolTipPlacement AutoToolTipPlacement
    {
        get => GetValue(AutoToolTipPlacementProperty);
        set => SetValue(AutoToolTipPlacementProperty, value);
    }

    // As WPF's Slider.AutoToolTipPrecision: the number of decimals of the value in the drag ToolTip, from 0 to 99, the
    // range of NumberFormatInfo.NumberDecimalDigits.
    [MarkupValueConstraint(MarkupValueConstraint.NonNegative)]
    public int AutoToolTipPrecision
    {
        get => GetValue(AutoToolTipPrecisionProperty);
        set => SetValue(AutoToolTipPrecisionProperty, value);
    }

    // The ToolTip that a thumb drag opens when AutoToolTipPlacement is not None; null before the first such drag.
    internal ToolTip? AutoToolTip => autoToolTip;

    public Track Track
    {
        get
        {
            ApplyTemplate();
            return track ?? throw new InvalidOperationException("Slider template did not provide the required part 'PART_Track'.");
        }
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        ApplyTemplate();
        SyncTrack();
        LayoutSize size = base.MeasureCore(context);
        autoToolTip?.Measure(context);
        return size;
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        SyncTrack();
        LayoutRect arranged = base.ArrangeCore(context);

        // The ToolTip draws its content in the overlay layer beside the thumb, and takes no space here: it gets the zero
        // size that it measures, so it never covers the thumb or the track for hit testing.
        autoToolTip?.Arrange(new ArrangeContext(new LayoutRect(context.FinalRect.X, context.FinalRect.Y, 0, 0), context.Rounding));
        return arranged;
    }

    protected override void OnTemplateApplied(ComponentTemplateInstance? instance)
    {
        if (track is not null)
        {
            track.ValueChangedWithReason -= OnTrackValueChanged;
        }

        track = null;
        selectionRange = null;

        // A drag ToolTip of the old template's thumb closes, and keeps no reference to that thumb.
        if (autoToolTip is not null)
        {
            autoToolTip.IsOpen = false;
            autoToolTip.SetCustomPlacement(null, null);
        }

        if (instance is null)
        {
            return;
        }

        track = GetRequiredTemplatePart<Track>("PART_Track");
        track.ValueChangedWithReason += OnTrackValueChanged;
        selectionRange = GetOptionalTemplatePart<UIElement>("PART_SelectionRange");
        UpdateSelectionRangeVisibility();
        SyncTrack();
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (!syncingTrack &&
            (ReferenceEquals(args.Property, MinimumProperty) ||
            ReferenceEquals(args.Property, MaximumProperty) ||
            ReferenceEquals(args.Property, ValueProperty) ||
            ReferenceEquals(args.Property, SmallChangeProperty) ||
            ReferenceEquals(args.Property, LargeChangeProperty) ||
            ReferenceEquals(args.Property, OrientationProperty) ||
            ReferenceEquals(args.Property, IsDirectionReversedProperty) ||
            ReferenceEquals(args.Property, DelayProperty) ||
            ReferenceEquals(args.Property, IntervalProperty)))
        {
            SyncTrack();
        }

        // A ClearValue can reveal a value that no write coerced, such as the default 0 below Minimum 2: it is corrected
        // at the source that holds it, as RangeBase.Value is (#888).
        if (ReferenceEquals(args.Property, SelectionStartProperty))
        {
            float selectionStart = CoerceSelectionStart(this, SelectionStart);
            if (selectionStart != SelectionStart)
            {
                Correct(this, SelectionStartProperty, selectionStart);
            }
        }

        if (ReferenceEquals(args.Property, SelectionEndProperty))
        {
            float selectionEnd = CoerceSelectionEnd(this, SelectionEnd);
            if (selectionEnd != SelectionEnd)
            {
                Correct(this, SelectionEndProperty, selectionEnd);
            }
        }

        // As WPF's Slider, which coerces SelectionStart again when Minimum or Maximum changes, and SelectionEnd again
        // when SelectionStart changes.
        if (ReferenceEquals(args.Property, MinimumProperty) || ReferenceEquals(args.Property, MaximumProperty))
        {
            float selectionStart = CoerceSelectionStart(this, SelectionStart);
            if (selectionStart != SelectionStart)
            {
                SelectionStart = selectionStart;
            }
        }

        if (ReferenceEquals(args.Property, MinimumProperty) ||
            ReferenceEquals(args.Property, MaximumProperty) ||
            ReferenceEquals(args.Property, SelectionStartProperty))
        {
            float selectionEnd = CoerceSelectionEnd(this, SelectionEnd);
            if (selectionEnd != SelectionEnd)
            {
                SelectionEnd = selectionEnd;
            }
        }

        if (ReferenceEquals(args.Property, IsSelectionRangeEnabledProperty))
        {
            UpdateSelectionRangeVisibility();
        }

        // As WPF's Slider.OnThumbDragDelta: the open drag ToolTip shows each new value.
        if (ReferenceEquals(args.Property, ValueProperty) && autoToolTip is { IsOpen: true })
        {
            UpdateAutoToolTipText();
        }

        if (ReferenceEquals(args.Property, ComponentTemplateProperty) &&
            ComponentTemplate is null &&
            GetSourceValue(ComponentTemplateProperty, UiPropertyValueSource.AspectBase) is ComponentTemplate)
        {
            ClearValue(ComponentTemplateProperty);
        }
    }

    private void SyncTrack()
    {
        if (track is null)
        {
            return;
        }

        syncingTrack = true;
        try
        {
            track.Minimum = Minimum;
            track.Maximum = Maximum;
            track.Value = Value;
            track.SmallChange = SmallChange;
            track.LargeChange = LargeChange;
            track.Orientation = Orientation;
            track.IsDirectionReversed = IsDirectionReversed;
            SyncRepeatButton(track.DecreaseRepeatButton);
            SyncRepeatButton(track.IncreaseRepeatButton);
        }
        finally
        {
            syncingTrack = false;
        }
    }

    private void SyncRepeatButton(RepeatButton? button)
    {
        if (button is null)
        {
            return;
        }

        button.Delay = Delay;
        button.Interval = Interval;
    }

    // As a WPF Slider focuses itself on a left MouseDown inside it, also one that its Thumb already handled, a press on
    // a focusable slider moves keyboard focus to it unless the focus is already inside it (#662).
    private void OnMouseDown(object _, RoutedEventArgs args)
    {
        if (args is not MouseButtonEventArgs { ChangedButton: InputMouseButton.Left, RouteMap: { } routeMap } ||
            IsKeyboardFocusWithin ||
            Root is not UIRoot { ActiveFocusManager: { } focusManager })
        {
            return;
        }

        if (focusManager.Focus(this, routeMap))
        {
            args.Handled = true;
        }
    }

    // As WPF's Slider.OnPreviewMouseLeftButtonDown with IsMoveToPointEnabled: a left press on the slider outside the
    // thumb, on the track or on a TickBar beside it, moves the value to the pressed point, snapped to a tick. The handled
    // preview keeps the press from the repeat buttons, so it does not page and does not repeat. A press on the thumb
    // still starts its drag.
    private void OnPreviewMouseDown(object _, RoutedEventArgs args)
    {
        if (!IsMoveToPointEnabled ||
            track is null ||
            args is not MouseButtonEventArgs { ChangedButton: InputMouseButton.Left } mouseArgs)
        {
            return;
        }

        // Hit testing applies render transforms, so the press is compared where the track and thumb are drawn.
        Vector2 rootPoint = new(mouseArgs.RootX, mouseArgs.RootY);
        if (!InputCoordinateConverter.TryRootToElement(track, rootPoint, out Vector2 trackPoint) ||
            IsInside(track.Thumb, rootPoint))
        {
            return;
        }

        Value = SnapToTick(track.ValueFromPoint(track.ArrangedBounds.X + trackPoint.X, track.ArrangedBounds.Y + trackPoint.Y));
        args.Handled = true;
    }

    private static bool IsInside(UIElement element, Vector2 rootPoint)
    {
        return InputCoordinateConverter.TryRootToElement(element, rootPoint, out Vector2 point) &&
            point.X >= 0 &&
            point.Y >= 0 &&
            point.X < element.ArrangedBounds.Width &&
            point.Y < element.ArrangedBounds.Height;
    }

    // A thumb drag snaps the value that the pointer reached since the drag started, so a thumb that snapping keeps in
    // place still follows the pointer to the next tick.
    private void OnThumbDragStarted(object _, RoutedEventArgs args)
    {
        if (track is not null && ReferenceEquals(args.OriginalSource, track.Thumb))
        {
            dragStartValue = Value;
            OpenAutoToolTip(track.Thumb);
        }
    }

    // As WPF's Slider.OnThumbDragCompleted: the end of the drag closes the drag ToolTip.
    private void OnThumbDragCompleted(object _, RoutedEventArgs args)
    {
        if (autoToolTip is not null && track is not null && ReferenceEquals(args.OriginalSource, track.Thumb))
        {
            autoToolTip.IsOpen = false;
        }
    }

    // As WPF's Slider.OnThumbDragStarted with an AutoToolTipPlacement: the slider's own ToolTip opens beside the thumb
    // with the value. The slider hosts the ToolTip, so it works with any template that has a Track.
    private void OpenAutoToolTip(Thumb thumb)
    {
        if (AutoToolTipPlacement == AutoToolTipPlacement.None)
        {
            return;
        }

        if (autoToolTip is null)
        {
            // The ToolTip hosts element content, so the number is the Text of a TextBlock.
            autoToolTip = new ToolTip { Content = new TextBlock() };
            LogicalChildren.Add(autoToolTip);
            VisualChildren.Add(autoToolTip);
        }

        autoToolTip.SetCustomPlacement(thumb, PlaceAutoToolTip);
        UpdateAutoToolTipText();
        autoToolTip.IsOpen = true;
    }

    // WPF's Slider.AutoToolTipCustomPlacementCallback: TopLeft centers the ToolTip above a horizontal thumb and left of
    // a vertical one, BottomRight below and right.
    private LayoutPoint PlaceAutoToolTip(LayoutRect thumb, LayoutSize size)
    {
        bool topLeft = AutoToolTipPlacement == AutoToolTipPlacement.TopLeft;
        return Orientation == Orientation.Horizontal
            ? new LayoutPoint(
                thumb.X + ((thumb.Width - size.Width) * 0.5f),
                topLeft ? thumb.Y - size.Height : thumb.Y + thumb.Height)
            : new LayoutPoint(
                topLeft ? thumb.X - size.Width : thumb.X + thumb.Width,
                thumb.Y + ((thumb.Height - size.Height) * 0.5f));
    }

    // WPF's Slider.GetAutoToolTipNumber: the value in the current culture's number format with AutoToolTipPrecision
    // decimals, for example 4.7 with precision 1, or 1,234.50 with precision 2 in English.
    private void UpdateAutoToolTipText()
    {
        NumberFormatInfo format = (NumberFormatInfo)NumberFormatInfo.CurrentInfo.Clone();
        format.NumberDecimalDigits = AutoToolTipPrecision;
        ((TextBlock)autoToolTip!.Content!).Text = Value.ToString("N", format);
    }

    private void UpdateSelectionRangeVisibility()
    {
        if (selectionRange is not null)
        {
            selectionRange.Visibility = IsSelectionRangeEnabled ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    // A value that is not finite stays as it is, so the validation rejects it, as WPF validates before it coerces.
    private static float CoerceSelectionStart(UiObject owner, float value)
    {
        Slider slider = (Slider)owner;
        return float.IsFinite(value) ? Clamp(value, slider.Minimum, slider.Maximum) : value;
    }

    private static float CoerceSelectionEnd(UiObject owner, float value)
    {
        Slider slider = (Slider)owner;
        return float.IsFinite(value) ? Clamp(value, slider.SelectionStart, slider.Maximum) : value;
    }

    // The track reports each change that its thumb, its repeat buttons and its own methods make, and the slider decides
    // the value, as WPF's Slider handles the drag and the commands of its track: a drag or a move to a point snaps to a
    // tick, a small or large step moves to the next tick, and the track then shows the slider's value.
    private void OnTrackValueChanged(object? sender, TrackValueChangedEventArgs args)
    {
        if (syncingTrack || !ReferenceEquals(sender, track))
        {
            return;
        }

        float? value = args.Reason switch
        {
            TrackValueChangeReason.SmallDecrement => NextTickValue(-SmallChange),
            TrackValueChangeReason.SmallIncrement => NextTickValue(SmallChange),
            TrackValueChangeReason.LargeDecrement => NextTickValue(-LargeChange),
            TrackValueChangeReason.LargeIncrement => NextTickValue(LargeChange),
            TrackValueChangeReason.ThumbTrack => SnapToTick(GetThumbTrackValue(args.NewValue)),
            _ => args.NewValue
        };

        if (value is { } next)
        {
            Value = next;
        }

        SyncTrack();
    }

    private float GetThumbTrackValue(float trackValue)
    {
        Thumb thumb = track!.Thumb;
        return thumb.IsDragging
            ? Clamp(dragStartValue + track.ValueFromDistance(thumb.TotalHorizontalChange, thumb.TotalVerticalChange), Minimum, Maximum)
            : trackValue;
    }

    // As WPF's Slider commands and SliderGesture: with no modifier key, Up and Right raise the value by SmallChange and
    // Down and Left lower it, swapped when IsDirectionReversed is true; PageUp and PageDown change it by LargeChange, Home
    // sets Minimum and End sets Maximum. Each of these keys is handled, also when the value is already at the end.
    private void OnKeyDown(object _, RoutedEventArgs args)
    {
        if (args is not KeyEventArgs keyArgs ||
            !IsEnabled ||
            keyArgs.IsControlDown ||
            keyArgs.IsShiftDown ||
            keyArgs.IsAltDown)
        {
            return;
        }

        float smallStep = IsDirectionReversed ? -SmallChange : SmallChange;
        switch (keyArgs.Key)
        {
            case InputKey.Up:
            case InputKey.Right:
                MoveToNextTick(smallStep);
                break;
            case InputKey.Down:
            case InputKey.Left:
                MoveToNextTick(-smallStep);
                break;
            case InputKey.PageUp:
                MoveToNextTick(LargeChange);
                break;
            case InputKey.PageDown:
                MoveToNextTick(-LargeChange);
                break;
            case InputKey.Home:
                Value = Minimum;
                break;
            case InputKey.End:
                Value = Maximum;
                break;
            default:
                return;
        }

        args.Handled = true;
    }

    private void MoveToNextTick(float direction)
    {
        if (NextTickValue(direction) is { } next)
        {
            Value = next;
        }
    }

    // WPF's Slider.MoveToNextTick, which returns the value to set instead of setting it: null when the value stays. Unlike
    // WPF, Minimum and Maximum are also the next ticks of explicit Ticks, so the keys can reach them.
    private float? NextTickValue(float direction)
    {
        if (direction == 0)
        {
            return null;
        }

        float value = Value;
        float next = SnapToTick(MathF.Max(Minimum, MathF.Min(Maximum, value + direction)));
        bool greaterThan = direction > 0;
        if (next == value &&
            !(greaterThan && value == Maximum) &&
            !(!greaterThan && value == Minimum))
        {
            if (Ticks is { Count: > 0 } ticks)
            {
                // Exact comparisons, as in SnapToTick: the float tolerance of FloatComparison (about 0.24 at 1000000)
                // would skip a listed tick near Value and move past it to Maximum or Minimum (#927).
                foreach (float tick in ticks)
                {
                    if ((greaterThan && tick > value && (tick < next || next == value)) ||
                        (!greaterThan && tick < value && (tick > next || next == value)))
                    {
                        next = tick;
                    }
                }

                // Minimum and Maximum count as ticks, as SnapToTick and the TickBar treat them, also when Ticks does not
                // list them: past the last listed tick, the next tick is Maximum, and before the first one Minimum (#928).
                if (next == value)
                {
                    next = greaterThan ? Maximum : Minimum;
                }
            }
            else if (TickFrequency > 0)
            {
                // Math.Round rounds a midpoint to the even number, as WPF's Math.Round does. Computed in double, as in
                // SnapToTick, so a very small TickFrequency neither overflows nor loses its multiples (#927).
                double tickNumber = Math.Round((value - (double)Minimum) / TickFrequency);
                tickNumber += greaterThan ? 1 : -1;
                next = (float)(Minimum + (tickNumber * TickFrequency));
            }
        }

        return next != value ? next : null;
    }

    // WPF's Slider.SnapToTick: with IsSnapToTickEnabled, the value moves to the nearest tick, or to Minimum or Maximum
    // when they are nearer; a value halfway between two ticks moves to the greater one.
    // WPF computes in double and its tolerance of DoubleUtil is far below any tick distance. The float tolerance of
    // FloatComparison grows with the size of the values (about 0.24 at 1000000) and does not shrink below about 1e-6, so
    // the ticks are computed in double, and the halfway test is relative to the distance between the two ticks (#927).
    private float SnapToTick(float value)
    {
        if (!IsSnapToTickEnabled)
        {
            return value;
        }

        double previous = Minimum;
        double next = Maximum;
        if (Ticks is { Count: > 0 } ticks)
        {
            foreach (float tick in ticks)
            {
                if (tick == value)
                {
                    return value;
                }

                if (tick < value && tick > previous)
                {
                    previous = tick;
                }
                else if (tick > value && tick < next)
                {
                    next = tick;
                }
            }
        }
        else if (TickFrequency > 0)
        {
            // Math.Round rounds a midpoint to the even number, as WPF's Math.Round does.
            previous = Minimum + (Math.Round((value - (double)Minimum) / TickFrequency) * TickFrequency);
            next = Math.Min(Maximum, previous + TickFrequency);
        }

        double distance = next - previous;
        return (float)(distance <= 0 || FloatComparison.GreaterThanOrClose((float)((value - previous) / distance), 0.5f)
            ? next
            : previous);
    }
}

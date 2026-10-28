using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Controls;

// The layout of WPF's default Slider template: a horizontal slider has a TickBar 4 pixels high above its track and one
// below it, each 2 pixels from the track; a vertical slider has them 4 pixels wide at the left and at the right.
// TickPlacement shows the TickBar of its side and collapses the other, so a collapsed TickBar takes no space. The ticks
// keep the thumb's length free at their ends, so they line up with the thumb's center.
// As WPF's TrackBackground, the track bar and the selection range lie under the track, over the track's whole rect, so
// the track's transparent repeat buttons and its thumb stay on top of them.
internal sealed class SliderLayoutPanel : Cerneala.UI.Layout.Panels.Panel
{
    private const float TickBarThickness = 4;
    private const float TickBarGap = 2;
    private readonly TickBar topLeftTickBar;
    private readonly UIElement? trackBackground;
    private readonly UIElement? selectionRange;
    private readonly Track track;
    private readonly TickBar bottomRightTickBar;

    public SliderLayoutPanel(
        TickBar topLeftTickBar,
        UIElement? trackBackground,
        UIElement? selectionRange,
        Track track,
        TickBar bottomRightTickBar)
    {
        this.topLeftTickBar = topLeftTickBar;
        this.trackBackground = trackBackground;
        this.selectionRange = selectionRange;
        this.track = track;
        this.bottomRightTickBar = bottomRightTickBar;
        VisualChildren.Add(topLeftTickBar);
        if (trackBackground is not null)
        {
            VisualChildren.Add(trackBackground);
        }

        if (selectionRange is not null)
        {
            VisualChildren.Add(selectionRange);
        }

        VisualChildren.Add(track);
        VisualChildren.Add(bottomRightTickBar);
        UpdateTickBars();
    }

    public static readonly UiProperty<Orientation> OrientationProperty = UiProperty<Orientation>.Register(
        nameof(Orientation),
        typeof(SliderLayoutPanel),
        new UiPropertyMetadata<Orientation>(Orientation.Horizontal, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange));

    public static readonly UiProperty<TickPlacement> TickPlacementProperty = UiProperty<TickPlacement>.Register(
        nameof(TickPlacement),
        typeof(SliderLayoutPanel),
        new UiPropertyMetadata<TickPlacement>(TickPlacement.None, UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsArrange));

    public static readonly UiProperty<float> MinimumProperty = UiProperty<float>.Register(
        nameof(Minimum),
        typeof(SliderLayoutPanel),
        new UiPropertyMetadata<float>(0, UiPropertyOptions.AffectsArrange));

    public static readonly UiProperty<float> MaximumProperty = UiProperty<float>.Register(
        nameof(Maximum),
        typeof(SliderLayoutPanel),
        new UiPropertyMetadata<float>(1, UiPropertyOptions.AffectsArrange));

    public static readonly UiProperty<float> SelectionStartProperty = UiProperty<float>.Register(
        nameof(SelectionStart),
        typeof(SliderLayoutPanel),
        new UiPropertyMetadata<float>(0, UiPropertyOptions.AffectsArrange));

    public static readonly UiProperty<float> SelectionEndProperty = UiProperty<float>.Register(
        nameof(SelectionEnd),
        typeof(SliderLayoutPanel),
        new UiPropertyMetadata<float>(0, UiPropertyOptions.AffectsArrange));

    public static readonly UiProperty<bool> IsDirectionReversedProperty = UiProperty<bool>.Register(
        nameof(IsDirectionReversed),
        typeof(SliderLayoutPanel),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsArrange));

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public TickPlacement TickPlacement
    {
        get => GetValue(TickPlacementProperty);
        set => SetValue(TickPlacementProperty, value);
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

    public float SelectionStart
    {
        get => GetValue(SelectionStartProperty);
        set => SetValue(SelectionStartProperty, value);
    }

    public float SelectionEnd
    {
        get => GetValue(SelectionEndProperty);
        set => SetValue(SelectionEndProperty, value);
    }

    public bool IsDirectionReversed
    {
        get => GetValue(IsDirectionReversedProperty);
        set => SetValue(IsDirectionReversedProperty, value);
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        bool horizontal = Orientation == Orientation.Horizontal;
        float bands = GetBand(topLeftTickBar) + GetBand(bottomRightTickBar);
        LayoutSize available = context.AvailableSize;
        LayoutSize trackAvailable = horizontal
            ? new LayoutSize(available.Width, MathF.Max(0, available.Height - bands))
            : new LayoutSize(MathF.Max(0, available.Width - bands), available.Height);
        LayoutSize trackSize = track.Measure(new MeasureContext(trackAvailable, context.Rounding));
        LayoutSize tickBarAvailable = horizontal
            ? new LayoutSize(available.Width, TickBarThickness)
            : new LayoutSize(TickBarThickness, available.Height);
        MeasureTickBar(topLeftTickBar, tickBarAvailable, context);
        MeasureTickBar(bottomRightTickBar, tickBarAvailable, context);

        // The track bar and the selection range take the track's rect, so their own sizes do not change the slider's.
        trackBackground?.Measure(new MeasureContext(trackSize, context.Rounding));
        selectionRange?.Measure(new MeasureContext(
            selectionRange.Visibility == Visibility.Collapsed ? LayoutSize.Zero : trackSize,
            context.Rounding));

        return horizontal
            ? new LayoutSize(trackSize.Width, trackSize.Height + bands)
            : new LayoutSize(trackSize.Width + bands, trackSize.Height);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        LayoutRect final = context.FinalRect;
        bool horizontal = Orientation == Orientation.Horizontal;
        float leading = GetBand(topLeftTickBar);
        float trailing = GetBand(bottomRightTickBar);
        LayoutRect trackRect = horizontal
            ? new LayoutRect(final.X, final.Y + leading, final.Width, MathF.Max(0, final.Height - leading - trailing))
            : new LayoutRect(final.X + leading, final.Y, MathF.Max(0, final.Width - leading - trailing), final.Height);
        track.Arrange(new ArrangeContext(trackRect, context.Rounding));
        trackBackground?.Arrange(new ArrangeContext(trackRect, context.Rounding));

        LayoutRect thumb = track.Thumb.ArrangedBounds;
        ArrangeSelectionRange(trackRect, thumb, context);
        float reservedSpace = horizontal ? thumb.Width : thumb.Height;
        LayoutRect topLeftRect = horizontal
            ? new LayoutRect(final.X, final.Y, final.Width, TickBarThickness)
            : new LayoutRect(final.X, final.Y, TickBarThickness, final.Height);
        LayoutRect bottomRightRect = horizontal
            ? new LayoutRect(final.X, final.Y + final.Height - TickBarThickness, final.Width, TickBarThickness)
            : new LayoutRect(final.X + final.Width - TickBarThickness, final.Y, TickBarThickness, final.Height);
        ArrangeTickBar(topLeftTickBar, topLeftRect, reservedSpace, context);
        ArrangeTickBar(bottomRightTickBar, bottomRightRect, reservedSpace, context);
        return final;
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, OrientationProperty) || ReferenceEquals(args.Property, TickPlacementProperty))
        {
            UpdateTickBars();
        }
    }

    // WPF's Slider.UpdateSelectionRangeElementPositionAndSize: the range covers SelectionStart..SelectionEnd along the
    // travel of the thumb's center, which starts half a thumb from each end of the track, and the whole track across.
    // In double, as WPF computes it: Maximum - Minimum of two finite floats can overflow float (for example 3e38 - -3e38),
    // and so can the differences of SelectionStart and SelectionEnd with each other and with the ends (#1119).
    private void ArrangeSelectionRange(LayoutRect trackRect, LayoutRect thumb, ArrangeContext context)
    {
        if (selectionRange is null)
        {
            return;
        }

        if (selectionRange.Visibility == Visibility.Collapsed)
        {
            selectionRange.Arrange(new ArrangeContext(new LayoutRect(trackRect.X, trackRect.Y, 0, 0), context.Rounding));
            return;
        }

        bool horizontal = Orientation == Orientation.Horizontal;
        float trackLength = horizontal ? trackRect.Width : trackRect.Height;
        float thumbLength = horizontal ? thumb.Width : thumb.Height;
        double range = (double)Maximum - Minimum;
        // (float)range is infinite when the range overflows float, and an infinite range is not close to 0.
        double valueToSize = FloatComparison.AreClose((float)range, 0) || FloatComparison.AreClose(trackLength, thumbLength)
            ? 0
            : Math.Max(0, (trackLength - thumbLength) / range);
        float length = (float)Math.Max(0, ((double)SelectionEnd - SelectionStart) * valueToSize);
        LayoutRect rect;
        if (horizontal)
        {
            double before = IsDirectionReversed ? (double)Maximum - SelectionEnd : (double)SelectionStart - Minimum;
            float left = (thumbLength * 0.5f) + (float)(Math.Max(before, 0) * valueToSize);
            rect = new LayoutRect(trackRect.X + left, trackRect.Y, length, trackRect.Height);
        }
        else
        {
            double before = IsDirectionReversed ? (double)SelectionStart - Minimum : (double)Maximum - SelectionEnd;
            float top = (thumbLength * 0.5f) + (float)(Math.Max(before, 0) * valueToSize);
            rect = new LayoutRect(trackRect.X, trackRect.Y + top, trackRect.Width, length);
        }

        selectionRange.Arrange(new ArrangeContext(rect, context.Rounding));
    }

    private void UpdateTickBars()
    {
        bool horizontal = Orientation == Orientation.Horizontal;
        topLeftTickBar.Placement = horizontal ? TickBarPlacement.Top : TickBarPlacement.Left;
        bottomRightTickBar.Placement = horizontal ? TickBarPlacement.Bottom : TickBarPlacement.Right;
        topLeftTickBar.Visibility = TickPlacement is TickPlacement.TopLeft or TickPlacement.Both
            ? Visibility.Visible
            : Visibility.Collapsed;
        bottomRightTickBar.Visibility = TickPlacement is TickPlacement.BottomRight or TickPlacement.Both
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static float GetBand(TickBar tickBar)
    {
        return tickBar.Visibility == Visibility.Collapsed ? 0 : TickBarThickness + TickBarGap;
    }

    private static void MeasureTickBar(TickBar tickBar, LayoutSize available, MeasureContext context)
    {
        tickBar.Measure(new MeasureContext(
            tickBar.Visibility == Visibility.Collapsed ? LayoutSize.Zero : available,
            context.Rounding));
    }

    private static void ArrangeTickBar(TickBar tickBar, LayoutRect rect, float reservedSpace, ArrangeContext context)
    {
        if (tickBar.Visibility == Visibility.Collapsed)
        {
            tickBar.Arrange(new ArrangeContext(new LayoutRect(context.FinalRect.X, context.FinalRect.Y, 0, 0), context.Rounding));
            return;
        }

        tickBar.ReservedSpace = reservedSpace;
        tickBar.Arrange(new ArrangeContext(rect, context.Rounding));
    }
}

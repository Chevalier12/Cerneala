using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Markup;
using Cerneala.UI.Media;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Controls.Primitives;

// As WPF's TickBar: draws the ticks of a range along its length, as a Slider's template shows them beside its track. The
// ticks at Minimum and Maximum are as long as the TickBar is thick, the others three quarters of that. ReservedSpace is
// the length kept free at the two ends, half at each, usually the thumb's length, so the ticks line up with the thumb's
// center.
public class TickBar : UIElement
{
    public static readonly UiProperty<float> MinimumProperty = UiProperty<float>.Register(
        nameof(Minimum),
        typeof(TickBar),
        new UiPropertyMetadata<float>(0, UiPropertyOptions.AffectsRender, validateValue: float.IsFinite));

    public static readonly UiProperty<float> MaximumProperty = UiProperty<float>.Register(
        nameof(Maximum),
        typeof(TickBar),
        new UiPropertyMetadata<float>(100, UiPropertyOptions.AffectsRender, validateValue: float.IsFinite));

    public static readonly UiProperty<float> TickFrequencyProperty = UiProperty<float>.Register(
        nameof(TickFrequency),
        typeof(TickBar),
        new UiPropertyMetadata<float>(1, UiPropertyOptions.AffectsRender, validateValue: IsValidNonNegative));

    public static readonly UiProperty<IReadOnlyList<float>?> TicksProperty = UiProperty<IReadOnlyList<float>?>.Register(
        nameof(Ticks),
        typeof(TickBar),
        new UiPropertyMetadata<IReadOnlyList<float>?>(null, UiPropertyOptions.AffectsRender, validateValue: IsValidTicks));

    public static readonly UiProperty<TickBarPlacement> PlacementProperty = UiProperty<TickBarPlacement>.Register(
        nameof(Placement),
        typeof(TickBar),
        new UiPropertyMetadata<TickBarPlacement>(TickBarPlacement.Top, UiPropertyOptions.AffectsRender, validateValue: value => Enum.IsDefined(value)));

    public static readonly UiProperty<Brush?> FillProperty = UiProperty<Brush?>.Register(
        nameof(Fill),
        typeof(TickBar),
        new UiPropertyMetadata<Brush?>(null, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<bool> IsDirectionReversedProperty = UiProperty<bool>.Register(
        nameof(IsDirectionReversed),
        typeof(TickBar),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.AffectsRender));

    public static readonly UiProperty<float> ReservedSpaceProperty = UiProperty<float>.Register(
        nameof(ReservedSpace),
        typeof(TickBar),
        new UiPropertyMetadata<float>(0, UiPropertyOptions.AffectsRender, validateValue: IsValidNonNegative));

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

    // The distance between ticks from Minimum, used when Ticks is null or empty.
    [MarkupValueConstraint(MarkupValueConstraint.NonNegative)]
    public float TickFrequency
    {
        get => GetValue(TickFrequencyProperty);
        set => SetValue(TickFrequencyProperty, value);
    }

    // The tick values, in any order. When it has values, they replace TickFrequency.
    public IReadOnlyList<float>? Ticks
    {
        get => GetValue(TicksProperty);
        set => SetValue(TicksProperty, value);
    }

    public TickBarPlacement Placement
    {
        get => GetValue(PlacementProperty);
        set => SetValue(PlacementProperty, value);
    }

    // The brush of the ticks. Null, the default, draws nothing.
    public Brush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    // False puts Minimum at the left of a horizontal TickBar and at the bottom of a vertical one; true puts it at the
    // right and at the top.
    public bool IsDirectionReversed
    {
        get => GetValue(IsDirectionReversedProperty);
        set => SetValue(IsDirectionReversedProperty, value);
    }

    [MarkupValueConstraint(MarkupValueConstraint.NonNegative)]
    public float ReservedSpace
    {
        get => GetValue(ReservedSpaceProperty);
        set => SetValue(ReservedSpaceProperty, value);
    }

    // WPF's TickBar.OnRender, with one-pixel lines of Fill, without its selection ticks.
    protected override void OnRender(RenderContext context)
    {
        if (Fill is not { } fill)
        {
            return;
        }

        LayoutRect bounds = context.Bounds;
        float width = MathF.Max(0, bounds.Width);
        float height = MathF.Max(0, bounds.Height);
        // In double, as WPF: Maximum - Minimum of two finite floats can overflow float (for example 3e38 - -3e38), and so
        // can a tick's distance from Minimum and every value computed from them (#1118).
        double range = (double)Maximum - Minimum;
        float halfReservedSpace = ReservedSpace * 0.5f;
        float tickLength;
        double logicalToPhysical;
        DrawPoint start;
        DrawPoint end;
        bool vertical = Placement is TickBarPlacement.Left or TickBarPlacement.Right;
        switch (Placement)
        {
            case TickBarPlacement.Top:
                if (FloatComparison.GreaterThanOrClose(ReservedSpace, width))
                {
                    return;
                }

                width -= ReservedSpace;
                tickLength = -height;
                start = new DrawPoint(halfReservedSpace, height);
                end = new DrawPoint(halfReservedSpace + width, height);
                logicalToPhysical = width / range;
                break;
            case TickBarPlacement.Bottom:
                if (FloatComparison.GreaterThanOrClose(ReservedSpace, width))
                {
                    return;
                }

                width -= ReservedSpace;
                tickLength = height;
                start = new DrawPoint(halfReservedSpace, 0);
                end = new DrawPoint(halfReservedSpace + width, 0);
                logicalToPhysical = width / range;
                break;
            case TickBarPlacement.Left:
                if (FloatComparison.GreaterThanOrClose(ReservedSpace, height))
                {
                    return;
                }

                height -= ReservedSpace;
                tickLength = -width;
                start = new DrawPoint(width, height + halfReservedSpace);
                end = new DrawPoint(width, halfReservedSpace);
                logicalToPhysical = height / range * -1;
                break;
            default:
                if (FloatComparison.GreaterThanOrClose(ReservedSpace, height))
                {
                    return;
                }

                height -= ReservedSpace;
                tickLength = width;
                start = new DrawPoint(0, height + halfReservedSpace);
                end = new DrawPoint(0, halfReservedSpace);
                logicalToPhysical = height / range * -1;
                break;
        }

        float shortTickLength = tickLength * 0.75f;
        if (IsDirectionReversed)
        {
            logicalToPhysical *= -1;
            (start, end) = (end, start);
        }

        // A TickFrequency that would put ticks closer than one pixel is raised to the range of one pixel.
        double interval = TickFrequency;
        if (interval > 0)
        {
            double minimumInterval = range / (vertical ? height : width);
            if (interval < minimumInterval)
            {
                interval = minimumInterval;
            }
        }

        DrawingContext drawing = context.DrawingContext;
        float originX = bounds.X;
        float originY = bounds.Y;
        if (vertical)
        {
            DrawTick(drawing, fill, originX, originY, start.X, start.Y, tickLength, vertical);
            DrawTick(drawing, fill, originX, originY, start.X, end.Y, tickLength, vertical);
        }
        else
        {
            DrawTick(drawing, fill, originX, originY, start.X, start.Y, tickLength, vertical);
            DrawTick(drawing, fill, originX, originY, end.X, start.Y, tickLength, vertical);
        }

        float startAlong = vertical ? start.Y : start.X;
        if (Ticks is { Count: > 0 } ticks)
        {
            foreach (float tick in ticks)
            {
                if (FloatComparison.LessThanOrClose(tick, Minimum) || FloatComparison.GreaterThanOrClose(tick, Maximum))
                {
                    continue;
                }

                float along = (float)((((double)tick - Minimum) * logicalToPhysical) + startAlong);
                DrawAlong(drawing, fill, originX, originY, start, along, shortTickLength, vertical);
            }
        }
        else if (interval > 0)
        {
            for (double value = interval; value < range; value += interval)
            {
                float along = (float)((value * logicalToPhysical) + startAlong);
                DrawAlong(drawing, fill, originX, originY, start, along, shortTickLength, vertical);
            }
        }
    }

    private static void DrawAlong(
        DrawingContext drawing,
        Brush fill,
        float originX,
        float originY,
        DrawPoint start,
        float along,
        float length,
        bool vertical)
    {
        if (vertical)
        {
            DrawTick(drawing, fill, originX, originY, start.X, along, length, vertical);
        }
        else
        {
            DrawTick(drawing, fill, originX, originY, along, start.Y, length, vertical);
        }
    }

    // A tick from a point on the base line, across the TickBar: rightward or leftward on a vertical TickBar, downward or
    // upward on a horizontal one.
    private static void DrawTick(
        DrawingContext drawing,
        Brush fill,
        float originX,
        float originY,
        float x,
        float y,
        float length,
        bool vertical)
    {
        DrawPoint from = new(originX + x, originY + y);
        DrawPoint to = vertical
            ? new DrawPoint(originX + x + length, originY + y)
            : new DrawPoint(originX + x, originY + y + length);
        drawing.DrawLine(from, to, fill, 1);
    }

    private static bool IsValidNonNegative(float value)
    {
        return value >= 0 && float.IsFinite(value);
    }

    internal static bool IsValidTicks(IReadOnlyList<float>? ticks)
    {
        return ticks is null || ticks.All(float.IsFinite);
    }
}

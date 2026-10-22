using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;
using Cerneala.UI.Markup;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Controls;

public class Border : Decorator
{
    public static readonly UiProperty<CornerRadius> CornerRadiusProperty = UiProperty<CornerRadius>.Register(
        nameof(CornerRadius),
        typeof(Border),
        new UiPropertyMetadata<CornerRadius>(CornerRadius.Zero, UiPropertyOptions.AffectsRender, validateValue: IsValidCornerRadius));

    [MarkupValueConstraint(MarkupValueConstraint.NonNegative)]
    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    protected override void OnRender(RenderContext context)
    {
        RenderBox(this, context, context.Bounds, CornerRadius);
    }

    internal static void RenderBox(Control control, RenderContext context)
    {
        RenderBox(control, context, context.Bounds);
    }

    // Draws the box of control over bounds, a part of its own bounds, for a control whose chrome surrounds only one
    // area, as the TabControl content area below the header row. Only a Border has a corner radius; the other
    // controls draw square corners.
    internal static void RenderBox(Control control, RenderContext context, LayoutRect bounds, CornerRadius cornerRadius = default)
    {
        control.RenderBackground(context, bounds, cornerRadius);
        RenderBorder(control, context, bounds, cornerRadius);
    }

    internal static void RenderBorder(Control control, RenderContext context)
    {
        RenderBorder(control, context, context.Bounds, default);
    }

    // The border is painted inside the arranged bounds, each side with its own thickness, and a side with thickness 0
    // is not painted. One uniform thickness with square corners is a centred stroke of that thickness on the bounds
    // deflated by half of it. Anything else is the ring between the bounds and the bounds deflated by each side's
    // thickness, filled with the even-odd rule.
    private static void RenderBorder(Control control, RenderContext context, LayoutRect bounds, CornerRadius cornerRadius)
    {
        DrawRect rect = ToDrawRect(bounds);
        Thickness thickness = control.BorderThickness;
        if (control.BorderBrush is not { } borderBrush || rect.Width <= 0 || rect.Height <= 0 ||
            MathF.Max(MathF.Max(thickness.Left, thickness.Top), MathF.Max(thickness.Right, thickness.Bottom)) <= 0)
        {
            return;
        }

        DrawingContext drawing = context.DrawingContext;
        bool uniform = thickness.Left == thickness.Top && thickness.Left == thickness.Right && thickness.Left == thickness.Bottom;
        if (uniform && !HasCornerRadius(cornerRadius))
        {
            float stroke = thickness.Left;
            if (stroke * 2 >= rect.Width || stroke * 2 >= rect.Height)
            {
                drawing.FillRectangle(rect, borderBrush);
                return;
            }

            float inset = stroke / 2;
            drawing.DrawRectangle(
                new DrawRect(rect.X + inset, rect.Y + inset, rect.Width - stroke, rect.Height - stroke),
                borderBrush,
                stroke);
            return;
        }

        DrawPath outline = control.BorderOutline.Get(rect.Width, rect.Height, thickness, cornerRadius);
        bool hasOffset = rect.X != 0 || rect.Y != 0;
        if (hasOffset)
        {
            drawing.PushTransform(System.Numerics.Matrix3x2.CreateTranslation(rect.X, rect.Y));
        }

        drawing.FillPath(outline, borderBrush, DrawFillRule.EvenOdd);
        if (hasOffset)
        {
            drawing.PopTransform();
        }
    }

    internal static bool HasCornerRadius(CornerRadius cornerRadius)
    {
        return cornerRadius.TopLeft > 0 || cornerRadius.TopRight > 0 || cornerRadius.BottomRight > 0 || cornerRadius.BottomLeft > 0;
    }

    // The corner radius that fits rect: radii that would overlap on a side are scaled down together.
    internal static DrawCornerRadius ToDrawCornerRadius(CornerRadius cornerRadius, DrawRect rect)
    {
        return new DrawCornerRadius(cornerRadius.TopLeft, cornerRadius.TopRight, cornerRadius.BottomRight, cornerRadius.BottomLeft)
            .Normalize(rect);
    }

    internal static DrawRect ToDrawRect(LayoutRect rect)
    {
        return new DrawRect(rect.X, rect.Y, MathF.Max(0, rect.Width), MathF.Max(0, rect.Height));
    }

    private static bool IsValidCornerRadius(CornerRadius value)
    {
        return IsValidCorner(value.TopLeft) &&
            IsValidCorner(value.TopRight) &&
            IsValidCorner(value.BottomRight) &&
            IsValidCorner(value.BottomLeft);
    }

    private static bool IsValidCorner(float value)
    {
        return float.IsFinite(value) && value >= 0;
    }
}

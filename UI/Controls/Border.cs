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

    // Each side of the border has its own signed thickness. A negative side -t paints a band of t inside the arranged
    // bounds, a positive side +t paints a band of t outside them, and a side with thickness 0 is not painted. The ring is
    // the bounds grown by each positive side, minus the bounds shrunk by each negative side. Four equal positive sides
    // with square corners are a centred stroke of that thickness on the bounds inflated by half of it, and four equal
    // negative sides with square corners are one on the bounds deflated by half of it. Anything else is the ring filled
    // with the even-odd rule; its outer corner is square when the corner radius of the bounds is 0, and otherwise
    // elliptical with that radius plus the positive thickness of the two sides it joins.
    private static void RenderBorder(Control control, RenderContext context, LayoutRect bounds, CornerRadius cornerRadius)
    {
        DrawRect rect = ToDrawRect(bounds);
        Thickness thickness = control.BorderThickness;
        if (control.BorderBrush is not { } borderBrush || rect.Width <= 0 || rect.Height <= 0 ||
            (thickness.Left == 0 && thickness.Top == 0 && thickness.Right == 0 && thickness.Bottom == 0))
        {
            return;
        }

        DrawingContext drawing = context.DrawingContext;
        bool uniform = thickness.Left == thickness.Top && thickness.Left == thickness.Right && thickness.Left == thickness.Bottom;
        if (uniform && !HasCornerRadius(cornerRadius))
        {
            if (thickness.Left > 0)
            {
                float outset = thickness.Left / 2;
                drawing.DrawRectangle(
                    new DrawRect(rect.X - outset, rect.Y - outset, rect.Width + thickness.Left, rect.Height + thickness.Left),
                    borderBrush,
                    thickness.Left);
                return;
            }

            float stroke = -thickness.Left;
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
        // The path is local to the top-left of its outer figure: the bounds grown by each positive side.
        float originX = rect.X - MathF.Max(0, thickness.Left);
        float originY = rect.Y - MathF.Max(0, thickness.Top);
        bool hasOffset = originX != 0 || originY != 0;
        if (hasOffset)
        {
            drawing.PushTransform(System.Numerics.Matrix3x2.CreateTranslation(originX, originY));
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

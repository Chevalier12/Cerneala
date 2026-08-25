namespace Cerneala.Drawing;

public readonly partial record struct DrawCommand
{
    public static DrawCommand FillRoundedRectangle(
        DrawRect bounds,
        DrawCornerRadius cornerRadius,
        Color color)
    {
        DrawCornerRadius normalized = cornerRadius.Normalize(bounds);
        return new DrawCommand(
            DrawCommandKind.FillRoundedRectangle,
            bounds,
            color,
            0,
            null,
            null,
            default,
            default,
            null,
            null,
            null,
            1,
            sourceRect: bounds,
            path: DrawPathFactory.RoundedRectangle(bounds, normalized),
            cornerRadius: normalized);
    }

    public static DrawCommand FillRoundedRectangle(
        DrawRect bounds,
        DrawCornerRadius cornerRadius,
        IDrawBrush brush,
        float opacity = 1)
    {
        ArgumentNullException.ThrowIfNull(brush);
        ThrowIfNotOpacity(opacity);
        DrawCornerRadius normalized = cornerRadius.Normalize(bounds);
        return new DrawCommand(
            DrawCommandKind.FillRoundedRectangle,
            bounds,
            default,
            0,
            null,
            null,
            default,
            default,
            null,
            null,
            brush,
            opacity,
            sourceRect: bounds,
            path: DrawPathFactory.RoundedRectangle(bounds, normalized),
            cornerRadius: normalized);
    }

    public static DrawCommand DrawRoundedRectangle(
        DrawRect bounds,
        DrawCornerRadius cornerRadius,
        Color color,
        float thickness)
    {
        DrawArgument.ThrowIfNotValidPixelSize(thickness, nameof(thickness));
        DrawCornerRadius normalized = cornerRadius.Normalize(bounds);
        return new DrawCommand(
            DrawCommandKind.DrawRoundedRectangle,
            bounds,
            color,
            thickness,
            null,
            null,
            default,
            default,
            null,
            null,
            null,
            1,
            sourceRect: bounds,
            path: DrawPathFactory.RoundedRectangle(bounds, normalized),
            cornerRadius: normalized);
    }

    public static DrawCommand DrawRoundedRectangle(
        DrawRect bounds,
        DrawCornerRadius cornerRadius,
        IDrawBrush brush,
        float thickness,
        float opacity = 1) =>
        DrawRoundedRectangle(
            bounds,
            cornerRadius,
            new DrawPen(brush, thickness),
            opacity);

    public static DrawCommand DrawRoundedRectangle(
        DrawRect bounds,
        DrawCornerRadius cornerRadius,
        DrawPen pen) =>
        DrawRoundedRectangle(bounds, cornerRadius, pen, opacity: 1);

    internal static DrawCommand DrawRoundedRectangle(
        DrawRect bounds,
        DrawCornerRadius cornerRadius,
        DrawPen pen,
        float opacity)
    {
        ArgumentNullException.ThrowIfNull(pen);
        ThrowIfNotOpacity(opacity);
        DrawCornerRadius normalized = cornerRadius.Normalize(bounds);
        return new DrawCommand(
            DrawCommandKind.DrawRoundedRectangle,
            bounds,
            default,
            pen.Thickness,
            null,
            null,
            default,
            default,
            null,
            null,
            pen.Brush,
            opacity,
            sourceRect: bounds,
            path: DrawPathFactory.RoundedRectangle(bounds, normalized),
            pen: pen,
            cornerRadius: normalized);
    }
}

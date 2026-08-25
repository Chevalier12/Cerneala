namespace Cerneala.Drawing;

public readonly partial record struct DrawCommand
{
    public static DrawCommand DrawTextLayout(
        DrawTextLayout layout,
        DrawPoint origin)
    {
        return DrawTextLayout(layout, origin, 1);
    }

    internal static DrawCommand DrawTextLayout(
        DrawTextLayout layout,
        DrawPoint origin,
        float opacity)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ThrowIfPointOutsidePixelRange(origin, nameof(origin));
        ThrowIfNotOpacity(opacity);
        DrawRect bounds = new(
            origin.X + layout.Bounds.X,
            origin.Y + layout.Bounds.Y,
            layout.Bounds.Width,
            layout.Bounds.Height);
        return new DrawCommand(
            DrawCommandKind.DrawTextLayout,
            bounds,
            default,
            0,
            null,
            null,
            origin,
            default,
            null,
            null,
            null,
            opacity,
            textLayout: layout);
    }
}

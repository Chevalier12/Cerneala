namespace Cerneala.Drawing;

public readonly record struct DrawCommand
{
    private DrawCommand(
        DrawCommandKind kind,
        DrawRect rect,
        Color color,
        float thickness,
        string? text,
        DrawTextRun? textRun,
        DrawPoint position,
        DrawPoint endPoint,
        IDrawImage? image,
        IDrawFont? font,
        IDrawBrush? brush,
        float brushOpacity)
    {
        Kind = kind;
        Rect = rect;
        Color = color;
        Thickness = thickness;
        Text = text;
        TextRun = textRun;
        Position = position;
        EndPoint = endPoint;
        Image = image;
        Font = font;
        Brush = brush;
        BrushOpacity = brushOpacity;
    }

    public DrawCommandKind Kind { get; }

    public DrawRect Rect { get; }

    public Color Color { get; }

    public float Thickness { get; }

    public string? Text { get; }

    public DrawTextRun? TextRun { get; }

    public DrawPoint Position { get; }

    public DrawPoint EndPoint { get; }

    public IDrawImage? Image { get; }

    public IDrawFont? Font { get; }

    public IDrawBrush? Brush { get; }

    public float BrushOpacity { get; }

    public static DrawCommand FillRectangle(DrawRect rect, Color color)
    {
        return new DrawCommand(DrawCommandKind.FillRectangle, rect, color, 0, null, null, default, default, null, null, null, 1);
    }

    public static DrawCommand FillRectangle(DrawRect rect, IDrawBrush brush, float opacity = 1)
    {
        return CreateBrushCommand(DrawCommandKind.FillRectangle, rect, default, default, brush, 0, opacity);
    }

    public static DrawCommand DrawRectangle(DrawRect rect, Color color, float thickness)
    {
        DrawArgument.ThrowIfNotValidPixelSize(thickness, nameof(thickness));

        return new DrawCommand(DrawCommandKind.DrawRectangle, rect, color, thickness, null, null, default, default, null, null, null, 1);
    }

    public static DrawCommand DrawRectangle(DrawRect rect, IDrawBrush brush, float thickness, float opacity = 1)
    {
        DrawArgument.ThrowIfNotValidPixelSize(thickness, nameof(thickness));
        return CreateBrushCommand(DrawCommandKind.DrawRectangle, rect, default, default, brush, thickness, opacity);
    }

    public static DrawCommand FillEllipse(DrawRect bounds, Color color)
    {
        return new DrawCommand(DrawCommandKind.FillEllipse, bounds, color, 0, null, null, default, default, null, null, null, 1);
    }

    public static DrawCommand FillEllipse(DrawRect bounds, IDrawBrush brush, float opacity = 1)
    {
        return CreateBrushCommand(DrawCommandKind.FillEllipse, bounds, default, default, brush, 0, opacity);
    }

    public static DrawCommand DrawEllipse(DrawRect bounds, Color color, float thickness)
    {
        DrawArgument.ThrowIfNotValidPixelSize(thickness, nameof(thickness));

        return new DrawCommand(DrawCommandKind.DrawEllipse, bounds, color, thickness, null, null, default, default, null, null, null, 1);
    }

    public static DrawCommand DrawEllipse(DrawRect bounds, IDrawBrush brush, float thickness, float opacity = 1)
    {
        DrawArgument.ThrowIfNotValidPixelSize(thickness, nameof(thickness));
        return CreateBrushCommand(DrawCommandKind.DrawEllipse, bounds, default, default, brush, thickness, opacity);
    }

    public static DrawCommand DrawLine(DrawPoint start, DrawPoint end, Color color, float thickness)
    {
        ThrowIfPointOutsidePixelRange(start, nameof(start));
        ThrowIfPointOutsidePixelRange(end, nameof(end));
        DrawArgument.ThrowIfNotValidPixelSize(thickness, nameof(thickness));

        return new DrawCommand(DrawCommandKind.DrawLine, default, color, thickness, null, null, start, end, null, null, null, 1);
    }

    public static DrawCommand DrawLine(DrawPoint start, DrawPoint end, IDrawBrush brush, float thickness, float opacity = 1)
    {
        ThrowIfPointOutsidePixelRange(start, nameof(start));
        ThrowIfPointOutsidePixelRange(end, nameof(end));
        DrawArgument.ThrowIfNotValidPixelSize(thickness, nameof(thickness));
        return CreateBrushCommand(DrawCommandKind.DrawLine, default, start, end, brush, thickness, opacity);
    }

    public static DrawCommand DrawText(DrawTextRun textRun, DrawPoint position, Color color)
    {
        ArgumentNullException.ThrowIfNull(textRun);
        ThrowIfPointOutsidePixelRange(position, nameof(position));

        return new DrawCommand(DrawCommandKind.DrawText, default, color, 0, textRun.Text, textRun, position, default, null, textRun.Font, null, 1);
    }

    public static DrawCommand DrawText(DrawTextRun textRun, DrawPoint position, IDrawBrush brush, float opacity = 1)
    {
        ArgumentNullException.ThrowIfNull(textRun);
        ThrowIfPointOutsidePixelRange(position, nameof(position));
        ArgumentNullException.ThrowIfNull(brush);
        if (!float.IsFinite(opacity) || opacity < 0 || opacity > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(opacity));
        }

        return new DrawCommand(DrawCommandKind.DrawText, default, default, 0, textRun.Text, textRun, position, default, null, textRun.Font, brush, opacity);
    }

    public static DrawCommand DrawImage(IDrawImage image, DrawRect destination, Color color)
    {
        ArgumentNullException.ThrowIfNull(image);

        return new DrawCommand(DrawCommandKind.DrawImage, destination, color, 0, null, null, default, default, image, null, null, 1);
    }

    public static DrawCommand PushClip(DrawRect rect)
    {
        return new DrawCommand(DrawCommandKind.PushClip, rect, default, 0, null, null, default, default, null, null, null, 1);
    }

    public static DrawCommand PopClip()
    {
        return new DrawCommand(DrawCommandKind.PopClip, default, default, 0, null, null, default, default, null, null, null, 1);
    }

    private static DrawCommand CreateBrushCommand(
        DrawCommandKind kind,
        DrawRect rect,
        DrawPoint position,
        DrawPoint endPoint,
        IDrawBrush brush,
        float thickness,
        float opacity)
    {
        ArgumentNullException.ThrowIfNull(brush);
        if (!float.IsFinite(opacity) || opacity < 0 || opacity > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(opacity));
        }

        return new DrawCommand(kind, rect, default, thickness, null, null, position, endPoint, null, null, brush, opacity);
    }

    private static void ThrowIfPointOutsidePixelRange(DrawPoint point, string parameterName)
    {
        DrawArgument.ThrowIfNotValidPixelCoordinate(point.X, parameterName);
        DrawArgument.ThrowIfNotValidPixelCoordinate(point.Y, parameterName);
    }
}

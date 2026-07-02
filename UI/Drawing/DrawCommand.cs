namespace Cerneala.Drawing;

public readonly record struct DrawCommand
{
    private DrawCommand(
        DrawCommandKind kind,
        DrawRect rect,
        DrawColor color,
        float thickness,
        string? text,
        DrawTextRun? textRun,
        DrawPoint position,
        IDrawImage? image,
        IDrawFont? font)
    {
        Kind = kind;
        Rect = rect;
        Color = color;
        Thickness = thickness;
        Text = text;
        TextRun = textRun;
        Position = position;
        Image = image;
        Font = font;
    }

    public DrawCommandKind Kind { get; }

    public DrawRect Rect { get; }

    public DrawColor Color { get; }

    public float Thickness { get; }

    public string? Text { get; }

    public DrawTextRun? TextRun { get; }

    public DrawPoint Position { get; }

    public IDrawImage? Image { get; }

    public IDrawFont? Font { get; }

    public static DrawCommand FillRectangle(DrawRect rect, DrawColor color)
    {
        return new DrawCommand(DrawCommandKind.FillRectangle, rect, color, 0, null, null, default, null, null);
    }

    public static DrawCommand DrawRectangle(DrawRect rect, DrawColor color, float thickness)
    {
        DrawArgument.ThrowIfNotValidPixelSize(thickness, nameof(thickness));

        return new DrawCommand(DrawCommandKind.DrawRectangle, rect, color, thickness, null, null, default, null, null);
    }

    public static DrawCommand DrawText(DrawTextRun textRun, DrawPoint position, DrawColor color)
    {
        ArgumentNullException.ThrowIfNull(textRun);

        return new DrawCommand(DrawCommandKind.DrawText, default, color, 0, textRun.Text, textRun, position, null, textRun.Font);
    }

    public static DrawCommand DrawImage(IDrawImage image, DrawRect destination, DrawColor color)
    {
        ArgumentNullException.ThrowIfNull(image);

        return new DrawCommand(DrawCommandKind.DrawImage, destination, color, 0, null, null, default, image, null);
    }

    public static DrawCommand PushClip(DrawRect rect)
    {
        return new DrawCommand(DrawCommandKind.PushClip, rect, default, 0, null, null, default, null, null);
    }

    public static DrawCommand PopClip()
    {
        return new DrawCommand(DrawCommandKind.PopClip, default, default, 0, null, null, default, null, null);
    }
}

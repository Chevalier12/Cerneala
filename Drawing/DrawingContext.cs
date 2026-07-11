namespace Cerneala.Drawing;

public sealed class DrawingContext
{
    private readonly DrawCommandList _commands;

    public DrawingContext(DrawCommandList commands)
    {
        _commands = commands ?? throw new ArgumentNullException(nameof(commands));
    }

    public void FillRectangle(DrawRect rect, Color color)
    {
        _commands.Add(DrawCommand.FillRectangle(rect, color));
    }

    public void DrawRectangle(DrawRect rect, Color color, float thickness)
    {
        _commands.Add(DrawCommand.DrawRectangle(rect, color, thickness));
    }

    public void FillEllipse(DrawRect bounds, Color color)
    {
        _commands.Add(DrawCommand.FillEllipse(bounds, color));
    }

    public void DrawEllipse(DrawRect bounds, Color color, float thickness)
    {
        _commands.Add(DrawCommand.DrawEllipse(bounds, color, thickness));
    }

    public void DrawLine(DrawPoint start, DrawPoint end, Color color, float thickness)
    {
        _commands.Add(DrawCommand.DrawLine(start, end, color, thickness));
    }

    public void DrawText(DrawTextRun textRun, DrawPoint position, Color color)
    {
        _commands.Add(DrawCommand.DrawText(textRun, position, color));
    }

    public void DrawImage(IDrawImage image, DrawRect destination, Color color)
    {
        _commands.Add(DrawCommand.DrawImage(image, destination, color));
    }

    public void PushClip(DrawRect rect)
    {
        _commands.Add(DrawCommand.PushClip(rect));
    }

    public void PopClip()
    {
        _commands.Add(DrawCommand.PopClip());
    }
}

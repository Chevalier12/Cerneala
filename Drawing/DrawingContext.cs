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

    public void FillRectangle(DrawRect rect, IDrawBrush brush)
    {
        _commands.Add(DrawCommand.FillRectangle(rect, brush));
    }

    public void DrawRectangle(DrawRect rect, Color color, float thickness)
    {
        _commands.Add(DrawCommand.DrawRectangle(rect, color, thickness));
    }

    public void DrawRectangle(DrawRect rect, IDrawBrush brush, float thickness)
    {
        _commands.Add(DrawCommand.DrawRectangle(rect, brush, thickness));
    }

    public void FillEllipse(DrawRect bounds, Color color)
    {
        _commands.Add(DrawCommand.FillEllipse(bounds, color));
    }

    public void FillEllipse(DrawRect bounds, IDrawBrush brush)
    {
        _commands.Add(DrawCommand.FillEllipse(bounds, brush));
    }

    public void DrawEllipse(DrawRect bounds, Color color, float thickness)
    {
        _commands.Add(DrawCommand.DrawEllipse(bounds, color, thickness));
    }

    public void DrawEllipse(DrawRect bounds, IDrawBrush brush, float thickness)
    {
        _commands.Add(DrawCommand.DrawEllipse(bounds, brush, thickness));
    }

    public void DrawLine(DrawPoint start, DrawPoint end, Color color, float thickness)
    {
        _commands.Add(DrawCommand.DrawLine(start, end, color, thickness));
    }

    public void DrawLine(DrawPoint start, DrawPoint end, IDrawBrush brush, float thickness)
    {
        _commands.Add(DrawCommand.DrawLine(start, end, brush, thickness));
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

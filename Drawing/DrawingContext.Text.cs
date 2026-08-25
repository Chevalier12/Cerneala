namespace Cerneala.Drawing;

public sealed partial class DrawingContext
{
    public void DrawTextLayout(DrawTextLayout layout, DrawPoint origin)
    {
        ensureActive?.Invoke();
        _commands.Add(DrawCommand.DrawTextLayout(layout, origin));
    }
}

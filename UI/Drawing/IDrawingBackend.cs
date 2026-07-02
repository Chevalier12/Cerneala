namespace Cerneala.Drawing;

public interface IDrawingBackend
{
    void Render(DrawCommandList commands);
}

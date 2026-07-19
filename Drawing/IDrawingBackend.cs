namespace Cerneala.Drawing;

public interface IDrawingBackend
{
    // Backends must treat the submitted command list as read-only for the duration of Render.
    // Retained UI may reuse the same command-list instance across unchanged draw frames.
    void Render(DrawCommandList commands, in DrawingFrameContext frameContext);
}

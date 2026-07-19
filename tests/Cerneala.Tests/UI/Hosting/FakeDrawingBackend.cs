using Cerneala.Drawing;

namespace Cerneala.Tests.UI.Hosting;

internal sealed class FakeDrawingBackend : IDrawingBackend
{
    public int RenderCalls { get; private set; }

    public DrawCommandList? LastCommands { get; private set; }

    public DrawingFrameContext? LastFrameContext { get; private set; }

    public void Render(
        DrawCommandList commands,
        in DrawingFrameContext frameContext)
    {
        frameContext.EnsureCurrent(commands);
        RenderCalls++;
        LastCommands = commands;
        LastFrameContext = frameContext;
    }
}

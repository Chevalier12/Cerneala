using Cerneala.Drawing;

namespace Cerneala.Tests.UI.Hosting;

internal sealed class FakeDrawingBackend : IDrawingBackend
{
    public int RenderCalls { get; private set; }

    public DrawCommandList? LastCommands { get; private set; }

    public void Render(DrawCommandList commands)
    {
        RenderCalls++;
        LastCommands = commands;
    }
}

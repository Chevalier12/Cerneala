using Cerneala.Drawing;

namespace Cerneala.Tests.Drawing;

public sealed class DrawCommandListTests
{
    [Fact]
    public void AddStoresCommandsInOrder()
    {
        DrawCommandList commands = new();

        commands.Add(DrawCommand.FillRectangle(new DrawRect(0, 0, 10, 20), Color.White));
        commands.Add(DrawCommand.DrawRectangle(new DrawRect(2, 3, 4, 5), Color.Black, 2));

        Assert.Equal(2, commands.Count);
        Assert.Equal(DrawCommandKind.FillRectangle, commands[0].Kind);
        Assert.Equal(DrawCommandKind.DrawRectangle, commands[1].Kind);
    }

    [Fact]
    public void ClearRemovesAllCommands()
    {
        DrawCommandList commands = new();

        commands.Add(DrawCommand.FillRectangle(new DrawRect(0, 0, 10, 20), Color.White));
        commands.Clear();

        Assert.Empty(commands);
    }
}

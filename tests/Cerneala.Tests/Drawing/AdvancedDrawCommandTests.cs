using Cerneala.Drawing;

namespace Cerneala.Tests.Drawing;

public sealed class AdvancedDrawCommandTests
{
    [Fact]
    public void DrawingContextRecordsEllipseCommands()
    {
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        DrawRect bounds = new(1, 2, 30, 20);

        drawing.FillEllipse(bounds, DrawColor.White);
        drawing.DrawEllipse(bounds, DrawColor.Black, 2);

        Assert.Equal(2, commands.Count);
        Assert.Equal(DrawCommandKind.FillEllipse, commands[0].Kind);
        Assert.Equal(bounds, commands[0].Rect);
        Assert.Equal(DrawCommandKind.DrawEllipse, commands[1].Kind);
        Assert.Equal(2, commands[1].Thickness);
    }

    [Fact]
    public void DrawingContextRecordsLineCommand()
    {
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);

        drawing.DrawLine(new DrawPoint(1, 2), new DrawPoint(3, 4), DrawColor.Black, 2);

        DrawCommand command = Assert.Single(commands);
        Assert.Equal(DrawCommandKind.DrawLine, command.Kind);
        Assert.Equal(new DrawPoint(1, 2), command.Position);
        Assert.Equal(new DrawPoint(3, 4), command.EndPoint);
        Assert.Equal(DrawColor.Black, command.Color);
        Assert.Equal(2, command.Thickness);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void AdvancedStrokeCommandsRejectInvalidThickness(float thickness)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => DrawCommand.DrawEllipse(new DrawRect(0, 0, 10, 10), DrawColor.Black, thickness));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => DrawCommand.DrawLine(new DrawPoint(0, 0), new DrawPoint(10, 10), DrawColor.Black, thickness));
    }

    [Theory]
    [InlineData(2_147_483_648f, 0, 0, 0)]
    [InlineData(0, -2_147_483_648f, 0, 0)]
    [InlineData(0, 0, 2_147_483_648f, 0)]
    [InlineData(0, 0, 0, -2_147_483_648f)]
    public void DrawLineRejectsPointsOutsidePixelRange(float startX, float startY, float endX, float endY)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => DrawCommand.DrawLine(new DrawPoint(startX, startY), new DrawPoint(endX, endY), DrawColor.Black, 1));
    }

    [Fact]
    public void MonoGameBackendHandlesAdvancedCommands()
    {
        string backendText = File.ReadAllText(FindRepositoryPath("UI", "Drawing", "MonoGame", "MonoGameDrawingBackend.cs"));

        Assert.Contains("case DrawCommandKind.FillEllipse:", backendText, StringComparison.Ordinal);
        Assert.Contains("case DrawCommandKind.DrawEllipse:", backendText, StringComparison.Ordinal);
        Assert.Contains("case DrawCommandKind.DrawLine:", backendText, StringComparison.Ordinal);
    }

    private static string FindRepositoryPath(params string[] segments)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
            {
                return Path.Combine(new[] { directory.FullName }.Concat(segments).ToArray());
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}

using Cerneala.Drawing;

namespace Cerneala.Tests.Drawing;

public sealed class DrawingContextTests
{
    [Fact]
    public void DrawRectExposesEdges()
    {
        DrawRect rect = new(10, 20, 30, 40);

        Assert.Equal(40, rect.Right);
        Assert.Equal(60, rect.Bottom);
    }

    [Theory]
    [InlineData(float.NaN, 0, 1, 1)]
    [InlineData(float.PositiveInfinity, 0, 1, 1)]
    [InlineData(0, float.NaN, 1, 1)]
    [InlineData(0, float.PositiveInfinity, 1, 1)]
    [InlineData(0, 0, float.NaN, 1)]
    [InlineData(0, 0, float.PositiveInfinity, 1)]
    [InlineData(0, 0, -1, 1)]
    [InlineData(0, 0, 1, float.NaN)]
    [InlineData(0, 0, 1, float.PositiveInfinity)]
    [InlineData(0, 0, 1, -1)]
    public void DrawRectRejectsInvalidValues(float x, float y, float width, float height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawRect(x, y, width, height));
    }

    [Theory]
    [InlineData(float.MaxValue, 0, float.MaxValue, 1)]
    [InlineData(0, float.MaxValue, 1, float.MaxValue)]
    public void DrawRectRejectsValuesThatOverflowEdges(float x, float y, float width, float height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawRect(x, y, width, height));
    }

    [Theory]
    [InlineData(2_147_483_648f, 0, 1, 1)]
    [InlineData(0, 2_147_483_648f, 1, 1)]
    [InlineData(0, 0, 2_147_483_648f, 1)]
    [InlineData(0, 0, 1, 2_147_483_648f)]
    public void DrawRectRejectsValuesOutsidePixelRange(float x, float y, float width, float height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawRect(x, y, width, height));
    }

    [Fact]
    public void DrawColorCreatesOpaqueColorByDefault()
    {
        DrawColor color = new(1, 2, 3);

        Assert.Equal(1, color.R);
        Assert.Equal(2, color.G);
        Assert.Equal(3, color.B);
        Assert.Equal(255, color.A);
    }

    [Theory]
    [InlineData(float.NaN, 0)]
    [InlineData(float.PositiveInfinity, 0)]
    [InlineData(0, float.NaN)]
    [InlineData(0, float.PositiveInfinity)]
    public void DrawPointRejectsInvalidValues(float x, float y)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawPoint(x, y));
    }

    [Fact]
    public void FillRectangleRecordsFillRectangleCommand()
    {
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);

        drawing.FillRectangle(new DrawRect(1, 2, 3, 4), DrawColor.White);

        Assert.Single(commands);
        Assert.Equal(DrawCommandKind.FillRectangle, commands[0].Kind);
        Assert.Equal(new DrawRect(1, 2, 3, 4), commands[0].Rect);
        Assert.Equal(DrawColor.White, commands[0].Color);
    }

    [Fact]
    public void DrawTextRecordsTextCommand()
    {
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        FakeDrawFont font = new();
        DrawTextRun textRun = new(font, "Cerneala", 16);

        drawing.DrawText(textRun, new DrawPoint(5, 6), DrawColor.Black);

        Assert.Single(commands);
        Assert.Equal(DrawCommandKind.DrawText, commands[0].Kind);
        Assert.Same(textRun, commands[0].TextRun);
        Assert.Same(font, commands[0].Font);
        Assert.Equal("Cerneala", commands[0].Text);
        Assert.Equal(new DrawPoint(5, 6), commands[0].Position);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void DrawTextRunRejectsInvalidSize(float size)
    {
        FakeDrawFont font = new();

        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawTextRun(font, "Cerneala", size));
    }

    [Fact]
    public void DrawImageRecordsImageCommand()
    {
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        FakeDrawImage image = new();

        drawing.DrawImage(image, new DrawRect(10, 20, 30, 40), DrawColor.White);

        Assert.Single(commands);
        Assert.Equal(DrawCommandKind.DrawImage, commands[0].Kind);
        Assert.Same(image, commands[0].Image);
        Assert.Equal(new DrawRect(10, 20, 30, 40), commands[0].Rect);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.MaxValue)]
    [InlineData(2_147_483_648f)]
    public void DrawRectangleRejectsInvalidThickness(float thickness)
    {
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => drawing.DrawRectangle(new DrawRect(0, 0, 10, 10), DrawColor.White, thickness));
    }

    [Fact]
    public void ClipCommandsAreRecordedInOrder()
    {
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);

        drawing.PushClip(new DrawRect(0, 0, 50, 50));
        drawing.PopClip();

        Assert.Equal(DrawCommandKind.PushClip, commands[0].Kind);
        Assert.Equal(DrawCommandKind.PopClip, commands[1].Kind);
    }
}

public sealed class FakeDrawImage : IDrawImage
{
    public int Width => 16;

    public int Height => 32;
}

public sealed class FakeDrawFont : IDrawFont
{
    public string FamilyName => "Fake";

    public float Size => 16;
}

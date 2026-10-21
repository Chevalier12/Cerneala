using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Documents;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.Tests.Controls;

// As in WPF, TextBlock.Background fills the render bounds of the TextBlock, under its text (#724).
public sealed class TextBlockBackgroundTests
{
    private static readonly SolidColorBrush Red = new(Color.Red);

    [Fact]
    public void BackgroundFillsTheArrangedBoundsBeforeTheText()
    {
        DrawCommandList commands = Render(new TextBlock { Width = 200, Text = "Hello", Background = Red });

        int fill = IndexOfBackground(commands);
        Assert.True(fill >= 0, "No FillRectangle with the Background brush.");
        DrawRect rect = commands[fill].Rect;
        Assert.Equal((0f, 0f, 200f, 20f), (rect.X, rect.Y, rect.Width, rect.Height));
        int firstText = IndexOfFirst(commands, DrawCommandKind.DrawText);
        Assert.True(firstText > fill, $"DrawText at {firstText} is not after the background at {fill}.");
    }

    [Fact]
    public void BackgroundIsDrawnWhenTextIsEmpty()
    {
        DrawCommandList commands = Render(new TextBlock { Width = 200, Background = Red });

        Assert.True(IndexOfBackground(commands) >= 0, "No FillRectangle with the Background brush.");
    }

    [Fact]
    public void BackgroundIsDrawnForInlines()
    {
        TextBlock textBlock = new() { Width = 200, Background = Red };
        textBlock.Inlines.Add(new Run("Regular "));
        textBlock.Inlines.Add(new Bold(new Run("bold")));

        DrawCommandList commands = Render(textBlock);

        int fill = IndexOfBackground(commands);
        Assert.True(fill >= 0, "No FillRectangle with the Background brush.");
        Assert.True(IndexOfFirst(commands, DrawCommandKind.DrawText) > fill);
    }

    [Fact]
    public void NullBackgroundDrawsNoRectangle()
    {
        DrawCommandList commands = Render(new TextBlock { Width = 200, Text = "Hello" });

        Assert.Equal(-1, IndexOfFirst(commands, DrawCommandKind.FillRectangle));
    }

    [Fact]
    public void BackgroundChangeInvalidatesRenderOnly()
    {
        TextBlock textBlock = new() { Text = "Hello" };
        textBlock.Measure(new MeasureContext(new LayoutSize(100, 100)));
        textBlock.DirtyState.ClearAll();

        textBlock.Background = Red;

        Assert.False(textBlock.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Render));
    }

    private static DrawCommandList Render(TextBlock textBlock)
    {
        UIRoot root = new(300, 100);
        root.VisualChildren.Add(textBlock);
        root.ProcessFrame();
        textBlock.Arrange(new ArrangeContext(new LayoutRect(0, 0, 200, 20)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();
        return root.RetainedRenderer.Commit(root);
    }

    private static int IndexOfBackground(DrawCommandList commands)
    {
        for (int i = 0; i < commands.Count; i++)
        {
            if (commands[i].Kind == DrawCommandKind.FillRectangle && ReferenceEquals(commands[i].Brush, Red))
            {
                return i;
            }
        }

        return -1;
    }

    private static int IndexOfFirst(DrawCommandList commands, DrawCommandKind kind)
    {
        for (int i = 0; i < commands.Count; i++)
        {
            if (commands[i].Kind == kind)
            {
                return i;
            }
        }

        return -1;
    }
}

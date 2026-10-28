using Cerneala.Drawing;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.Tests.Controls.Primitives;

public sealed class TickBarTests
{
    [Fact]
    public void DefaultsMatchWpfTickBar()
    {
        TickBar tickBar = new();

        Assert.Equal(0, tickBar.Minimum);
        Assert.Equal(100, tickBar.Maximum);
        Assert.Equal(1, tickBar.TickFrequency);
        Assert.Null(tickBar.Ticks);
        Assert.Equal(TickBarPlacement.Top, tickBar.Placement);
        Assert.Null(tickBar.Fill);
        Assert.False(tickBar.IsDirectionReversed);
        Assert.Equal(0, tickBar.ReservedSpace);
    }

    [Fact]
    public void InvalidTickFrequencyReservedSpaceAndTicksAreRejected()
    {
        TickBar tickBar = new();

        Assert.ThrowsAny<ArgumentException>(() => tickBar.TickFrequency = -1);
        Assert.ThrowsAny<ArgumentException>(() => tickBar.TickFrequency = float.NaN);
        Assert.ThrowsAny<ArgumentException>(() => tickBar.ReservedSpace = -1);
        Assert.ThrowsAny<ArgumentException>(() => tickBar.Ticks = [1, float.PositiveInfinity]);
    }

    // As WPF's TickBar.OnRender with Placement Bottom: for 0..10 with TickFrequency 2 on 110 pixels and a reserved space
    // of 10, the ticks run from x 5 to x 105, 10 pixels for each unit. Minimum and Maximum get full-height ticks, x 5 and
    // x 105 from y 0 to y 4; the ticks at 2, 4, 6 and 8 are three quarters as long, x 25, 45, 65 and 85 from y 0 to y 3.
    [Fact]
    public void BottomTickBarDrawsEndTicksAndFrequencyTicks()
    {
        TickBar tickBar = CreateTickBar(TickBarPlacement.Bottom, 110, 4);

        IReadOnlyList<(float X1, float Y1, float X2, float Y2)> lines = RenderLines(tickBar);

        Assert.Equal(
            [(5, 0, 5, 4), (105, 0, 105, 4), (25, 0, 25, 3), (45, 0, 45, 3), (65, 0, 65, 3), (85, 0, 85, 3)],
            lines);
    }

    // Placement Top draws the same ticks from the bottom edge upward: Minimum from y 4 to y 0, the tick at 2 from y 4 to
    // y 1.
    [Fact]
    public void TopTickBarDrawsTicksUpFromItsBottomEdge()
    {
        TickBar tickBar = CreateTickBar(TickBarPlacement.Top, 110, 4);

        IReadOnlyList<(float X1, float Y1, float X2, float Y2)> lines = RenderLines(tickBar);

        Assert.Equal(
            [(5, 4, 5, 0), (105, 4, 105, 0), (25, 4, 25, 1), (45, 4, 45, 1), (65, 4, 65, 1), (85, 4, 85, 1)],
            lines);
    }

    // A vertical TickBar has Minimum at the bottom: with Placement Right on 110 pixels, Minimum is at y 105, Maximum at
    // y 5, and the tick at 2 at y 85.
    [Fact]
    public void RightTickBarPutsMinimumAtTheBottom()
    {
        TickBar tickBar = CreateTickBar(TickBarPlacement.Right, 4, 110);

        IReadOnlyList<(float X1, float Y1, float X2, float Y2)> lines = RenderLines(tickBar);

        Assert.Equal(
            [(0, 105, 4, 105), (0, 5, 4, 5), (0, 85, 3, 85), (0, 65, 3, 65), (0, 45, 3, 45), (0, 25, 3, 25)],
            lines);
    }

    // Placement Left draws from the right edge leftward.
    [Fact]
    public void LeftTickBarDrawsTicksLeftFromItsRightEdge()
    {
        TickBar tickBar = CreateTickBar(TickBarPlacement.Left, 4, 110);
        tickBar.TickFrequency = 5;

        IReadOnlyList<(float X1, float Y1, float X2, float Y2)> lines = RenderLines(tickBar);

        Assert.Equal([(4, 105, 0, 105), (4, 5, 0, 5), (4, 55, 1, 55)], lines);
    }

    // Ticks replaces TickFrequency, and values at or outside Minimum and Maximum draw no extra tick: Ticks 0, 3, 7.5, 10
    // and 12 draw the end ticks and the ticks at 3 (x 35) and 7.5 (x 80).
    [Fact]
    public void TicksCollectionReplacesTickFrequency()
    {
        TickBar tickBar = CreateTickBar(TickBarPlacement.Bottom, 110, 4);
        tickBar.Ticks = [0, 3, 7.5f, 10, 12];

        IReadOnlyList<(float X1, float Y1, float X2, float Y2)> lines = RenderLines(tickBar);

        Assert.Equal([(5, 0, 5, 4), (105, 0, 105, 4), (35, 0, 35, 3), (80, 0, 80, 3)], lines);
    }

    // IsDirectionReversed mirrors the ticks: Minimum is at x 105 and the tick at 3 moves from x 35 to x 75.
    [Fact]
    public void ReversedTickBarMirrorsTickPositions()
    {
        TickBar tickBar = CreateTickBar(TickBarPlacement.Bottom, 110, 4);
        tickBar.Ticks = [3];
        tickBar.IsDirectionReversed = true;

        IReadOnlyList<(float X1, float Y1, float X2, float Y2)> lines = RenderLines(tickBar);

        Assert.Equal([(105, 0, 105, 4), (5, 0, 5, 4), (75, 0, 75, 3)], lines);
    }

    // As WPF's TickBar, a TickFrequency smaller than the range of one pixel is raised to it: 0.001 on 0..10 over 100
    // pixels draws at most one tick per pixel, not ten thousand.
    [Fact]
    public void TinyTickFrequencyDrawsAtMostOneTickPerPixel()
    {
        TickBar tickBar = CreateTickBar(TickBarPlacement.Bottom, 110, 4);
        tickBar.TickFrequency = 0.001f;

        IReadOnlyList<(float X1, float Y1, float X2, float Y2)> lines = RenderLines(tickBar);

        Assert.InRange(lines.Count, 2 + 99, 2 + 100);
    }

    // As WPF's TickBar.OnRender, which computes in double: Maximum - Minimum of two finite floats can overflow float, for
    // example 3e38 - -3e38, and the tick at the midpoint 0 must still be drawn in the middle of the tick area, at 55 on
    // 110 pixels with a reserved space of 10, for every Placement (#1118). The rows -1..1 are the ordinary-range control.
    [Theory]
    [InlineData(-1f, 1f, TickBarPlacement.Bottom, 55f, 0f, 55f, 3f)]
    [InlineData(-3e38f, 3e38f, TickBarPlacement.Bottom, 55f, 0f, 55f, 3f)]
    [InlineData(-3e38f, 3e38f, TickBarPlacement.Top, 55f, 4f, 55f, 1f)]
    [InlineData(-1f, 1f, TickBarPlacement.Right, 0f, 55f, 3f, 55f)]
    [InlineData(-3e38f, 3e38f, TickBarPlacement.Right, 0f, 55f, 3f, 55f)]
    [InlineData(-3e38f, 3e38f, TickBarPlacement.Left, 4f, 55f, 1f, 55f)]
    public void TickAtMidpointOfLargeFiniteRangeIsDrawnInTheMiddle(
        float minimum,
        float maximum,
        TickBarPlacement placement,
        float x1,
        float y1,
        float x2,
        float y2)
    {
        bool vertical = placement is TickBarPlacement.Left or TickBarPlacement.Right;
        TickBar tickBar = CreateTickBar(placement, vertical ? 4 : 110, vertical ? 110 : 4);
        tickBar.Minimum = minimum;
        tickBar.Maximum = maximum;
        tickBar.Ticks = [0];

        IReadOnlyList<(float X1, float Y1, float X2, float Y2)> lines = RenderLines(tickBar);

        Assert.Equal(3, lines.Count);
        Assert.Equal((x1, y1, x2, y2), lines[2]);
    }

    // With TickFrequency on -3e38..3e38 the interval is raised to the range of one pixel, 6e36, as on an ordinary range:
    // the TickBar draws about one tick per pixel, not none.
    [Fact]
    public void TickFrequencyOnLargeFiniteRangeDrawsAtMostOneTickPerPixel()
    {
        TickBar tickBar = CreateTickBar(TickBarPlacement.Bottom, 110, 4);
        tickBar.Minimum = -3e38f;
        tickBar.Maximum = 3e38f;
        tickBar.TickFrequency = 1;

        IReadOnlyList<(float X1, float Y1, float X2, float Y2)> lines = RenderLines(tickBar);

        Assert.InRange(lines.Count, 2 + 99, 2 + 100);
        Assert.All(lines.Skip(2), line => Assert.InRange(line.X1, 5, 105));
    }

    // A TickBar without Fill draws nothing, as a WPF pen without a brush; a reserved space as wide as the TickBar also
    // draws nothing.
    [Fact]
    public void TickBarWithoutFillOrWithoutRoomDrawsNothing()
    {
        TickBar withoutFill = CreateTickBar(TickBarPlacement.Bottom, 110, 4);
        withoutFill.Fill = null;
        TickBar withoutRoom = CreateTickBar(TickBarPlacement.Bottom, 110, 4);
        withoutRoom.ReservedSpace = 110;

        Assert.Empty(RenderLines(withoutFill));
        Assert.Empty(RenderLines(withoutRoom));
    }

    private static TickBar CreateTickBar(TickBarPlacement placement, float width, float height)
    {
        return new TickBar
        {
            Minimum = 0,
            Maximum = 10,
            TickFrequency = 2,
            ReservedSpace = 10,
            Placement = placement,
            Fill = new SolidColorBrush(Color.Black),
            Width = width,
            Height = height,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
    }

    // The lines that the TickBar draws, relative to its arranged top-left corner.
    internal static IReadOnlyList<(float X1, float Y1, float X2, float Y2)> RenderLines(UIElement element)
    {
        UIRoot root = new(200, 200);
        root.VisualChildren.Add(element);
        root.ProcessFrame();
        LayoutRect bounds = element.ArrangedBounds;
        return root.RetainedRenderer.Commit(root)
            .Where(command => command.Kind == DrawCommandKind.DrawLine)
            .Select(command => (
                command.Position.X - bounds.X,
                command.Position.Y - bounds.Y,
                command.EndPoint.X - bounds.X,
                command.EndPoint.Y - bounds.Y))
            .ToList();
    }
}

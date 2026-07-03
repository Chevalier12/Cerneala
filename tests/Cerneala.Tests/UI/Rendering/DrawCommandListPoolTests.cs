using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Rendering;

public sealed class DrawCommandListPoolTests
{
    [Fact]
    public void ReturnedCommandListIsClearedBeforeReuse()
    {
        DrawCommandListPool pool = new();
        DrawCommandList commands = pool.Rent();
        commands.Add(DrawCommand.FillRectangle(new DrawRect(0, 0, 1, 1), DrawColor.White));

        pool.Return(commands);
        DrawCommandList reused = pool.Rent();

        Assert.Same(commands, reused);
        Assert.Empty(reused);
    }

    [Fact]
    public void PoolDoesNotRetainMoreThanConfiguredCapacity()
    {
        DrawCommandListPool pool = new(maxRetained: 1);
        DrawCommandList first = pool.Rent();
        DrawCommandList second = pool.Rent();

        pool.Return(first);
        pool.Return(second);

        Assert.Equal(1, pool.AvailableCount);
    }

    [Fact]
    public void RentCreatesNewListWhenPoolIsEmpty()
    {
        DrawCommandListPool pool = new();
        DrawCommandList first = pool.Rent();
        DrawCommandList second = pool.Rent();

        Assert.NotSame(first, second);
    }
}

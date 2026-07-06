using Cerneala.Drawing.MonoGame;
using Microsoft.Xna.Framework;

namespace Cerneala.Tests.Drawing.MonoGame;

public sealed class MonoGameClipStackTests
{
    [Fact]
    public void InitialClipUsesViewportBounds()
    {
        Rectangle viewport = new(0, 0, 100, 80);

        MonoGameClipStack stack = new(viewport);

        Assert.Equal(viewport, stack.CurrentClip);
        Assert.Equal(0, stack.Depth);
    }

    [Fact]
    public void PushClipIntersectsWithPreviousClip()
    {
        MonoGameClipStack stack = new(new Rectangle(0, 0, 100, 100));

        stack.Push(new Rectangle(40, 50, 80, 30));

        Assert.Equal(new Rectangle(40, 50, 60, 30), stack.CurrentClip);
        Assert.Equal(1, stack.Depth);
    }

    [Fact]
    public void NestedPopRestoresPreviousClip()
    {
        MonoGameClipStack stack = new(new Rectangle(0, 0, 100, 100));
        stack.Push(new Rectangle(10, 10, 80, 80));
        stack.Push(new Rectangle(20, 20, 10, 10));

        stack.Pop();

        Assert.Equal(new Rectangle(10, 10, 80, 80), stack.CurrentClip);
        Assert.Equal(1, stack.Depth);
    }

    [Fact]
    public void EmptyIntersectionProducesEmptyClip()
    {
        MonoGameClipStack stack = new(new Rectangle(0, 0, 100, 100));

        stack.Push(new Rectangle(150, 150, 10, 10));

        Assert.Equal(new Rectangle(0, 0, 0, 0), stack.CurrentClip);
    }

    [Fact]
    public void PopUnderflowLeavesClipUnchanged()
    {
        Rectangle viewport = new(0, 0, 100, 100);
        MonoGameClipStack stack = new(viewport);

        stack.Pop();

        Assert.Equal(viewport, stack.CurrentClip);
        Assert.Equal(0, stack.Depth);
    }

    [Fact]
    public void BalancedRenderLeavesClipStackEmpty()
    {
        Rectangle viewport = new(0, 0, 100, 100);
        MonoGameClipStack stack = new(viewport);
        stack.Push(new Rectangle(10, 10, 80, 80));
        stack.Push(new Rectangle(20, 20, 10, 10));

        stack.Pop();
        stack.Pop();

        Assert.Equal(viewport, stack.CurrentClip);
        Assert.Equal(0, stack.Depth);
    }
}

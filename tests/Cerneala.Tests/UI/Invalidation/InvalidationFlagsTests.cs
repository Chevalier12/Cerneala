using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.UI.Invalidation;

public sealed class InvalidationFlagsTests
{
    [Fact]
    public void FlagsCanBeCombined()
    {
        InvalidationFlags flags = InvalidationFlags.Measure | InvalidationFlags.Render;

        Assert.True(flags.HasFlag(InvalidationFlags.Measure));
        Assert.True(flags.HasFlag(InvalidationFlags.Render));
    }

    [Fact]
    public void NoneRepresentsNoWork()
    {
        Assert.Equal(InvalidationFlags.None, default);
    }

    [Fact]
    public void SpecializedFlagsRemainExplicit()
    {
        InvalidationFlags specialized =
            InvalidationFlags.Text |
            InvalidationFlags.Image |
            InvalidationFlags.Resource |
            InvalidationFlags.Aspect |
            InvalidationFlags.InputVisual |
            InvalidationFlags.HitTest |
            InvalidationFlags.Subtree;

        Assert.True(specialized.HasFlag(InvalidationFlags.Text));
        Assert.True(specialized.HasFlag(InvalidationFlags.Image));
        Assert.True(specialized.HasFlag(InvalidationFlags.Resource));
        Assert.True(specialized.HasFlag(InvalidationFlags.Aspect));
        Assert.True(specialized.HasFlag(InvalidationFlags.InputVisual));
        Assert.True(specialized.HasFlag(InvalidationFlags.HitTest));
        Assert.True(specialized.HasFlag(InvalidationFlags.Subtree));
    }
}

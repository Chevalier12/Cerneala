using Cerneala.Drawing;

namespace Cerneala.Tests.Drawing;

public sealed class RetainedCommandVersionAlignmentTests
{
    [Fact]
    public void ShiftedChangedOwnerKeepsItsResourceSnapshotButRecomputesItsBounds()
    {
        object firstOwner = new(), secondOwner = new(), insertedOwner = new();
        Image firstImage = new(), secondImage = new(), insertedImage = new();
        DrawCommandList before = new();
        before.AddRetained(DrawCommand.DrawImage(firstImage, new(0, 0, 16, 16), Color.White), new(firstOwner, 1));
        before.AddRetained(DrawCommand.DrawImage(secondImage, new(16, 0, 16, 16), Color.White), new(secondOwner, 1));
        DrawCommandStateAnalyzer analyzer = new();
        var previous = analyzer.Analyze(before);

        DrawCommandList after = new();
        after.AddRetained(DrawCommand.DrawImage(insertedImage, new(0, 0, 8, 8), Color.White), new(insertedOwner, 1));
        after.AddRetained(DrawCommand.DrawImage(firstImage, new(40, 0, 32, 16), Color.Red), new(firstOwner, 2));
        after.AddRetained(before[1], new(secondOwner, 1));
        var current = analyzer.Analyze(after, previous.Entries);
        var cold = analyzer.Analyze(after);

        Assert.Equal(new DrawRect(40, 0, 32, 16), current.Entries[1].Bounds);
        Assert.NotSame(previous.Entries[0].Metadata, current.Entries[1].Metadata);
        Assert.Equal(after[1], current.Entries[1].Metadata!.RetainedIdentity);
        Assert.Same(previous.Entries[0].Metadata!.Resources, current.Entries[1].Metadata!.Resources);
        for (int i = 0; i < after.Count; i++)
        {
            Assert.Equal(cold.Entries[i].Bounds, current.Entries[i].Bounds);
            Assert.Equal(cold.Entries[i].Metadata!.RetainedIdentity, current.Entries[i].Metadata!.RetainedIdentity);
            Assert.Equal(cold.Entries[i].Metadata!.Resources, current.Entries[i].Metadata!.Resources);
        }
    }

    private sealed class Image : IDrawImage
    {
        public int Width => 16;
        public int Height => 16;
    }
}

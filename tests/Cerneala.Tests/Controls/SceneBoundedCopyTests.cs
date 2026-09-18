using Cerneala.UI.Controls;
using Xunit.Abstractions;

namespace Cerneala.Tests.Controls;

public sealed class SceneBoundedCopyTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(256)]
    [InlineData(4096)]
    public void ArrayBackedCellsAllocateOnlyTheDefensiveCopy(int count)
    {
        TileCell2D[] source = new TileCell2D[count];
        source[0] = new TileCell2D(7, TileFlip2D.Horizontal);
        _ = Scene2DModelValidator.CopyBounded(source, count, "cells");

        long before = GC.GetAllocatedBytesForCurrentThread();
        TileCell2D[] copied = Scene2DModelValidator.CopyBounded(source, count, "cells");
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        long maximum = (long)count * System.Runtime.CompilerServices.Unsafe.SizeOf<TileCell2D>() + 64;

        output.WriteLine($"Cells={count}; allocated={allocated}; maximum={maximum}");
        Assert.NotSame(source, copied);
        Assert.Equal(source, copied);
        source[0] = default;
        Assert.Equal(new TileCell2D(7, TileFlip2D.Horizontal), copied[0]);
        Assert.True(allocated <= maximum, $"Bounded array copy allocated {allocated} bytes; maximum {maximum}.");
    }

    [Fact]
    public void CovariantArrayIsCopiedIntoTheRequestedElementType()
    {
        string[] source = ["value"];
        object[] copied = Scene2DModelValidator.CopyBounded<object>(source, 1, "items");

        Assert.Equal(source[0], copied[0]);
        object replacement = new();
        copied[0] = replacement;
        Assert.Same(replacement, copied[0]);
        Assert.Equal("value", source[0]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(8)]
    public void ArraysKeepTheSameInclusiveBoundAndDiagnostic(int maximum)
    {
        Assert.Equal(maximum, Scene2DModelValidator.CopyBounded(new int[maximum], maximum, "items").Length);
        ArgumentException error = Assert.Throws<ArgumentException>(() =>
            Scene2DModelValidator.CopyBounded(new int[maximum + 1], maximum, "items", "SCN2D005"));
        Assert.Equal("items", error.ParamName);
        Assert.Equal("SCN2D005", Scene2DModelValidator.GetDiagnostic(error)!.Code);
    }
}

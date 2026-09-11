using Cerneala.Backends.SdlGpu;
using Cerneala.Platforms.Sdl3;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuTextAtlasAllocatorTests
{
    [Fact]
    public void FreeSpansAndEmptyShelvesCoalesceWithoutMovingLiveRectangles()
    {
        SdlGpuTextAtlasAllocator allocator = new(64);
        SdlRect[] rectangles = new SdlRect[8];
        for (int index = 0; index < rectangles.Length; index++)
        {
            Assert.True(allocator.TryAllocate(32, 16, out rectangles[index]));
        }
        Assert.False(allocator.TryAllocate(1, 1, out _));
        foreach (int index in new[] { 3, 0, 7, 2, 1, 4, 6, 5 })
        {
            allocator.Free(rectangles[index]);
        }
        Assert.True(allocator.TryAllocate(64, 64, out SdlRect full));
        Assert.Equal(new SdlRect(0, 0, 64, 64), full);
    }

    [Fact]
    public void FailedAllocationLeavesExistingFreeSpansAvailable()
    {
        SdlGpuTextAtlasAllocator allocator = new(64);
        Assert.True(allocator.TryAllocate(32, 64, out _));
        Assert.False(allocator.TryAllocate(64, 64, out _));
        Assert.True(allocator.TryAllocate(32, 64, out SdlRect remaining));
        Assert.Equal(new SdlRect(32, 0, 32, 64), remaining);
    }

    [Theory]
    [InlineData(17)]
    [InlineData(1337)]
    [InlineData(99173)]
    public void DeterministicChurnNeverOverlapsAndRecoversTheEntirePage(int seed)
    {
        const int dimension = 64;
        SdlGpuTextAtlasAllocator allocator = new(dimension);
        Random random = new(seed);
        List<SdlRect> live = [];
        bool[,] occupied = new bool[dimension, dimension];
        for (int operation = 0; operation < 20_000; operation++)
        {
            if (live.Count != 0 && random.Next(2) == 0)
            {
                int index = random.Next(live.Count);
                SdlRect rectangle = live[index];
                SetOccupied(rectangle, false);
                allocator.Free(rectangle);
                live.RemoveAt(index);
            }
            else if (allocator.TryAllocate(random.Next(1, 25), random.Next(1, 25), out SdlRect rectangle))
            {
                SetOccupied(rectangle, true);
                live.Add(rectangle);
            }
        }
        foreach (SdlRect rectangle in live)
        {
            SetOccupied(rectangle, false);
            allocator.Free(rectangle);
        }
        Assert.True(allocator.TryAllocate(dimension, dimension, out SdlRect full));
        Assert.Equal(new SdlRect(0, 0, dimension, dimension), full);

        void SetOccupied(SdlRect rectangle, bool value)
        {
            Assert.InRange(rectangle.X, 0, dimension - rectangle.Width);
            Assert.InRange(rectangle.Y, 0, dimension - rectangle.Height);
            for (int y = rectangle.Y; y < rectangle.Y + rectangle.Height; y++)
            {
                for (int x = rectangle.X; x < rectangle.X + rectangle.Width; x++)
                {
                    Assert.NotEqual(value, occupied[x, y]);
                    occupied[x, y] = value;
                }
            }
        }
    }
}

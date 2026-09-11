using Cerneala.Platforms.Sdl3;

namespace Cerneala.Backends.SdlGpu;

// Dynamic shelf packing: allocated rectangles never move. Free horizontal
// spans coalesce within a shelf; entirely empty adjacent shelves coalesce
// vertically. The owner supplies padded dimensions and frees exact allocations.
internal sealed class SdlGpuTextAtlasAllocator
{
    private readonly int dimension;
    private readonly List<Shelf> shelves = [];

    public SdlGpuTextAtlasAllocator(int dimension)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dimension);
        this.dimension = dimension;
        shelves.Add(new Shelf(0, dimension, dimension));
    }

    public bool TryAllocate(int width, int height, out SdlRect rectangle)
    {
        rectangle = default;
        if (width <= 0 || height <= 0 || width > dimension || height > dimension)
        {
            return false;
        }

        int selectedShelf = -1;
        int selectedSpan = -1;
        int bestHeight = int.MaxValue;
        for (int shelfIndex = 0; shelfIndex < shelves.Count; shelfIndex++)
        {
            Shelf shelf = shelves[shelfIndex];
            if (shelf.Height < height || shelf.Height >= bestHeight)
            {
                continue;
            }
            for (int spanIndex = 0; spanIndex < shelf.Free.Count; spanIndex++)
            {
                if (shelf.Free[spanIndex].Width >= width)
                {
                    selectedShelf = shelfIndex;
                    selectedSpan = spanIndex;
                    bestHeight = shelf.Height;
                    break;
                }
            }
        }
        if (selectedShelf < 0)
        {
            return false;
        }

        Shelf selected = shelves[selectedShelf];
        if (selected.AllocationCount == 0 && selected.Height > height)
        {
            shelves.Insert(selectedShelf + 1,
                new Shelf(selected.Y + height, selected.Height - height, dimension));
            selected.Height = height;
        }
        FreeSpan span = selected.Free[selectedSpan];
        if (span.Width == width)
        {
            selected.Free.RemoveAt(selectedSpan);
        }
        else
        {
            selected.Free[selectedSpan] = new FreeSpan(span.X + width, span.Width - width);
        }
        selected.AllocationCount++;
        rectangle = new SdlRect(span.X, selected.Y, width, height);
        return true;
    }

    public void Free(SdlRect rectangle)
    {
        int shelfIndex = 0;
        while (shelfIndex < shelves.Count && shelves[shelfIndex].Y != rectangle.Y)
        {
            shelfIndex++;
        }
        if (shelfIndex == shelves.Count)
        {
            throw new InvalidOperationException("The atlas allocation has no owning shelf.");
        }
        Shelf shelf = shelves[shelfIndex];
        int insertion = 0;
        while (insertion < shelf.Free.Count && shelf.Free[insertion].X < rectangle.X)
        {
            insertion++;
        }
        int x = rectangle.X;
        int width = rectangle.Width;
        if (insertion > 0)
        {
            FreeSpan previous = shelf.Free[insertion - 1];
            if (previous.X + previous.Width == x)
            {
                x = previous.X;
                width += previous.Width;
                shelf.Free.RemoveAt(--insertion);
            }
        }
        if (insertion < shelf.Free.Count)
        {
            FreeSpan next = shelf.Free[insertion];
            if (x + width == next.X)
            {
                width += next.Width;
                shelf.Free.RemoveAt(insertion);
            }
        }
        shelf.Free.Insert(insertion, new FreeSpan(x, width));
        shelf.AllocationCount--;
        if (shelf.AllocationCount != 0)
        {
            return;
        }

        if (shelfIndex + 1 < shelves.Count && shelves[shelfIndex + 1].AllocationCount == 0)
        {
            shelf.Height += shelves[shelfIndex + 1].Height;
            shelves.RemoveAt(shelfIndex + 1);
        }
        if (shelfIndex > 0 && shelves[shelfIndex - 1].AllocationCount == 0)
        {
            shelves[shelfIndex - 1].Height += shelf.Height;
            shelves.RemoveAt(shelfIndex);
        }
    }

    private sealed class Shelf(int y, int height, int width)
    {
        public int Y { get; } = y;
        public int Height { get; set; } = height;
        public int AllocationCount { get; set; }
        public List<FreeSpan> Free { get; } = [new FreeSpan(0, width)];
    }

    private readonly record struct FreeSpan(int X, int Width);
}

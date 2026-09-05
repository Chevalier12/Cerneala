using Cerneala.Drawing;

namespace Cerneala.UI.Controls;

internal sealed class SparseCollisionGrid2D
{
    internal const float CellSize = 128;
    private const int MaxCellsPerEntry = 256;
    private const int MaxCellsPerQuery = 4096;
    private const float Epsilon = CollisionNarrowPhase2D.Epsilon;

    private readonly Dictionary<long, List<int>> cells = [];
    private readonly List<GridMembership> memberships = [];
    private readonly List<int> largeEntries = [];
    private int[] stamps = [];
    private int currentStamp;
    private int activeCount;

    internal int Count => activeCount;

    internal int CellCount => cells.Count;

    internal long EstimatedRetainedBytes
    {
        get
        {
            long bytes = (long)cells.Count * 32L;
            foreach (List<int> entries in cells.Values)
            {
                bytes += 24L + ((long)entries.Capacity * sizeof(int));
            }

            bytes += (long)memberships.Capacity * 24L;
            bytes += 24L + ((long)largeEntries.Capacity * sizeof(int));
            bytes += 24L + ((long)stamps.Length * sizeof(int));
            return bytes;
        }
    }

    internal void Clear()
    {
        cells.Clear();
        memberships.Clear();
        largeEntries.Clear();
        memberships.Clear();
        Array.Clear(stamps);
        currentStamp = 0;
        activeCount = 0;
    }

    internal void AddOrUpdate(int id, DrawRect bounds)
    {
        EnsureCapacity(id);
        CellRange range = GetRange(bounds);
        long cellCount = range.CellCount;
        bool isLarge = cellCount > MaxCellsPerEntry;
        GridMembership previous = memberships[id];
        if (previous.Active &&
            previous.IsLarge == isLarge &&
            (isLarge || GetRange(previous.Bounds) == range))
        {
            memberships[id] = new GridMembership(bounds, isLarge, Active: true);
            return;
        }

        Remove(id);
        memberships[id] = new GridMembership(bounds, isLarge, Active: true);
        activeCount++;
        if (isLarge)
        {
            largeEntries.Add(id);
            return;
        }

        for (int y = range.MinY; y <= range.MaxY; y++)
        {
            for (int x = range.MinX; x <= range.MaxX; x++)
            {
                long key = GetKey(x, y);
                if (!cells.TryGetValue(key, out List<int>? bucket))
                {
                    bucket = [];
                    cells.Add(key, bucket);
                }

                bucket.Add(id);
            }
        }
    }

    internal void Remove(int id)
    {
        if ((uint)id >= (uint)memberships.Count || !memberships[id].Active)
        {
            return;
        }

        GridMembership membership = memberships[id];
        memberships[id] = default;
        activeCount--;

        if (membership.IsLarge)
        {
            largeEntries.Remove(id);
            return;
        }

        CellRange range = GetRange(membership.Bounds);
        for (int y = range.MinY; y <= range.MaxY; y++)
        {
            for (int x = range.MinX; x <= range.MaxX; x++)
            {
                long key = GetKey(x, y);
                if (!cells.TryGetValue(key, out List<int>? bucket))
                {
                    continue;
                }

                bucket.Remove(id);
                if (bucket.Count == 0)
                {
                    cells.Remove(key);
                }
            }
        }
    }

    internal void Query(DrawRect bounds, List<int> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        results.Clear();
        int stamp = NextStamp();
        CellRange range = GetRange(bounds);
        if (range.CellCount > MaxCellsPerQuery)
        {
            for (int id = 0; id < memberships.Count; id++)
            {
                GridMembership membership = memberships[id];
                if (membership.Active)
                {
                    AddIfOverlapping(id, membership.Bounds, bounds, stamp, results);
                }
            }

            return;
        }

        for (int y = range.MinY; y <= range.MaxY; y++)
        {
            for (int x = range.MinX; x <= range.MaxX; x++)
            {
                if (!cells.TryGetValue(GetKey(x, y), out List<int>? bucket))
                {
                    continue;
                }

                foreach (int id in bucket)
                {
                    AddCandidate(id, stamp, results);
                }
            }
        }

        foreach (int id in largeEntries)
        {
            AddIfOverlapping(id, memberships[id].Bounds, bounds, stamp, results);
        }
    }

    private void AddCandidate(int id, int stamp, List<int> results)
    {
        if (stamps[id] == stamp)
        {
            return;
        }

        stamps[id] = stamp;
        results.Add(id);
    }

    private void AddIfOverlapping(
        int id,
        DrawRect entryBounds,
        DrawRect queryBounds,
        int stamp,
        List<int> results)
    {
        if (stamps[id] == stamp || !BoundsOverlap(entryBounds, queryBounds))
        {
            return;
        }

        stamps[id] = stamp;
        results.Add(id);
    }

    private int NextStamp()
    {
        if (currentStamp == int.MaxValue)
        {
            Array.Clear(stamps);
            currentStamp = 0;
        }

        return ++currentStamp;
    }

    private void EnsureCapacity(int id)
    {
        if (id >= stamps.Length)
        {
            int length = Math.Max(id + 1, Math.Max(16, stamps.Length * 2));
            Array.Resize(ref stamps, length);
        }

        while (memberships.Count <= id)
        {
            memberships.Add(default);
        }
    }

    private static bool BoundsOverlap(DrawRect a, DrawRect b) =>
        a.X <= b.Right + Epsilon &&
        a.Right + Epsilon >= b.X &&
        a.Y <= b.Bottom + Epsilon &&
        a.Bottom + Epsilon >= b.Y;

    private static CellRange GetRange(DrawRect bounds) => new(
        ToCell(bounds.X),
        ToCell(bounds.Y),
        ToCell(bounds.Right),
        ToCell(bounds.Bottom));

    private static int ToCell(float coordinate)
    {
        double cell = Math.Floor(coordinate / CellSize);
        return cell switch
        {
            < int.MinValue => int.MinValue,
            > int.MaxValue => int.MaxValue,
            _ => (int)cell
        };
    }

    private static long GetKey(int x, int y) => ((long)x << 32) | (uint)y;

    private readonly record struct GridMembership(
        DrawRect Bounds,
        bool IsLarge,
        bool Active);

    private readonly record struct CellRange(int MinX, int MinY, int MaxX, int MaxY)
    {
        internal long CellCount
        {
            get
            {
                long width = (long)MaxX - MinX + 1L;
                long height = (long)MaxY - MinY + 1L;
                return width > long.MaxValue / height ? long.MaxValue : width * height;
            }
        }
    }
}

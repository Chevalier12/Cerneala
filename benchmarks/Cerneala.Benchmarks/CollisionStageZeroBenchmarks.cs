using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Cerneala.Benchmarks;

internal static class CollisionStageZeroBenchmarkRunner
{
    private const int Seed = 0xC0111D3;
    private const int WarmupPasses = 8;
    private const int MeasurementPasses = 48;

    internal static void Run(string reportPath)
    {
        string fullPath = Path.GetFullPath(reportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        CollisionFixture[] fixtures = CollisionFixture.CreateAll(Seed);
        List<CollisionCandidateReport> reports = [];
        foreach (CollisionFixture fixture in fixtures)
        {
            foreach (Func<ICollisionBroadphasePrototype> factory in Factories)
            {
                reports.Add(Measure(fixture, factory));
            }
        }

        CollisionStageZeroReport report = new(
            SchemaVersion: 1,
            TimestampUtc: DateTimeOffset.UtcNow,
            Commit: ResolveCommit(),
            Runtime: RuntimeInformation.FrameworkDescription,
            OperatingSystem: RuntimeInformation.OSDescription,
            ProcessArchitecture: RuntimeInformation.ProcessArchitecture.ToString(),
            Processor: Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "unknown",
            LogicalProcessorCount: Environment.ProcessorCount,
            StopwatchFrequency: Stopwatch.Frequency,
            Seed,
            WarmupPasses,
            MeasurementPasses,
            Fixtures: fixtures.Select(static fixture => fixture.Description).ToArray(),
            Candidates: reports);

        File.WriteAllText(
            fullPath,
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);

        Console.WriteLine($"Collision stage-0 market benchmark: {fullPath}");
        foreach (CollisionCandidateReport candidate in reports)
        {
            Console.WriteLine(
                $"{candidate.Scenario,-18} {candidate.Algorithm,-20} " +
                $"build={candidate.BuildMicroseconds,9:F2}us update-p95={candidate.UpdateP95Microseconds,9:F2}us " +
                $"query-p95={candidate.QueryP95Microseconds,9:F2}us candidates={candidate.AverageCandidates,8:F2} " +
                $"alloc={candidate.AllocatedBytesPerPass,10:F0}B retained={candidate.EstimatedRetainedBytes,10}B");
        }
    }

    private static readonly Func<ICollisionBroadphasePrototype>[] Factories =
    [
        static () => new ExhaustiveBroadphase(),
        static () => new SparseUniformGridBroadphase(cellSize: 32),
        static () => new RebuildingQuadtreeBroadphase(),
        static () => new DynamicAabbTreeBroadphase(fatMargin: 2)
    ];

    private static CollisionCandidateReport Measure(
        CollisionFixture fixture,
        Func<ICollisionBroadphasePrototype> factory)
    {
        for (int pass = 0; pass < WarmupPasses; pass++)
        {
            ICollisionBroadphasePrototype warmup = factory();
            warmup.Build(fixture.Boxes);
            Exercise(warmup, fixture, pass);
        }

        ICollisionBroadphasePrototype broadphase = factory();
        long allocatedBeforeBuild = GC.GetAllocatedBytesForCurrentThread();
        long buildStarted = Stopwatch.GetTimestamp();
        broadphase.Build(fixture.Boxes);
        double buildMicroseconds = Stopwatch.GetElapsedTime(buildStarted).TotalMicroseconds;
        long buildAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBeforeBuild;

        double[] updateSamples = new double[MeasurementPasses];
        double[] querySamples = new double[MeasurementPasses];
        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        long candidateTotal = 0;
        List<int> hits = new(fixture.Boxes.Length);
        for (int pass = 0; pass < MeasurementPasses; pass++)
        {
            long updateStarted = Stopwatch.GetTimestamp();
            ApplyUpdates(broadphase, fixture, pass);
            updateSamples[pass] = Stopwatch.GetElapsedTime(updateStarted).TotalMicroseconds;

            long queryStarted = Stopwatch.GetTimestamp();
            foreach (CollisionAabb query in fixture.Queries)
            {
                hits.Clear();
                broadphase.Query(query, hits);
                candidateTotal += hits.Count;
            }
            querySamples[pass] = Stopwatch.GetElapsedTime(queryStarted).TotalMicroseconds;
        }
        long measuredAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Array.Sort(updateSamples);
        Array.Sort(querySamples);

        return new CollisionCandidateReport(
            fixture.Description.Name,
            broadphase.Name,
            buildMicroseconds,
            Percentile(updateSamples, 0.50),
            Percentile(updateSamples, 0.95),
            Percentile(querySamples, 0.50),
            Percentile(querySamples, 0.95),
            (double)candidateTotal / (MeasurementPasses * fixture.Queries.Length),
            buildAllocatedBytes,
            (double)measuredAllocatedBytes / MeasurementPasses,
            broadphase.EstimatedRetainedBytes,
            fixture.Boxes.Length,
            fixture.Queries.Length,
            fixture.MovingIndices.Length);
    }

    private static void Exercise(ICollisionBroadphasePrototype broadphase, CollisionFixture fixture, int pass)
    {
        ApplyUpdates(broadphase, fixture, pass);
        List<int> hits = [];
        foreach (CollisionAabb query in fixture.Queries)
        {
            hits.Clear();
            broadphase.Query(query, hits);
        }
    }

    private static void ApplyUpdates(ICollisionBroadphasePrototype broadphase, CollisionFixture fixture, int pass)
    {
        float direction = (pass & 1) == 0 ? 1 : -1;
        for (int update = 0; update < fixture.MovingIndices.Length; update++)
        {
            int index = fixture.MovingIndices[update];
            CollisionAabb original = fixture.Boxes[index];
            float multiplier = fixture.Description.FastMotion ? 128 : 0.75f;
            broadphase.Update(index, original.Translate(direction * multiplier, ((update & 1) * 2 - 1) * multiplier));
        }
    }

    private static double Percentile(double[] sorted, double percentile)
    {
        int index = (int)Math.Ceiling((sorted.Length * percentile) - 1);
        return sorted[Math.Clamp(index, 0, sorted.Length - 1)];
    }

    private static string ResolveCommit()
    {
        try
        {
            ProcessStartInfo start = new("git", "rev-parse HEAD")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using Process? process = Process.Start(start);
            if (process is null)
            {
                return "unknown";
            }
            string commit = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return process.ExitCode == 0 && commit.Length > 0 ? commit : "unknown";
        }
        catch
        {
            return "unknown";
        }
    }
}

internal interface ICollisionBroadphasePrototype
{
    string Name { get; }
    long EstimatedRetainedBytes { get; }
    void Build(ReadOnlySpan<CollisionAabb> boxes);
    void Update(int id, CollisionAabb box);
    void Query(CollisionAabb query, List<int> results);
}

internal readonly record struct CollisionAabb(float MinX, float MinY, float MaxX, float MaxY)
{
    internal bool Intersects(CollisionAabb other) =>
        MinX <= other.MaxX && MaxX >= other.MinX && MinY <= other.MaxY && MaxY >= other.MinY;

    internal bool Contains(CollisionAabb other) =>
        MinX <= other.MinX && MinY <= other.MinY && MaxX >= other.MaxX && MaxY >= other.MaxY;

    internal float Area => MathF.Max(0, MaxX - MinX) * MathF.Max(0, MaxY - MinY);

    internal CollisionAabb Translate(float x, float y) => new(MinX + x, MinY + y, MaxX + x, MaxY + y);

    internal CollisionAabb Expand(float margin) => new(MinX - margin, MinY - margin, MaxX + margin, MaxY + margin);

    internal static CollisionAabb Union(CollisionAabb left, CollisionAabb right) => new(
        MathF.Min(left.MinX, right.MinX),
        MathF.Min(left.MinY, right.MinY),
        MathF.Max(left.MaxX, right.MaxX),
        MathF.Max(left.MaxY, right.MaxY));
}

internal sealed class ExhaustiveBroadphase : ICollisionBroadphasePrototype
{
    private CollisionAabb[] boxes = [];

    public string Name => "exhaustive";
    public long EstimatedRetainedBytes => boxes.LongLength * 4 * sizeof(float);
    public void Build(ReadOnlySpan<CollisionAabb> source) => boxes = source.ToArray();
    public void Update(int id, CollisionAabb box) => boxes[id] = box;
    public void Query(CollisionAabb query, List<int> results)
    {
        for (int id = 0; id < boxes.Length; id++)
        {
            if (boxes[id].Intersects(query))
            {
                results.Add(id);
            }
        }
    }
}

internal sealed class SparseUniformGridBroadphase(float cellSize) : ICollisionBroadphasePrototype
{
    private readonly Dictionary<long, List<int>> cells = [];
    private CollisionAabb[] boxes = [];
    private int[] stamps = [];
    private int stamp;

    public string Name => "sparse-uniform-grid";
    public long EstimatedRetainedBytes =>
        boxes.LongLength * 4 * sizeof(float) + stamps.LongLength * sizeof(int) +
        cells.Sum(static pair => 32L + pair.Value.Capacity * sizeof(int));

    public void Build(ReadOnlySpan<CollisionAabb> source)
    {
        boxes = source.ToArray();
        stamps = new int[boxes.Length];
        cells.Clear();
        for (int id = 0; id < boxes.Length; id++)
        {
            Add(id, boxes[id]);
        }
    }

    public void Update(int id, CollisionAabb box)
    {
        Remove(id, boxes[id]);
        boxes[id] = box;
        Add(id, box);
    }

    public void Query(CollisionAabb query, List<int> results)
    {
        stamp++;
        if (stamp == int.MaxValue)
        {
            Array.Clear(stamps);
            stamp = 1;
        }
        CellRange range = GetRange(query);
        for (int y = range.MinY; y <= range.MaxY; y++)
        {
            for (int x = range.MinX; x <= range.MaxX; x++)
            {
                if (!cells.TryGetValue(Key(x, y), out List<int>? ids))
                {
                    continue;
                }
                foreach (int id in ids)
                {
                    if (stamps[id] != stamp)
                    {
                        stamps[id] = stamp;
                        results.Add(id);
                    }
                }
            }
        }
    }

    private void Add(int id, CollisionAabb box)
    {
        CellRange range = GetRange(box);
        for (int y = range.MinY; y <= range.MaxY; y++)
        {
            for (int x = range.MinX; x <= range.MaxX; x++)
            {
                long key = Key(x, y);
                if (!cells.TryGetValue(key, out List<int>? ids))
                {
                    ids = [];
                    cells.Add(key, ids);
                }
                ids.Add(id);
            }
        }
    }

    private void Remove(int id, CollisionAabb box)
    {
        CellRange range = GetRange(box);
        for (int y = range.MinY; y <= range.MaxY; y++)
        {
            for (int x = range.MinX; x <= range.MaxX; x++)
            {
                long key = Key(x, y);
                if (!cells.TryGetValue(key, out List<int>? ids))
                {
                    continue;
                }
                ids.Remove(id);
                if (ids.Count == 0)
                {
                    cells.Remove(key);
                }
            }
        }
    }

    private CellRange GetRange(CollisionAabb box) => new(
        (int)MathF.Floor(box.MinX / cellSize),
        (int)MathF.Floor(box.MinY / cellSize),
        (int)MathF.Floor(box.MaxX / cellSize),
        (int)MathF.Floor(box.MaxY / cellSize));

    private static long Key(int x, int y) => ((long)x << 32) ^ (uint)y;
    private readonly record struct CellRange(int MinX, int MinY, int MaxX, int MaxY);
}

internal sealed class RebuildingQuadtreeBroadphase : ICollisionBroadphasePrototype
{
    private CollisionAabb[] boxes = [];
    private Node? root;

    public string Name => "rebuilding-quadtree";
    public long EstimatedRetainedBytes => boxes.LongLength * 4 * sizeof(float) + (root?.EstimateBytes() ?? 0);

    public void Build(ReadOnlySpan<CollisionAabb> source)
    {
        boxes = source.ToArray();
        Rebuild();
    }

    public void Update(int id, CollisionAabb box)
    {
        boxes[id] = box;
        Rebuild();
    }

    public void Query(CollisionAabb query, List<int> results) => root?.Query(query, results);

    private void Rebuild()
    {
        if (boxes.Length == 0)
        {
            root = null;
            return;
        }
        CollisionAabb bounds = boxes[0];
        for (int index = 1; index < boxes.Length; index++)
        {
            bounds = CollisionAabb.Union(bounds, boxes[index]);
        }
        root = new Node(bounds.Expand(1), 0);
        for (int id = 0; id < boxes.Length; id++)
        {
            root.Insert(id, boxes[id]);
        }
    }

    private sealed class Node(CollisionAabb bounds, int depth)
    {
        private const int Capacity = 12;
        private const int MaxDepth = 10;
        private readonly List<(int Id, CollisionAabb Box)> entries = [];
        private Node[]? children;

        internal void Insert(int id, CollisionAabb box)
        {
            if (children is not null && TryChild(box, out int child))
            {
                children[child].Insert(id, box);
                return;
            }
            entries.Add((id, box));
            if (entries.Count > Capacity && depth < MaxDepth)
            {
                Split();
            }
        }

        internal void Query(CollisionAabb query, List<int> results)
        {
            if (!bounds.Intersects(query))
            {
                return;
            }
            foreach ((int id, CollisionAabb box) in entries)
            {
                if (box.Intersects(query))
                {
                    results.Add(id);
                }
            }
            if (children is null)
            {
                return;
            }
            foreach (Node child in children)
            {
                child.Query(query, results);
            }
        }

        internal long EstimateBytes() =>
            64 + entries.Capacity * 24L + (children?.Sum(static child => child.EstimateBytes()) ?? 0);

        private void Split()
        {
            if (children is not null)
            {
                return;
            }
            float centerX = (bounds.MinX + bounds.MaxX) * 0.5f;
            float centerY = (bounds.MinY + bounds.MaxY) * 0.5f;
            children =
            [
                new Node(new CollisionAabb(bounds.MinX, bounds.MinY, centerX, centerY), depth + 1),
                new Node(new CollisionAabb(centerX, bounds.MinY, bounds.MaxX, centerY), depth + 1),
                new Node(new CollisionAabb(bounds.MinX, centerY, centerX, bounds.MaxY), depth + 1),
                new Node(new CollisionAabb(centerX, centerY, bounds.MaxX, bounds.MaxY), depth + 1)
            ];
            for (int index = entries.Count - 1; index >= 0; index--)
            {
                (int id, CollisionAabb box) = entries[index];
                if (TryChild(box, out int child))
                {
                    entries.RemoveAt(index);
                    children[child].Insert(id, box);
                }
            }
        }

        private bool TryChild(CollisionAabb box, out int child)
        {
            float centerX = (bounds.MinX + bounds.MaxX) * 0.5f;
            float centerY = (bounds.MinY + bounds.MaxY) * 0.5f;
            bool left = box.MaxX <= centerX;
            bool right = box.MinX >= centerX;
            bool top = box.MaxY <= centerY;
            bool bottom = box.MinY >= centerY;
            child = left && top ? 0 : right && top ? 1 : left && bottom ? 2 : right && bottom ? 3 : -1;
            return child >= 0;
        }
    }
}

internal sealed class DynamicAabbTreeBroadphase(float fatMargin) : ICollisionBroadphasePrototype
{
    private readonly List<Node> nodes = [];
    private int[] leafById = [];
    private int root = -1;

    public string Name => "dynamic-aabb-tree";
    public long EstimatedRetainedBytes => nodes.Capacity * 48L + leafById.LongLength * sizeof(int);

    public void Build(ReadOnlySpan<CollisionAabb> boxes)
    {
        nodes.Clear();
        root = -1;
        leafById = new int[boxes.Length];
        for (int id = 0; id < boxes.Length; id++)
        {
            int leaf = AddNode(new Node(boxes[id].Expand(fatMargin), id));
            leafById[id] = leaf;
            InsertLeaf(leaf);
        }
    }

    public void Update(int id, CollisionAabb box)
    {
        int leaf = leafById[id];
        if (nodes[leaf].Bounds.Contains(box))
        {
            return;
        }
        RemoveLeaf(leaf);
        Node node = nodes[leaf];
        node.Bounds = box.Expand(fatMargin);
        nodes[leaf] = node;
        InsertLeaf(leaf);
    }

    public void Query(CollisionAabb query, List<int> results)
    {
        if (root < 0)
        {
            return;
        }
        Stack<int> stack = new();
        stack.Push(root);
        while (stack.Count > 0)
        {
            Node node = nodes[stack.Pop()];
            if (!node.Bounds.Intersects(query))
            {
                continue;
            }
            if (node.IsLeaf)
            {
                results.Add(node.Id);
            }
            else
            {
                stack.Push(node.Left);
                stack.Push(node.Right);
            }
        }
    }

    private int AddNode(Node node)
    {
        nodes.Add(node);
        return nodes.Count - 1;
    }

    private void InsertLeaf(int leaf)
    {
        if (root < 0)
        {
            root = leaf;
            SetParent(leaf, -1);
            return;
        }
        CollisionAabb leafBounds = nodes[leaf].Bounds;
        int sibling = root;
        while (!nodes[sibling].IsLeaf)
        {
            Node current = nodes[sibling];
            float leftCost = CollisionAabb.Union(nodes[current.Left].Bounds, leafBounds).Area - nodes[current.Left].Bounds.Area;
            float rightCost = CollisionAabb.Union(nodes[current.Right].Bounds, leafBounds).Area - nodes[current.Right].Bounds.Area;
            sibling = leftCost <= rightCost ? current.Left : current.Right;
        }
        int oldParent = nodes[sibling].Parent;
        int parent = AddNode(new Node(CollisionAabb.Union(nodes[sibling].Bounds, leafBounds), -1)
        {
            Parent = oldParent,
            Left = sibling,
            Right = leaf
        });
        SetParent(sibling, parent);
        SetParent(leaf, parent);
        if (oldParent < 0)
        {
            root = parent;
        }
        else
        {
            ReplaceChild(oldParent, sibling, parent);
        }
        Refit(parent);
    }

    private void RemoveLeaf(int leaf)
    {
        if (leaf == root)
        {
            root = -1;
            return;
        }
        int parent = nodes[leaf].Parent;
        int grandParent = nodes[parent].Parent;
        int sibling = nodes[parent].Left == leaf ? nodes[parent].Right : nodes[parent].Left;
        if (grandParent < 0)
        {
            root = sibling;
            SetParent(sibling, -1);
        }
        else
        {
            ReplaceChild(grandParent, parent, sibling);
            SetParent(sibling, grandParent);
            Refit(grandParent);
        }
        SetParent(leaf, -1);
    }

    private void ReplaceChild(int parent, int oldChild, int newChild)
    {
        Node node = nodes[parent];
        if (node.Left == oldChild)
        {
            node.Left = newChild;
        }
        else
        {
            node.Right = newChild;
        }
        nodes[parent] = node;
    }

    private void Refit(int index)
    {
        while (index >= 0)
        {
            Node node = nodes[index];
            if (!node.IsLeaf)
            {
                node.Bounds = CollisionAabb.Union(nodes[node.Left].Bounds, nodes[node.Right].Bounds);
                nodes[index] = node;
            }
            index = node.Parent;
        }
    }

    private void SetParent(int index, int parent)
    {
        Node node = nodes[index];
        node.Parent = parent;
        nodes[index] = node;
    }

    private struct Node(CollisionAabb bounds, int id)
    {
        internal CollisionAabb Bounds = bounds;
        internal int Id = id;
        internal int Parent = -1;
        internal int Left = -1;
        internal int Right = -1;
        internal readonly bool IsLeaf => Id >= 0;
    }
}

internal sealed record CollisionFixture(
    CollisionFixtureDescription Description,
    CollisionAabb[] Boxes,
    CollisionAabb[] Queries,
    int[] MovingIndices)
{
    internal static CollisionFixture[] CreateAll(int seed) =>
    [
        Random("small-world", seed + 1, 128, 512, 64, 8, movingFraction: 0.08f),
        Random("large-sparse", seed + 2, 12_000, 100_000, 512, 12, movingFraction: 0.01f),
        Fence(seed + 3),
        Corners(seed + 4),
        InitialOverlap(seed + 5),
        Random("fast-motion", seed + 6, 2_048, 8_192, 256, 10, movingFraction: 0.08f, fastMotion: true),
        Random("high-churn", seed + 7, 4_096, 16_384, 256, 10, movingFraction: 0.25f)
    ];

    private static CollisionFixture Random(
        string name,
        int seed,
        int count,
        int extent,
        int queryCount,
        int maximumSize,
        float movingFraction,
        bool fastMotion = false)
    {
        Random random = new(seed);
        CollisionAabb[] boxes = new CollisionAabb[count];
        for (int index = 0; index < count; index++)
        {
            float width = 2 + random.NextSingle() * maximumSize;
            float height = 2 + random.NextSingle() * maximumSize;
            float x = random.NextSingle() * (extent - width) - (extent * 0.25f);
            float y = random.NextSingle() * (extent - height) - (extent * 0.25f);
            boxes[index] = new CollisionAabb(x, y, x + width, y + height);
        }
        CollisionAabb[] queries = CreateQueries(random, queryCount, extent, maximumSize * 2);
        int movingCount = Math.Max(1, (int)(count * movingFraction));
        int[] movers = Enumerable.Range(0, movingCount).Select(index => (index * 7919) % count).Distinct().ToArray();
        return new CollisionFixture(new CollisionFixtureDescription(name, count, queryCount, movers.Length, fastMotion), boxes, queries, movers);
    }

    private static CollisionFixture Fence(int seed)
    {
        const int count = 4_096;
        CollisionAabb[] boxes = new CollisionAabb[count];
        for (int index = 0; index < count; index++)
        {
            float x = index * 16;
            boxes[index] = new CollisionAabb(x, -2, x + 15, 2);
        }
        Random random = new(seed);
        CollisionAabb[] queries = CreateQueries(random, 256, count * 16, 24);
        int[] movers = Enumerable.Range(0, 32).Select(index => index * 127).ToArray();
        return new CollisionFixture(new CollisionFixtureDescription("long-fence", count, 256, movers.Length, false), boxes, queries, movers);
    }

    private static CollisionFixture Corners(int seed)
    {
        const int side = 48;
        List<CollisionAabb> boxes = [];
        for (int y = 0; y < side; y++)
        {
            for (int x = 0; x < side; x++)
            {
                boxes.Add(new CollisionAabb(x * 16, y * 16, x * 16 + 16, y * 16 + 16));
            }
        }
        Random random = new(seed);
        CollisionAabb[] queries = CreateQueries(random, 256, side * 16, 16);
        int[] movers = Enumerable.Range(0, 48).Select(index => index * side + index).ToArray();
        return new CollisionFixture(new CollisionFixtureDescription("tile-corners", boxes.Count, 256, movers.Length, false), boxes.ToArray(), queries, movers);
    }

    private static CollisionFixture InitialOverlap(int seed)
    {
        const int count = 2_048;
        Random random = new(seed);
        CollisionAabb[] boxes = new CollisionAabb[count];
        for (int index = 0; index < count; index++)
        {
            float x = random.NextSingle() * 64;
            float y = random.NextSingle() * 64;
            boxes[index] = new CollisionAabb(x, y, x + 32, y + 32);
        }
        CollisionAabb[] queries = CreateQueries(random, 128, 96, 32);
        int[] movers = Enumerable.Range(0, 128).ToArray();
        return new CollisionFixture(new CollisionFixtureDescription("initial-overlap", count, 128, movers.Length, false), boxes, queries, movers);
    }

    private static CollisionAabb[] CreateQueries(Random random, int count, float extent, float size)
    {
        CollisionAabb[] queries = new CollisionAabb[count];
        for (int index = 0; index < count; index++)
        {
            float x = random.NextSingle() * extent - extent * 0.25f;
            float y = random.NextSingle() * extent - extent * 0.25f;
            queries[index] = new CollisionAabb(x, y, x + size, y + size);
        }
        return queries;
    }
}

internal sealed record CollisionStageZeroReport(
    int SchemaVersion,
    DateTimeOffset TimestampUtc,
    string Commit,
    string Runtime,
    string OperatingSystem,
    string ProcessArchitecture,
    string Processor,
    int LogicalProcessorCount,
    long StopwatchFrequency,
    int Seed,
    int WarmupPasses,
    int MeasurementPasses,
    IReadOnlyList<CollisionFixtureDescription> Fixtures,
    IReadOnlyList<CollisionCandidateReport> Candidates);

internal sealed record CollisionFixtureDescription(
    string Name,
    int ObjectCount,
    int QueryCount,
    int MovingObjectCount,
    bool FastMotion);

internal sealed record CollisionCandidateReport(
    string Scenario,
    string Algorithm,
    double BuildMicroseconds,
    double UpdateP50Microseconds,
    double UpdateP95Microseconds,
    double QueryP50Microseconds,
    double QueryP95Microseconds,
    double AverageCandidates,
    long BuildAllocatedBytes,
    double AllocatedBytesPerPass,
    long EstimatedRetainedBytes,
    int ObjectCount,
    int QueryCount,
    int MovingObjectCount);

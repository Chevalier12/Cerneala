using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text.Json;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.Benchmarks;

internal static class TileMapStage4BenchmarkRunner
{
    private const int WarmupIterations = 64;
    private const int MeasurementIterations = 512;
    private const int RuntimePrimerBlocks = 8;
    private const int RuntimePrimerBlockIterations = 128;
    private const int RuntimePrimerPauseMilliseconds = 100;
    private const int JitQuietMilliseconds = 100;
    private const int JitQuietTimeoutMilliseconds = 2_000;

    internal static void Run(string reportPath)
    {
        string fullPath = Path.GetFullPath(reportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        TileMapStage4Scenario warmStatic = MeasureScenario(
            "warm-static",
            static (workload, iteration) => workload.RecordWarmStatic(),
            isProcessColdFixture: true);
        TileMapStage4Scenario cameraPan = MeasureScenario(
            "camera-pan",
            static (workload, iteration) => workload.RecordCameraPan(iteration));
        TileMapStage4Scenario chunkMutation = MeasureScenario(
            "chunk-mutation",
            static (workload, iteration) => workload.RecordChunkMutation());

        long fullFixtureRetainedBytes;
        using (TileMapStage4Workload workload = new())
        {
            _ = workload.RecordFullFixture();
            fullFixtureRetainedBytes = workload.RecordFullFixture().RetainedBytes;
        }

        TileMapStage4Scenario[] scenarios = [warmStatic, cameraPan, chunkMutation];
        TileMapStage4Gate[] gates = EvaluateGates(
            warmStatic,
            cameraPan,
            chunkMutation,
            fullFixtureRetainedBytes);
        string commit = ResolveGit("rev-parse HEAD");
        bool workingTreeDirty = ResolveGit("status --porcelain").Length != 0;
        TileMapStage4Report report = new(
            Schema: "cerneala-tilemap-stage4-v5-separated-phases",
            TimestampUtc: DateTimeOffset.UtcNow,
            Commit: commit.Length == 0 ? "unknown" : commit,
            WorkingTreeDirty: workingTreeDirty,
            Runtime: RuntimeInformation.FrameworkDescription,
            OperatingSystem: RuntimeInformation.OSDescription,
            ProcessArchitecture: RuntimeInformation.ProcessArchitecture.ToString(),
            Processor: Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "unknown",
            LogicalProcessorCount: Environment.ProcessorCount,
            StopwatchFrequency: Stopwatch.Frequency,
            WarmupIterations,
            MeasurementIterations,
            RuntimePrimerBlocks,
            RuntimePrimerBlockIterations,
            RuntimePrimerPauseMilliseconds,
            JitQuietMilliseconds,
            JitQuietTimeoutMilliseconds,
            Fixture: TileMapStage4FixtureDescription.Current,
            Baseline: TileMapStage4Baseline.Current,
            Scenarios: scenarios,
            FullFixtureRetainedBytes: fullFixtureRetainedBytes,
            Gates: gates);
        File.WriteAllText(
            fullPath,
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);

        Console.WriteLine($"Tilemap stage 4 benchmark: {fullPath}");
        foreach (TileMapStage4Scenario scenario in scenarios)
        {
            Console.WriteLine(
                $"{scenario.Name}: p50={scenario.CpuP50Microseconds:F3}us, " +
                $"p95={scenario.CpuP95Microseconds:F3}us, allocated={scenario.AllocatedBytesPerOperation:F0}B/op, " +
                $"commands={scenario.Counters.DrawCommands}, rebuilds={scenario.Counters.BatchesRebuilt}, " +
                $"reused={scenario.Counters.BatchesReused}, measured-jit={scenario.CompletedJitCompilations}");
        }
        Console.WriteLine($"Process-cold fixture construction + first recording: {warmStatic.Initialization.ConstructionAndFirstRecordingMicroseconds / 1000:F3}ms (maximum 300ms)");

        TileMapStage4Gate[] failures = gates.Where(static gate => !gate.Passed).ToArray();
        if (failures.Length != 0)
        {
            throw new InvalidOperationException(
                "TileMap2D stage 4 benchmark gate failed: " +
                string.Join("; ", failures.Select(static gate => $"{gate.Name}: {gate.Evidence}")));
        }
    }

    private static TileMapStage4Scenario MeasureScenario(
        string name,
        Func<TileMapStage4Workload, int, TileMapStage4Counters> action,
        bool isProcessColdFixture = false)
    {
        long constructionAllocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        long constructionStarted = Stopwatch.GetTimestamp();
        using TileMapStage4Workload workload = new();
        double constructionMicroseconds = Stopwatch.GetElapsedTime(constructionStarted).TotalMicroseconds;
        long constructionAllocated = GC.GetAllocatedBytesForCurrentThread() - constructionAllocatedBefore;
        long warmupStarted = Stopwatch.GetTimestamp();
        TileMapStage4Samples warmup = RecordSamples(workload, action, WarmupIterations);
        double warmupMicroseconds = Stopwatch.GetElapsedTime(warmupStarted).TotalMicroseconds;

        // Train runtime code on a separate fixture, not the target's retained
        // presentation. Extra target recordings would hide measured preparation.
        TileMapStage4RuntimePrimer primer = PrimeRuntime(action);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        TileMapStage4JitQuiescence quiescence = WaitForJitQuiescence();
        TileMapStage4Samples measured = RecordSamples(workload, action, MeasurementIterations);
        double[] sortedSamples = (double[])measured.OperationMicroseconds.Clone();
        Array.Sort(sortedSamples);
        return new TileMapStage4Scenario(
            name,
            CpuP50Microseconds: Percentile(sortedSamples, 0.50),
            CpuP95Microseconds: Percentile(sortedSamples, 0.95),
            AllocatedBytesPerOperation: (double)measured.AllocatedBytes / MeasurementIterations,
            measured.Counters,
            measured.MaximumRebuilds, measured.MaximumPreparedTiles, measured.TotalPreparedBatches, measured.MaximumWarmChunks,
            measured.MaximumWarmChargedBytes, measured.MaximumWarmRetainedBytes, measured.MaximumWarmImageBytes,
            new TileMapStage4Initialization(
                isProcessColdFixture, constructionMicroseconds, constructionAllocated,
                warmup.OperationMicroseconds[0], warmupMicroseconds, warmup.AllocatedBytes,
                warmup.CompletedJitCompilations, warmup.OperationMicroseconds),
            primer, quiescence, measured.CompletedJitCompilations, measured.OperationMicroseconds);
    }

    private static TileMapStage4Samples RecordSamples(
        TileMapStage4Workload workload,
        Func<TileMapStage4Workload, int, TileMapStage4Counters> action,
        int iterations)
    {
        double[] samples = new double[iterations];
        long jitBefore = JitInfo.GetCompiledMethodCount();
        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        TileMapStage4Counters counters = default;
        int maximumRebuilds = 0, maximumPreparedTiles = 0, totalPreparedBatches = 0, maximumWarmChunks = 0;
        long maximumWarmChargedBytes = 0, maximumWarmRetainedBytes = 0, maximumWarmImageBytes = 0;
        for (int iteration = 0; iteration < iterations; iteration++)
        {
            long started = Stopwatch.GetTimestamp();
            counters = action(workload, iteration);
            samples[iteration] = Stopwatch.GetElapsedTime(started).TotalMicroseconds;
            maximumRebuilds = Math.Max(maximumRebuilds, counters.BatchesRebuilt);
            maximumPreparedTiles = Math.Max(maximumPreparedTiles, counters.WarmTilesPrepared);
            totalPreparedBatches += counters.WarmBatchesPrepared;
            maximumWarmChunks = Math.Max(maximumWarmChunks, counters.WarmChunks);
            maximumWarmChargedBytes = Math.Max(maximumWarmChargedBytes, counters.WarmChargedBytes);
            maximumWarmRetainedBytes = Math.Max(maximumWarmRetainedBytes, counters.WarmRetainedBytes);
            maximumWarmImageBytes = Math.Max(maximumWarmImageBytes, counters.WarmImageBytes);
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        long compiled = JitInfo.GetCompiledMethodCount() - jitBefore;
        return new TileMapStage4Samples(
            samples, allocated, compiled, counters,
            maximumRebuilds, maximumPreparedTiles, totalPreparedBatches, maximumWarmChunks,
            maximumWarmChargedBytes, maximumWarmRetainedBytes, maximumWarmImageBytes);
    }

    private static TileMapStage4RuntimePrimer PrimeRuntime(
        Func<TileMapStage4Workload, int, TileMapStage4Counters> action)
    {
        long jitBefore = JitInfo.GetCompiledMethodCount();
        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        long started = Stopwatch.GetTimestamp();
        double actionMicroseconds = 0;
        using (TileMapStage4Workload primer = new())
        {
            for (int block = 0; block < RuntimePrimerBlocks; block++)
            {
                TileMapStage4Samples samples = RecordSamples(primer, action, RuntimePrimerBlockIterations);
                actionMicroseconds += samples.OperationMicroseconds.Sum();
                Thread.Sleep(RuntimePrimerPauseMilliseconds);
            }
        }
        double elapsedMicroseconds = Stopwatch.GetElapsedTime(started).TotalMicroseconds;
        long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        long compiled = JitInfo.GetCompiledMethodCount() - jitBefore;
        return new TileMapStage4RuntimePrimer(
            RuntimePrimerBlocks * RuntimePrimerBlockIterations, elapsedMicroseconds, actionMicroseconds, allocated, compiled);
    }

    private static TileMapStage4JitQuiescence WaitForJitQuiescence()
    {
        long started = Stopwatch.GetTimestamp();
        long lastChange = started;
        long firstCount = JitInfo.GetCompiledMethodCount();
        long lastCount = firstCount;
        while (true)
        {
            Thread.Sleep(10);
            long now = Stopwatch.GetTimestamp();
            long count = JitInfo.GetCompiledMethodCount();
            if (count != lastCount)
            {
                lastCount = count;
                lastChange = now;
            }
            double elapsed = Stopwatch.GetElapsedTime(started, now).TotalMilliseconds;
            if (elapsed >= JitQuietTimeoutMilliseconds)
            {
                return new TileMapStage4JitQuiescence(false, elapsed, count - firstCount);
            }
            if (Stopwatch.GetElapsedTime(lastChange, now).TotalMilliseconds >= JitQuietMilliseconds)
            {
                return new TileMapStage4JitQuiescence(true, elapsed, count - firstCount);
            }
        }
    }

    private static TileMapStage4Gate[] EvaluateGates(
        TileMapStage4Scenario warm,
        TileMapStage4Scenario pan,
        TileMapStage4Scenario mutation,
        long fullFixtureRetainedBytes)
    {
        List<TileMapStage4Gate> gates = [];
        AddMaximum(gates, "process-cold-fixture-us", warm.Initialization.ConstructionAndFirstRecordingMicroseconds, 300_000);
        AddMaximum(gates, "warm-static-p95-us", warm.CpuP95Microseconds, 875);
        AddMaximum(gates, "warm-static-allocated-bytes", warm.AllocatedBytesPerOperation, 30_000);
        AddExact(gates, "warm-static-rebuilds", warm.MaximumRebuilds, 0);
        AddMinimum(gates, "warm-static-reused-segments", warm.Counters.BatchesReused, 36);
        AddMaximum(gates, "warm-static-draw-commands", warm.Counters.DrawCommands, 36);
        // The approved warm-memory policy adds a separately bounded optional
        // working set. Preserve the former visible-work estimate limit, report
        // total memory unchanged, and gate the added charge independently.
        AddMaximum(gates, "warm-static-required-retained-bytes", warm.Counters.RetainedBytes - warm.Counters.WarmRetainedBytes, 1_048_576);

        AddMaximum(gates, "camera-pan-p95-us", pan.CpuP95Microseconds, 1_460);
        AddMaximum(gates, "camera-pan-allocated-bytes", pan.AllocatedBytesPerOperation, 192_000);
        AddExact(gates, "camera-pan-rebuilds", pan.MaximumRebuilds, 0);
        AddMaximum(gates, "camera-pan-draw-commands", pan.Counters.DrawCommands, 48);

        AddMaximum(gates, "chunk-mutation-p95-us", mutation.CpuP95Microseconds, 1_135);
        AddMaximum(gates, "chunk-mutation-allocated-bytes", mutation.AllocatedBytesPerOperation, 717_000);
        AddExact(gates, "chunk-mutation-rebuilds", mutation.Counters.BatchesRebuilt, 1);
        AddExact(
            gates,
            "chunk-mutation-other-visible-segments-reused",
            mutation.Counters.BatchesReused,
            mutation.Counters.DrawCommands - 1);
        AddExact(gates, "chunk-mutation-tile-invalidations", mutation.Counters.TileInvalidations, 1);

        foreach (TileMapStage4Scenario scenario in new[] { warm, pan, mutation })
        {
            gates.Add(new TileMapStage4Gate(
                $"{scenario.Name}-jit-quiescence", scenario.JitQuiescence.Reached,
                $"reached={scenario.JitQuiescence.Reached}, elapsedMs={scenario.JitQuiescence.ElapsedMilliseconds.ToString("0.###", CultureInfo.InvariantCulture)}"));
            // Completed JIT work remains in every raw sample and is reported
            // diagnostically; the approved budgets decide acceptance.
            AddExact(gates, $"{scenario.Name}-measured-optional-batches", scenario.TotalPreparedBatches,
                scenario.Name == "camera-pan" ? 647 : 19);
            AddMaximum(gates, $"{scenario.Name}-warm-charge-bytes", scenario.MaximumWarmChargedBytes,
                TileMap2D.WarmCacheBudgetBytes * TileMapStage4ModelFactory.LayerCount);
            AddMaximum(gates, $"{scenario.Name}-warm-prepared-cells", scenario.MaximumPreparedTiles,
                RenderSurface2D.WarmPreparationTileBudget);
            gates.Add(new TileMapStage4Gate(
                $"{scenario.Name}-indexed-culling",
                scenario.Counters.CandidateChunks < scenario.Counters.TotalChunks &&
                    scenario.Counters.CandidateTiles <=
                        scenario.Counters.VisibleChunks * TileMapStage4ModelFactory.CellsPerChunk,
                $"candidateChunks={scenario.Counters.CandidateChunks}, totalChunks={scenario.Counters.TotalChunks}, " +
                    $"candidateTiles={scenario.Counters.CandidateTiles}, visibleChunks={scenario.Counters.VisibleChunks}"));
        }
        AddMaximum(gates, "full-fixture-retained-bytes", fullFixtureRetainedBytes, 5_242_880);
        return gates.ToArray();
    }

    private static void AddMaximum(
        ICollection<TileMapStage4Gate> gates,
        string name,
        double actual,
        double maximum) =>
        gates.Add(new TileMapStage4Gate(
            name,
            actual <= maximum,
            $"actual={actual.ToString("0.###", CultureInfo.InvariantCulture)}, " +
                $"maximum={maximum.ToString("0.###", CultureInfo.InvariantCulture)}"));

    private static void AddMinimum(
        ICollection<TileMapStage4Gate> gates,
        string name,
        double actual,
        double minimum) =>
        gates.Add(new TileMapStage4Gate(
            name,
            actual >= minimum,
            $"actual={actual.ToString("0.###", CultureInfo.InvariantCulture)}, " +
                $"minimum={minimum.ToString("0.###", CultureInfo.InvariantCulture)}"));

    private static void AddExact(
        ICollection<TileMapStage4Gate> gates,
        string name,
        long actual,
        long expected) =>
        gates.Add(new TileMapStage4Gate(
            name,
            actual == expected,
            $"actual={actual}, expected={expected}"));

    private static double Percentile(double[] sorted, double percentile)
    {
        int index = (int)Math.Ceiling((sorted.Length * percentile) - 1);
        return sorted[Math.Clamp(index, 0, sorted.Length - 1)];
    }

    private static string ResolveGit(string arguments)
    {
        try
        {
            ProcessStartInfo start = new("git", arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using Process? process = Process.Start(start);
            if (process is null)
            {
                return string.Empty;
            }
            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return process.ExitCode == 0 ? output : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}

internal sealed class TileMapStage4Workload : IDisposable
{
    private static readonly DrawRect FrameBounds = new(
        0,
        0,
        TileMapStage4ModelFactory.ViewWidthInTiles * TileMapStage4ModelFactory.TileSize,
        TileMapStage4ModelFactory.ViewHeightInTiles * TileMapStage4ModelFactory.TileSize);

    private readonly UIRoot root = new();
    private readonly RenderSurface2D surface;
    private readonly Scene2D scene;
    private readonly TileMap2D[] maps;
    private readonly TileMap2DModel[] originalModels;
    private readonly TileMap2DModel[] mutatedModels;
    private readonly TileMapStage4Publication[] publications;
    private readonly DrawCommandList commands = new();
    private bool useMutatedModel;

    internal TileMapStage4Workload()
    {
        originalModels = TileMapStage4ModelFactory.Create(mutated: false);
        mutatedModels = TileMapStage4ModelFactory.Create(mutated: true);
        publications = originalModels.Select((model, index) => new TileMapStage4Publication(model, mutatedModels[index])).ToArray();
        maps = publications.Select((publication, index) => new TileMap2D
            { Source = publication.Source, Layer = originalModels[index].Order }).ToArray();
        scene = new Scene2D { OrderMode = SceneOrderMode.Layer };
        foreach (TileMap2D map in maps) { scene.Children.Add(map); }
        surface = new RenderSurface2D
        {
            Scene = scene,
            ViewBox = TileMapStage4ModelFactory.CameraView(32)
        };
        surface.Resources.SetResource(
            TileMapStage4ModelFactory.TerrainResourceId,
            new ImageResource("terrain.png"));
        surface.Resources.SetResource(
            TileMapStage4ModelFactory.StructuresResourceId,
            new ImageResource("structures.png"));
        root.SetImageLoader(new InlineImageLoader(new Dictionary<string, IDrawImage>(StringComparer.Ordinal)
        {
            ["terrain.png"] = new InlineImage(192, 16, "terrain"),
            ["structures.png"] = new InlineImage(128, 16, "structures")
        }));
        root.VisualChildren.Add(surface);
    }

    internal TileMapStage4Counters RecordWarmStatic()
    {
        return Record(TileMapStage4ModelFactory.CameraView(32), FrameBounds);
    }

    internal TileMapStage4Counters RecordCameraPan(int iteration)
    {
        return Record(
            TileMapStage4ModelFactory.CameraView(32 + (iteration % 32)),
            FrameBounds);
    }

    internal TileMapStage4Counters RecordChunkMutation()
    {
        useMutatedModel = !useMutatedModel;
        publications[1].Publish(useMutatedModel);
        return Record(TileMapStage4ModelFactory.CameraView(32), FrameBounds);
    }

    internal TileMapStage4Counters RecordFullFixture()
    {
        DrawRect view = new(
            0,
            0,
            TileMapStage4ModelFactory.WidthInTiles * TileMapStage4ModelFactory.TileSize,
            TileMapStage4ModelFactory.HeightInTiles * TileMapStage4ModelFactory.TileSize);
        return Record(view, view);
    }

    public void Dispose() => root.VisualChildren.Remove(surface);

    private TileMapStage4Counters Record(DrawRect viewBox, DrawRect bounds)
    {
        // Source publication and camera preparation belong to the operation,
        // including its process-cold first sample; no extra target warmups.
        root.Width = surface.Width = bounds.Width;
        root.Height = surface.Height = bounds.Height;
        surface.ViewBox = viewBox;
        root.ProcessFrame();
        ((ITimeSensitiveRenderElement)surface).UpdateRenderTime(TimeSpan.Zero);
        if (surface.PresentationState != RenderSurface2DPresentationState.Ready)
            throw new InvalidOperationException("The inline benchmark source did not finish required preparation.", surface.PresentationError);
        commands.Clear();
        float scaleX = bounds.Width / viewBox.Width;
        float scaleY = bounds.Height / viewBox.Height;
        Matrix3x2 transform = Matrix3x2.CreateTranslation(-viewBox.X, -viewBox.Y) *
            Matrix3x2.CreateScale(scaleX, scaleY) *
            Matrix3x2.CreateTranslation(bounds.X, bounds.Y);
        RenderSurface2DFrame frame = new(commands, bounds, TimeSpan.Zero);
        frame.PushClip(bounds);
        frame.PushTransform(transform);
        try
        {
            scene.Record(new Scene2DRecordContext(surface, frame, transform, bounds));
        }
        finally
        {
            frame.PopTransform();
            frame.PopClip();
            frame.Complete();
        }
        return TileMapStage4Counters.From(maps);
    }

    private sealed record InlineImage(int Width, int Height, string Name) : IDrawImage;

    private sealed class InlineImageLoader(IReadOnlyDictionary<string, IDrawImage> images) : IImageLoader, IAsyncImageLoader
    {
        public IDrawImage Load(string path) => images[path];
        public ValueTask<IDrawImage> LoadAsync(string path, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Load(path));
    }
}

internal sealed class TileMapStage4Publication
{
    private readonly TileMapSource2D original;
    private readonly TileMapSource2D mutated;
    internal TileMapSource2D Source { get; }

    internal TileMapStage4Publication(TileMap2DModel originalModel, TileMap2DModel mutatedModel)
    {
        original = TileMapSource2D.FromModel(originalModel);
        mutated = TileMapSource2D.FromModel(mutatedModel);
        Source = new(original.Catalog, (catalog, chunk, token) =>
            (ReferenceEquals(catalog, original.Catalog) ? original : mutated).LoadAsync(chunk.Spatial, token));
    }

    internal void Publish(bool useMutation) => Source.SetCatalog(useMutation ? mutated.Catalog : original.Catalog);
}

internal static class TileMapStage4ModelFactory
{
    internal const int TileSize = 16;
    internal const int ChunkSize = TileMapBaselineWorkload.ChunkSize;
    internal const int CellsPerChunk = ChunkSize * ChunkSize;
    internal const int LayerCount = TileMapBaselineWorkload.LayerCount;
    internal const int WidthInTiles = TileMapBaselineWorkload.WidthInTiles;
    internal const int HeightInTiles = TileMapBaselineWorkload.HeightInTiles;
    internal const int ViewWidthInTiles = TileMapBaselineWorkload.ViewWidthInTiles;
    internal const int ViewHeightInTiles = TileMapBaselineWorkload.ViewHeightInTiles;
    internal const int VillageSeed = TileMapBaselineWorkload.VillageSeed;

    internal static readonly ResourceId<ImageResource> TerrainResourceId = new("VillageTerrain");
    internal static readonly ResourceId<ImageResource> StructuresResourceId = new("VillageStructures");

    internal static TileMap2DModel[] Create(bool mutated)
    {
        TileSet2D[] tileSets =
        [
            new TileSet2D(
                "Terrain",
                TerrainResourceId,
                Enumerable.Range(1, 12).Select(static id =>
                    new TileDefinition2D(id, new DrawRect((id - 1) * TileSize, 0, TileSize, TileSize)))),
            new TileSet2D(
                "Structures",
                StructuresResourceId,
                Enumerable.Range(100, 8).Select(static id =>
                    new TileDefinition2D(id, new DrawRect((id - 100) * TileSize, 0, TileSize, TileSize))))
        ];
        return Enumerable.Range(0, LayerCount)
            .Select(layer => CreateMap(layer, mutated, tileSets))
            .ToArray();
    }

    internal static DrawRect CameraView(int cameraTileX) =>
        new(
            cameraTileX * TileSize,
            24 * TileSize,
            ViewWidthInTiles * TileSize,
            ViewHeightInTiles * TileSize);

    private static TileMap2DModel CreateMap(int layer, bool mutated, IReadOnlyList<TileSet2D> tileSets)
    {
        List<TileChunk2D> chunks = [];
        for (int chunkY = 0; chunkY < HeightInTiles / ChunkSize; chunkY++)
        {
            for (int chunkX = 0; chunkX < WidthInTiles / ChunkSize; chunkX++)
            {
                TileCell2D[] cells = CreateCells(layer, chunkX, chunkY);
                bool changed = mutated && layer == 1 && chunkX == 2 && chunkY == 2;
                if (changed)
                {
                    TileCell2D original = cells[0];
                    cells[0] = new TileCell2D(
                        original.TileId,
                        original.Flip ^ TileFlip2D.Horizontal);
                }
                chunks.Add(new TileChunk2D(
                    new TileCoordinate2D(chunkX * ChunkSize, chunkY * ChunkSize),
                    ChunkSize,
                    ChunkSize,
                    cells,
                    version: changed ? 2 : 1));
            }
        }
        return new TileMap2DModel(
            $"Layer{layer}",
            new DrawSize(TileSize, TileSize),
            tileSets,
            chunks,
            new TileMapBounds2D(0, 0, WidthInTiles, HeightInTiles),
            order: layer,
            version: mutated && layer == 1 ? 2 : 1);
    }

    private static TileCell2D[] CreateCells(int layer, int chunkX, int chunkY)
    {
        TileCell2D[] cells = new TileCell2D[CellsPerChunk];
        for (int localY = 0; localY < ChunkSize; localY++)
        {
            for (int localX = 0; localX < ChunkSize; localX++)
            {
                int worldX = (chunkX * ChunkSize) + localX;
                int worldY = (chunkY * ChunkSize) + localY;
                int selector = Math.Abs(unchecked(
                    (worldX * 31) +
                    (worldY * 17) +
                    (layer * 13) +
                    VillageSeed));
                int tileId = selector % 11 == 0
                    ? 0
                    : layer == 2 && selector % 5 == 0
                        ? 100 + (selector % 8)
                        : 1 + (selector % 12);
                int flipSelector = Math.Abs(unchecked(
                    (worldX * 31) +
                    (worldY * 17) +
                    (layer * 13)));
                TileFlip2D flip = TileFlip2D.None;
                if (flipSelector % 29 == 0)
                {
                    flip |= TileFlip2D.Horizontal;
                }
                if (flipSelector % 31 == 0)
                {
                    flip |= TileFlip2D.Vertical;
                }
                cells[(localY * ChunkSize) + localX] = new TileCell2D(tileId, flip);
            }
        }
        return cells;
    }
}

internal readonly record struct TileMapStage4Counters(
    int TotalChunks,
    int CandidateChunks,
    int VisibleChunks,
    int CandidateTiles,
    int DrawnTiles,
    int BatchesBuilt,
    int BatchesRebuilt,
    int BatchesReused,
    int DrawCommands,
    long RetainedBytes,
    int RetainedObjects,
    int TileInvalidations)
{
    public int WarmChunks { get; init; }
    public int WarmBatchesPrepared { get; init; }
    public int WarmTilesPrepared { get; init; }
    public long WarmRetainedBytes { get; init; }
    public long WarmChargedBytes { get; init; }
    public long WarmImageBytes { get; init; }

    internal static TileMapStage4Counters From(IReadOnlyList<TileMap2D> maps)
    {
        TileMapStage4Counters total = default;
        for (int index = 0; index < maps.Count; index++)
        {
            TileMap2DDiagnosticsSnapshot snapshot = maps[index].GetDiagnosticsSnapshot();
            total = new(
                total.TotalChunks + snapshot.TotalChunks,
                total.CandidateChunks + snapshot.CandidateChunks,
                total.VisibleChunks + snapshot.VisibleChunks,
                total.CandidateTiles + snapshot.CandidateTiles,
                total.DrawnTiles + snapshot.DrawnTiles,
                total.BatchesBuilt + snapshot.BatchesBuilt,
                total.BatchesRebuilt + snapshot.BatchesRebuilt,
                total.BatchesReused + snapshot.BatchesReused,
                total.DrawCommands + snapshot.DrawCommands,
                total.RetainedBytes + snapshot.RetainedBytes,
                total.RetainedObjects + snapshot.RetainedObjects,
                total.TileInvalidations + snapshot.TileInvalidations)
            {
                WarmChunks = total.WarmChunks + snapshot.WarmChunks,
                WarmBatchesPrepared = total.WarmBatchesPrepared + snapshot.WarmBatchesPrepared,
                WarmTilesPrepared = total.WarmTilesPrepared + snapshot.WarmTilesPrepared,
                WarmRetainedBytes = total.WarmRetainedBytes + snapshot.WarmRetainedBytes,
                WarmChargedBytes = total.WarmChargedBytes + snapshot.WarmChargedBytes,
                WarmImageBytes = total.WarmImageBytes + snapshot.WarmImageBytes
            };
        }
        return total;
    }
}

internal sealed record TileMapStage4Scenario(
    string Name,
    double CpuP50Microseconds,
    double CpuP95Microseconds,
    double AllocatedBytesPerOperation,
    TileMapStage4Counters Counters,
    int MaximumRebuilds,
    int MaximumPreparedTiles,
    int TotalPreparedBatches,
    int MaximumWarmChunks,
    long MaximumWarmChargedBytes,
    long MaximumWarmRetainedBytes,
    long MaximumWarmImageBytes,
    TileMapStage4Initialization Initialization,
    TileMapStage4RuntimePrimer RuntimePrimer,
    TileMapStage4JitQuiescence JitQuiescence,
    long CompletedJitCompilations,
    double[] OperationMicroseconds);

internal sealed record TileMapStage4Samples(
    double[] OperationMicroseconds,
    long AllocatedBytes,
    long CompletedJitCompilations,
    TileMapStage4Counters Counters,
    int MaximumRebuilds,
    int MaximumPreparedTiles,
    int TotalPreparedBatches,
    int MaximumWarmChunks,
    long MaximumWarmChargedBytes,
    long MaximumWarmRetainedBytes,
    long MaximumWarmImageBytes);

internal sealed record TileMapStage4Initialization(
    bool IsProcessColdFixture,
    double ConstructionMicroseconds,
    long ConstructionAllocatedBytes,
    double FirstRecordingMicroseconds,
    double WarmupElapsedMicroseconds,
    long WarmupAllocatedBytes,
    long WarmupCompletedJitCompilations,
    double[] WarmupOperationMicroseconds)
{
    public double ConstructionAndFirstRecordingMicroseconds => ConstructionMicroseconds + FirstRecordingMicroseconds;
}

internal sealed record TileMapStage4RuntimePrimer(
    int Operations,
    double ElapsedMicroseconds,
    double ActionMicroseconds,
    long AllocatedBytes,
    long CompletedJitCompilations);

internal sealed record TileMapStage4JitQuiescence(
    bool Reached,
    double ElapsedMilliseconds,
    long CompletedJitCompilations);

internal sealed record TileMapStage4Gate(
    string Name,
    bool Passed,
    string Evidence);

internal sealed record TileMapStage4Report(
    string Schema,
    DateTimeOffset TimestampUtc,
    string Commit,
    bool WorkingTreeDirty,
    string Runtime,
    string OperatingSystem,
    string ProcessArchitecture,
    string Processor,
    int LogicalProcessorCount,
    long StopwatchFrequency,
    int WarmupIterations,
    int MeasurementIterations,
    int RuntimePrimerBlocks,
    int RuntimePrimerBlockIterations,
    int RuntimePrimerPauseMilliseconds,
    int JitQuietMilliseconds,
    int JitQuietTimeoutMilliseconds,
    TileMapStage4FixtureDescription Fixture,
    TileMapStage4Baseline Baseline,
    IReadOnlyList<TileMapStage4Scenario> Scenarios,
    long FullFixtureRetainedBytes,
    IReadOnlyList<TileMapStage4Gate> Gates);

internal sealed record TileMapStage4FixtureDescription(
    int Seed,
    int WidthInTiles,
    int HeightInTiles,
    int LayerCount,
    int ChunkSize,
    int ViewWidthInTiles,
    int ViewHeightInTiles)
{
    internal static TileMapStage4FixtureDescription Current { get; } = new(
        TileMapStage4ModelFactory.VillageSeed,
        TileMapStage4ModelFactory.WidthInTiles,
        TileMapStage4ModelFactory.HeightInTiles,
        TileMapStage4ModelFactory.LayerCount,
        TileMapStage4ModelFactory.ChunkSize,
        TileMapStage4ModelFactory.ViewWidthInTiles,
        TileMapStage4ModelFactory.ViewHeightInTiles);
}

internal sealed record TileMapStage4Baseline(
    string Artifact,
    double WarmStaticP95Microseconds,
    double WarmStaticAllocatedBytes,
    double CameraPanP95Microseconds,
    double CameraPanAllocatedBytes,
    double ChunkMutationP95Microseconds,
    double ChunkMutationAllocatedBytes)
{
    internal static TileMapStage4Baseline Current { get; } = new(
        "benchmarks/Cerneala.Benchmarks/results/2026-09-04-tilemap-baseline/baseline.json",
        3495.2,
        2_910_116,
        2915.2,
        3_822_182.203125,
        2262.2,
        2_865_317.140625);
}

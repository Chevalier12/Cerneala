using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Cerneala.Drawing;

namespace Cerneala.Benchmarks;

[MemoryDiagnoser]
public sealed class TileMapBaselineBenchmarks
{
    private TileMapBaselineWorkload workload = null!;
    private int iteration;

    [GlobalSetup]
    public void Setup() => workload = new TileMapBaselineWorkload();

    [Benchmark(Baseline = true)]
    public TileMapBaselineCounters WarmStatic() => workload.RecordFrame(cameraTileX: 32, mutateChunk: false);

    [Benchmark]
    public TileMapBaselineCounters CameraPan()
    {
        iteration++;
        return workload.RecordFrame(cameraTileX: 32 + (iteration % 32), mutateChunk: false);
    }

    [Benchmark]
    public TileMapBaselineCounters ChunkMutation()
    {
        iteration++;
        return workload.RecordFrame(cameraTileX: 32, mutateChunk: true);
    }
}

internal sealed class TileMapBaselineWorkload
{
    internal const int ChunkSize = 16;
    internal const int LayerCount = 3;
    internal const int WidthInTiles = 128;
    internal const int HeightInTiles = 96;
    internal const int ViewWidthInTiles = 48;
    internal const int ViewHeightInTiles = 32;
    internal const int VillageSeed = 0x5A17;

    private readonly BaselineImage terrain = new(256, 256, "VillageTerrain");
    private readonly BaselineImage structures = new(256, 256, "VillageStructures");
    private readonly BaselineChunk[] chunks;
    private readonly DrawCommandList commands = new();
    private readonly DrawingContext drawing;
    private int mutationVersion;

    internal TileMapBaselineWorkload()
    {
        chunks = CreateChunks();
        drawing = new DrawingContext(commands);
    }

    internal TileMapBaselineCounters RecordFrame(int cameraTileX, bool mutateChunk)
    {
        commands.Clear();
        if (mutateChunk)
        {
            mutationVersion++;
            BaselineChunk changed = chunks.First(chunk =>
                chunk.Layer == 1 && chunk.ChunkX == 2 && chunk.ChunkY == 2);
            int index = mutationVersion % changed.TileIds.Length;
            changed.TileIds[index] = changed.TileIds[index] == 0 ? 1 : 0;
        }

        int cameraTileY = 24;
        int minChunkX = FloorDiv(cameraTileX, ChunkSize);
        int minChunkY = FloorDiv(cameraTileY, ChunkSize);
        int maxChunkX = FloorDiv(cameraTileX + ViewWidthInTiles - 1, ChunkSize);
        int maxChunkY = FloorDiv(cameraTileY + ViewHeightInTiles - 1, ChunkSize);
        int visibleChunks = 0;
        int candidateTiles = 0;
        int drawnTiles = 0;
        int batchesBuilt = 0;
        long retainedBytes = 0;

        foreach (BaselineChunk chunk in chunks)
        {
            if (chunk.ChunkX < minChunkX || chunk.ChunkX > maxChunkX ||
                chunk.ChunkY < minChunkY || chunk.ChunkY > maxChunkY)
            {
                continue;
            }

            visibleChunks++;
            candidateTiles += chunk.TileIds.Length;
            List<DrawSprite2D> terrainSprites = [];
            List<DrawSprite2D> structureSprites = [];
            for (int index = 0; index < chunk.TileIds.Length; index++)
            {
                int tileId = chunk.TileIds[index];
                if (tileId == 0)
                {
                    continue;
                }

                int localX = index % ChunkSize;
                int localY = index / ChunkSize;
                int worldX = (chunk.ChunkX * ChunkSize) + localX;
                int worldY = (chunk.ChunkY * ChunkSize) + localY;
                bool structure = tileId >= 100;
                int localId = structure ? tileId - 100 : tileId - 1;
                DrawRect source = new(
                    (localId % 16) * 16,
                    (localId / 16) * 16,
                    16,
                    16);
                DrawImageFlip flip = ResolveFlip(worldX, worldY, chunk.Layer);
                DrawSprite2D sprite = new(
                    new DrawRect(worldX * 16, worldY * 16, 16, 16),
                    new DrawImageOptions(
                        source: source,
                        flip: flip,
                        sampling: DrawSamplingMode.Point));
                (structure ? structureSprites : terrainSprites).Add(sprite);
                drawnTiles++;
            }

            batchesBuilt += EmitBatch(terrain, terrainSprites);
            batchesBuilt += EmitBatch(structures, structureSprites);
            retainedBytes += ((long)terrainSprites.Count + structureSprites.Count) * (4 * 20L + 6 * sizeof(int));
        }

        return new TileMapBaselineCounters(
            TotalChunks: chunks.Length,
            CandidateChunks: chunks.Length,
            VisibleChunks: visibleChunks,
            CandidateTiles: candidateTiles,
            DrawnTiles: drawnTiles,
            BatchesBuilt: batchesBuilt,
            BatchesRebuilt: batchesBuilt,
            BatchesReused: 0,
            Commands: commands.Count,
            RetainedBytes: retainedBytes,
            Invalidations: mutateChunk ? 1 : 0);
    }

    private int EmitBatch(IDrawImage image, List<DrawSprite2D> sprites)
    {
        if (sprites.Count == 0)
        {
            return 0;
        }

        drawing.DrawSpriteBatch(new DrawSpriteBatch(image, sprites));
        return 1;
    }

    private static BaselineChunk[] CreateChunks()
    {
        List<BaselineChunk> result = [];
        for (int layer = 0; layer < LayerCount; layer++)
        {
            for (int chunkY = 0; chunkY < HeightInTiles / ChunkSize; chunkY++)
            {
                for (int chunkX = 0; chunkX < WidthInTiles / ChunkSize; chunkX++)
                {
                    int[] tileIds = new int[ChunkSize * ChunkSize];
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
                            tileIds[(localY * ChunkSize) + localX] = selector % 11 == 0
                                ? 0
                                : layer == 2 && selector % 5 == 0
                                    ? 100 + (selector % 8)
                                    : 1 + (selector % 12);
                        }
                    }
                    result.Add(new BaselineChunk(layer, chunkX, chunkY, tileIds));
                }
            }
        }
        return result.ToArray();
    }

    private static DrawImageFlip ResolveFlip(int worldX, int worldY, int layer)
    {
        int selector = Math.Abs(unchecked((worldX * 31) + (worldY * 17) + (layer * 13)));
        DrawImageFlip result = DrawImageFlip.None;
        if (selector % 29 == 0)
        {
            result |= DrawImageFlip.Horizontal;
        }
        if (selector % 31 == 0)
        {
            result |= DrawImageFlip.Vertical;
        }
        return result;
    }

    private static int FloorDiv(int value, int divisor)
    {
        int quotient = value / divisor;
        int remainder = value % divisor;
        return remainder < 0 ? quotient - 1 : quotient;
    }

    private sealed record BaselineImage(int Width, int Height, string Name) : IDrawImage;

    private sealed record BaselineChunk(int Layer, int ChunkX, int ChunkY, int[] TileIds);
}

public readonly record struct TileMapBaselineCounters(
    int TotalChunks,
    int CandidateChunks,
    int VisibleChunks,
    int CandidateTiles,
    int DrawnTiles,
    int BatchesBuilt,
    int BatchesRebuilt,
    int BatchesReused,
    int Commands,
    long RetainedBytes,
    int Invalidations);

internal static class TileMapBaselineBenchmarkRunner
{
    private const int WarmupIterations = 64;
    private const int MeasurementIterations = 512;

    internal static void Run(string reportPath)
    {
        string fullPath = Path.GetFullPath(reportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        TileMapBaselineWorkload workload = new();

        for (int iteration = 0; iteration < WarmupIterations; iteration++)
        {
            _ = workload.RecordFrame(32 + (iteration % 32), mutateChunk: iteration % 8 == 0);
        }

        TileMapBaselineScenario[] scenarios =
        [
            Measure("warm-static", iteration => workload.RecordFrame(32, mutateChunk: false)),
            Measure("camera-pan", iteration => workload.RecordFrame(32 + (iteration % 32), mutateChunk: false)),
            Measure("chunk-mutation", _ => workload.RecordFrame(32, mutateChunk: true))
        ];
        TileMapBaselineReport report = new(
            SchemaVersion: 1,
            TimestampUtc: DateTimeOffset.UtcNow,
            Commit: ResolveCommit(),
            Runtime: RuntimeInformation.FrameworkDescription,
            OperatingSystem: RuntimeInformation.OSDescription,
            ProcessArchitecture: RuntimeInformation.ProcessArchitecture.ToString(),
            Processor: Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "unknown",
            LogicalProcessorCount: Environment.ProcessorCount,
            StopwatchFrequency: Stopwatch.Frequency,
            WarmupIterations,
            MeasurementIterations,
            Fixture: new TileMapBaselineFixture(
                TileMapBaselineWorkload.VillageSeed,
                TileMapBaselineWorkload.WidthInTiles,
                TileMapBaselineWorkload.HeightInTiles,
                TileMapBaselineWorkload.LayerCount,
                TileMapBaselineWorkload.ChunkSize,
                TileMapBaselineWorkload.ViewWidthInTiles,
                TileMapBaselineWorkload.ViewHeightInTiles),
            Scenarios: scenarios);
        File.WriteAllText(
            fullPath,
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);

        Console.WriteLine($"Tilemap baseline: {fullPath}");
        foreach (TileMapBaselineScenario scenario in scenarios)
        {
            Console.WriteLine(
                $"{scenario.Name}: p50={scenario.CpuP50Microseconds:F3}us, " +
                $"p95={scenario.CpuP95Microseconds:F3}us, allocated={scenario.AllocatedBytesPerOperation:F0}B/op, " +
                $"commands={scenario.Counters.Commands}, rebuilds={scenario.Counters.BatchesRebuilt}");
        }
    }

    private static TileMapBaselineScenario Measure(
        string name,
        Func<int, TileMapBaselineCounters> action)
    {
        double[] samples = new double[MeasurementIterations];
        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        TileMapBaselineCounters counters = default;
        for (int iteration = 0; iteration < MeasurementIterations; iteration++)
        {
            long started = Stopwatch.GetTimestamp();
            counters = action(iteration);
            samples[iteration] = Stopwatch.GetElapsedTime(started).TotalMicroseconds;
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Array.Sort(samples);
        return new TileMapBaselineScenario(
            name,
            CpuP50Microseconds: Percentile(samples, 0.50),
            CpuP95Microseconds: Percentile(samples, 0.95),
            AllocatedBytesPerOperation: (double)allocated / MeasurementIterations,
            counters);
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

    private sealed record TileMapBaselineReport(
        int SchemaVersion,
        DateTimeOffset TimestampUtc,
        string Commit,
        string Runtime,
        string OperatingSystem,
        string ProcessArchitecture,
        string Processor,
        int LogicalProcessorCount,
        long StopwatchFrequency,
        int WarmupIterations,
        int MeasurementIterations,
        TileMapBaselineFixture Fixture,
        IReadOnlyList<TileMapBaselineScenario> Scenarios);

    private sealed record TileMapBaselineFixture(
        int Seed,
        int WidthInTiles,
        int HeightInTiles,
        int LayerCount,
        int ChunkSize,
        int ViewWidthInTiles,
        int ViewHeightInTiles);
}

internal sealed record TileMapBaselineScenario(
    string Name,
    double CpuP50Microseconds,
    double CpuP95Microseconds,
    double AllocatedBytesPerOperation,
    TileMapBaselineCounters Counters);

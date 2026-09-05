using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Resources;

namespace Cerneala.Benchmarks;

internal static class SceneDebugOverlayBenchmarkRunner
{
    private const int WarmupIterations = 512;
    private const int MeasurementIterations = 2048;

    internal static void Run(string reportPath)
    {
        List<Result> results = [];
        foreach (int remoteChunks in new[] { 0, 4096 })
        {
            Workload workload = new(remoteChunks);
            Result absent = Measure("frame-absent", workload, workload.RecordFrame);
            workload.Scene.Children.Add(workload.Overlay);
            Result disabled = Measure("frame-disabled", workload, workload.RecordFrame);
            Result disabledOnly = Measure("overlay-only-disabled", workload, workload.RecordDisabledOverlay);
            workload.Overlay.Flags = Scene2DDebugFlags.All;
            Result enabled = Measure("frame-all-flags", workload, workload.RecordFrame);
            results.AddRange([absent, disabled, disabledOnly, enabled]);

            if (disabled.Commands != absent.Commands || disabledOnly.Commands != 0 ||
                disabledOnly.AllocatedBytesPerOperation != 0 || disabled.Debug.Primitives != 0 ||
                disabled.AllocatedBytesPerOperation != absent.AllocatedBytesPerOperation)
            {
                throw new InvalidOperationException("Disabled overlay added commands or allocations after warmup.");
            }
            if (enabled.Debug.CandidateChunks > 12 || enabled.Debug.VisitedTiles != 192 ||
                enabled.Debug.NavigationCells != 192 || enabled.Map.BatchesRebuilt != 0)
            {
                throw new InvalidOperationException("Debug work escaped the viewport or rebuilt static tile batches.");
            }
        }

        string path = Path.GetFullPath(reportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            Schema = "cerneala-scene-debug-overlay-cost-v1",
            TimestampUtc = DateTimeOffset.UtcNow,
            Runtime = RuntimeInformation.FrameworkDescription,
            OperatingSystem = RuntimeInformation.OSDescription,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            Environment.ProcessorCount,
            Stopwatch.Frequency,
            WarmupIterations,
            MeasurementIterations,
            Scope = "CPU command recording only; no backend submission, GPU timing, or manual interaction.",
            Fixture = "256x192 viewport, 16x12 visible 16px tiles in twelve 4x4 chunks, one promoted tile, 8 visible and 32 remote colliders, 192 visible navigation cells.",
            Results = results
        }, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
        foreach (Result result in results)
        {
            Console.WriteLine($"{result.Name}, remote={result.RemoteChunks}: " +
                $"p50={result.CpuP50Microseconds:F3}us p95={result.CpuP95Microseconds:F3}us " +
                $"allocated={result.AllocatedBytesPerOperation:F0}B commands={result.Commands}");
        }
        Console.WriteLine(path);
    }

    private static Result Measure(string name, Workload workload, Action operation)
    {
        for (int i = 0; i < WarmupIterations; i++) { operation(); }
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        double[] samples = new double[MeasurementIterations];
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < samples.Length; i++)
        {
            long start = Stopwatch.GetTimestamp();
            operation();
            samples[i] = Stopwatch.GetElapsedTime(start).TotalMicroseconds;
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Array.Sort(samples);
        return new Result(name, workload.RemoteChunks,
            samples[(int)Math.Ceiling(samples.Length * 0.50) - 1],
            samples[(int)Math.Ceiling(samples.Length * 0.95) - 1],
            (double)allocated / samples.Length, workload.Commands.Count,
            workload.Overlay.GetDiagnosticsSnapshot(), workload.Map.GetDiagnosticsSnapshot());
    }

    private sealed record Result(string Name, int RemoteChunks, double CpuP50Microseconds,
        double CpuP95Microseconds, double AllocatedBytesPerOperation, int Commands,
        Scene2DDebugOverlayDiagnostics Debug, TileMap2DDiagnosticsSnapshot Map);

    private sealed class Workload
    {
        private static readonly DrawRect Bounds = new(0, 0, 256, 192);
        private readonly RenderSurface2D surface;
        private readonly Scene2DRecordContext disabledContext;
        internal int RemoteChunks { get; }
        internal Scene2D Scene { get; } = new();
        internal Scene2DDebugOverlay Overlay { get; } = new() { NavigationGrid = new Grid(), FontSize = 5 };
        internal TileMap2D Map { get; }
        internal DrawCommandList Commands { get; } = new();

        internal Workload(int remoteChunks)
        {
            RemoteChunks = remoteChunks;
            List<TileChunk2D> chunks = [];
            for (int y = 0; y < 3; y++)
            for (int x = 0; x < 4; x++)
                chunks.Add(Chunk(x * 4, y * 4));
            for (int i = 0; i < remoteChunks; i++) { chunks.Add(Chunk(10000 + i * 4, 10000)); }
            ResourceId<ImageResource> atlas = new("DebugBenchmarkAtlas");
            Map = new TileMap2D { Model = new TileMap2DModel(new DrawSize(16, 16),
                [new TileSet2D("atlas", atlas, [new TileDefinition2D(1, new DrawRect(0, 0, 16, 16))])],
                [new TileLayer2DModel("ground", chunks)]) };
            Map.Resources.SetResource(atlas, new ImageResource(new InlineImage()));
            Map.Promote(new TileCellKey2D("ground", 1, 1)).TranslateX = 3;
            Scene.Children.Add(Map);
            for (int i = 0; i < 8; i++)
                Scene.Children.Add(new BoxCollider2D { Width = 8, Height = 8, TranslateX = i * 24, TranslateY = 32 });
            for (int i = 0; i < 32; i++)
                Scene.Children.Add(new BoxCollider2D { Width = 8, Height = 8, TranslateX = 10000 + i * 24 });
            surface = new RenderSurface2D { Scene = Scene, ViewBox = Bounds };
            RenderSurface2DFrame frame = new(Commands, Bounds, TimeSpan.Zero);
            disabledContext = new Scene2DRecordContext(surface, frame, Matrix3x2.Identity, Bounds);
        }

        internal void RecordFrame()
        {
            Commands.Clear();
            ((IRenderSurface2DFrameSource)surface).RecordFrame(Commands, Bounds);
        }

        internal void RecordDisabledOverlay()
        {
            Commands.Clear();
            Overlay.Record(disabledContext);
        }

        private static TileChunk2D Chunk(int x, int y) =>
            new(new TileCoordinate2D(x, y), 4, 4, Enumerable.Repeat(new TileCell2D(1), 16));
    }

    private sealed class InlineImage : IDrawImage
    {
        public int Width => 16;
        public int Height => 16;
    }

    private sealed class Grid : IScene2DDebugNavigationGrid
    {
        public TileMapBounds2D Bounds => new(-10000, -10000, 20000, 20000);
        public DrawPoint Origin => default;
        public DrawSize CellSize => new(16, 16);
        public bool TryGetCell(int x, int y, out bool blocked) { blocked = (x + y) % 3 == 0; return true; }
    }
}

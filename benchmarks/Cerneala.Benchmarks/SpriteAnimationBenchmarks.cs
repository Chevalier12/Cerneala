using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.Benchmarks;

internal static class SpriteAnimationBenchmarkRunner
{
    private const int Warmup = 128;
    private const int Iterations = 256;
    private static readonly TimeSpan Delta = TimeSpan.FromMilliseconds(16);
    private static readonly DrawRect Bounds = new(0, 0, 1600, 1600);

    internal static void Run(string reportPath)
    {
        List<object> results = [];
        foreach (int count in new[] { 1, 100, 10_000 })
        {
            foreach (int active in new[] { 0, Math.Max(1, count / 10), count }.Distinct())
            {
                using Workload workload = new(count, active, tiles: false, prism: false);
                results.Add(Measure(workload, $"sprites-{count}-active-{active}", record: false));
                results.Add(Measure(workload, $"sprites-{count}-active-{active}", record: true));
            }
        }
        foreach (int promoted in new[] { 0, 1, 100 })
        {
            foreach (bool prism in promoted == 0 ? new[] { false } : new[] { false, true })
            {
                using Workload workload = new(1024, promoted, tiles: true, prism);
                results.Add(Measure(workload, $"tiles-1024-promoted-{promoted}-prism-{prism}", record: false));
                results.Add(Measure(workload, $"tiles-1024-promoted-{promoted}-prism-{prism}", record: true));
            }
        }
        string path = Path.GetFullPath(reportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            Schema = "sprite-animation-stage3-v1",
            Timestamp = DateTimeOffset.UtcNow,
            Runtime = RuntimeInformation.FrameworkDescription,
            OS = RuntimeInformation.OSDescription,
            Processor = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"),
            Warmup, Iterations, DeltaMilliseconds = Delta.TotalMilliseconds,
            Scope = "UI temporal traversal; separately traversal + root commit + dirty surface command recording. No input, GPU execution or presentation.",
            Results = results
        }, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
        Console.WriteLine(path);
    }

    private static object Measure(Workload workload, string name, bool record)
    {
        for (int i = 0; i < Warmup; i++) workload.Tick(record);
        double[] samples = new double[Iterations];
        long version = workload.Source.FrameVersion;
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < Iterations; i++)
        {
            long start = Stopwatch.GetTimestamp();
            workload.Tick(record);
            samples[i] = Stopwatch.GetElapsedTime(start).TotalMicroseconds;
        }
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        Array.Sort(samples);
        long invalidations = workload.Source.FrameVersion - version;
        if (workload.Surface.ActiveAnimationCount == 0 && invalidations != 0)
            throw new InvalidOperationException("Inactive animation invalidated the surface.");
        Console.WriteLine($"{name} record={record} p95={samples[243]:F2}us bytes={allocated / (double)Iterations:F0} invalidations={invalidations}");
        return new
        {
            Name = name, RecordAndCommit = record,
            Active = workload.Surface.ActiveAnimationCount,
            P50Microseconds = samples[127], P95Microseconds = samples[243],
            AllocatedBytesPerTick = allocated / (double)Iterations,
            Invalidations = invalidations,
            Commands = workload.Commands.Count,
            TileCounters = workload.Map?.GetDiagnosticsSnapshot()
        };
    }

    private sealed class Workload : IDisposable
    {
        private readonly UIRoot root = new();
        private readonly List<IDisposable> effects = [];
        private long recordedVersion = -1;
        internal RenderSurface2D Surface { get; }
        internal IRenderSurface2DFrameSource Source => Surface;
        internal DrawCommandList Commands { get; } = new();
        internal TileMap2D? Map { get; }

        internal Workload(int count, int active, bool tiles, bool prism)
        {
            SpriteAnimationSet clips = new([new SpriteAnimationClip("Walk", [
                new SpriteAnimationFrame(new DrawRect(0, 0, 16, 16), TimeSpan.FromMilliseconds(100)),
                new SpriteAnimationFrame(new DrawRect(16, 0, 16, 16), TimeSpan.FromMilliseconds(100))])]);
            Scene2D scene = new();
            Surface = new RenderSurface2D { Scene = scene, RedrawMode = RenderSurface2DRedrawMode.OnDemand };
            if (tiles)
            {
                ResourceId<ImageResource> atlas = new("AnimationAtlas");
                Map = new TileMap2D { Model = new TileMap2DModel(new DrawSize(16, 16),
                    [new TileSet2D("World", atlas, [new TileDefinition2D(1, new DrawRect(0, 0, 16, 16))])],
                    [new TileLayer2DModel("Ground", [new TileChunk2D(new TileCoordinate2D(0, 0), 32, 32,
                        Enumerable.Repeat(new TileCell2D(1), count))])], new TileMapBounds2D(0, 0, 32, 32)) };
                scene.Children.Add(Map);
                Surface.Resources.SetResource(atlas, new ImageResource("animation-atlas.png"));
                root.SetImageLoader(new ImageLoader());
                for (int i = 0; i < active; i++)
                {
                    int cell = 1 + i * 3;
                    TileInstance2D tile = Map.Promote(new TileCellKey2D("Ground", cell % 32, cell / 32));
                    tile.Animations = clips;
                    tile.AnimationState = "Walk";
                    if (prism)
                        effects.Add(GeneratedMarkup.AttachPrism(tile, () => new PrismInstance(
                            new PrismCompositionDefinition("AnimatedTile", [new PrismLayerDefinition(
                                new PrismNodeId(1), "Content", filters: [new PrismFilterDefinition(PrismFilterId.Blur)])]))));
                }
            }
            else
            {
                IDrawImage image = new Image();
                for (int i = 0; i < count; i++)
                    scene.Children.Add(new Sprite2D { Source = image,
                        Destination = new DrawRect(i % 100 * 16, i / 100 * 16, 16, 16),
                        Animations = clips, AnimationState = "Walk", IsAnimationPaused = i >= active });
            }
            root.VisualChildren.Add(Surface);
            root.ProcessFrame();
            Record();
            if (Surface.ActiveAnimationCount != active)
                throw new InvalidOperationException("Active registration count does not match fixture.");
        }

        internal void Tick(bool record)
        {
            TimeSensitiveRenderInvalidator.Invalidate(root, Delta);
            if (record)
            {
                root.ProcessFrame();
                if (Source.FrameVersion != recordedVersion) Record();
            }
        }

        private void Record()
        {
            Commands.Clear();
            Source.RecordFrame(Commands, Bounds);
            recordedVersion = Source.FrameVersion;
        }

        public void Dispose()
        {
            root.VisualChildren.Remove(Surface);
            foreach (IDisposable effect in effects) effect.Dispose();
            if (Surface.ActiveAnimationCount != 0)
                throw new InvalidOperationException("Detached surface retained animations.");
        }
    }

    private sealed class Image : IDrawImage
    {
        public int Width => 32;
        public int Height => 16;
    }

    private sealed class ImageLoader : IImageLoader
    {
        public IDrawImage Load(string path) => new Image();
    }
}

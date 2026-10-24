using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;

namespace Cerneala.Benchmarks;

// Sorting, Scene2D Source and SceneItems2D: the scenarios the culled-ordering fix covers. The gate is deterministic:
// the image commands of every recorded frame are bounded by the sprites that intersect the camera, whatever N is.
internal static class Rs2dOrderingBenchmarkRunner
{
    private static readonly int[] DefaultSizes = [1_000, 10_000, 100_000];

    internal static void Run(string reportPath, string[] sizeArguments)
    {
        int[]? sizes = Rs2dStressScene.ParseSizes(sizeArguments);
        Rs2dStressSpec[] specs =
        [
            new("S1 Scene Source pan", Rs2dStressKind.SceneSprites, Gated: true),
            new("S1 Scene Source STATIC cont", Rs2dStressKind.SceneSprites, Camera: Rs2dStressCamera.Static, Gated: true),
            new("S1 Scene Source STATIC ondemand", Rs2dStressKind.SceneSprites, Camera: Rs2dStressCamera.Static,
                Redraw: RenderSurface2DRedrawMode.OnDemand, Gated: true),
            new("S2 Scene Layer pan (all layer 0)", Rs2dStressKind.SceneSprites, SceneOrderMode.Layer, Gated: true),
            new("S2b Scene Layer pan (layers 0..3)", Rs2dStressKind.SceneSprites, SceneOrderMode.Layer, RandomLayers: true, Gated: true),
            new("S3 Scene LayerThenY pan", Rs2dStressKind.SceneSprites, SceneOrderMode.LayerThenY, Gated: true),
            new("S3 Scene LayerThenY STATIC cont", Rs2dStressKind.SceneSprites, SceneOrderMode.LayerThenY,
                Camera: Rs2dStressCamera.Static, Gated: true),
            new("S3 Scene LayerThenY STATIC ondemand", Rs2dStressKind.SceneSprites, SceneOrderMode.LayerThenY,
                Camera: Rs2dStressCamera.Static, Redraw: RenderSurface2DRedrawMode.OnDemand, Gated: true),
            new("S4 SceneItems2D pan", Rs2dStressKind.Items, Gated: true)
        ];
        Rs2dHeadlessSuite.Run(reportPath, "cerneala-rs2d-ordering-v1",
            "CPU frame cost of UI clock traversal, scheduler pass and command recording for one RenderSurface2D; no backend " +
            "submission, GPU timing or input. Gate: image commands per recorded frame stay within the sprites that intersect the camera.",
            specs, sizes, DefaultSizes);
    }
}

// Scenarios the optimizations do not cover yet. They report numbers and fail only when the fixture itself is wrong
// (cells not realized, tile maps never ready); they have no performance gate.
internal static class Rs2dGroupsBenchmarkRunner
{
    private static readonly int[] DefaultSizes = [1_000, 10_000, 100_000];

    // A TileMap2D attach takes minutes at 10,000 maps, so the TileMap2D and per-cell SceneItems2D children stop there.
    private static readonly int[] ChildSizes = [1_000, 10_000];

    internal static void Run(string reportPath, string[] sizeArguments)
    {
        int[]? sizes = Rs2dStressScene.ParseSizes(sizeArguments);
        Rs2dStressSpec[] specs =
        [
            // S5: N nested Scene2D groups. Measures a scene whose children are groups: a group is visited whether or not
            // it is on screen, so frame cost is expected to grow with N.
            new("S5a Scene2D group+1 sprite pan", Rs2dStressKind.GroupWithSprite),
            new("S5b Scene2D empty group pan", Rs2dStressKind.EmptyGroup),
            // S6: animated sprites. Measures the clock traversal and recording of N animated sprites, of which only the
            // camera's ~70 are visible: offscreen Scene2D animations keep advancing, so cost is expected to grow with N.
            new("S6 Scene Source ANIMATED pan", Rs2dStressKind.SceneSprites, Animated: true),
            new("S6 SceneItems2D ANIMATED pan", Rs2dStressKind.Items, Animated: true),
            // M1: N one-cell children of a non-sprite kind, against a Sprite2D control. M1a is N TileMap2D (4x4 tiles),
            // M1b is N SceneItems2D with one item; both are visited whether or not they are on screen.
            new("M1a Scene>TileMap2D(4x4) pan", Rs2dStressKind.TileMaps, DefaultSizes: ChildSizes),
            new("M1b Scene>SceneItems2D(1 item) pan", Rs2dStressKind.ItemsOne, DefaultSizes: ChildSizes),
            new("M1c Scene>Sprite2D control pan", Rs2dStressKind.SceneSprites)
        ];
        Rs2dHeadlessSuite.Run(reportPath, "cerneala-rs2d-groups-v1",
            "CPU frame cost of UI clock traversal, scheduler pass and command recording for one RenderSurface2D; no backend " +
            "submission, GPU timing or input. Report only: no performance gate on any row.",
            specs, sizes, DefaultSizes);
    }
}

internal static class Rs2dHeadlessSuite
{
    private const int Warmup = 200;
    private const int Measured = 500;
    private const int CountedFrames = 100;
    private const double SkipSetupMilliseconds = 20_000;
    private static readonly TimeSpan Delta = TimeSpan.FromMilliseconds(16);

    internal static void Run(string reportPath, string schema, string scope, Rs2dStressSpec[] specs, int[]? explicitSizes, int[] defaultSizes)
    {
        List<Rs2dHeadlessRow> rows = [];
        foreach (Rs2dStressSpec spec in specs)
        {
            double previousSetupMilliseconds = 0;
            foreach (int n in explicitSizes ?? spec.DefaultSizes ?? defaultSizes)
            {
                if (previousSetupMilliseconds > SkipSetupMilliseconds)
                {
                    Console.WriteLine($"{spec.Name} N={n}: SKIPPED, previous N setup {previousSetupMilliseconds:F0} ms");
                    continue;
                }
                Rs2dHeadlessRow row = Measure(spec, n);
                previousSetupMilliseconds = row.BuildMilliseconds + row.AttachMilliseconds;
                rows.Add(row);
                Console.WriteLine(Format(row));
            }
        }

        string path = Path.GetFullPath(reportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            Schema = schema,
            TimestampUtc = DateTimeOffset.UtcNow,
            Runtime = RuntimeInformation.FrameworkDescription,
            OperatingSystem = RuntimeInformation.OSDescription,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            Processor = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"),
            Environment.ProcessorCount,
            Stopwatch.Frequency,
            Warmup,
            Measured,
            CountedFrames,
            DeltaMilliseconds = Delta.TotalMilliseconds,
            Scope = scope,
            Fixture = "800x600 camera panning 1 px per frame over +-300 px; 16 px sprites at 96 px spacing; fixed-seed shuffle " +
                "assigns cells to children. A setup longer than 20 s skips the next size of that scenario.",
            Results = rows
        }, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
        Console.WriteLine(path);
    }

    private static string Format(Rs2dHeadlessRow row)
    {
        CultureInfo c = CultureInfo.InvariantCulture;
        string F(double value) => value.ToString("F1", c);
        return $"{row.Name,-37} N={row.N,6} total p50={F(row.TotalP50Microseconds)}us p95={F(row.TotalP95Microseconds)}us " +
            $"time={F(row.ClockTraversalP50Microseconds)}us process={F(row.ProcessFrameP50Microseconds)}us " +
            $"record p50={F(row.RecordP50Microseconds)}us recorded={row.RecordedFraction:F2} images={F(row.ImagesAverage)} " +
            $"(max {row.ImagesMax}, limit {row.ExpectedVisibleMax?.ToString(c) ?? "-"}) sprites={F(row.SpritesAverage)} " +
            $"cmds={F(row.CommandsAverage)} bytes/frame={F(row.AllocatedBytesPerFrame)} gen0={row.Gen0Collections} " +
            $"setup build={F(row.BuildMilliseconds)}ms attach={F(row.AttachMilliseconds)}ms active/scheduled={row.ActiveAnimations}/{row.ScheduledAnimations}";
    }

    private static Rs2dHeadlessRow Measure(Rs2dStressSpec spec, int n)
    {
        using Rs2dHeadlessRig rig = Rs2dHeadlessRig.Create(spec, n);
        RenderSurface2D surface = rig.Surface;
        IRenderSurface2DFrameSource source = surface;
        DrawCommandList commands = new();
        DrawRect bounds = new(0, 0, Rs2dStressScene.ViewWidth, Rs2dStressScene.ViewHeight);
        long[] total = new long[Measured], clock = new long[Measured], process = new long[Measured], record = new long[Measured];
        long lastRecorded = -1;
        int frameIndex = 0;

        // One frame as UiHost drives it: camera, UI clock traversal, scheduler pass, record when the frame changed.
        bool Frame(int slot)
        {
            long t0 = Stopwatch.GetTimestamp();
            if (spec.Camera == Rs2dStressCamera.Pan) { surface.ViewBox = rig.Scene.ViewBoxAt(spec.Camera, frameIndex); }
            frameIndex++;
            long t1 = Stopwatch.GetTimestamp();
            TimeSensitiveRenderInvalidator.Invalidate(rig.Root, Delta);
            long t2 = Stopwatch.GetTimestamp();
            if (rig.Root.Scheduler.HasWork) { rig.Root.ProcessFrame(); }
            long t3 = Stopwatch.GetTimestamp();
            long t4 = t3;
            bool recorded = source.FrameVersion != lastRecorded;
            if (recorded)
            {
                commands.Clear();
                source.RecordFrame(commands, bounds);
                lastRecorded = source.FrameVersion;
                t4 = Stopwatch.GetTimestamp();
            }
            if (slot >= 0)
            {
                clock[slot] = t2 - t1;
                process[slot] = t3 - t2;
                record[slot] = recorded ? t4 - t3 : 0;
                total[slot] = t4 - t0;
            }
            return recorded;
        }

        for (int i = 0; i < Warmup; i++) { Frame(-1); }
        int active = surface.ActiveAnimationCount, scheduled = surface.ScheduledAnimationCount;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        int gen0 = GC.CollectionCount(0);
        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < Measured; i++) { Frame(i); }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        int gen0Collections = GC.CollectionCount(0) - gen0;

        // Counting pass, separate from timing: what each recorded frame contains.
        int recordedFrames = 0, commandTotal = 0, imageTotal = 0, spriteTotal = 0, imagesMax = 0;
        int? expectedMax = null;
        bool countsVisible = spec.Kind is Rs2dStressKind.SceneSprites or Rs2dStressKind.Items
            or Rs2dStressKind.GroupWithSprite or Rs2dStressKind.ItemsOne;
        for (int i = 0; i < CountedFrames; i++)
        {
            if (!Frame(-1)) { continue; }
            recordedFrames++;
            int images = Rs2dStressScene.CountImages(commands);
            commandTotal += commands.Count;
            imageTotal += images;
            spriteTotal += Rs2dStressScene.CountSprites(commands);
            imagesMax = Math.Max(imagesMax, images);
            if (!countsVisible) { continue; }
            (int min, int max) = Rs2dStressScene.VisibleSprites(n, surface.ViewBox!.Value);
            expectedMax = Math.Max(expectedMax ?? 0, max);
            if (spec.Gated && (images > max || images < min))
            {
                throw new InvalidOperationException(
                    $"{spec.Name} N={n}: a frame drew {images} image commands; the camera sees {min}..{max} sprites.");
            }
        }
        if (spec.Gated && recordedFrames == 0 && spec.Redraw == RenderSurface2DRedrawMode.Continuous)
        {
            throw new InvalidOperationException($"{spec.Name} N={n}: a continuous surface recorded no frame.");
        }

        long[] recordedOnly = record.Where(value => value > 0).ToArray();
        double microseconds = 1e6 / Stopwatch.Frequency;
        return new Rs2dHeadlessRow
        {
            Name = spec.Name,
            N = n,
            Gated = spec.Gated,
            BuildMilliseconds = rig.Scene.BuildMilliseconds,
            AttachMilliseconds = rig.AttachMilliseconds,
            RealizedItems = rig.Realized,
            ActiveAnimations = active,
            ScheduledAnimations = scheduled,
            TotalP50Microseconds = Rs2dStressScene.Percentile(total, 0.50) * microseconds,
            TotalP95Microseconds = Rs2dStressScene.Percentile(total, 0.95) * microseconds,
            ClockTraversalP50Microseconds = Rs2dStressScene.Percentile(clock, 0.50) * microseconds,
            ProcessFrameP50Microseconds = Rs2dStressScene.Percentile(process, 0.50) * microseconds,
            RecordP50Microseconds = recordedOnly.Length == 0 ? 0 : Rs2dStressScene.Percentile(recordedOnly, 0.50) * microseconds,
            RecordP95Microseconds = recordedOnly.Length == 0 ? 0 : Rs2dStressScene.Percentile(recordedOnly, 0.95) * microseconds,
            RecordedFraction = recordedOnly.Length / (double)Measured,
            AllocatedBytesPerFrame = allocated / (double)Measured,
            Gen0Collections = gen0Collections,
            CommandsAverage = recordedFrames == 0 ? 0 : commandTotal / (double)recordedFrames,
            ImagesAverage = recordedFrames == 0 ? 0 : imageTotal / (double)recordedFrames,
            ImagesMax = imagesMax,
            SpritesAverage = recordedFrames == 0 ? 0 : spriteTotal / (double)recordedFrames,
            ExpectedVisibleMax = expectedMax
        };
    }
}

internal sealed class Rs2dHeadlessRow
{
    public string Name { get; init; } = "";
    public int N { get; init; }
    public bool Gated { get; init; }
    public double BuildMilliseconds { get; init; }
    public double AttachMilliseconds { get; init; }
    public int RealizedItems { get; init; }
    public int ActiveAnimations { get; init; }
    public int ScheduledAnimations { get; init; }
    public double TotalP50Microseconds { get; init; }
    public double TotalP95Microseconds { get; init; }
    public double ClockTraversalP50Microseconds { get; init; }
    public double ProcessFrameP50Microseconds { get; init; }
    public double RecordP50Microseconds { get; init; }
    public double RecordP95Microseconds { get; init; }
    public double RecordedFraction { get; init; }
    public double AllocatedBytesPerFrame { get; init; }
    public int Gen0Collections { get; init; }
    public double CommandsAverage { get; init; }
    public double ImagesAverage { get; init; }
    public int ImagesMax { get; init; }
    public double SpritesAverage { get; init; }
    public int? ExpectedVisibleMax { get; init; }
}

using System.Diagnostics;
using System.Globalization;
using Cerneala.Benchmarks;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Rendering;
using Xunit.Sdk;

namespace Cerneala.StressTests;

// Headless frame cost and recording gates of RenderSurface2D viewport virtualization, driven the way UiHost drives a frame
// (camera, UI clock traversal, scheduler pass, record when the frame changed). An 800x600 camera pans over N 16 px sprites
// on a 96 px grid, so about 55 sprites are visible whatever N is. The scenarios that are virtualized gate what is drawn,
// recorded and ordered per frame; the scenarios that are not virtualized yet gate only that drawing is correct.
[Collection(StressSettings.CollectionName)]
public sealed class RenderSurface2DVirtualizationStressTests(ITestOutputHelper output)
{
    private const int Warmup = 200;
    private const int Measured = 500;
    private const int VirtualizedCountedFrames = 600; // one full 300 px ping-pong of the camera
    private const int OtherCountedFrames = 100;
    private const double SkipSetupMilliseconds = 20_000;
    private const double MedianFrameCeilingMicroseconds = 2_000;
    private static readonly TimeSpan Delta = TimeSpan.FromMilliseconds(16);
    private static readonly int[] DefaultSizes = [1_000, 10_000, 100_000];

    // A TileMap2D attach takes minutes at 10,000 maps, so the TileMap2D and per-cell SceneItems2D children stop there.
    private static readonly int[] ChildSizes = [1_000, 10_000];

    [StressFact]
    public void S1_SceneSource_IsVirtualized() => Run(Virtualized("S1 Scene Source pan", Rs2dStressKind.SceneSprites));

    [StressFact]
    public void S2_SceneLayer_IsVirtualized() =>
        Run(Virtualized("S2 Scene Layer pan", Rs2dStressKind.SceneSprites, SceneOrderMode.Layer));

    [StressFact]
    public void S3_SceneLayerThenY_IsVirtualized() =>
        Run(Virtualized("S3 Scene LayerThenY pan", Rs2dStressKind.SceneSprites, SceneOrderMode.LayerThenY));

    [StressFact]
    public void S4_SceneItems_IsVirtualized() => Run(Virtualized("S4 SceneItems2D pan", Rs2dStressKind.Items));

    // A nested Scene2D group of sprites is culled by its parent like a sprite: one group per visible sprite, none when empty.
    [StressFact]
    public void S5_Groups_IsVirtualized() => Run(
        Virtualized("S5a Scene2D group+1 sprite pan", Rs2dStressKind.GroupWithSprite),
        Virtualized("S5b Scene2D empty group pan", Rs2dStressKind.EmptyGroup, expected: (n, view) => (0, 0)));

    // Known open gap: offscreen Scene2D animations keep advancing (they are not parked), so cost grows with N.
    // Only the drawing is gated, plus the parking that SceneItems2D already does.
    [StressFact]
    public void S6_AnimatedSprites_DrawOnlyWhatTheCameraSees() => Run(
        Drawing("S6 Scene Source ANIMATED pan", Rs2dStressKind.SceneSprites, DefaultSizes, VisibleSprites, animated: true),
        Drawing("S6 SceneItems2D ANIMATED pan", Rs2dStressKind.Items, DefaultSizes, VisibleSprites, animated: true,
            gateParking: true));

    // Known open gap: a TileMap2D child is visited whether or not it is on screen. A 4x4 map of 16 px tiles is a 64 px cell.
    [StressFact]
    public void M1a_TileMapChildren_DrawOnlyWhatTheCameraSees() => Run(
        Drawing("M1a Scene>TileMap2D(4x4) pan", Rs2dStressKind.TileMaps, ChildSizes,
            (n, view) => (0, 16 * Rs2dStressScene.VisibleCells(n, view, 64).Max), countBatchedSprites: true));

    // Known open gap: a SceneItems2D child is visited whether or not it is on screen.
    [StressFact]
    public void M1b_OneItemSceneItemsChildren_DrawOnlyWhatTheCameraSees() => Run(
        Drawing("M1b Scene>SceneItems2D(1 item) pan", Rs2dStressKind.ItemsOne, ChildSizes, VisibleSprites));

    private static (int Min, int Max) VisibleSprites(int n, DrawRect view) => Rs2dStressScene.VisibleSprites(n, view);

    private static Scenario Virtualized(string name, Rs2dStressKind kind, SceneOrderMode order = SceneOrderMode.Source,
        Func<int, DrawRect, (int Min, int Max)>? expected = null) =>
        new(new Rs2dStressSpec(name, kind, order), DefaultSizes, true, expected ?? VisibleSprites);

    private static Scenario Drawing(string name, Rs2dStressKind kind, int[] defaultSizes,
        Func<int, DrawRect, (int Min, int Max)> expected, bool animated = false, bool countBatchedSprites = false,
        bool gateParking = false) =>
        new(new Rs2dStressSpec(name, kind, Animated: animated), defaultSizes, false, expected, countBatchedSprites, gateParking);

    private sealed record Scenario(
        Rs2dStressSpec Spec,
        int[] DefaultSizes,
        bool Virtualized,
        Func<int, DrawRect, (int Min, int Max)> Expected,
        bool CountBatchedSprites = false,
        bool GateParking = false);

    private void Run(params Scenario[] scenarios)
    {
        List<string> violations = [];
        foreach (Scenario scenario in scenarios)
        {
            double previousSetupMilliseconds = 0;
            foreach (int n in StressSettings.Sizes(scenario.DefaultSizes))
            {
                if (previousSetupMilliseconds > SkipSetupMilliseconds)
                {
                    output.WriteLine($"{scenario.Spec.Name} N={n}: SKIPPED, previous N setup {previousSetupMilliseconds:F0} ms > {SkipSetupMilliseconds:F0} ms");
                    continue;
                }
                (string line, double setupMilliseconds, string? violation) = Measure(scenario, n);
                previousSetupMilliseconds = setupMilliseconds;
                output.WriteLine(line);
                if (violation is not null) { violations.Add(violation); }
            }
        }
        if (violations.Count > 0) { throw new XunitException(string.Join(Environment.NewLine, violations)); }
    }

    private static (string Line, double SetupMilliseconds, string? Violation) Measure(Scenario scenario, int n)
    {
        Rs2dStressSpec spec = scenario.Spec;
        using Rs2dHeadlessRig rig = Rs2dHeadlessRig.Create(spec, n);
        RenderSurface2D surface = rig.Surface;
        IRenderSurface2DFrameSource source = surface;
        // The container that records the N cells: the one SceneItems2D for S4, otherwise the root scene.
        SceneRecordCounters2D counters = rig.Scene.Items?.RecordCounters ?? rig.Scene.Scene.RecordCounters;
        DrawCommandList commands = new();
        DrawRect bounds = new(0, 0, Rs2dStressScene.ViewWidth, Rs2dStressScene.ViewHeight);
        long[] total = new long[Measured];
        long lastRecorded = -1;
        int frameIndex = 0;

        bool Frame(int slot)
        {
            long t0 = Stopwatch.GetTimestamp();
            surface.ViewBox = rig.Scene.ViewBoxAt(spec.Camera, frameIndex);
            frameIndex++;
            TimeSensitiveRenderInvalidator.Invalidate(rig.Root, Delta);
            if (rig.Root.Scheduler.HasWork) { rig.Root.ProcessFrame(); }
            bool recorded = source.FrameVersion != lastRecorded;
            if (recorded)
            {
                commands.Clear();
                source.RecordFrame(commands, bounds);
                lastRecorded = source.FrameVersion;
            }
            if (slot >= 0) { total[slot] = Stopwatch.GetTimestamp() - t0; }
            return recorded;
        }

        for (int i = 0; i < Warmup; i++) { Frame(-1); }
        int active = surface.ActiveAnimationCount, scheduled = surface.ScheduledAnimationCount;
        int parkedAfterWarmup = surface.ParkedAnimationCount;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        int gen0 = GC.CollectionCount(0);
        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < Measured; i++) { Frame(i); }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        int gen0Collections = GC.CollectionCount(0) - gen0;

        // Counting pass, outside the timed loops: what each recorded frame contains.
        int counted = scenario.Virtualized ? VirtualizedCountedFrames : OtherCountedFrames;
        int recordedFrames = 0, drawnMax = 0, limitMax = 0;
        long drawnTotal = 0, recordedTotal = 0, recordedMax = 0, orderedTotal = 0, orderedMax = 0;
        string? violation = null;
        for (int i = 0; i < counted; i++)
        {
            long recordedBefore = counters.RecordedChildren, orderedBefore = counters.OrderedChildren;
            if (!Frame(-1)) { continue; }
            recordedFrames++;
            (int min, int max) = scenario.Expected(n, surface.ViewBox!.Value);
            int drawn = scenario.CountBatchedSprites
                ? Rs2dStressScene.CountSprites(commands)
                : Rs2dStressScene.CountImages(commands);
            long recordedDelta = counters.RecordedChildren - recordedBefore, orderedDelta = counters.OrderedChildren - orderedBefore;
            drawnTotal += drawn;
            drawnMax = Math.Max(drawnMax, drawn);
            limitMax = Math.Max(limitMax, max);
            recordedTotal += recordedDelta;
            recordedMax = Math.Max(recordedMax, recordedDelta);
            orderedTotal += orderedDelta;
            orderedMax = Math.Max(orderedMax, orderedDelta);

            string where = $"{spec.Name} N={n} counted frame {i}: ";
            if (drawn < min || drawn > max)
            {
                violation ??= where + $"drew {drawn} {(scenario.CountBatchedSprites ? "sprites" : "image commands")}; the camera sees {min}..{max}.";
            }
            if (!scenario.Virtualized) { continue; }
            if (recordedDelta > max)
            {
                violation ??= where + $"RecordedChildren delta {recordedDelta} exceeds the {max} sprites the camera sees.";
            }
            long orderedLimit = spec.Order == SceneOrderMode.Source ? 0 : max;
            if (orderedDelta > orderedLimit)
            {
                violation ??= where + $"OrderedChildren delta {orderedDelta} exceeds {orderedLimit} " +
                    $"({spec.Order}; the camera sees {min}..{max} sprites).";
            }
        }
        if (recordedFrames == 0)
        {
            violation ??= $"{spec.Name} N={n}: a continuous panning surface recorded no frame in {counted} frames.";
        }
        else if (drawnTotal == 0 && limitMax > 0)
        {
            violation ??= $"{spec.Name} N={n}: {recordedFrames} frames recorded and none drew anything (fixture sanity).";
        }

        double microseconds = 1e6 / Stopwatch.Frequency;
        double medianMicroseconds = Rs2dStressScene.Percentile(total, 0.50) * microseconds;
        double p95Microseconds = Rs2dStressScene.Percentile(total, 0.95) * microseconds;
        if (scenario.Virtualized && medianMicroseconds >= MedianFrameCeilingMicroseconds)
        {
            violation ??= $"{spec.Name} N={n}: median frame time {medianMicroseconds:F1} us is not below {MedianFrameCeilingMicroseconds:F0} us.";
        }

        int parked = surface.ParkedAnimationCount;
        if (scenario.GateParking && Rs2dStressScene.VisibleSprites(n, surface.ViewBox!.Value).Max < n && (parkedAfterWarmup <= 0 || parked <= 0))
        {
            violation ??= $"{spec.Name} N={n}: offscreen animations are not parked (parked {parkedAfterWarmup} after warmup, {parked} after the counting pass).";
        }

        CultureInfo c = CultureInfo.InvariantCulture;
        string F(double value) => value.ToString("F1", c);
        double frames = Math.Max(1, recordedFrames);
        string extra = spec.Kind == Rs2dStressKind.TileMaps ? $" drawnTiles={DrawnTiles(rig.Scene.Scene)}" : "";
        string line = $"{spec.Name,-37} N={n,6} {(scenario.Virtualized ? "gated" : "report")} total p50={F(medianMicroseconds)}us p95={F(p95Microseconds)}us " +
            $"recordedChildren/frame={F(recordedTotal / frames)} (max {recordedMax}) orderedChildren/frame={F(orderedTotal / frames)} (max {orderedMax}) " +
            $"drawn/frame={F(drawnTotal / frames)} (max {drawnMax}, limit {limitMax}) frames={recordedFrames} bytes/frame={F(allocated / (double)Measured)} gen0={gen0Collections} " +
            $"active/scheduled/parked={active}/{scheduled}/{parked}{extra} setup build={F(rig.Scene.BuildMilliseconds)}ms attach={F(rig.AttachMilliseconds)}ms";
        return (line, rig.Scene.BuildMilliseconds + rig.AttachMilliseconds, violation);
    }

    private static int DrawnTiles(Scene2D scene)
    {
        int drawn = 0;
        foreach (SceneNode2D child in scene.Children)
        {
            if (child is TileMap2D map) { drawn += map.GetDiagnosticsSnapshot().DrawnTiles; }
        }
        return drawn;
    }
}

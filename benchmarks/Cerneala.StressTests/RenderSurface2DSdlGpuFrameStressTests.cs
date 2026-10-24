using System.Diagnostics;
using System.Globalization;
using Cerneala.Backends.SdlGpu;
using Cerneala.Benchmarks;
using Cerneala.Drawing;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Resources;
using SkiaSharp;
using Xunit.Sdk;

namespace Cerneala.StressTests;

// Real SDL_GPU window: the full UiHost frame (retained update, recording, backend submission, present) for the virtualized
// scenarios. A window opens per scenario, size and present mode. Gates: per rendered frame the backend submits at most
// 6 vertices and 1 draw call per sprite the camera sees, and it submits some geometry; in Immediate mode the p95 frame
// interval stays below one 60 Hz frame.
[Collection(StressSettings.CollectionName)]
public sealed class RenderSurface2DSdlGpuFrameStressTests(ITestOutputHelper output)
{
    private const int Warmup = 200;
    private const int Measured = 600;
    private const double BudgetMilliseconds = 1000.0 / 60.0;
    private const double SkipSetupMilliseconds = 20_000;
    private const int MaximumVerticesPerSprite = 6;
    private static readonly int[] DefaultSizes = [1_000, 10_000, 100_000];

    [StressSdlFact, Trait("Category", "Native")]
    public void S1_SceneSource_SubmitsOnlyVisibleSprites() =>
        Run(new Rs2dStressSpec("S1 Scene Source", Rs2dStressKind.SceneSprites));

    [StressSdlFact, Trait("Category", "Native")]
    public void S2_SceneLayer_SubmitsOnlyVisibleSprites() =>
        Run(new Rs2dStressSpec("S2 Scene Layer", Rs2dStressKind.SceneSprites, SceneOrderMode.Layer));

    [StressSdlFact, Trait("Category", "Native")]
    public void S3_SceneLayerThenY_SubmitsOnlyVisibleSprites() =>
        Run(new Rs2dStressSpec("S3 Scene LayerThenY", Rs2dStressKind.SceneSprites, SceneOrderMode.LayerThenY));

    [StressSdlFact, Trait("Category", "Native")]
    public void S4_SceneItems_SubmitsOnlyVisibleSprites() =>
        Run(new Rs2dStressSpec("S4 SceneItems2D", Rs2dStressKind.Items));

    private void Run(Rs2dStressSpec spec)
    {
        string directory = Path.Combine(Path.GetTempPath(), $"Cerneala-rs2d-sdl-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string atlasPath = Path.Combine(directory, "sprite.png");
        List<string> violations = [];
        try
        {
            using (SKBitmap bitmap = new(16, 16))
            {
                bitmap.Erase(new SKColor(255, 160, 0));
                using SKData encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                File.WriteAllBytes(atlasPath, encoded.ToArray());
            }

            double previousSetupMilliseconds = 0;
            foreach (int n in StressSettings.Sizes(DefaultSizes))
            {
                if (previousSetupMilliseconds > SkipSetupMilliseconds)
                {
                    output.WriteLine($"{spec.Name} N={n}: SKIPPED, previous N setup {previousSetupMilliseconds:F0} ms > {SkipSetupMilliseconds:F0} ms");
                    continue;
                }
                double setupAtN = 0;
                foreach (SdlGpuPresentMode requested in new[] { SdlGpuPresentMode.VSync, SdlGpuPresentMode.Immediate })
                {
                    (string line, double setupMilliseconds, string? violation) = Measure(spec, n, requested, atlasPath);
                    setupAtN = Math.Max(setupAtN, setupMilliseconds);
                    output.WriteLine(line);
                    if (violation is not null) { violations.Add(violation); }
                }
                previousSetupMilliseconds = setupAtN;
            }
        }
        finally
        {
            try { File.Delete(atlasPath); Directory.Delete(directory); } catch { }
        }
        if (violations.Count > 0) { throw new XunitException(string.Join(Environment.NewLine, violations)); }
    }

    private static (string Line, double SetupMilliseconds, string? Violation) Measure(
        Rs2dStressSpec spec, int n, SdlGpuPresentMode requested, string atlasPath)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        NativeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory graphics = new(api, new SdlGpuPresentationOptions(
            SdlGpuSwapchainComposition.Sdr, requested, SdlGpuSampleCount.One));
        CapturingFactory capture = new(graphics);
        using SdlWindowPlatform platform = new(api, capture, coordinateScaleOverride: 1);
        using WindowApplicationRuntime runtime = new(platform);

        ResourceId<ImageResource> atlas = new("Sprite");
        Rs2dStressScene stress = Rs2dStressScene.Create(spec, n, new ImageReference(atlas));
        SceneRecordCounters2D counters = stress.Items?.RecordCounters ?? stress.Scene.RecordCounters;
        RenderSurface2D surface = new()
        {
            Width = Rs2dStressScene.ViewWidth,
            Height = Rs2dStressScene.ViewHeight,
            ViewBox = new DrawRect(stress.CameraX, stress.CameraY, Rs2dStressScene.ViewWidth, Rs2dStressScene.ViewHeight),
            Scene = stress.Scene,
            ClearColor = Color.Black,
            RedrawMode = RenderSurface2DRedrawMode.Continuous
        };
        Window window = new()
        {
            Title = $"RS2D stress {spec.Name} N={n}",
            Width = (int)Rs2dStressScene.ViewWidth,
            Height = (int)Rs2dStressScene.ViewHeight,
            Content = surface
        };
        window.Resources.SetResource(atlas, new ImageResource(atlasPath));
        Stopwatch clock = Stopwatch.StartNew();
        TimeSpan lastPump = clock.Elapsed;
        int frameIndex = 0;

        bool Pump(bool pan)
        {
            if (pan) { surface.ViewBox = stress.ViewBoxAt(Rs2dStressCamera.Pan, frameIndex); }
            frameIndex++;
            TimeSpan now = clock.Elapsed;
            bool rendered = runtime.PumpOnce(now - lastPump);
            lastPump = now;
            return rendered;
        }

        try
        {
            Stopwatch setup = Stopwatch.StartNew();
            runtime.Show(window, modal: false);
            bool ready = SpinWait.SpinUntil(() =>
            {
                Pump(pan: false);
                return surface.PresentationState == RenderSurface2DPresentationState.Ready &&
                    window.LastFrame is not null && (stress.Items is null || stress.Items.RealizedItemCount >= n);
            }, TimeSpan.FromSeconds(300));
            double setupMilliseconds = stress.BuildMilliseconds + setup.Elapsed.TotalMilliseconds;
            if (!ready)
            {
                throw new InvalidOperationException(
                    $"{spec.Name} N={n}: scene never became ready (state {surface.PresentationState}, realized {stress.Items?.RealizedItemCount}).");
            }
            if (surface.ArrangedBounds.Width != Rs2dStressScene.ViewWidth || surface.ArrangedBounds.Height != Rs2dStressScene.ViewHeight)
            {
                throw new InvalidOperationException($"{spec.Name} N={n}: the surface was not arranged at the camera size.");
            }

            SdlGpuWindowGraphicsSession session = capture.Session!;
            SdlGpuPresentMode actual = session.Diagnostics.PresentMode;
            string fallbacks = string.Join("; ", session.Diagnostics.Fallbacks);
            SdlGpuDrawingBackend backend = (SdlGpuDrawingBackend)session.DrawingBackend;

            for (int i = 0; i < Warmup; i++) { Pump(pan: true); }

            double[] interval = new double[Measured], cpuNoPresent = new double[Measured],
                vertices = new double[Measured], drawCalls = new double[Measured];
            long[] recordedChildren = new long[Measured], orderedChildren = new long[Measured];
            bool[] rendered = new bool[Measured];
            DrawRect[] views = new DrawRect[Measured];
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            int g0 = GC.CollectionCount(0), g1 = GC.CollectionCount(1), g2 = GC.CollectionCount(2);
            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            long previousStamp = Stopwatch.GetTimestamp();
            int notRendered = 0;
            for (int i = 0; i < Measured; i++)
            {
                long stamp = Stopwatch.GetTimestamp();
                long recordedBefore = counters.RecordedChildren, orderedBefore = counters.OrderedChildren;
                rendered[i] = Pump(pan: true);
                if (!rendered[i]) { notRendered++; }
                interval[i] = Milliseconds(stamp - previousStamp);
                previousStamp = stamp;
                views[i] = surface.ViewBox!.Value;
                recordedChildren[i] = counters.RecordedChildren - recordedBefore;
                orderedChildren[i] = counters.OrderedChildren - orderedBefore;
                UiFrameTiming timing = window.LastFrame!.DiagnosticsTiming;
                // Frame cost without the swapchain acquire / present wait that lives in CompleteFrame.
                cpuNoPresent[i] = window.LastFrame.ProcessingTime.TotalMilliseconds - timing.CompleteFrame.TotalMilliseconds;
                SdlGpuDrawingFrameCounters frameCounters = backend.LastFrameCounters;
                drawCalls[i] = frameCounters.DrawCallCount;
                vertices[i] = frameCounters.VertexCount;
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            int gen0 = GC.CollectionCount(0) - g0, gen1 = GC.CollectionCount(1) - g1, gen2 = GC.CollectionCount(2) - g2;

            // Gates, outside the timed loop: only frames that rendered carry fresh counters.
            string? violation = null;
            int vertexLimit = 0, renderedFrames = 0;
            long recordedTotal = 0, orderedTotal = 0, recordedMax = 0, orderedMax = 0;
            for (int i = 0; i < Measured; i++)
            {
                recordedTotal += recordedChildren[i];
                orderedTotal += orderedChildren[i];
                recordedMax = Math.Max(recordedMax, recordedChildren[i]);
                orderedMax = Math.Max(orderedMax, orderedChildren[i]);
                if (!rendered[i]) { continue; }
                renderedFrames++;
                int visibleMax = Rs2dStressScene.VisibleSprites(n, views[i]).Max;
                int limit = visibleMax * MaximumVerticesPerSprite;
                vertexLimit = Math.Max(vertexLimit, limit);
                if (vertices[i] > limit || drawCalls[i] > visibleMax)
                {
                    violation ??= $"{spec.Name} N={n} {requested} frame {i}: submitted {vertices[i]:F0} vertices (limit {limit}) " +
                        $"and {drawCalls[i]:F0} draw calls (limit {visibleMax}) for at most {visibleMax} visible sprites.";
                }
            }
            if (renderedFrames == 0 || vertices.Max() == 0)
            {
                violation ??= $"{spec.Name} N={n} {requested}: the backend submitted no geometry ({renderedFrames} rendered frames, max {vertices.Max():F0} vertices).";
            }
            double intervalP95 = Rs2dStressScene.Percentile(interval, 0.95);
            if (requested == SdlGpuPresentMode.Immediate && actual == SdlGpuPresentMode.Immediate && intervalP95 >= BudgetMilliseconds)
            {
                violation ??= $"{spec.Name} N={n} {requested}: p95 frame interval {intervalP95:F2} ms is not below {BudgetMilliseconds:F2} ms.";
            }

            SdlGpuPresentMode actualEnd = session.Diagnostics.PresentMode;
            CultureInfo c = CultureInfo.InvariantCulture;
            string F(double value) => value.ToString("F2", c);
            double frames = Math.Max(1, renderedFrames);
            string line = $"{spec.Name,-22} N={n,6} {requested}->{actual}{(actualEnd != actual ? "->" + actualEnd : "")}" +
                $"{(fallbacks.Length > 0 ? " [fallbacks: " + fallbacks + "]" : "")}: " +
                $"interval p50={F(Rs2dStressScene.Percentile(interval, 0.50))}ms p95={F(intervalP95)}ms max={F(interval.Max())}ms " +
                $"over16.67ms={interval.Count(value => value > BudgetMilliseconds)}/{Measured} notRendered={notRendered} | " +
                $"cpuNoPresent p50={F(Rs2dStressScene.Percentile(cpuNoPresent, 0.50))}ms p95={F(Rs2dStressScene.Percentile(cpuNoPresent, 0.95))}ms | " +
                $"recordedChildren/frame={recordedTotal / frames:F1} (max {recordedMax}) orderedChildren/frame={orderedTotal / frames:F1} (max {orderedMax}) | " +
                $"drawCalls max={drawCalls.Max():F0} vertices p50={Rs2dStressScene.Percentile(vertices, 0.50):F0} max={vertices.Max():F0} (limit {vertexLimit}) | " +
                $"bytes/frame={allocated / (double)Measured:F0} gc0/1/2={gen0}/{gen1}/{gen2} setup={setupMilliseconds / 1000:F2}s";
            return (line, setupMilliseconds, violation);
        }
        finally
        {
            try { runtime.Close(window, force: true); } catch { }
        }
    }

    private static double Milliseconds(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;

    private sealed class CapturingFactory(IWindowGraphicsSessionFactory inner) : IWindowGraphicsSessionFactory
    {
        public SdlGpuWindowGraphicsSession? Session;

        public IWindowGraphicsSession Create(IWindowSurface windowSurface, int pixelWidth, int pixelHeight, float coordinateScale)
        {
            IWindowGraphicsSession session = inner.Create(windowSurface, pixelWidth, pixelHeight, coordinateScale);
            Session = session as SdlGpuWindowGraphicsSession;
            return session;
        }
    }
}

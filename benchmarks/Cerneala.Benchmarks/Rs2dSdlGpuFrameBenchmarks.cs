using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Resources;
using SkiaSharp;

namespace Cerneala.Benchmarks;

// Real SDL_GPU window: the full UiHost frame (retained update, recording, backend submission, present) for the
// ordering scenarios of --rs2d-ordering. A window opens per scenario, size and present mode. The gate is deterministic:
// the vertices and draw calls the backend submits every frame stay bounded by the sprites that intersect the camera.
internal static class Rs2dSdlGpuFrameBenchmarkRunner
{
    private const int Warmup = 200;
    private const int Measured = 600;
    private const double BudgetMilliseconds = 1000.0 / 60.0;
    private const double SkipSetupMilliseconds = 20_000;
    private const int MaximumVerticesPerSprite = 6;
    private static readonly int[] DefaultSizes = [1_000, 10_000, 100_000];

    internal static void Run(string reportPath, string[] sizeArguments)
    {
        int[] sizes = Rs2dStressScene.ParseSizes(sizeArguments) ?? DefaultSizes;
        Rs2dStressSpec[] specs =
        [
            new("S1 Scene Source", Rs2dStressKind.SceneSprites),
            new("S2 Scene Layer", Rs2dStressKind.SceneSprites, SceneOrderMode.Layer),
            new("S3 Scene LayerThenY", Rs2dStressKind.SceneSprites, SceneOrderMode.LayerThenY),
            new("S4 SceneItems2D", Rs2dStressKind.Items)
        ];
        string directory = Path.Combine(Path.GetTempPath(), $"Cerneala-rs2d-sdl-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string atlasPath = Path.Combine(directory, "sprite.png");
        try
        {
            using (SKBitmap bitmap = new(16, 16))
            {
                bitmap.Erase(new SKColor(255, 160, 0));
                using SKData encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                File.WriteAllBytes(atlasPath, encoded.ToArray());
            }

            List<Rs2dSdlGpuRow> rows = [];
            foreach (Rs2dStressSpec spec in specs)
            {
                double previousSetupMilliseconds = 0;
                foreach (int n in sizes)
                {
                    if (previousSetupMilliseconds > SkipSetupMilliseconds)
                    {
                        Console.WriteLine($"{spec.Name} N={n}: SKIPPED, previous N setup {previousSetupMilliseconds:F0} ms");
                        continue;
                    }
                    double setupAtN = 0;
                    foreach (SdlGpuPresentMode requested in new[] { SdlGpuPresentMode.VSync, SdlGpuPresentMode.Immediate })
                    {
                        Rs2dSdlGpuRow row = Measure(spec, n, requested, atlasPath);
                        setupAtN = Math.Max(setupAtN, row.SetupMilliseconds);
                        rows.Add(row);
                        Console.WriteLine(Format(row));
                    }
                    previousSetupMilliseconds = setupAtN;
                }
            }

            string path = Path.GetFullPath(reportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(new
            {
                Schema = "cerneala-rs2d-sdlgpu-v1",
                TimestampUtc = DateTimeOffset.UtcNow,
                Runtime = RuntimeInformation.FrameworkDescription,
                OperatingSystem = RuntimeInformation.OSDescription,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                Processor = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"),
                Environment.ProcessorCount,
                Stopwatch.Frequency,
                Warmup,
                Measured,
                BudgetMilliseconds,
                Scope = "Real 800x600 SDL_GPU window at scale 1, no MSAA, driven by WindowApplicationRuntime.PumpOnce. Frame " +
                    "intervals include the swapchain wait (VSync) or not (Immediate); CpuNoPresent excludes CompleteFrame. " +
                    "No isolated GPU time. Gate: vertices and draw calls per frame bounded by the sprites the camera sees.",
                Fixture = "800x600 camera panning 1 px per frame over +-300 px; 16 px sprites at 96 px spacing; fixed-seed " +
                    "shuffle assigns cells to children. A setup longer than 20 s skips the next size of that scenario.",
                Results = rows
            }, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
            Console.WriteLine(path);
        }
        finally
        {
            try { File.Delete(atlasPath); Directory.Delete(directory); } catch { }
        }
    }

    private static string Format(Rs2dSdlGpuRow row)
    {
        CultureInfo c = CultureInfo.InvariantCulture;
        string F(double value) => value.ToString("F2", c);
        return $"{row.Name,-22} N={row.N,6} {row.RequestedPresentMode}->{row.ActualPresentMode}: " +
            $"interval p50={F(row.IntervalP50Milliseconds)}ms p95={F(row.IntervalP95Milliseconds)}ms max={F(row.IntervalMaxMilliseconds)}ms " +
            $"over16.67ms={row.FramesOverBudget}/{Measured} notRendered={row.NotRendered} | " +
            $"cpuNoPresent p50={F(row.CpuNoPresentP50Milliseconds)}ms p95={F(row.CpuNoPresentP95Milliseconds)}ms " +
            $"retainedUpdate={F(row.RetainedUpdateP50Milliseconds)}ms drawing={F(row.DrawingP50Milliseconds)}ms " +
            $"backendPrep={F(row.BackendPreparationP50Milliseconds)}ms cmdRender={F(row.BackendCommandRenderingP50Milliseconds)}ms " +
            $"completeFrame={F(row.CompleteFrameP50Milliseconds)}ms | drawCalls max={row.DrawCallsMax} " +
            $"vertices p50={row.VerticesP50:F0} max={row.VerticesMax} (limit {row.VertexLimit}) | " +
            $"bytes/frame={row.AllocatedBytesPerFrame:F0} gc0/1/2={row.Gen0}/{row.Gen1}/{row.Gen2} setup={row.SetupMilliseconds / 1000:F2}s";
    }

    private static Rs2dSdlGpuRow Measure(Rs2dStressSpec spec, int n, SdlGpuPresentMode requested, string atlasPath)
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
            Title = $"RS2D benchmark {spec.Name} N={n}",
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

            double[] interval = new double[Measured], cpuNoPresent = new double[Measured], retained = new double[Measured],
                drawing = new double[Measured], preparation = new double[Measured], commandRendering = new double[Measured],
                complete = new double[Measured], vertices = new double[Measured], drawCalls = new double[Measured];
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
                rendered[i] = Pump(pan: true);
                if (!rendered[i]) { notRendered++; }
                interval[i] = Milliseconds(stamp - previousStamp);
                previousStamp = stamp;
                views[i] = surface.ViewBox!.Value;
                UiFrameTiming timing = window.LastFrame!.DiagnosticsTiming;
                retained[i] = timing.RetainedUpdate.TotalMilliseconds;
                drawing[i] = timing.Drawing.TotalMilliseconds;
                preparation[i] = timing.DrawingBackend.Preparation.TotalMilliseconds;
                commandRendering[i] = timing.DrawingBackend.CommandRendering.TotalMilliseconds;
                complete[i] = timing.CompleteFrame.TotalMilliseconds;
                // Frame cost without the swapchain acquire / present wait that lives in CompleteFrame.
                cpuNoPresent[i] = window.LastFrame.ProcessingTime.TotalMilliseconds - complete[i];
                SdlGpuDrawingFrameCounters counters = backend.LastFrameCounters;
                drawCalls[i] = counters.DrawCallCount;
                vertices[i] = counters.VertexCount;
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            int gen0 = GC.CollectionCount(0) - g0, gen1 = GC.CollectionCount(1) - g1, gen2 = GC.CollectionCount(2) - g2;

            // Gate, outside the timed loop: only frames that rendered carry fresh counters.
            int vertexLimit = 0, renderedFrames = 0;
            for (int i = 0; i < Measured; i++)
            {
                if (!rendered[i]) { continue; }
                renderedFrames++;
                int limit = Rs2dStressScene.VisibleSprites(n, views[i]).Max * MaximumVerticesPerSprite;
                vertexLimit = Math.Max(vertexLimit, limit);
                if (vertices[i] > limit || drawCalls[i] > Rs2dStressScene.VisibleSprites(n, views[i]).Max)
                {
                    throw new InvalidOperationException(
                        $"{spec.Name} N={n} {requested}: a frame submitted {vertices[i]:F0} vertices and {drawCalls[i]:F0} draw calls " +
                        $"for at most {limit / MaximumVerticesPerSprite} visible sprites.");
                }
            }
            if (renderedFrames == 0 || vertices.Max() == 0)
            {
                throw new InvalidOperationException($"{spec.Name} N={n} {requested}: the backend submitted no geometry.");
            }

            SdlGpuPresentMode actualEnd = session.Diagnostics.PresentMode;
            return new Rs2dSdlGpuRow
            {
                Name = spec.Name,
                N = n,
                RequestedPresentMode = requested.ToString(),
                ActualPresentMode = actual + (actualEnd != actual ? "->" + actualEnd : "") +
                    (fallbacks.Length > 0 ? " [fallbacks: " + fallbacks + "]" : ""),
                SetupMilliseconds = setupMilliseconds,
                IntervalP50Milliseconds = Rs2dStressScene.Percentile(interval, 0.50),
                IntervalP95Milliseconds = Rs2dStressScene.Percentile(interval, 0.95),
                IntervalP99Milliseconds = Rs2dStressScene.Percentile(interval, 0.99),
                IntervalMaxMilliseconds = interval.Max(),
                FramesOverBudget = interval.Count(value => value > BudgetMilliseconds),
                NotRendered = notRendered,
                CpuNoPresentP50Milliseconds = Rs2dStressScene.Percentile(cpuNoPresent, 0.50),
                CpuNoPresentP95Milliseconds = Rs2dStressScene.Percentile(cpuNoPresent, 0.95),
                RetainedUpdateP50Milliseconds = Rs2dStressScene.Percentile(retained, 0.50),
                DrawingP50Milliseconds = Rs2dStressScene.Percentile(drawing, 0.50),
                BackendPreparationP50Milliseconds = Rs2dStressScene.Percentile(preparation, 0.50),
                BackendCommandRenderingP50Milliseconds = Rs2dStressScene.Percentile(commandRendering, 0.50),
                CompleteFrameP50Milliseconds = Rs2dStressScene.Percentile(complete, 0.50),
                DrawCallsMax = (int)drawCalls.Max(),
                VerticesP50 = Rs2dStressScene.Percentile(vertices, 0.50),
                VerticesMax = (int)vertices.Max(),
                VertexLimit = vertexLimit,
                AllocatedBytesPerFrame = allocated / (double)Measured,
                Gen0 = gen0,
                Gen1 = gen1,
                Gen2 = gen2
            };
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

internal sealed class Rs2dSdlGpuRow
{
    public string Name { get; init; } = "";
    public int N { get; init; }
    public string RequestedPresentMode { get; init; } = "";
    public string ActualPresentMode { get; init; } = "";
    public double SetupMilliseconds { get; init; }
    public double IntervalP50Milliseconds { get; init; }
    public double IntervalP95Milliseconds { get; init; }
    public double IntervalP99Milliseconds { get; init; }
    public double IntervalMaxMilliseconds { get; init; }
    public int FramesOverBudget { get; init; }
    public int NotRendered { get; init; }
    public double CpuNoPresentP50Milliseconds { get; init; }
    public double CpuNoPresentP95Milliseconds { get; init; }
    public double RetainedUpdateP50Milliseconds { get; init; }
    public double DrawingP50Milliseconds { get; init; }
    public double BackendPreparationP50Milliseconds { get; init; }
    public double BackendCommandRenderingP50Milliseconds { get; init; }
    public double CompleteFrameP50Milliseconds { get; init; }
    public int DrawCallsMax { get; init; }
    public double VerticesP50 { get; init; }
    public int VerticesMax { get; init; }
    public int VertexLimit { get; init; }
    public double AllocatedBytesPerFrame { get; init; }
    public int Gen0 { get; init; }
    public int Gen1 { get; init; }
    public int Gen2 { get; init; }
}

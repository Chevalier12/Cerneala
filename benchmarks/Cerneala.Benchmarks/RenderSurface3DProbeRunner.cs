using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Platforms.Sdl3;
using Cerneala.SmokeTests;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Invalidation;

namespace Cerneala.Benchmarks;

internal static class RenderSurface3DProbeRunner
{
    private const int Width = 800, Height = 600;

    internal static void TestArgumentBranch()
    {
        ProbeOptions valid = Parse(["--rendersurface3d-probe", "probe.json", "20", "1", "static", "120", "600"]);
        if (valid.JointCount != 20 || valid.Warmup != 120 || valid.Measured != 600 || valid.Moving)
            throw new InvalidOperationException("The static 20-joint probe arguments were not parsed correctly.");
        ProbeOptions moving = Parse(["--rendersurface3d-probe", "probe.json", "200", "2", "moving", "120", "600"]);
        if (moving.JointCount != 200 || moving.Dpi != 2 || !moving.Moving)
            throw new InvalidOperationException("The moving 200-joint probe arguments were not parsed correctly.");
        foreach (string[] invalid in new[]
        {
            new[] { "--rendersurface3d-probe", "probe.json", "21", "1", "static", "120", "600" },
            new[] { "--rendersurface3d-probe", "probe.json", "20", "3", "static", "120", "600" },
            new[] { "--rendersurface3d-probe", "probe.json", "20", "1", "animated", "120", "600" },
            new[] { "--rendersurface3d-probe", "probe.json", "20", "1", "static", "120" },
            new[] { "--rendersurface3d-probe", "probe.json", "20", "1", "static", "-1", "600" },
            new[] { "--rendersurface3d-probe", "probe.json", "20", "1", "static", "120", "0" },
            new[] { "--rendersurface3d-probe", " ", "20", "1", "static", "120", "600" }
        })
        {
            try { _ = Parse(invalid); throw new InvalidOperationException("Invalid arguments were accepted."); }
            catch (ArgumentException) { }
        }
        Console.WriteLine("RENDERSURFACE3D_PROBE_ARGS_OK valid=2 invalid=7");
    }

    internal static void Run(string[] args)
    {
        ProbeOptions options = Parse(args);
        using ProbeWindow window = new(options.Dpi);
        RenderSurface3DConformanceFixture fixture = RenderSurface3DConformanceFixture.Create(options.JointCount);
        UIRoot root = new(Width, Height, options.Dpi);
        root.SetImageLoader(window.Session.ImageLoader ?? throw new InvalidOperationException("SDL image loader unavailable."));
        root.VisualChildren.Add(fixture.Surface);
        try
        {
            for (int index = 0; index < options.Warmup; index++) Render(index);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            double[] cpu = new double[options.Measured];
            double[] presentation = new double[options.Measured];
            double[] total = new double[options.Measured];
            long allocations = 0;
            long recordings = 0, passes = 0, uploads = 0, uploadBytes = 0, draws = 0, creates = 0, retires = 0;
            long measureCalls = 0, arrangeCalls = 0;
            for (int index = 0; index < options.Measured; index++)
            {
                FrameSample sample = Render(index + options.Warmup);
                cpu[index] = sample.CpuMilliseconds;
                presentation[index] = sample.PresentationMilliseconds;
                total[index] = sample.TotalMilliseconds;
                allocations += sample.AllocatedBytes;
                measureCalls += sample.MeasureCalls;
                arrangeCalls += sample.ArrangeCalls;
                recordings += sample.Counters.RecordingCount;
                passes += sample.Counters.PassCount;
                uploads += sample.Counters.UploadCount;
                uploadBytes += sample.Counters.UploadBytes;
                draws += sample.Counters.DrawCount;
                creates += sample.Counters.TargetCreateCount;
                retires += sample.Counters.TargetRetireCount;
            }
            Array.Sort(cpu);
            Array.Sort(presentation);
            Array.Sort(total);
            string output = Path.GetFullPath(options.Output);
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            File.WriteAllText(output, JsonSerializer.Serialize(new
            {
                Schema = "cerneala-rendersurface3d-probe-v2",
                Scope = "CPU includes moving-camera mutation, UI ProcessFrame, retained commit, frame begin and SDL_GPU backend Render; excludes CompleteFrame(true), present/submit wait and VSync. Presentation is CompleteFrame(true), including submit and possible wait. Total includes CPU, presentation and event pump. Allocations cover the entire frame on this same render thread.",
                GpuTime = "unavailable: no timestamp query",
                Runtime = RuntimeInformation.FrameworkDescription,
                OS = RuntimeInformation.OSDescription,
                options.JointCount,
                options.Dpi,
                options.Moving,
                options.Warmup,
                options.Measured,
                WidthDips = Width,
                HeightDips = Height,
                CpuMedianMilliseconds = Percentile(cpu, .5),
                CpuP95Milliseconds = Percentile(cpu, .95),
                PresentationMedianMilliseconds = Percentile(presentation, .5),
                PresentationP95Milliseconds = Percentile(presentation, .95),
                TotalMedianMilliseconds = Percentile(total, .5),
                TotalP95Milliseconds = Percentile(total, .95),
                RenderThreadAllocatedBytesPerFrame = (double)allocations / options.Measured,
                MeasureCalls = measureCalls,
                ArrangeCalls = arrangeCalls,
                RecordingCount = recordings,
                PassCount = passes,
                UploadCount = uploads,
                UploadBytes = uploadBytes,
                DrawCount = draws,
                TargetCreateCount = creates,
                TargetRetireCount = retires,
                LastLiveTargetCount = window.Backend.LastFrameRenderSurface3DCounters.LiveTargetCount,
                FixtureJointCount = fixture.RenderedJointCount
            }, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
            Console.WriteLine($"RENDERSURFACE3D_PROBE_OK path={output} frames={options.Measured}");
        }
        finally { root.VisualChildren.Remove(fixture.Surface); }

        FrameSample Render(int frame)
        {
            long totalStart = Stopwatch.GetTimestamp();
            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            long cpuStart = Stopwatch.GetTimestamp();
            if (options.Moving) fixture.SetPresetDirection(frame % 8);
            FrameStats stats = root.ProcessFrame();
            DrawCommandList commands = root.RetainedRenderer.Commit(root);
            PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
            window.Session.BeginFrame(new Color(8, 16, 26));
            DrawingFrameContext drawingFrame = new(analysis);
            window.Session.DrawingBackend.Render(commands, in drawingFrame);
            double cpuMs = Stopwatch.GetElapsedTime(cpuStart).TotalMilliseconds;
            long presentationStart = Stopwatch.GetTimestamp();
            window.Session.CompleteFrame(present: true);
            double presentationMs = Stopwatch.GetElapsedTime(presentationStart).TotalMilliseconds;
            SdlGpuRenderSurface3DFrameCounters counters = window.Backend.LastFrameRenderSurface3DCounters;
            window.PumpEvents();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            return new(cpuMs, presentationMs, Stopwatch.GetElapsedTime(totalStart).TotalMilliseconds,
                allocated, stats.MeasureCalls, stats.ArrangeCalls, counters);
        }
    }

    private static ProbeOptions Parse(string[] args)
    {
        if (args.Length != 7 || args[0] != "--rendersurface3d-probe" ||
            !int.TryParse(args[2], out int joints) || joints is not (20 or 200) ||
            !int.TryParse(args[3], out int dpi) || dpi is not (1 or 2) ||
            args[4] is not ("static" or "moving") ||
            !int.TryParse(args[5], out int warmup) || warmup < 0 ||
            !int.TryParse(args[6], out int measured) || measured <= 0 ||
            string.IsNullOrWhiteSpace(args[1]))
            throw new ArgumentException("Usage: --rendersurface3d-probe <output.json> <20|200> <1|2> <static|moving> <warmup>=0+ <measured>=1+");
        return new(args[1], joints, dpi, args[4] == "moving", warmup, measured);
    }

    private static double Percentile(double[] sorted, double fraction) =>
        sorted[Math.Clamp((int)Math.Ceiling(sorted.Length * fraction) - 1, 0, sorted.Length - 1)];

    private readonly record struct ProbeOptions(string Output, int JointCount, int Dpi, bool Moving, int Warmup, int Measured);

    private readonly record struct FrameSample(double CpuMilliseconds, double PresentationMilliseconds,
        double TotalMilliseconds, long AllocatedBytes, int MeasureCalls, int ArrangeCalls,
        SdlGpuRenderSurface3DFrameCounters Counters);

    private sealed class ProbeWindow : IDisposable
    {
        private readonly NativeSdlApi api = new();
        private readonly SdlGpuWindowGraphicsSessionFactory graphics;
        private readonly SdlWindowPlatform platform;
        private readonly IPlatformWindow window;

        internal ProbeWindow(int dpi)
        {
            graphics = new SdlGpuWindowGraphicsSessionFactory(api, useMultisampling: true);
            platform = new SdlWindowPlatform(api, graphics, coordinateScaleOverride: dpi);
            window = platform.CreateWindow(new Window { Title = "RenderSurface3D probe", Width = Width, Height = Height }, new Sink());
            window.Show();
            platform.PumpEvents();
            Session = window.GraphicsSession as SdlGpuWindowGraphicsSession ?? throw new InvalidOperationException("No SDL_GPU session.");
            Backend = Session.DrawingBackend as SdlGpuDrawingBackend ?? throw new InvalidOperationException("No SDL_GPU backend.");
        }

        internal SdlGpuWindowGraphicsSession Session { get; }
        internal SdlGpuDrawingBackend Backend { get; }
        internal void PumpEvents() => platform.PumpEvents();
        public void Dispose() { window.Dispose(); platform.Dispose(); graphics.Dispose(); }
    }

    private sealed class Sink : IWindowPlatformCallbacks
    {
        public void RequestClose() { }
        public void ActivationChanged(bool active) { }
        public void BoundsChanged(UiViewport viewport, float left, float top, WindowState state) { }
        public void RenderRequested() { }
    }
}

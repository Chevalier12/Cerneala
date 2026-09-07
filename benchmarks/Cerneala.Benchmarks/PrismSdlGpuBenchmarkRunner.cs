using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Benchmarks;

internal static class PrismSdlGpuBenchmarkRunner
{
    private const int Width = 256;
    private const int Height = 144;
    private const int WarmupFrameCount = 12;
    private const int MeasuredFrameCount = 96;

    public static void Run()
    {
        DrawCommandList commands = CreateScenario();
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        if (analysis.RequiresBackdrop)
        {
            throw new InvalidOperationException(
                "The benchmark scenario must not require a backend-specific backdrop lease.");
        }

        BenchmarkResult sdlGpu;
        using (SdlGpuFixture fixture = new())
        {
            sdlGpu = Measure(
                "SDL_GPU",
                fixture.Session,
                commands,
                analysis,
                fixture.PumpEvents,
                () => fixture.Session.DrawingResources.PrismResources.PeakBytes);
        }

        BenchmarkReport report = new(
            Schema: "cerneala-prism-sdlgpu-benchmark-v1",
            Scenario: "256x144 retained GaussianBlur+Emboss+HueSaturation",
            WarmupFrames: WarmupFrameCount,
            MeasuredFrames: MeasuredFrameCount,
            SdlGpu: sdlGpu);

        Console.WriteLine(JsonSerializer.Serialize(
            report,
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private static BenchmarkResult Measure(
        string backend,
        IWindowGraphicsSession session,
        DrawCommandList commands,
        PrismFrameAnalysis analysis,
        Action pumpEvents,
        Func<long> peakGpuBytes)
    {
        for (int frame = 0; frame < WarmupFrameCount; frame++)
        {
            RenderFrame(session, commands, analysis);
            pumpEvents();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long allocationStart = GC.GetAllocatedBytesForCurrentThread();
        Stopwatch stopwatch = Stopwatch.StartNew();
        for (int frame = 0; frame < MeasuredFrameCount; frame++)
        {
            RenderFrame(session, commands, analysis);
            pumpEvents();
        }
        stopwatch.Stop();
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocationStart;

        PrismExecutionDiagnostics diagnostics = session.PrismExecutionDiagnostics ??
            throw new InvalidOperationException($"{backend} did not expose Prism diagnostics.");
        PrismExecutionCounters counters = diagnostics.Counters;
        if (diagnostics.Count != 0)
        {
            throw new InvalidOperationException(
                $"{backend} reported {diagnostics.Count} Prism fallback(s): {diagnostics.LastFallback}");
        }
        if (counters.ActiveSurfaceCount != 0)
        {
            throw new InvalidOperationException(
                $"{backend} retained {counters.ActiveSurfaceCount} active transient Prism surface(s).");
        }

        return new BenchmarkResult(
            Backend: backend,
            CpuFrameMilliseconds: stopwatch.Elapsed.TotalMilliseconds / MeasuredFrameCount,
            FrameSubmitCount: MeasuredFrameCount,
            ManagedAllocatedBytes: allocatedBytes,
            ManagedAllocatedBytesPerFrame: (double)allocatedBytes / MeasuredFrameCount,
            PeakGpuPrismResourceBytes: peakGpuBytes(),
            LastFramePrismPassCount: counters.PassCount,
            LastFramePrismCpuSubmitMicroseconds: counters.CpuSubmitTime.TotalMicroseconds,
            FallbackCount: diagnostics.Count,
            ActiveSurfaceCount: counters.ActiveSurfaceCount);
    }

    private static void RenderFrame(
        IWindowGraphicsSession session,
        DrawCommandList commands,
        PrismFrameAnalysis analysis)
    {
        session.BeginFrame(new Color(18, 22, 30, 255));
        DrawingFrameContext frame = new(analysis);
        session.DrawingBackend.Render(commands, in frame);
        session.CompleteFrame(present: true);
    }

    private static DrawCommandList CreateScenario()
    {
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "benchmark-layer",
            filters:
            [
                new PrismFilterDefinition(PrismFilterId.GaussianBlur),
                new PrismFilterDefinition(PrismFilterId.Emboss),
                new PrismFilterDefinition(PrismFilterId.HueSaturation)
            ]);
        PrismInstance instance = new(new PrismCompositionDefinition(
            "retained-benchmark",
            [layer]));
        PrismDrawScope scope = new(
            instance,
            new PrismCacheOwnerToken(7),
            new DrawRect(0, 0, Width, Height),
            Matrix3x2.Identity,
            pixelScale: 1,
            visualContentVersion: 1);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.BeginPrism(scope));
        commands.Add(DrawCommand.FillRectangle(
            new DrawRect(0, 0, Width, Height),
            new Color(36, 62, 112, 255)));
        commands.Add(DrawCommand.FillRectangle(
            new DrawRect(28, 22, 104, 86),
            new Color(224, 112, 64, 216)));
        commands.Add(DrawCommand.FillRectangle(
            new DrawRect(118, 46, 110, 70),
            new Color(58, 190, 154, 192)));
        commands.Add(DrawCommand.EndPrism());
        return commands;
    }

    private sealed class SdlGpuFixture : IDisposable
    {
        private readonly NativeSdlApi api = new();
        private readonly SdlGpuWindowGraphicsSessionFactory graphics;
        private readonly SdlWindowPlatform platform;
        private readonly IPlatformWindow window;

        public SdlGpuFixture()
        {
            graphics = new SdlGpuWindowGraphicsSessionFactory(api, useMultisampling: false);
            platform = new SdlWindowPlatform(api, graphics, coordinateScaleOverride: 1);
            window = platform.CreateWindow(
                new Window
                {
                    Title = "Cerneala Prism SDL_GPU benchmark",
                    Width = Width,
                    Height = Height
                },
                new CallbackSink());
            window.Show();
            platform.PumpEvents();
            Session = window.GraphicsSession as SdlGpuWindowGraphicsSession ??
                throw new InvalidOperationException(
                    "The benchmark window did not create an SDL_GPU session.");
        }

        public SdlGpuWindowGraphicsSession Session { get; }

        public void PumpEvents() => platform.PumpEvents();

        public void Dispose()
        {
            window.Dispose();
            platform.Dispose();
            graphics.Dispose();
        }
    }

    private sealed class CallbackSink : IWindowPlatformCallbacks
    {
        public void RequestClose()
        {
        }

        public void ActivationChanged(bool active)
        {
        }

        public void BoundsChanged(
            UiViewport viewport,
            float left,
            float top,
            WindowState state)
        {
        }

        public void RenderRequested()
        {
        }
    }

    private sealed record BenchmarkResult(
        string Backend,
        double CpuFrameMilliseconds,
        int FrameSubmitCount,
        long ManagedAllocatedBytes,
        double ManagedAllocatedBytesPerFrame,
        long PeakGpuPrismResourceBytes,
        int LastFramePrismPassCount,
        double LastFramePrismCpuSubmitMicroseconds,
        int FallbackCount,
        int ActiveSurfaceCount);

    private sealed record BenchmarkReport(
        string Schema,
        string Scenario,
        int WarmupFrames,
        int MeasuredFrames,
        BenchmarkResult SdlGpu);
}

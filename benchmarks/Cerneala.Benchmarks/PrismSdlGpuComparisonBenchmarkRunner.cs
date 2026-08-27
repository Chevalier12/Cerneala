using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Hosting.Windows;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Benchmarks;

internal static class PrismSdlGpuComparisonBenchmarkRunner
{
    private const int Width = 256;
    private const int Height = 144;
    private const int WarmupFrameCount = 12;
    private const int MeasuredFrameCount = 96;

    public static void Run()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "The WindowsDX/SDL_GPU comparison benchmark requires Windows.");
        }

        DrawCommandList commands = CreateScenario();
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        if (analysis.RequiresBackdrop)
        {
            throw new InvalidOperationException(
                "The comparison scenario must not require a backend-specific backdrop lease.");
        }

        BenchmarkResult windowsDx;
        using (WindowsDxFixture fixture = new())
        {
            windowsDx = Measure(
                "WindowsDX",
                fixture.Session,
                commands,
                analysis,
                fixture.PumpEvents,
                () => AssertMonoGameBackend(fixture.Session)
                    .RendererDiagnostics
                    .PeakTotalByteCount);
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

        double cpuRatio = sdlGpu.CpuFrameMilliseconds / windowsDx.CpuFrameMilliseconds;
        ComparisonResult comparison = new(
            Schema: "cerneala-prism-backend-comparison-v1",
            Scenario: "256x144 retained GaussianBlur+Emboss+HueSaturation",
            WarmupFrames: WarmupFrameCount,
            MeasuredFrames: MeasuredFrameCount,
            WindowsDx: windowsDx,
            SdlGpu: sdlGpu,
            SdlGpuCpuRatio: cpuRatio,
            RequiresInvestigation: cpuRatio > 1.25);

        Console.WriteLine(JsonSerializer.Serialize(
            comparison,
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
            "comparison-layer",
            filters:
            [
                new PrismFilterDefinition(PrismFilterId.GaussianBlur),
                new PrismFilterDefinition(PrismFilterId.Emboss),
                new PrismFilterDefinition(PrismFilterId.HueSaturation)
            ]);
        PrismInstance instance = new(new PrismCompositionDefinition(
            "comparison",
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

    private static MonoGameDrawingBackend AssertMonoGameBackend(
        WindowsDxWindowGraphicsSession session) =>
        session.DrawingBackend as MonoGameDrawingBackend ??
        throw new InvalidOperationException("WindowsDX did not expose the MonoGame drawing backend.");

    private sealed class WindowsDxFixture : IDisposable
    {
        private readonly Win32WindowPlatform platform = new(
            new WindowsDxWindowGraphicsSessionFactory(useMultisampling: false));
        private readonly IPlatformWindow window;

        public WindowsDxFixture()
        {
            window = platform.CreateWindow(
                new Window
                {
                    Title = "Cerneala Prism WindowsDX comparison",
                    Width = Width,
                    Height = Height
                },
                new CallbackSink());
            window.Show();
            platform.PumpEvents();
            Session = window.GraphicsSession as WindowsDxWindowGraphicsSession ??
                throw new InvalidOperationException(
                    "The comparison window did not create a WindowsDX session.");
        }

        public WindowsDxWindowGraphicsSession Session { get; }

        public void PumpEvents() => platform.PumpEvents();

        public void Dispose()
        {
            window.Dispose();
            platform.Dispose();
        }
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
                    Title = "Cerneala Prism SDL_GPU comparison",
                    Width = Width,
                    Height = Height
                },
                new CallbackSink());
            window.Show();
            platform.PumpEvents();
            Session = window.GraphicsSession as SdlGpuWindowGraphicsSession ??
                throw new InvalidOperationException(
                    "The comparison window did not create an SDL_GPU session.");
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

    private sealed record ComparisonResult(
        string Schema,
        string Scenario,
        int WarmupFrames,
        int MeasuredFrames,
        BenchmarkResult WindowsDx,
        BenchmarkResult SdlGpu,
        double SdlGpuCpuRatio,
        bool RequiresInvestigation);
}

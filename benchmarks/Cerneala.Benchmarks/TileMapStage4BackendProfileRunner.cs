using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Hosting.Windows;
using Cerneala.UI.Resources;
using SkiaSharp;
using XnaRectangle = Microsoft.Xna.Framework.Rectangle;

namespace Cerneala.Benchmarks;

internal static class TileMapStage4BackendProfileRunner
{
    private const int WarmupFrames = 12;
    private const int MeasuredFrames = 96;
    private static readonly DrawRect SurfaceBounds = new(
        0,
        0,
        TileMapStage4ModelFactory.ViewWidthInTiles * TileMapStage4ModelFactory.TileSize,
        TileMapStage4ModelFactory.ViewHeightInTiles * TileMapStage4ModelFactory.TileSize);

    internal static void Run(string reportPath)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "The TileMap2D WindowsDX/SDL_GPU backend profile requires Windows.");
        }

        string fullPath = Path.GetFullPath(reportPath);
        string artifactDirectory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(artifactDirectory);
        string terrainPath = Path.Combine(artifactDirectory, "profile-terrain.png");
        string structuresPath = Path.Combine(artifactDirectory, "profile-structures.png");
        WriteAtlas(terrainPath, tileCount: 12, hueOffset: 0);
        WriteAtlas(structuresPath, tileCount: 8, hueOffset: 96);

        TileMapStage4BackendProfile windowsDx;
        using (WindowsDxFixture fixture = new((int)SurfaceBounds.Width, (int)SurfaceBounds.Height))
        {
            windowsDx = MeasureBackend(
                "WindowsDX",
                fixture.Session,
                fixture.PumpEvents,
                terrainPath,
                structuresPath,
                "Retained command diff computes damage bounds; unchanged surface versions reuse the offscreen surface without rasterization.");
        }

        TileMapStage4BackendProfile sdlGpu;
        using (SdlGpuFixture fixture = new((int)SurfaceBounds.Width, (int)SurfaceBounds.Height))
        {
            sdlGpu = MeasureBackend(
                "SDL_GPU",
                fixture.Session,
                fixture.PumpEvents,
                terrainPath,
                structuresPath,
                "Frame-version invalidation rerenders the complete offscreen surface; this backend exposes no retained damage rectangle.");
        }

        TileMapStage4BackendReport report = new(
            Schema: "cerneala-tilemap-stage4-backend-profile-v1",
            TimestampUtc: DateTimeOffset.UtcNow,
            Commit: ResolveGit("rev-parse HEAD"),
            WorkingTreeDirty: ResolveGit("status --porcelain").Length != 0,
            Runtime: RuntimeInformation.FrameworkDescription,
            OperatingSystem: RuntimeInformation.OSDescription,
            ProcessArchitecture: RuntimeInformation.ProcessArchitecture.ToString(),
            Processor: Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "unknown",
            LogicalProcessorCount: Environment.ProcessorCount,
            StopwatchFrequency: Stopwatch.Frequency,
            WarmupFrames,
            MeasuredFrames,
            SurfaceWidth: (int)SurfaceBounds.Width,
            SurfaceHeight: (int)SurfaceBounds.Height,
            TerrainAtlas: Path.GetRelativePath(Environment.CurrentDirectory, terrainPath),
            StructuresAtlas: Path.GetRelativePath(Environment.CurrentDirectory, structuresPath),
            WindowsDx: windowsDx,
            SdlGpu: sdlGpu);
        File.WriteAllText(
            fullPath,
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);

        Console.WriteLine($"Tilemap stage 4 backend profile: {fullPath}");
        PrintProfile(windowsDx);
        PrintProfile(sdlGpu);
    }

    private static TileMapStage4BackendProfile MeasureBackend(
        string backend,
        IWindowGraphicsSession session,
        Action pumpEvents,
        string terrainPath,
        string structuresPath,
        string damageTrackingContract)
    {
        TileMapStage4BackendScenario[] scenarios =
        [
            MeasureScenario(
                backend,
                "warm-static",
                session,
                pumpEvents,
                terrainPath,
                structuresPath,
                static (workload, iteration) => workload.PrepareWarmStatic(iteration)),
            MeasureScenario(
                backend,
                "camera-pan",
                session,
                pumpEvents,
                terrainPath,
                structuresPath,
                static (workload, iteration) => workload.PrepareCameraPan(iteration)),
            MeasureScenario(
                backend,
                "chunk-mutation",
                session,
                pumpEvents,
                terrainPath,
                structuresPath,
                static (workload, _) => workload.PrepareChunkMutation())
        ];

        TileMapStage4BackendScenario warm = scenarios[0];
        if (backend == "WindowsDX" && warm.WindowsDx!.RasterizedFrameCount != 0)
        {
            throw new InvalidOperationException(
                "WindowsDX rerasterized an unchanged TileMap2D surface after warmup.");
        }
        if (scenarios[1].CoreCounters.BatchesRebuilt != 0)
        {
            throw new InvalidOperationException(
                $"{backend} rebuilt tile batches during camera pan.");
        }
        if (scenarios[2].CoreCounters.BatchesRebuilt != 1)
        {
            throw new InvalidOperationException(
                $"{backend} did not rebuild exactly one tile batch for the local mutation.");
        }

        return new TileMapStage4BackendProfile(
            backend,
            damageTrackingContract,
            scenarios);
    }

    private static TileMapStage4BackendScenario MeasureScenario(
        string backend,
        string scenario,
        IWindowGraphicsSession session,
        Action pumpEvents,
        string terrainPath,
        string structuresPath,
        Action<TileMapStage4BackendWorkload, int> prepare)
    {
        using TileMapStage4BackendWorkload workload = new(
            session,
            terrainPath,
            structuresPath,
            SurfaceBounds);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.RenderSurface2D(workload.Surface, SurfaceBounds, Color.White));
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);

        for (int frame = 0; frame < WarmupFrames; frame++)
        {
            prepare(workload, frame);
            RenderFrame(session, commands, analysis, pumpEvents);
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        double[] wallSamples = new double[MeasuredFrames];
        double[] commandSamples = new double[MeasuredFrames];
        long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        int rasterizedFrames = 0;
        long damagePixels = 0;
        long replayedCommands = 0;
        string lastMissReason = "Unavailable";
        long sdlDrawCalls = 0;
        long sdlSubmissions = 0;
        long sdlMergedSubmissions = 0;
        long sdlVertexBytes = 0;
        long sdlIndexBytes = 0;

        MonoGameRenderSurface2DSession? monoSurface = backend == "WindowsDX"
            ? workload.RequireMonoGameSurface((WindowsDxWindowGraphicsSession)session)
            : null;
        int previousRasterizedCount = monoSurface?.RasterizedFrameCount ?? 0;

        for (int frame = 0; frame < MeasuredFrames; frame++)
        {
            prepare(workload, frame + WarmupFrames);
            long started = Stopwatch.GetTimestamp();
            RenderFrame(session, commands, analysis, pumpEvents);
            wallSamples[frame] = Stopwatch.GetElapsedTime(started).TotalMicroseconds;
            DrawingBackendFrameTiming timing =
                ((IDrawingBackendFrameTimingSource)session.DrawingBackend).LastFrameTiming;
            commandSamples[frame] = timing.CommandRendering.TotalMicroseconds;

            if (monoSurface is not null &&
                monoSurface.RasterizedFrameCount != previousRasterizedCount)
            {
                rasterizedFrames += monoSurface.RasterizedFrameCount - previousRasterizedCount;
                previousRasterizedCount = monoSurface.RasterizedFrameCount;
                if (monoSurface.LastDamageBounds is XnaRectangle damage)
                {
                    damagePixels += (long)damage.Width * damage.Height;
                }
                replayedCommands += monoSurface.LastReplayedCommandCount;
                lastMissReason = monoSurface.LastRetainedMissReason.ToString();
            }

            if (session.DrawingBackend is SdlGpuDrawingBackend sdlBackend)
            {
                SdlGpuDrawingFrameCounters counters = sdlBackend.LastFrameCounters;
                sdlDrawCalls += counters.DrawCallCount;
                sdlSubmissions += counters.SubmissionCount;
                sdlMergedSubmissions += counters.MergedSubmissionCount;
                sdlVertexBytes += counters.VertexBytes;
                sdlIndexBytes += counters.IndexBytes;
            }
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Array.Sort(wallSamples);
        Array.Sort(commandSamples);
        TileMapStage4Counters coreCounters =
            TileMapStage4Counters.From(workload.Map.GetDiagnosticsSnapshot());
        return new TileMapStage4BackendScenario(
            scenario,
            WallP50Microseconds: Percentile(wallSamples, 0.50),
            WallP95Microseconds: Percentile(wallSamples, 0.95),
            CommandRenderingP50Microseconds: Percentile(commandSamples, 0.50),
            CommandRenderingP95Microseconds: Percentile(commandSamples, 0.95),
            ManagedAllocatedBytesPerFrame: (double)allocated / MeasuredFrames,
            CoreCounters: coreCounters,
            WindowsDx: monoSurface is null
                ? null
                : new TileMapStage4WindowsDxTelemetry(
                    rasterizedFrames,
                    DamagedFrameCount: rasterizedFrames,
                    AverageDamagePixels: rasterizedFrames == 0 ? 0 : (double)damagePixels / rasterizedFrames,
                    AverageReplayedCommands: rasterizedFrames == 0 ? 0 : (double)replayedCommands / rasterizedFrames,
                    lastMissReason),
            SdlGpu: session.DrawingBackend is not SdlGpuDrawingBackend
                ? null
                : new TileMapStage4SdlGpuTelemetry(
                    AverageDrawCalls: (double)sdlDrawCalls / MeasuredFrames,
                    AverageSubmissions: (double)sdlSubmissions / MeasuredFrames,
                    AverageMergedSubmissions: (double)sdlMergedSubmissions / MeasuredFrames,
                    AverageVertexBytes: (double)sdlVertexBytes / MeasuredFrames,
                    AverageIndexBytes: (double)sdlIndexBytes / MeasuredFrames,
                    RerendersFullOffscreenSurfaceOnFrameVersionChange: scenario != "warm-static",
                    ExposesDamageRectangle: false));
    }

    private static void RenderFrame(
        IWindowGraphicsSession session,
        DrawCommandList commands,
        PrismFrameAnalysis analysis,
        Action pumpEvents)
    {
        session.BeginFrame(new Color(12, 16, 24, 255));
        try
        {
            DrawingFrameContext frame = new(analysis);
            session.DrawingBackend.Render(commands, in frame);
            session.CompleteFrame(present: false);
        }
        catch
        {
            try
            {
                session.CompleteFrame(present: false);
            }
            catch
            {
            }
            throw;
        }
        pumpEvents();
    }

    private static double Percentile(double[] sorted, double percentile)
    {
        int index = (int)Math.Ceiling((sorted.Length * percentile) - 1);
        return sorted[Math.Clamp(index, 0, sorted.Length - 1)];
    }

    private static void WriteAtlas(string path, int tileCount, int hueOffset)
    {
        using SKBitmap bitmap = new(tileCount * TileMapStage4ModelFactory.TileSize, TileMapStage4ModelFactory.TileSize);
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.Transparent);
        using SKPaint paint = new() { IsAntialias = false };
        for (int tile = 0; tile < tileCount; tile++)
        {
            byte red = (byte)(48 + ((tile * 37 + hueOffset) % 192));
            byte green = (byte)(48 + ((tile * 67 + hueOffset) % 192));
            byte blue = (byte)(48 + ((tile * 97 + hueOffset) % 192));
            paint.Color = new SKColor(red, green, blue, 255);
            canvas.DrawRect(
                tile * TileMapStage4ModelFactory.TileSize,
                0,
                TileMapStage4ModelFactory.TileSize,
                TileMapStage4ModelFactory.TileSize,
                paint);
            paint.Color = new SKColor(255, 255, 255, 96);
            canvas.DrawRect(
                tile * TileMapStage4ModelFactory.TileSize,
                tile % 4,
                TileMapStage4ModelFactory.TileSize,
                2,
                paint);
        }
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData png = image.Encode(SKEncodedImageFormat.Png, 100);
        using FileStream output = File.Create(path);
        png.SaveTo(output);
    }

    private static string ResolveGit(string arguments)
    {
        try
        {
            ProcessStartInfo start = new("git", arguments)
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
            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return process.ExitCode == 0 && output.Length != 0 ? output : "unknown";
        }
        catch
        {
            return "unknown";
        }
    }

    private static void PrintProfile(TileMapStage4BackendProfile profile)
    {
        foreach (TileMapStage4BackendScenario scenario in profile.Scenarios)
        {
            Console.WriteLine(
                $"{profile.Backend}/{scenario.Name}: wall p50={scenario.WallP50Microseconds.ToString("0.0", CultureInfo.InvariantCulture)}us, " +
                $"p95={scenario.WallP95Microseconds.ToString("0.0", CultureInfo.InvariantCulture)}us, " +
                $"command p95={scenario.CommandRenderingP95Microseconds.ToString("0.0", CultureInfo.InvariantCulture)}us, " +
                $"commands={scenario.CoreCounters.DrawCommands}");
        }
    }

    private sealed class TileMapStage4BackendWorkload : IDisposable
    {
        private readonly UIRoot root = new();
        private readonly TileMap2DModel originalModel = TileMapStage4ModelFactory.Create(mutated: false);
        private readonly TileMap2DModel mutatedModel = TileMapStage4ModelFactory.Create(mutated: true);
        private bool useMutatedModel;

        internal TileMapStage4BackendWorkload(
            IWindowGraphicsSession session,
            string terrainPath,
            string structuresPath,
            DrawRect bounds)
        {
            Map = new TileMap2D { Model = originalModel };
            Scene2D scene = new();
            scene.Children.Add(Map);
            Surface = new RenderSurface2D
            {
                Scene = scene,
                ViewBox = TileMapStage4ModelFactory.CameraView(32),
                RedrawMode = RenderSurface2DRedrawMode.OnDemand,
                Width = bounds.Width,
                Height = bounds.Height
            };
            Surface.Resources.SetResource(
                TileMapStage4ModelFactory.TerrainResourceId,
                new ImageResource(terrainPath));
            Surface.Resources.SetResource(
                TileMapStage4ModelFactory.StructuresResourceId,
                new ImageResource(structuresPath));
            root.SetImageLoader(session.ImageLoader ??
                throw new InvalidOperationException("The backend profile requires an image loader."));
            root.VisualChildren.Add(Surface);
        }

        internal RenderSurface2D Surface { get; }

        internal TileMap2D Map { get; }

        internal void PrepareWarmStatic(int frame)
        {
            if (frame == 1)
            {
                Surface.InvalidateFrame();
            }
        }

        internal void PrepareCameraPan(int frame)
        {
            Surface.ViewBox = TileMapStage4ModelFactory.CameraView(32 + (frame % 32));
        }

        internal void PrepareChunkMutation()
        {
            useMutatedModel = !useMutatedModel;
            Map.Model = useMutatedModel ? mutatedModel : originalModel;
            Surface.InvalidateFrame();
        }

        internal MonoGameRenderSurface2DSession RequireMonoGameSurface(
            WindowsDxWindowGraphicsSession session) =>
            ((IRenderSurface2DFrameSource)Surface).GetBackendState(session.GraphicsDevice)
                as MonoGameRenderSurface2DSession ??
            throw new InvalidOperationException("WindowsDX did not create a retained RenderSurface2D session.");

        public void Dispose() => root.VisualChildren.Remove(Surface);
    }

    private sealed class WindowsDxFixture : IDisposable
    {
        private readonly Win32WindowPlatform platform;
        private readonly IPlatformWindow window;

        internal WindowsDxFixture(int width, int height)
        {
            platform = new Win32WindowPlatform(
                new WindowsDxWindowGraphicsSessionFactory(useMultisampling: false),
                coordinateScaleOverride: 1);
            window = platform.CreateWindow(
                new Window
                {
                    Title = "Cerneala TileMap2D WindowsDX profile",
                    Width = width,
                    Height = height
                },
                new CallbackSink());
            window.Show();
            platform.PumpEvents();
            Session = window.GraphicsSession as WindowsDxWindowGraphicsSession ??
                throw new InvalidOperationException("The profile window did not create a WindowsDX session.");
        }

        internal WindowsDxWindowGraphicsSession Session { get; }

        internal void PumpEvents() => platform.PumpEvents();

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

        internal SdlGpuFixture(int width, int height)
        {
            graphics = new SdlGpuWindowGraphicsSessionFactory(api, useMultisampling: false);
            platform = new SdlWindowPlatform(api, graphics, coordinateScaleOverride: 1);
            window = platform.CreateWindow(
                new Window
                {
                    Title = "Cerneala TileMap2D SDL_GPU profile",
                    Width = width,
                    Height = height
                },
                new CallbackSink());
            window.Show();
            platform.PumpEvents();
            Session = window.GraphicsSession as SdlGpuWindowGraphicsSession ??
                throw new InvalidOperationException("The profile window did not create an SDL_GPU session.");
        }

        internal SdlGpuWindowGraphicsSession Session { get; }

        internal void PumpEvents() => platform.PumpEvents();

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

        public void BoundsChanged(UiViewport viewport, float left, float top, WindowState state)
        {
        }

        public void RenderRequested()
        {
        }
    }
}

internal sealed record TileMapStage4BackendReport(
    string Schema,
    DateTimeOffset TimestampUtc,
    string Commit,
    bool WorkingTreeDirty,
    string Runtime,
    string OperatingSystem,
    string ProcessArchitecture,
    string Processor,
    int LogicalProcessorCount,
    long StopwatchFrequency,
    int WarmupFrames,
    int MeasuredFrames,
    int SurfaceWidth,
    int SurfaceHeight,
    string TerrainAtlas,
    string StructuresAtlas,
    TileMapStage4BackendProfile WindowsDx,
    TileMapStage4BackendProfile SdlGpu);

internal sealed record TileMapStage4BackendProfile(
    string Backend,
    string DamageTrackingContract,
    IReadOnlyList<TileMapStage4BackendScenario> Scenarios);

internal sealed record TileMapStage4BackendScenario(
    string Name,
    double WallP50Microseconds,
    double WallP95Microseconds,
    double CommandRenderingP50Microseconds,
    double CommandRenderingP95Microseconds,
    double ManagedAllocatedBytesPerFrame,
    TileMapStage4Counters CoreCounters,
    TileMapStage4WindowsDxTelemetry? WindowsDx,
    TileMapStage4SdlGpuTelemetry? SdlGpu);

internal sealed record TileMapStage4WindowsDxTelemetry(
    int RasterizedFrameCount,
    int DamagedFrameCount,
    double AverageDamagePixels,
    double AverageReplayedCommands,
    string LastRetainedMissReason);

internal sealed record TileMapStage4SdlGpuTelemetry(
    double AverageDrawCalls,
    double AverageSubmissions,
    double AverageMergedSubmissions,
    double AverageVertexBytes,
    double AverageIndexBytes,
    bool RerendersFullOffscreenSurfaceOnFrameVersionChange,
    bool ExposesDamageRectangle);

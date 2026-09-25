using System.Diagnostics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Hosting.Sdl;
using Xunit.Abstractions;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuImageDomainWarmBaselineTests(ITestOutputHelper output)
{
    private const int QuadCount = 1_024;
    private const int WarmupFrames = 32;
    private const int MeasuredFrames = 20;
    private const int FrameSize = 256;
    private static readonly DrawRect Source = new(2, 2, 16, 16);

    [Fact]
    public void WarmImageWorkloadsRecordCpuAllocationsAndBatchingWithoutAnArbitraryThreshold()
    {
        output.WriteLine(
            $"Fake SDL, Release={IsReleaseBuild()}, quads={QuadCount}, " +
            $"warmup={WarmupFrames}, measured={MeasuredFrames}, frame={FrameSize}x{FrameSize}; " +
            "CPU timings include fake-API observation, not native GPU work.");

        Measure("ordinary Linear+Clamp images", Workload.Ordinary);
        Measure("Point+Clamp images", Workload.PointImages);
        Measure("alternating Linear/Point images", Workload.MixedSampling);
        Measure("alternating Point images/authored quads, same sampler", Workload.MixedProvenance);

        output.WriteLine("GPU time: unavailable; fake SDL has no GPU timestamp measurement.");
    }

    private void Measure(string name, Workload workload)
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow(name, FrameSize, FrameSize, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(
                new SdlWindowSurface(window, api.GetWindowId(window)),
                FrameSize,
                FrameSize,
                coordinateScale: 1));
        using SdlGpuImage image = new(20, 20, CreateImagePixels());
        DrawCommandList commands = CreateCommands(image, workload);
        DrawingFrameContext frame = new(new PrismFrameAnalyzer().Analyze(commands));
        SdlGpuDrawingBackend backend = Assert.IsType<SdlGpuDrawingBackend>(session.DrawingBackend);

        for (int frameIndex = 0; frameIndex < WarmupFrames; frameIndex++)
        {
            Render(session, commands, frame);
            ClearObservations(api);
        }

        double[] milliseconds = new double[MeasuredFrames];
        long[] allocatedBytes = new long[MeasuredFrames];
        int createdBuffers = 0;
        int createdTransfers = 0;
        SdlGpuDrawingFrameCounters firstCounters = default;
        for (int frameIndex = 0; frameIndex < MeasuredFrames; frameIndex++)
        {
            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            long started = Stopwatch.GetTimestamp();
            Render(session, commands, frame);
            milliseconds[frameIndex] = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            allocatedBytes[frameIndex] =
                GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

            SdlGpuDrawingFrameCounters counters = backend.LastFrameCounters;
            Assert.Equal(QuadCount, counters.SubmissionCount);
            Assert.Equal(QuadCount * 4, counters.VertexCount);
            if (frameIndex == 0)
            {
                firstCounters = counters;
            }
            else
            {
                Assert.Equal(firstCounters, counters);
            }

            createdBuffers += api.GpuActions.Count(static action =>
                action.StartsWith("create-buffer:", StringComparison.Ordinal));
            createdTransfers += api.GpuActions.Count(static action =>
                action.StartsWith("create-transfer:", StringComparison.Ordinal));
            ClearObservations(api);
        }

        output.WriteLine(
            $"{name}: CPU ms min/median/p95/max={Summary(milliseconds)}; " +
            $"allocated bytes min/median/p95/max={Summary(allocatedBytes)}; " +
            $"vertexBytes={firstCounters.VertexBytes}; " +
            $"submissions={firstCounters.SubmissionCount}; " +
            $"merged={firstCounters.MergedSubmissionCount}; " +
            $"draws={firstCounters.DrawCallCount}; " +
            $"pipelineBinds={firstCounters.PipelineBindCount}; " +
            $"samplerBinds={firstCounters.SamplerBindCount}; " +
            $"warmNewBuffers={createdBuffers}; warmNewTransfers={createdTransfers}.");
    }

    private static DrawCommandList CreateCommands(SdlGpuImage image, Workload workload)
    {
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        DrawImageOptions linear = new(source: Source, sampling: DrawSamplingMode.Linear);
        DrawImageOptions point = new(source: Source, sampling: DrawSamplingMode.Point);
        float left = Source.X / image.Width;
        float top = Source.Y / image.Height;
        float right = Source.Right / image.Width;
        float bottom = Source.Bottom / image.Height;
        for (int index = 0; index < QuadCount; index++)
        {
            DrawRect destination = new(index % 32 * 8, index / 32 * 8, 8, 8);
            bool alternate = (index & 1) != 0;
            if (workload == Workload.MixedProvenance && alternate)
            {
                drawing.DrawImageQuad(
                    image,
                    new DrawVertex2D(
                        new DrawPoint(destination.X, destination.Y), Color.White, new DrawPoint(left, top)),
                    new DrawVertex2D(
                        new DrawPoint(destination.Right, destination.Y), Color.White, new DrawPoint(right, top)),
                    new DrawVertex2D(
                        new DrawPoint(destination.Right, destination.Bottom), Color.White, new DrawPoint(right, bottom)),
                    new DrawVertex2D(
                        new DrawPoint(destination.X, destination.Bottom), Color.White, new DrawPoint(left, bottom)),
                    sampling: DrawSamplingMode.Point);
                continue;
            }

            bool pointSampled = workload is Workload.PointImages or Workload.MixedProvenance ||
                (workload == Workload.MixedSampling && alternate);
            drawing.DrawImage(image, destination, pointSampled ? point : linear);
        }
        return commands;
    }

    private static byte[] CreateImagePixels()
    {
        byte[] pixels = new byte[20 * 20 * 4];
        for (int offset = 0; offset < pixels.Length; offset += 4)
        {
            pixels[offset] = 64;
            pixels[offset + 1] = 128;
            pixels[offset + 2] = 192;
            pixels[offset + 3] = 255;
        }
        return pixels;
    }

    private static void Render(
        SdlGpuWindowGraphicsSession session,
        DrawCommandList commands,
        DrawingFrameContext frame)
    {
        session.BeginFrame(Color.Transparent);
        session.DrawingBackend.Render(commands, in frame);
        session.CompleteFrame(present: false);
    }

    private static void ClearObservations(FakeSdlApi api)
    {
        api.GpuActions.Clear();
        api.FragmentSamplerBindings.Clear();
        api.RenderTargets.Clear();
    }

    private static string Summary(double[] samples)
    {
        double[] sorted = samples.OrderBy(static sample => sample).ToArray();
        return $"{sorted[0]:F3}/{sorted[sorted.Length / 2]:F3}/" +
            $"{sorted[(int)Math.Ceiling(sorted.Length * 0.95) - 1]:F3}/{sorted[^1]:F3}";
    }

    private static string Summary(long[] samples)
    {
        long[] sorted = samples.OrderBy(static sample => sample).ToArray();
        return $"{sorted[0]}/{sorted[sorted.Length / 2]}/" +
            $"{sorted[(int)Math.Ceiling(sorted.Length * 0.95) - 1]}/{sorted[^1]}";
    }

    private static bool IsReleaseBuild()
    {
#if DEBUG
        return false;
#else
        return true;
#endif
    }

    private enum Workload
    {
        Ordinary,
        PointImages,
        MixedSampling,
        MixedProvenance
    }
}

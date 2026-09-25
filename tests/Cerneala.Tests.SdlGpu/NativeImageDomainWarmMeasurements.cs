using System.Diagnostics;
using System.Globalization;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.SdlGpu;
using Xunit.Abstractions;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class NativeImageDomainWarmMeasurements(ITestOutputHelper output)
{
    private const int QuadCount = 1_024;
    private const int WarmupFrames = 32;
    private const int MeasuredFrames = 20;
    private const int FrameSize = 256;
    private static readonly DrawRect Source = new(2, 2, 16, 16);

    [SdlNativeFact]
    public void WarmImageWorkloadsRecordNativeCpuSubmissionAndCurrentThreadAllocations()
    {
        output.WriteLine(
            $"Native SDL, quads={QuadCount}, warmup={WarmupFrames}, " +
            $"measured={MeasuredFrames}, frame={FrameSize}x{FrameSize}. " +
            "CPU time includes BeginFrame, backend Render, and CompleteFrame(present: false); " +
            "it is not GPU time or a swapchain presentation measurement. " +
            "Allocated bytes are for the measured thread only. No GPU readback occurs in the measured path.");

        Measure("ordinary Linear+Clamp images", Workload.Ordinary);
        Measure("Point+Clamp images", Workload.PointImages);
        Measure("alternating Linear/Point images", Workload.MixedSampling);
        Measure("alternating Point images/authored quads, same sampler", Workload.MixedProvenance);

        output.WriteLine("GPU time: unavailable; no GPU timestamp query is used.");
    }

    private void Measure(string name, Workload workload)
    {
        // Each fixture owns SDL's process-wide platform lifetime. Dispose it
        // before creating the next independent workload's fixture.
        using SdlDrawingFixture fixture = new(FrameSize, FrameSize, useMultisampling: false);
        using SdlGpuImage image = new(20, 20, CreateImagePixels());
        DrawCommandList commands = CreateCommands(image, workload);
        DrawingFrameContext frame = new(new PrismFrameAnalyzer().Analyze(commands));
        SdlGpuWindowGraphicsSession session = fixture.Session;
        SdlGpuDrawingBackend backend = fixture.Backend;

        for (int frameIndex = 0; frameIndex < WarmupFrames; frameIndex++)
        {
            Render(session, commands, frame);
        }

        double[] milliseconds = new double[MeasuredFrames];
        long[] allocatedBytes = new long[MeasuredFrames];
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
        }

        output.WriteLine(
            $"{name}: native CPU ms min/median/p95/max={Summary(milliseconds)}; " +
            $"current-thread allocated bytes min/median/p95/max={Summary(allocatedBytes)}; " +
            $"vertexBytes={firstCounters.VertexBytes}; " +
            $"submissions={firstCounters.SubmissionCount}; " +
            $"merged={firstCounters.MergedSubmissionCount}; " +
            $"draws={firstCounters.DrawCallCount}; " +
            $"pipelineBinds={firstCounters.PipelineBindCount}; " +
            $"samplerBinds={firstCounters.SamplerBindCount}.");
        output.WriteLine($"{name}: CPU ms samples=[{Samples(milliseconds)}]");
        output.WriteLine($"{name}: current-thread allocation samples=[{string.Join(", ", allocatedBytes)}]");
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
                    new DrawVertex2D(new DrawPoint(destination.X, destination.Y),
                        Color.White, new DrawPoint(left, top)),
                    new DrawVertex2D(new DrawPoint(destination.Right, destination.Y),
                        Color.White, new DrawPoint(right, top)),
                    new DrawVertex2D(new DrawPoint(destination.Right, destination.Bottom),
                        Color.White, new DrawPoint(right, bottom)),
                    new DrawVertex2D(new DrawPoint(destination.X, destination.Bottom),
                        Color.White, new DrawPoint(left, bottom)),
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
        try
        {
            session.DrawingBackend.Render(commands, in frame);
        }
        finally
        {
            session.CompleteFrame(present: false);
        }
    }

    private static string Summary(double[] values)
    {
        double[] ordered = (double[])values.Clone();
        Array.Sort(ordered);
        double median = (ordered[9] + ordered[10]) / 2;
        return string.Join("/", new[] { ordered[0], median, ordered[18], ordered[^1] }
            .Select(value => value.ToString("F3", CultureInfo.InvariantCulture)));
    }

    private static string Summary(long[] values)
    {
        long[] ordered = (long[])values.Clone();
        Array.Sort(ordered);
        long median = (ordered[9] + ordered[10]) / 2;
        return $"{ordered[0]}/{median}/{ordered[18]}/{ordered[^1]}";
    }

    private static string Samples(double[] values) =>
        string.Join(", ", values.Select(value => value.ToString("F3", CultureInfo.InvariantCulture)));

    private enum Workload
    {
        Ordinary,
        PointImages,
        MixedSampling,
        MixedProvenance
    }
}

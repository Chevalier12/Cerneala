using System.Numerics;
using System.Runtime.InteropServices;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Hosting.Sdl;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuImageDomainPayloadTests
{
    [Fact]
    public void TwoPointSpriteCropsKeepSeparateFlatDomainsInOneDraw()
    {
        (SdlGpuImageDomainVertex[] vertices, SdlGpuDrawingFrameCounters counters) =
            RenderAndCapture((drawing, image) => drawing.DrawSpriteBatch(
                new DrawSpriteBatch(image,
                [
                    new DrawSprite2D(new DrawRect(4, 4, 16, 16),
                        new DrawImageOptions(source: new DrawRect(2, 2, 6, 6),
                            sampling: DrawSamplingMode.Point)),
                    new DrawSprite2D(new DrawRect(24, 4, 16, 16),
                        new DrawImageOptions(source: new DrawRect(12, 12, 6, 6),
                            sampling: DrawSamplingMode.Point))
                ])));

        Assert.Equal(1, counters.DrawCallCount);
        Assert.Equal(8 * 64, counters.VertexBytes);
        Assert.Equal(8, vertices.Length);
        AssertDomain(vertices.AsSpan(0, 4),
            new Vector4(4, 4, 20, 4), new Vector4(20, 20, 4, 20));
        AssertDomain(vertices.AsSpan(4, 4),
            new Vector4(24, 4, 40, 4), new Vector4(40, 20, 24, 20));
        Assert.NotEqual(vertices[0].TextureCoordinate, vertices[4].TextureCoordinate);
    }

    [Fact]
    public void NineSlicePatchesCarryTheWholeLogicalImageDomain()
    {
        (SdlGpuImageDomainVertex[] vertices, SdlGpuDrawingFrameCounters counters) =
            RenderAndCapture((drawing, image) => drawing.DrawNineSlice(
                image,
                new DrawRect(4, 4, 32, 32),
                new DrawInsets(4),
                new DrawImageOptions(source: new DrawRect(2, 2, 16, 16),
                    sampling: DrawSamplingMode.Point)));

        Assert.Equal(1, counters.DrawCallCount);
        Assert.Equal(16 * 64, counters.VertexBytes);
        Assert.Equal(16, vertices.Length);
        AssertDomain(vertices,
            new Vector4(4, 4, 36, 4), new Vector4(36, 36, 4, 36));
    }

    private static void AssertDomain(
        ReadOnlySpan<SdlGpuImageDomainVertex> vertices,
        Vector4 firstCorners,
        Vector4 lastCorners)
    {
        foreach (SdlGpuImageDomainVertex vertex in vertices)
        {
            Assert.Equal(firstCorners, vertex.FirstCorners);
            Assert.Equal(lastCorners, vertex.LastCorners);
        }
    }

    private static (SdlGpuImageDomainVertex[] Vertices, SdlGpuDrawingFrameCounters Counters)
        RenderAndCapture(Action<DrawingContext, SdlGpuImage> record)
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1, CaptureGpuBufferUploads = true };
        nint window = api.CreateWindow("image-domain-payload", 64, 64, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(
                new SdlWindowSurface(window, api.GetWindowId(window)),
                64, 64, coordinateScale: 1));
        byte[] pixels = new byte[20 * 20 * 4];
        for (int offset = 0; offset < pixels.Length; offset += 4)
        {
            pixels[offset] = 64;
            pixels[offset + 1] = 128;
            pixels[offset + 2] = 192;
            pixels[offset + 3] = 255;
        }
        using SdlGpuImage image = new(20, 20, pixels);
        DrawCommandList commands = new();
        record(new DrawingContext(commands), image);
        DrawingFrameContext frame = new(new PrismFrameAnalyzer().Analyze(commands));
        SdlGpuDrawingBackend backend = Assert.IsType<SdlGpuDrawingBackend>(session.DrawingBackend);

        session.BeginFrame(Color.Transparent);
        session.DrawingBackend.Render(commands, in frame);
        session.CompleteFrame(present: false);

        SdlGpuDrawingFrameCounters counters = backend.LastFrameCounters;
        var upload = Assert.Single(api.GpuBufferUploads.Where(upload =>
            api.GpuBuffers[upload.Buffer].CreateInfo.Usage == SdlGpuBufferUsage.Vertex));
        Assert.Equal((uint)counters.VertexBytes, upload.Size);
        ReadOnlySpan<byte> bytes = api.GpuBuffers[upload.Buffer].Data.Span.Slice(
            checked((int)upload.BufferOffset), checked((int)upload.Size));
        return (MemoryMarshal.Cast<byte, SdlGpuImageDomainVertex>(bytes).ToArray(), counters);
    }
}

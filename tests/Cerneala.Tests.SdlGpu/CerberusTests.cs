using System.Reflection;
using System.Numerics;
using System.Runtime.InteropServices;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Hosting.Sdl;

namespace Cerneala.Tests.SdlGpu;

public sealed class CerberusTests
{
    [Fact]
    public void AdjacentIdenticalKeysMergeAndRebaseIndices()
    {
        Cerberus cerberus = new();
        cerberus.Begin(Target(1));

        cerberus.Allocate(3, [0, 1, 2], Key(texture: 1));
        cerberus.Allocate(3, [0, 1, 2], Key(texture: 1));

        Assert.Equal(1, GetIntField(cerberus, "drawCount"));
        Assert.Equal(
            [0, 1, 2, 3, 4, 5],
            GetStorage(cerberus, "indices").Cast<int>().Take(6).ToArray());
    }

    [Fact]
    public void ImageDomainLayoutMergesOnlyAdjacentCompatibleDrawsAndRebasesWithinItsOwnStream()
    {
        Cerberus cerberus = new();
        cerberus.Begin(Target(1));
        CerberusBatchKey ordinary = Key(texture: 1);
        CerberusBatchKey imageDomain = ordinary with { PointClampImageDomain = true };

        cerberus.Allocate(4, [0, 1, 2, 0, 2, 3], ordinary);
        cerberus.AllocateImageDomain(4, [0, 1, 2, 0, 2, 3], imageDomain);
        cerberus.AllocateImageDomain(4, [0, 1, 2, 0, 2, 3], imageDomain);
        cerberus.Allocate(4, [0, 1, 2, 0, 2, 3], ordinary);

        Assert.Equal(3, GetIntField(cerberus, "drawCount"));
        Assert.Equal(8, GetIntField(cerberus, "vertexCount"));
        Assert.Equal(8, GetIntField(cerberus, "imageDomainVertexCount"));
        Assert.Equal(
            [
                0, 1, 2, 0, 2, 3,
                0, 1, 2, 0, 2, 3,
                4, 5, 6, 4, 6, 7,
                0, 1, 2, 0, 2, 3
            ],
            GetStorage(cerberus, "indices").Cast<int>().Take(24).ToArray());
    }

    [Fact]
    public void OrdinaryBatchesLeaveSelectedStorageEmptyUntilFirstImageDomainAllocation()
    {
        Cerberus cerberus = new();
        Assert.Empty(GetStorage(cerberus, "imageDomainVertices"));

        cerberus.Begin(Target(1));
        CerberusBatchKey ordinary = Key(texture: 1);
        CerberusBatchKey imageDomain = ordinary with { PointClampImageDomain = true };
        cerberus.Allocate(4, [0, 1, 2, 0, 2, 3], ordinary);
        Assert.Empty(GetStorage(cerberus, "imageDomainVertices"));

        Span<SdlGpuImageDomainVertex> first = cerberus.AllocateImageDomain(
            4, [0, 1, 2, 0, 2, 3], imageDomain);
        Assert.Equal(4, first.Length);
        Assert.Equal(4, GetStorage(cerberus, "imageDomainVertices").Length);

        Span<SdlGpuImageDomainVertex> second = cerberus.AllocateImageDomain(
            4, [0, 1, 2, 0, 2, 3], imageDomain);
        Assert.Equal(4, second.Length);
        Assert.Equal(8, GetStorage(cerberus, "imageDomainVertices").Length);
        Assert.Equal(8, GetIntField(cerberus, "imageDomainVertexCount"));
        Assert.Equal(2, GetIntField(cerberus, "drawCount"));
        Assert.Equal(
            [
                0, 1, 2, 0, 2, 3,
                0, 1, 2, 0, 2, 3,
                4, 5, 6, 4, 6, 7
            ],
            GetStorage(cerberus, "indices").Cast<int>().Take(18).ToArray());
    }

    [Fact]
    public void ImageDomainCapacityGrowsByHalfAndIsReusedAfterDiscard()
    {
        Cerberus cerberus = new();
        CerberusBatchKey key = Key(texture: 1) with { PointClampImageDomain = true };
        cerberus.Begin(Target(1));
        cerberus.AllocateImageDomain(4, [0, 1, 2], key);
        Assert.Equal(4, GetStorage(cerberus, "imageDomainVertices").Length);
        cerberus.AllocateImageDomain(4, [0, 1, 2], key);
        Assert.Equal(8, GetStorage(cerberus, "imageDomainVertices").Length);
        cerberus.AllocateImageDomain(1, [0], key);
        Array expanded = GetStorage(cerberus, "imageDomainVertices");
        Assert.Equal(12, expanded.Length);

        cerberus.Discard();
        cerberus.Begin(Target(2));
        cerberus.AllocateImageDomain(4, [0, 1, 2], key);

        Assert.Same(expanded, GetStorage(cerberus, "imageDomainVertices"));
    }

    [Fact]
    public void MixedImageDomainAndOrdinaryLayoutsUploadOnceAndBindInOriginalDrawOrder()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1, CaptureGpuBufferUploads = true };
        nint window = api.CreateWindow("cerberus-domain-layout", 64, 48, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(
                new SdlWindowSurface(window, api.GetWindowId(window)),
                64,
                48,
                coordinateScale: 1));
        session.BeginFrame(Color.Transparent);
        SdlGpuTextureResource texture = session.DrawingResources.GetOrCreateTexture(
            session, new object(), 1, 1, [255, 255, 255, 255]);
        CerberusBatchKey ordinary = Key(texture.Handle);
        CerberusBatchKey imageDomain = ordinary with { PointClampImageDomain = true };
        Cerberus cerberus = new();
        cerberus.Begin(session.WindowRenderTarget);

        Span<SdlGpuVertex> firstOrdinary = cerberus.Allocate(
            4, [0, 1, 2, 0, 2, 3], ordinary);
        for (int index = 0; index < firstOrdinary.Length; index++)
        {
            firstOrdinary[index] = new SdlGpuVertex(
                new Vector2(10 + index, 20 + index),
                new Vector2(index / 8f, index / 4f),
                new Vector4(1, 0, 0, 1));
        }
        SdlGpuVertex[] firstExpected = firstOrdinary.ToArray();
        Span<SdlGpuImageDomainVertex> selected = cerberus.AllocateImageDomain(
            4, [0, 1, 2, 0, 2, 3], imageDomain);
        for (int index = 0; index < selected.Length; index++)
        {
            selected[index] = new SdlGpuImageDomainVertex(
                new Vector2(index, index),
                Vector2.Zero,
                Vector4.One,
                new Vector4(0, 0, 1, 0),
                new Vector4(1, 1, 0, 1));
        }
        SdlGpuImageDomainVertex[] selectedExpected = selected.ToArray();
        Span<SdlGpuVertex> lastOrdinary = cerberus.Allocate(
            4, [0, 1, 2, 0, 2, 3], ordinary);
        for (int index = 0; index < lastOrdinary.Length; index++)
        {
            lastOrdinary[index] = new SdlGpuVertex(
                new Vector2(30 + index, 40 + index),
                new Vector2(index / 2f, index / 16f),
                new Vector4(0, 1, 0, 1));
        }
        SdlGpuVertex[] ordinaryExpected = firstExpected.Concat(lastOrdinary.ToArray()).ToArray();

        CerberusFlushMetrics metrics = cerberus.Flush(
            new CerberusExecutionContext(session, session.DrawingResources));

        Assert.Equal(3, metrics.DrawCallCount);
        Assert.Equal((8 * 32) + (4 * 64), metrics.VertexBytes);
        Assert.Equal(12, metrics.VertexCount);
        Assert.Equal(3, metrics.PipelineBindCount);
        Assert.Equal(2, api.GpuBufferUploads.Count);
        string[] vertexBindings = api.GpuActions
            .Where(static action => action.StartsWith("bind-vertex:", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(3, vertexBindings.Length);
        Assert.EndsWith(":0", vertexBindings[0], StringComparison.Ordinal);
        Assert.EndsWith(":256", vertexBindings[1], StringComparison.Ordinal);
        Assert.EndsWith(":0", vertexBindings[2], StringComparison.Ordinal);
        var vertexUpload = Assert.Single(api.GpuBufferUploads.Where(upload =>
            api.GpuBuffers[upload.Buffer].CreateInfo.Usage == SdlGpuBufferUsage.Vertex));
        ReadOnlySpan<byte> uploaded = api.GpuBuffers[vertexUpload.Buffer].Data.Span.Slice(
            checked((int)vertexUpload.BufferOffset), checked((int)vertexUpload.Size));
        byte[] ordinaryBytes = MemoryMarshal.AsBytes(ordinaryExpected.AsSpan()).ToArray();
        byte[] selectedBytes = MemoryMarshal.AsBytes(selectedExpected.AsSpan()).ToArray();
        Assert.Equal(ordinaryBytes.Length + selectedBytes.Length, uploaded.Length);
        Assert.Equal(ordinaryBytes, uploaded[..ordinaryBytes.Length].ToArray());
        Assert.Equal(selectedBytes, uploaded[ordinaryBytes.Length..].ToArray());
        session.CompleteFrame(present: false);
    }

    [Fact]
    public void EveryBatchKeyFieldSeparatesAdjacentDraws()
    {
        CerberusBatchKey baseline = Key(texture: 1);
        CerberusBatchKey[] differentKeys =
        [
            baseline with { Topology = DrawPrimitiveTopology.TriangleStrip },
            baseline with { Texture = 2 },
            baseline with { Sampling = DrawSamplingMode.Linear },
            baseline with { AddressMode = DrawAddressMode.Wrap },
            baseline with { BlendMode = DrawBlendMode.Additive },
            baseline with { StencilMode = SdlGpuStencilMode.Test },
            baseline with { StencilReference = 1 },
            baseline with { Scissor = new SdlRect(1, 0, 63, 48) },
            baseline with { ColorWriteMask = SdlGpuColorWriteMask.Red },
            baseline with { AlphaMask = true },
            baseline with { PrismWorkingColorProfile = PrismColorProfile.LinearSrgb },
            baseline with { PointClampImageDomain = true }
        ];

        foreach (CerberusBatchKey different in differentKeys)
        {
            Cerberus cerberus = new();
            cerberus.Begin(Target(1));
            cerberus.Allocate(3, [0, 1, 2], baseline);
            if (different.PointClampImageDomain)
            {
                cerberus.AllocateImageDomain(3, [0, 1, 2], different);
            }
            else
            {
                cerberus.Allocate(3, [0, 1, 2], different);
            }
            Assert.Equal(2, GetIntField(cerberus, "drawCount"));
            Assert.False(baseline.CanMerge(different));
            Assert.False(different.CanMerge(baseline));
        }
    }

    [Fact]
    public void EqualKeysMergeOnlyForTriangleListsAcrossOptionalStateCombinations()
    {
        PrismColorProfile?[] profiles = [null, .. Enum.GetValues<PrismColorProfile>()
            .Select(static profile => (PrismColorProfile?)profile)];
        foreach (DrawPrimitiveTopology topology in Enum.GetValues<DrawPrimitiveTopology>())
            foreach (PrismColorProfile? profile in profiles)
                foreach (bool alphaMask in new[] { false, true })
                    foreach (bool imageDomain in new[] { false, true })
                    {
                        CerberusBatchKey key = Key(texture: 1) with
                        {
                            Topology = topology,
                            AlphaMask = alphaMask,
                            PrismWorkingColorProfile = profile,
                            PointClampImageDomain = imageDomain
                        };

                        Assert.Equal(topology == DrawPrimitiveTopology.TriangleList,
                            key.CanMerge(key with { }));
                    }
    }

    [Fact]
    public void EmptyFlushPreservesTargetWithoutRequiringExecutionContext()
    {
        Cerberus cerberus = new();
        SdlGpuRenderTarget target = Target(1);
        cerberus.Begin(target);

        Assert.Equal(default, cerberus.Flush(default));
        Assert.Same(target, cerberus.Target);

        cerberus.Discard();
        Assert.Throws<InvalidOperationException>(() => cerberus.Target);
    }

    [Fact]
    public void PipelineFailureResetsBothVertexStreamsAndRetainsTheirStorage()
    {
        FakeSdlApi api = new()
        {
            WindowPixelDensity = 1,
            FailPipelineCreationCount = 1
        };
        nint window = api.CreateWindow("cerberus-emission-failure", 64, 48, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(new SdlWindowSurface(window, api.GetWindowId(window)),
                64, 48, coordinateScale: 1));
        session.BeginFrame(Color.Transparent);
        SdlGpuTextureResource texture = session.DrawingResources.GetOrCreateTexture(
            session, new object(), 1, 1, [255, 255, 255, 255]);
        Cerberus cerberus = new();
        CerberusBatchKey ordinary = Key(texture.Handle);
        CerberusBatchKey domain = ordinary with { PointClampImageDomain = true };
        CerberusExecutionContext context = new(session, session.DrawingResources);
        cerberus.Begin(session.WindowRenderTarget);
        cerberus.Allocate(3, [0, 1, 2], ordinary);
        cerberus.AllocateImageDomain(3, [0, 1, 2], domain);
        Array vertices = GetStorage(cerberus, "vertices");
        Array imageDomainVertices = GetStorage(cerberus, "imageDomainVertices");

        Assert.Throws<InvalidOperationException>(() => cerberus.Flush(context));

        Assert.Throws<InvalidOperationException>(() => cerberus.Target);
        foreach (string counter in new[] { "vertexCount", "imageDomainVertexCount",
            "indexCount", "drawCount", "submissionCount", "mergedSubmissionCount" })
        {
            Assert.Equal(0, GetIntField(cerberus, counter));
        }
        Array packedVertices = GetStorage(cerberus, "packedVertices");
        Assert.Equal((3 * 32) + (3 * 64), packedVertices.Length);
        cerberus.Begin(session.WindowRenderTarget);
        cerberus.Allocate(3, [0, 1, 2], ordinary);
        cerberus.AllocateImageDomain(3, [0, 1, 2], domain);
        CerberusFlushMetrics metrics = cerberus.Flush(context);

        Assert.Equal(2, metrics.SubmissionCount);
        Assert.Equal(2, metrics.DrawCallCount);
        Assert.Equal(6, metrics.VertexCount);
        Assert.Equal(6, metrics.IndexCount);
        Assert.Equal(288, metrics.VertexBytes);
        Assert.Equal(24, metrics.IndexBytes);
        Assert.Equal(2, metrics.PipelineBindCount);
        Assert.Equal(2, metrics.SamplerBindCount);
        Assert.Equal(1, metrics.ScissorSetCount);
        Assert.Equal(1, metrics.StencilReferenceSetCount);
        Assert.Same(vertices, GetStorage(cerberus, "vertices"));
        Assert.Same(imageDomainVertices, GetStorage(cerberus, "imageDomainVertices"));
        Assert.Same(packedVertices, GetStorage(cerberus, "packedVertices"));

        cerberus.Begin(session.WindowRenderTarget);
        cerberus.Allocate(4, [0, 1, 2], ordinary);
        cerberus.AllocateImageDomain(4, [0, 1, 2], domain);
        CerberusFlushMetrics grownMetrics = cerberus.Flush(context);
        Assert.Equal(384, grownMetrics.VertexBytes);
        Assert.Equal(432, GetStorage(cerberus, "packedVertices").Length);
        session.CompleteFrame(present: false);
    }

    [Fact]
    public void ABASequenceRemainsThreeDraws()
    {
        Cerberus cerberus = new();
        cerberus.Begin(Target(1));

        cerberus.Allocate(3, [0, 1, 2], Key(texture: 1));
        cerberus.Allocate(3, [0, 1, 2], Key(texture: 2));
        cerberus.Allocate(3, [0, 1, 2], Key(texture: 1));

        Assert.Equal(3, GetIntField(cerberus, "drawCount"));
    }

    [Fact]
    public void TriangleStripsNeverMerge()
    {
        Cerberus cerberus = new();
        CerberusBatchKey strip = Key(texture: 1) with
        {
            Topology = DrawPrimitiveTopology.TriangleStrip
        };
        cerberus.Begin(Target(1));

        cerberus.Allocate(3, [0, 1, 2], strip);
        cerberus.Allocate(3, [0, 1, 2], strip);

        Assert.Equal(2, GetIntField(cerberus, "drawCount"));
    }

    [Fact]
    public void BeginRejectsNullAndQueuedTargetChanges()
    {
        Cerberus cerberus = new();
        SdlGpuRenderTarget first = Target(1);
        SdlGpuRenderTarget second = Target(2);

        Assert.Throws<ArgumentNullException>(() => cerberus.Begin(null!));
        cerberus.Begin(first);
        cerberus.Allocate(3, [0, 1, 2], Key(texture: 1));

        Assert.Throws<InvalidOperationException>(() => cerberus.Begin(second));
    }

    [Fact]
    public void EmptyAddDoesNotQueueGeometryOrBlockAnotherBegin()
    {
        Cerberus cerberus = new();
        cerberus.Begin(Target(1));

        cerberus.Add(new CerberusBatch(
            [],
            [],
            DrawPrimitiveTopology.TriangleList,
            1,
            DrawSamplingMode.Point,
            DrawAddressMode.Clamp,
            DrawBlendMode.Normal,
            SdlGpuStencilMode.Disabled,
            0,
            new SdlRect(0, 0, 1, 1)));

        cerberus.Begin(Target(2));
    }

    [Fact]
    public void DiscardClearsQueuedGeometryAndTarget()
    {
        Cerberus cerberus = new();
        cerberus.Begin(Target(1));
        cerberus.Allocate(3, [0, 1, 2], Key(texture: 1));

        cerberus.Discard();

        cerberus.Begin(Target(2));
        Assert.Empty(cerberus.Allocate(0, [0], Key(texture: 1)).ToArray());
    }

    [Fact]
    public void FailedFlushResetsQueueForReuse()
    {
        Cerberus cerberus = new();
        cerberus.Allocate(3, [0, 1, 2], Key(texture: 1));

        Assert.Throws<InvalidOperationException>(() => cerberus.Flush(default));

        cerberus.Begin(Target(2));
    }

    [Fact]
    public void ArithmeticOverflowIsRecoverableThroughDiscard()
    {
        Cerberus cerberus = new();
        SetIntField(cerberus, "vertexCount", int.MaxValue);

        Assert.Throws<OverflowException>(() =>
            cerberus.Allocate(1, [0], Key(texture: 1)));

        cerberus.Discard();
        cerberus.Begin(Target(1));
        Assert.Single(cerberus.Allocate(1, [0], Key(texture: 1)).ToArray());
    }

    [Fact]
    public void GrowthPastEveryInitialCapacityIsReusedAfterDiscard()
    {
        Cerberus cerberus = new();
        cerberus.Begin(Target(1));
        for (int draw = 0; draw < 257; draw++)
        {
            cerberus.Allocate(4, [0, 1, 2, 0, 2, 3], Key(texture: draw + 1));
        }
        Array vertices = GetStorage(cerberus, "vertices");
        Array indices = GetStorage(cerberus, "indices");
        Array draws = GetStorage(cerberus, "draws");
        Assert.True(vertices.Length > 1_024);
        Assert.True(indices.Length > 1_536);
        Assert.True(draws.Length > 256);

        cerberus.Discard();
        cerberus.Begin(Target(2));
        cerberus.Allocate(4, [0, 1, 2, 0, 2, 3], Key(texture: 1));

        Assert.Same(vertices, GetStorage(cerberus, "vertices"));
        Assert.Same(indices, GetStorage(cerberus, "indices"));
        Assert.Same(draws, GetStorage(cerberus, "draws"));
    }

    [Fact]
    public void SuccessfulFlushRetainsExpandedVertexAndIndexStorage()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("cerberus-storage", 64, 48, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(
                new SdlWindowSurface(window, api.GetWindowId(window)),
                64,
                48,
                coordinateScale: 1));
        session.BeginFrame(Color.Transparent);
        SdlGpuTextureResource texture = session.DrawingResources.GetOrCreateTexture(
            session,
            new object(),
            1,
            1,
            [255, 255, 255, 255]);
        Cerberus cerberus = new();
        int[] sourceIndices = Enumerable.Range(0, 2_000).ToArray();

        cerberus.Begin(session.WindowRenderTarget);
        cerberus.Allocate(2_000, sourceIndices, Key(texture.Handle));
        Array expandedVertices = GetStorage(cerberus, "vertices");
        Array expandedIndices = GetStorage(cerberus, "indices");

        CerberusFlushMetrics metrics = cerberus.Flush(
            new CerberusExecutionContext(session, session.DrawingResources));

        Assert.Equal(1, metrics.SubmissionCount);
        Assert.Equal(0, metrics.MergedSubmissionCount);
        Assert.Equal(2_000, metrics.VertexCount);
        Assert.Equal(2_000, metrics.IndexCount);
        Assert.Equal(1, metrics.DrawCallCount);
        Assert.Equal(1, metrics.PipelineBindCount);
        Assert.Equal(1, metrics.SamplerBindCount);
        Assert.Equal(1, metrics.ScissorSetCount);
        Assert.Equal(1, metrics.StencilReferenceSetCount);
        cerberus.Begin(session.WindowRenderTarget);
        cerberus.Allocate(3, [0, 1, 2], Key(texture.Handle));
        Assert.Same(expandedVertices, GetStorage(cerberus, "vertices"));
        Assert.Same(expandedIndices, GetStorage(cerberus, "indices"));
        cerberus.Discard();
        session.CompleteFrame(present: false);
    }

    private static Array GetStorage(Cerberus cerberus, string fieldName) =>
        Assert.IsAssignableFrom<Array>(typeof(Cerberus).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(cerberus));

    private static int GetIntField(Cerberus cerberus, string fieldName) =>
        Assert.IsType<int>(typeof(Cerberus).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(cerberus));

    private static void SetIntField(Cerberus cerberus, string fieldName, int value) =>
        typeof(Cerberus).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(cerberus, value);

    private static SdlGpuRenderTarget Target(nint texture) => new(
        texture,
        DepthStencilTexture: texture + 100,
        PixelWidth: 16,
        PixelHeight: 16,
        SdlGpuTextureFormat.R8G8B8A8Unorm,
        SdlGpuSampleCount.One);

    private static CerberusBatchKey Key(nint texture) => new(
        DrawPrimitiveTopology.TriangleList,
        texture,
        DrawSamplingMode.Point,
        DrawAddressMode.Clamp,
        DrawBlendMode.Normal,
        SdlGpuStencilMode.Disabled,
        0,
        new SdlRect(0, 0, 64, 48),
        SdlGpuColorWriteMask.All);
}

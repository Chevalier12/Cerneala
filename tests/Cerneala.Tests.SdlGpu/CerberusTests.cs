using System.Reflection;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
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
            baseline with { ColorWriteMask = SdlGpuColorWriteMask.Red }
        ];

        foreach (CerberusBatchKey different in differentKeys)
        {
            Cerberus cerberus = new();
            cerberus.Begin(Target(1));
            cerberus.Allocate(3, [0, 1, 2], baseline);
            cerberus.Allocate(3, [0, 1, 2], different);
            Assert.Equal(2, GetIntField(cerberus, "drawCount"));
        }
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

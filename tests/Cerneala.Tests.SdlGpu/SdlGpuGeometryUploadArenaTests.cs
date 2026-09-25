using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Platforms.Sdl3;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuGeometryUploadArenaTests
{
    [Fact]
    public void Upload_contract_accepts_distinct_unmanaged_vertex_types()
    {
        MethodInfo[] methods = typeof(SdlGpuGeometryUploadArena).GetMethods(
            BindingFlags.Instance | BindingFlags.Public);

        Assert.Contains(methods, static method =>
            method.Name == nameof(SdlGpuGeometryUploadArena.UploadGeometry) &&
            method.IsGenericMethodDefinition &&
            method.GetGenericArguments().Length == 1);
    }

    [Fact]
    public void PackedMixedLayoutBytesUseTheSameFrameSlotAndPreserveExactPayloads()
    {
        FakeSdlApi api = new() { CaptureGpuBufferUploads = true };
        nint window = api.CreateWindow("packed geometry upload", 8, 8, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window);
        byte[] ordinaryAndSelected = Enumerable.Range(0, (4 * 32) + (4 * 64))
            .Select(static value => checked((byte)(value % 256)))
            .ToArray();
        int[] indices = [0, 1, 2, 3, 4, 5];
        SdlGpuVertex[] following = [new(Vector2.One, Vector2.UnitY, Vector4.One)];

        session.BeginFrame(Color.Transparent);
        try
        {
            SdlGpuGeometryBinding packed = session.GeometryUploadArena.UploadGeometryBytes(
                session, ordinaryAndSelected, indices);
            SdlGpuGeometryBinding next = session.GeometryUploadArena.UploadGeometry<SdlGpuVertex>(
                session, following, [0]);

            Assert.Equal(packed.VertexBuffer, next.VertexBuffer);
            Assert.Equal(packed.IndexBuffer, next.IndexBuffer);
            Assert.Equal(0u, packed.VertexOffset);
            Assert.Equal((uint)ordinaryAndSelected.Length, next.VertexOffset);
            Assert.Equal((uint)(indices.Length * sizeof(int)), next.IndexOffset);
            Assert.Equal(
                ordinaryAndSelected,
                api.GpuBuffers[packed.VertexBuffer].Data.Span
                    .Slice((int)packed.VertexOffset, ordinaryAndSelected.Length).ToArray());
            Assert.Equal(indices, ReadIndices(
                api.GpuBuffers[packed.IndexBuffer].Data.Span,
                packed.IndexOffset,
                indices.Length));
            Assert.Equal(4, api.GpuBufferUploads.Count);
        }
        finally
        {
            session.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void PackedUploadRejectsUnalignedBytesWithoutReservingGpuStorage()
    {
        FakeSdlApi api = new();
        nint window = api.CreateWindow("unaligned packed upload", 8, 8, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window);

        session.BeginFrame(Color.Transparent);
        try
        {
            Assert.Throws<ArgumentException>(() =>
                session.GeometryUploadArena.UploadGeometryBytes(session, new byte[3], [0]));
            Assert.Empty(api.GpuBuffers.Where(static pair =>
                pair.Value.CreateInfo.Usage is SdlGpuBufferUsage.Vertex or SdlGpuBufferUsage.Index));
            Assert.Empty(api.TransferBuffers);
        }
        finally
        {
            session.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void Interleaved_layouts_preserve_bytes_alignment_offsets_and_int32_indices()
    {
        FakeSdlApi api = new() { CaptureGpuBufferUploads = true };
        nint window = api.CreateWindow("interleaved upload", 8, 8, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window);
        SdlGpuVertex[] firstVertices =
        [
            new(Vector2.Zero, Vector2.UnitX, Vector4.One),
            new(Vector2.One, Vector2.UnitY, new Vector4(1, 2, 3, 4))
        ];
        TestVertex[] middleVertices =
        [
            new(new Vector3(1, 2, 3), new Vector4(4, 5, 6, 7)),
            new(new Vector3(8, 9, 10), new Vector4(11, 12, 13, 14)),
            new(new Vector3(15, 16, 17), new Vector4(18, 19, 20, 21))
        ];
        SdlGpuVertex[] lastVertices =
        [
            new(new Vector2(10, 11), new Vector2(12, 13), new Vector4(14, 15, 16, 17))
        ];
        int[] firstIndices = [0, 1, int.MaxValue];
        int[] middleIndices = [2, -1, int.MinValue, 5, 4, 3];
        int[] lastIndices = [unchecked((int)0x89ABCDEF)];

        session.BeginFrame(Color.Transparent);
        try
        {
            SdlGpuGeometryBinding first = session.GeometryUploadArena.UploadGeometry<SdlGpuVertex>(
                session, firstVertices, firstIndices);
            SdlGpuGeometryBinding middle = session.GeometryUploadArena.UploadGeometry<TestVertex>(
                session, middleVertices, middleIndices);
            SdlGpuGeometryBinding last = session.GeometryUploadArena.UploadGeometry<SdlGpuVertex>(
                session, lastVertices, lastIndices);

            Assert.Equal(first.VertexBuffer, middle.VertexBuffer);
            Assert.Equal(first.VertexBuffer, last.VertexBuffer);
            Assert.Equal(first.IndexBuffer, middle.IndexBuffer);
            Assert.Equal(first.IndexBuffer, last.IndexBuffer);
            Assert.Equal(0u, first.VertexOffset);
            Assert.Equal((uint)MemoryMarshal.AsBytes(firstVertices.AsSpan()).Length, middle.VertexOffset);
            Assert.Equal(
                middle.VertexOffset + (uint)MemoryMarshal.AsBytes(middleVertices.AsSpan()).Length,
                last.VertexOffset);
            Assert.Equal(0u, first.IndexOffset);
            Assert.Equal((uint)(firstIndices.Length * sizeof(int)), middle.IndexOffset);
            Assert.Equal(
                middle.IndexOffset + (uint)(middleIndices.Length * sizeof(int)),
                last.IndexOffset);
            Assert.Equal(0u, first.VertexOffset % sizeof(float));
            Assert.Equal(0u, middle.VertexOffset % sizeof(float));
            Assert.Equal(0u, last.VertexOffset % sizeof(float));
            Assert.Equal(0u, first.IndexOffset % sizeof(int));
            Assert.Equal(0u, middle.IndexOffset % sizeof(int));
            Assert.Equal(0u, last.IndexOffset % sizeof(int));

            ReadOnlySpan<byte> vertexData = api.GpuBuffers[first.VertexBuffer].Data.Span;
            Assert.Equal(
                MemoryMarshal.AsBytes(firstVertices.AsSpan()).ToArray(),
                vertexData.Slice((int)first.VertexOffset, firstVertices.Length * 32).ToArray());
            Assert.Equal(
                MemoryMarshal.AsBytes(middleVertices.AsSpan()).ToArray(),
                vertexData.Slice(
                    (int)middle.VertexOffset,
                    MemoryMarshal.AsBytes(middleVertices.AsSpan()).Length).ToArray());
            Assert.Equal(
                MemoryMarshal.AsBytes(lastVertices.AsSpan()).ToArray(),
                vertexData.Slice((int)last.VertexOffset, lastVertices.Length * 32).ToArray());

            ReadOnlySpan<byte> indexData = api.GpuBuffers[first.IndexBuffer].Data.Span;
            Assert.Equal(firstIndices, ReadIndices(indexData, first.IndexOffset, firstIndices.Length));
            Assert.Equal(middleIndices, ReadIndices(indexData, middle.IndexOffset, middleIndices.Length));
            Assert.Equal(lastIndices, ReadIndices(indexData, last.IndexOffset, lastIndices.Length));

            string[] maps = api.GpuActions.Where(static action => action.StartsWith("map-transfer:")).ToArray();
            string[] uploads = api.GpuActions.Where(static action => action.StartsWith("upload-buffer:")).ToArray();
            Assert.Equal(3, maps.Length);
            Assert.All(maps, static action => Assert.EndsWith(":False", action));
            Assert.Equal(6, uploads.Length);
            Assert.All(uploads, static action => Assert.EndsWith(":False", action));
            Assert.All(api.GpuBufferUploads, static upload =>
            {
                Assert.Equal(0u, upload.TransferOffset % sizeof(float));
                Assert.Equal(0u, upload.BufferOffset % sizeof(float));
                Assert.False(upload.Cycle);
            });
            Assert.Equal(
                [first.VertexOffset, first.IndexOffset, middle.VertexOffset,
                    middle.IndexOffset, last.VertexOffset, last.IndexOffset],
                api.GpuBufferUploads.Select(static upload => upload.BufferOffset));
        }
        finally
        {
            session.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void Empty_invalid_and_overflowing_upload_inputs_are_rejected()
    {
        FakeSdlApi api = new();
        nint window = api.CreateWindow("invalid upload", 8, 8, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window);
        SdlGpuVertex[] vertices = [new(Vector2.Zero, Vector2.Zero, Vector4.One)];
        int[] indices = [0];

        Assert.Throws<ArgumentNullException>(() =>
            session.GeometryUploadArena.UploadGeometry<SdlGpuVertex>(null!, vertices, indices));
        Assert.Throws<InvalidOperationException>(() =>
            session.GeometryUploadArena.UploadGeometry<SdlGpuVertex>(session, vertices, indices));
        Assert.Throws<OverflowException>(() => SdlGpuGeometryUploadArena.AlignOffset(uint.MaxValue));

        session.BeginFrame(Color.Transparent);
        try
        {
            Assert.Throws<ArgumentException>(() =>
                session.GeometryUploadArena.UploadGeometry<SdlGpuVertex>(session, [], indices));
            Assert.Throws<ArgumentException>(() =>
                session.GeometryUploadArena.UploadGeometry<SdlGpuVertex>(session, vertices, []));
            Assert.Throws<ArgumentException>(() =>
                session.GeometryUploadArena.UploadGeometry<PackedVertex>(
                    session,
                    [new PackedVertex(1, 2)],
                    indices));
            Assert.Empty(api.GpuBuffers.Where(static pair =>
                pair.Value.CreateInfo.Usage is SdlGpuBufferUsage.Vertex or SdlGpuBufferUsage.Index));
            Assert.Empty(api.TransferBuffers);
        }
        finally
        {
            session.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void Byte_count_and_offset_overflow_preserve_existing_data_and_allow_recovery()
    {
        FakeSdlApi api = new() { CaptureGpuBufferUploads = true };
        nint window = api.CreateWindow("overflowing upload", 8, 8, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window);
        SdlGpuVertex[] vertices = [new(Vector2.One, Vector2.UnitY, Vector4.One)];
        int[] indices = [17];

        session.BeginFrame(Color.Transparent);
        try
        {
            Assert.Throws<OverflowException>(() =>
                UploadOversizedVertices(session.GeometryUploadArena, session, indices));
            Assert.Empty(api.GpuBuffers.Where(static pair =>
                pair.Value.CreateInfo.Usage is SdlGpuBufferUsage.Vertex or SdlGpuBufferUsage.Index));
            Assert.Empty(api.TransferBuffers);

            SdlGpuGeometryBinding stable = session.GeometryUploadArena.UploadGeometry<SdlGpuVertex>(
                session, vertices, indices);
            byte[] stableVertexBytes = MemoryMarshal.AsBytes(vertices.AsSpan()).ToArray();
            byte[] stableIndexBytes = MemoryMarshal.AsBytes(indices.AsSpan()).ToArray();
            int uploadCount = api.GpuBufferUploads.Count;
            SetActiveSlotOffset(session.GeometryUploadArena, "TransferOffset", uint.MaxValue - 3);

            Assert.Throws<OverflowException>(() =>
                session.GeometryUploadArena.UploadGeometry<SdlGpuVertex>(session, vertices, indices));
            Assert.Equal(uploadCount, api.GpuBufferUploads.Count);
            Assert.Equal(
                stableVertexBytes,
                api.GpuBuffers[stable.VertexBuffer].Data.Span[..stableVertexBytes.Length].ToArray());
            Assert.Equal(
                stableIndexBytes,
                api.GpuBuffers[stable.IndexBuffer].Data.Span[..stableIndexBytes.Length].ToArray());

            SetActiveSlotOffset(
                session.GeometryUploadArena,
                "TransferOffset",
                checked((uint)(stableVertexBytes.Length + stableIndexBytes.Length)));
            SdlGpuGeometryBinding recovered =
                session.GeometryUploadArena.UploadGeometry<SdlGpuVertex>(session, vertices, indices);

            Assert.Equal(32u, recovered.VertexOffset);
            Assert.Equal(4u, recovered.IndexOffset);
            Assert.Equal(
                stableVertexBytes,
                api.GpuBuffers[recovered.VertexBuffer].Data.Span
                    .Slice((int)recovered.VertexOffset, stableVertexBytes.Length).ToArray());
            Assert.Equal(indices, ReadIndices(
                api.GpuBuffers[recovered.IndexBuffer].Data.Span,
                recovered.IndexOffset,
                indices.Length));
        }
        finally
        {
            session.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void Growth_is_isolated_across_all_three_frame_slots_and_the_next_frame_reuses_its_slot()
    {
        FakeSdlApi api = new();
        nint window = api.CreateWindow("frame ring growth", 8, 8, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window);
        SdlGpuVertex[] smallVertices = [new(Vector2.Zero, Vector2.Zero, Vector4.One)];
        int[] smallIndices = [0];
        TestVertex[] largeVertices = new TestVertex[5_000];
        int[] largeIndices = Enumerable.Range(0, 20_000).ToArray();
        List<SdlGpuGeometryBinding> grownBindings = [];
        List<nint> grownTransfers = [];

        for (int frame = 0; frame < 3; frame++)
        {
            session.BeginFrame(Color.Transparent);
            SdlGpuGeometryBinding small = session.GeometryUploadArena.UploadGeometry<SdlGpuVertex>(
                session, smallVertices, smallIndices);
            nint smallTransfer = Assert.Single(api.TransferBuffers.Keys.Except(grownTransfers));
            SdlGpuGeometryBinding grown = session.GeometryUploadArena.UploadGeometry<TestVertex>(
                session, largeVertices, largeIndices);
            nint grownTransfer = Assert.Single(api.TransferBuffers.Keys
                .Except(grownTransfers)
                .Where(handle => handle != smallTransfer));

            Assert.NotEqual(small.VertexBuffer, grown.VertexBuffer);
            Assert.NotEqual(small.IndexBuffer, grown.IndexBuffer);
            Assert.Contains(small.VertexBuffer, api.GpuBuffers.Keys);
            Assert.Contains(small.IndexBuffer, api.GpuBuffers.Keys);
            Assert.Contains(smallTransfer, api.TransferBuffers.Keys);
            Assert.True(api.GpuBuffers[grown.VertexBuffer].CreateInfo.Size >= 140_000);
            Assert.True(api.GpuBuffers[grown.IndexBuffer].CreateInfo.Size >= 80_000);
            Assert.True(api.TransferBuffers[grownTransfer].Size >= 220_000);
            Assert.Equal(0u, grown.VertexOffset);
            Assert.Equal(0u, grown.IndexOffset);
            Assert.Equal(
                MemoryMarshal.AsBytes(largeVertices.AsSpan()).ToArray(),
                api.GpuBuffers[grown.VertexBuffer].Data.Span[..140_000].ToArray());
            Assert.Equal(
                largeIndices,
                ReadIndices(api.GpuBuffers[grown.IndexBuffer].Data.Span, 0, largeIndices.Length));

            session.CompleteFrame(present: false);
            Assert.DoesNotContain(small.VertexBuffer, api.GpuBuffers.Keys);
            Assert.DoesNotContain(small.IndexBuffer, api.GpuBuffers.Keys);
            Assert.DoesNotContain(smallTransfer, api.TransferBuffers.Keys);
            Assert.Contains(grown.VertexBuffer, api.GpuBuffers.Keys);
            Assert.Contains(grown.IndexBuffer, api.GpuBuffers.Keys);
            Assert.Contains(grownTransfer, api.TransferBuffers.Keys);
            grownBindings.Add(grown);
            grownTransfers.Add(grownTransfer);
        }

        Assert.Equal(3, grownBindings.Select(static binding => binding.VertexBuffer).Distinct().Count());
        Assert.Equal(3, grownBindings.Select(static binding => binding.IndexBuffer).Distinct().Count());
        Assert.Equal(3, grownTransfers.Distinct().Count());
        int createdBufferCount = api.GpuActions.Count(static action => action.StartsWith("create-buffer:"));
        int createdTransferCount = api.GpuActions.Count(static action => action.StartsWith("create-transfer:"));

        session.BeginFrame(Color.Transparent);
        SdlGpuGeometryBinding reused = session.GeometryUploadArena.UploadGeometry<SdlGpuVertex>(
            session, smallVertices, smallIndices);
        session.CompleteFrame(present: false);

        Assert.Equal(grownBindings[0].VertexBuffer, reused.VertexBuffer);
        Assert.Equal(grownBindings[0].IndexBuffer, reused.IndexBuffer);
        Assert.Equal(0u, reused.VertexOffset);
        Assert.Equal(0u, reused.IndexOffset);
        Assert.Equal(createdBufferCount,
            api.GpuActions.Count(static action => action.StartsWith("create-buffer:")));
        Assert.Equal(createdTransferCount,
            api.GpuActions.Count(static action => action.StartsWith("create-transfer:")));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Failed_map_or_upload_does_not_reuse_reserved_regions(bool failMap)
    {
        FakeSdlApi api = new();
        nint window = api.CreateWindow("failed upload", 8, 8, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window);
        SdlGpuVertex[] stableVertices = [new(Vector2.One, Vector2.UnitX, Vector4.One)];
        TestVertex[] failedVertices =
            [new(new Vector3(3, 4, 5), new Vector4(6, 7, 8, 9))];
        int[] indices = [7];

        session.BeginFrame(Color.Transparent);
        try
        {
            SdlGpuGeometryBinding stable = session.GeometryUploadArena.UploadGeometry<SdlGpuVertex>(
                session, stableVertices, indices);
            if (failMap)
            {
                api.FailTransferBufferMapCount = 1;
            }
            else
            {
                api.FailBufferUploadCount = 1;
            }

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                session.GeometryUploadArena.UploadGeometry<TestVertex>(
                    session, failedVertices, indices));
            if (failMap)
            {
                Assert.Contains("mapping", exception.Message, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                Assert.Equal("Configured fake GPU buffer upload failure.", exception.Message);
            }
            SdlGpuGeometryBinding afterFailure = session.GeometryUploadArena.UploadGeometry<SdlGpuVertex>(
                session, stableVertices, indices);

            Assert.Equal(stable.VertexBuffer, afterFailure.VertexBuffer);
            Assert.Equal(stable.IndexBuffer, afterFailure.IndexBuffer);
            Assert.Equal(60u, afterFailure.VertexOffset);
            Assert.Equal(8u, afterFailure.IndexOffset);
            ReadOnlySpan<byte> vertexData = api.GpuBuffers[stable.VertexBuffer].Data.Span;
            byte[] expected = MemoryMarshal.AsBytes(stableVertices.AsSpan()).ToArray();
            Assert.Equal(expected, vertexData.Slice(0, expected.Length).ToArray());
            Assert.Equal(expected,
                vertexData.Slice((int)afterFailure.VertexOffset, expected.Length).ToArray());
        }
        finally
        {
            session.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void Failed_index_upload_keeps_reservations_and_existing_payloads_before_recovery()
    {
        FakeSdlApi api = new() { CaptureGpuBufferUploads = true };
        nint window = api.CreateWindow("failed index upload", 8, 8, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window);
        SdlGpuVertex[] stableVertices = [new(Vector2.One, Vector2.UnitX, Vector4.One)];
        TestVertex[] failedVertices =
            [new(new Vector3(3, 4, 5), new Vector4(6, 7, 8, 9))];
        int[] indices = [7];

        session.BeginFrame(Color.Transparent);
        try
        {
            SdlGpuGeometryBinding stable = session.GeometryUploadArena.UploadGeometry<SdlGpuVertex>(
                session, stableVertices, indices);
            api.FailBufferUploadAtAttempt = api.BufferUploadAttemptCount + 2;

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                session.GeometryUploadArena.UploadGeometry<TestVertex>(
                    session, failedVertices, indices));
            Assert.Equal("Configured fake GPU buffer upload failure.", exception.Message);
            Assert.Equal(4, api.BufferUploadAttemptCount);
            Assert.Contains(api.GpuBufferUploads, upload =>
                upload.Buffer == stable.VertexBuffer &&
                upload.BufferOffset == 32u &&
                upload.Size == 28u);
            Assert.DoesNotContain(api.GpuBufferUploads, upload =>
                upload.Buffer == stable.IndexBuffer && upload.BufferOffset == 4u);

            SdlGpuGeometryBinding recovered =
                session.GeometryUploadArena.UploadGeometry<SdlGpuVertex>(
                    session, stableVertices, indices);

            Assert.Equal(60u, recovered.VertexOffset);
            Assert.Equal(8u, recovered.IndexOffset);
            ReadOnlySpan<byte> vertexData = api.GpuBuffers[stable.VertexBuffer].Data.Span;
            byte[] stableBytes = MemoryMarshal.AsBytes(stableVertices.AsSpan()).ToArray();
            byte[] failedBytes = MemoryMarshal.AsBytes(failedVertices.AsSpan()).ToArray();
            Assert.Equal(stableBytes, vertexData[..stableBytes.Length].ToArray());
            Assert.Equal(failedBytes, vertexData.Slice(32, failedBytes.Length).ToArray());
            Assert.Equal(
                stableBytes,
                vertexData.Slice((int)recovered.VertexOffset, stableBytes.Length).ToArray());
            ReadOnlySpan<byte> indexData = api.GpuBuffers[stable.IndexBuffer].Data.Span;
            Assert.Equal(indices, ReadIndices(indexData, stable.IndexOffset, indices.Length));
            Assert.Equal(indices, ReadIndices(indexData, recovered.IndexOffset, indices.Length));
        }
        finally
        {
            session.CompleteFrame(present: false);
        }
    }

    [Fact]
    public void Repeated_dispose_releases_owned_upload_buffers_once()
    {
        FakeSdlApi api = new();
        nint window = api.CreateWindow("upload disposal", 8, 8, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api);
        SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window);
        SdlGpuVertex[] vertices = [new(Vector2.Zero, Vector2.Zero, Vector4.One)];
        int[] indices = [0];
        session.BeginFrame(Color.Transparent);
        SdlGpuGeometryBinding binding = session.GeometryUploadArena.UploadGeometry<SdlGpuVertex>(
            session, vertices, indices);
        nint transfer = Assert.Single(api.TransferBuffers.Keys);
        session.CompleteFrame(present: false);

        session.Dispose();
        session.Dispose();

        Assert.DoesNotContain(binding.VertexBuffer, api.GpuBuffers.Keys);
        Assert.DoesNotContain(binding.IndexBuffer, api.GpuBuffers.Keys);
        Assert.DoesNotContain(transfer, api.TransferBuffers.Keys);
        Assert.Single(api.GpuActions.Where(action => action == $"release-buffer:{binding.VertexBuffer}"));
        Assert.Single(api.GpuActions.Where(action => action == $"release-buffer:{binding.IndexBuffer}"));
        Assert.Single(api.GpuActions.Where(action => action == $"release-transfer:{transfer}"));
    }

    private static SdlGpuWindowGraphicsSession CreateSession(
        SdlGpuWindowGraphicsSessionFactory factory,
        FakeSdlApi api,
        nint window) =>
        Assert.IsType<SdlGpuWindowGraphicsSession>(factory.Create(
            new SdlWindowSurface(window, api.GetWindowId(window)),
            pixelWidth: 8,
            pixelHeight: 8,
            coordinateScale: 1));

    private static int[] ReadIndices(
        ReadOnlySpan<byte> data,
        uint byteOffset,
        int count) =>
        MemoryMarshal.Cast<byte, int>(
            data.Slice(checked((int)byteOffset), checked(count * sizeof(int)))).ToArray();

    private static void UploadOversizedVertices(
        SdlGpuGeometryUploadArena arena,
        SdlGpuWindowGraphicsSession session,
        ReadOnlySpan<int> indices)
    {
        ref OverflowVertex nullVertex = ref Unsafe.NullRef<OverflowVertex>();
        ReadOnlySpan<OverflowVertex> vertices =
            MemoryMarshal.CreateReadOnlySpan(ref nullVertex, int.MaxValue);
        arena.UploadGeometry<OverflowVertex>(session, vertices, indices);
    }

    private static void SetActiveSlotOffset(
        SdlGpuGeometryUploadArena arena,
        string fieldName,
        uint value)
    {
        FieldInfo slotsField = typeof(SdlGpuGeometryUploadArena).GetField(
            "slots",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        FieldInfo activeSlotIndexField = typeof(SdlGpuGeometryUploadArena).GetField(
            "activeSlotIndex",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        Array slots = (Array)slotsField.GetValue(arena)!;
        int activeSlotIndex = (int)activeSlotIndexField.GetValue(arena)!;
        object slot = slots.GetValue(activeSlotIndex)!;
        FieldInfo offsetField = slot.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.Public)!;
        offsetField.SetValue(slot, value);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private readonly struct TestVertex(Vector3 position, Vector4 color)
    {
        public readonly Vector3 Position = position;
        public readonly Vector4 Color = color;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly struct PackedVertex(byte x, ushort y)
    {
        public readonly byte X = x;
        public readonly ushort Y = y;
    }

    private readonly struct OverflowVertex(uint value)
    {
        public readonly uint Value = value;
    }
}

using System.Runtime.InteropServices;
using Cerneala.Platforms.Sdl3;

namespace Cerneala.Backends.SdlGpu;

internal sealed class SdlGpuGeometryUploadArena : IDisposable
{
    private const int FrameSlotCount = 3;
    private const uint InitialCapacity = 64 * 1024;
    private const uint BufferOffsetAlignment = sizeof(float);
    private readonly ISdlApi api;
    private readonly nint device;
    private readonly FrameSlot[] slots = new FrameSlot[FrameSlotCount];
    private readonly List<nint> retiredBuffers = [];
    private readonly List<nint> retiredTransferBuffers = [];
    private int activeSlotIndex = -1;
    private bool disposed;

    public SdlGpuGeometryUploadArena(ISdlApi api, nint device)
    {
        this.api = api ?? throw new ArgumentNullException(nameof(api));
        this.device = device != 0
            ? device
            : throw new ArgumentOutOfRangeException(nameof(device));
        for (int index = 0; index < slots.Length; index++)
        {
            slots[index] = new FrameSlot();
        }
    }

    public void BeginFrame(long frameToken)
    {
        ThrowIfDisposed();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(frameToken);
        activeSlotIndex = checked((int)((frameToken - 1) % FrameSlotCount));
        slots[activeSlotIndex].ResetOffsets();
    }

    public unsafe SdlGpuGeometryBinding UploadGeometry<TVertex>(
        SdlGpuWindowGraphicsSession session,
        ReadOnlySpan<TVertex> vertices,
        ReadOnlySpan<int> indices)
        where TVertex : unmanaged
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(session);
        if (activeSlotIndex < 0)
        {
            throw new InvalidOperationException(
                "SDL GPU geometry uploads require an active frame slot.");
        }
        if (vertices.IsEmpty || indices.IsEmpty)
        {
            throw new ArgumentException("GPU geometry cannot be empty.");
        }
        if (sizeof(TVertex) % BufferOffsetAlignment != 0)
        {
            throw new ArgumentException(
                "GPU vertex strides must be aligned to four bytes.",
                nameof(vertices));
        }

        FrameSlot slot = slots[activeSlotIndex];
        ReadOnlySpan<byte> vertexBytes = MemoryMarshal.AsBytes(vertices);
        ReadOnlySpan<byte> indexBytes = MemoryMarshal.AsBytes(indices);
        uint vertexByteCount = checked((uint)vertexBytes.Length);
        uint indexByteCount = checked((uint)indexBytes.Length);
        uint requiredVertexEnd = checked(AlignOffset(slot.VertexOffset) + vertexByteCount);
        uint requiredIndexEnd = checked(AlignOffset(slot.IndexOffset) + indexByteCount);
        uint requiredTransferOffset = AlignOffset(slot.TransferOffset);
        uint requiredTransferIndexOffset = AlignOffset(
            checked(requiredTransferOffset + vertexByteCount));
        uint requiredTransferEnd = checked(requiredTransferIndexOffset + indexByteCount);
        EnsureVertexCapacity(slot, requiredVertexEnd);
        EnsureIndexCapacity(slot, requiredIndexEnd);
        EnsureTransferCapacity(slot, requiredTransferEnd);

        uint vertexOffset = AlignOffset(slot.VertexOffset);
        uint indexOffset = AlignOffset(slot.IndexOffset);
        uint transferOffset = AlignOffset(slot.TransferOffset);
        uint transferIndexOffset = AlignOffset(checked(transferOffset + vertexByteCount));
        uint nextVertexOffset = checked(vertexOffset + vertexByteCount);
        uint nextIndexOffset = checked(indexOffset + indexByteCount);
        uint nextTransferOffset = checked(transferIndexOffset + indexByteCount);

        slot.VertexOffset = nextVertexOffset;
        slot.IndexOffset = nextIndexOffset;
        slot.TransferOffset = nextTransferOffset;

        nint mapped = RequireHandle(
            api.MapGpuTransferBuffer(device, slot.TransferBuffer, cycle: false),
            "SDL GPU geometry upload-buffer mapping");
        try
        {
            nint destination = mapped + checked((int)transferOffset);
            CopyToUnmanaged(vertexBytes, destination);
            CopyToUnmanaged(
                indexBytes,
                mapped + checked((int)transferIndexOffset));
        }
        finally
        {
            api.UnmapGpuTransferBuffer(device, slot.TransferBuffer);
        }

        session.RunCopyPass(
            (Arena: this, Slot: slot, TransferOffset: transferOffset,
                TransferIndexOffset: transferIndexOffset,
                VertexOffset: vertexOffset, IndexOffset: indexOffset,
                VertexByteCount: vertexByteCount, IndexByteCount: indexByteCount),
            static (copyPass, upload) =>
        {
            upload.Arena.api.UploadToGpuBuffer(
                copyPass,
                upload.Slot.TransferBuffer,
                upload.TransferOffset,
                upload.Slot.VertexBuffer,
                upload.VertexOffset,
                upload.VertexByteCount,
                cycle: false);
            upload.Arena.api.UploadToGpuBuffer(
                copyPass,
                upload.Slot.TransferBuffer,
                upload.TransferIndexOffset,
                upload.Slot.IndexBuffer,
                upload.IndexOffset,
                upload.IndexByteCount,
                cycle: false);
        });
        return new SdlGpuGeometryBinding(
            slot.VertexBuffer,
            slot.IndexBuffer,
            vertexOffset,
            indexOffset);
    }

    public void FlushRetired()
    {
        ThrowIfDisposed();
        foreach (nint buffer in retiredBuffers)
        {
            api.ReleaseGpuBuffer(device, buffer);
        }
        retiredBuffers.Clear();
        foreach (nint transferBuffer in retiredTransferBuffers)
        {
            api.ReleaseGpuTransferBuffer(device, transferBuffer);
        }
        retiredTransferBuffers.Clear();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        FlushRetired();
        disposed = true;
        foreach (FrameSlot slot in slots)
        {
            if (slot.VertexBuffer != 0)
            {
                api.ReleaseGpuBuffer(device, slot.VertexBuffer);
            }
            if (slot.IndexBuffer != 0)
            {
                api.ReleaseGpuBuffer(device, slot.IndexBuffer);
            }
            if (slot.TransferBuffer != 0)
            {
                api.ReleaseGpuTransferBuffer(device, slot.TransferBuffer);
            }
        }
    }

    private void EnsureVertexCapacity(FrameSlot slot, uint required)
    {
        if (required <= slot.VertexCapacity)
        {
            return;
        }
        uint capacity = GrowCapacity(slot.VertexCapacity, required);
        nint created = RequireHandle(
            api.CreateGpuBuffer(
                device,
                new SdlGpuBufferCreateInfo(SdlGpuBufferUsage.Vertex, capacity)),
            "SDL GPU geometry vertex-buffer creation");
        if (slot.VertexBuffer != 0)
        {
            retiredBuffers.Add(slot.VertexBuffer);
        }
        slot.VertexBuffer = created;
        slot.VertexCapacity = capacity;
        slot.VertexOffset = 0;
    }

    private void EnsureIndexCapacity(FrameSlot slot, uint required)
    {
        if (required <= slot.IndexCapacity)
        {
            return;
        }
        uint capacity = GrowCapacity(slot.IndexCapacity, required);
        nint created = RequireHandle(
            api.CreateGpuBuffer(
                device,
                new SdlGpuBufferCreateInfo(SdlGpuBufferUsage.Index, capacity)),
            "SDL GPU geometry index-buffer creation");
        if (slot.IndexBuffer != 0)
        {
            retiredBuffers.Add(slot.IndexBuffer);
        }
        slot.IndexBuffer = created;
        slot.IndexCapacity = capacity;
        slot.IndexOffset = 0;
    }

    private void EnsureTransferCapacity(FrameSlot slot, uint required)
    {
        if (required <= slot.TransferCapacity)
        {
            return;
        }
        uint capacity = GrowCapacity(slot.TransferCapacity, required);
        nint created = RequireHandle(
            api.CreateGpuTransferBuffer(
                device,
                new SdlGpuTransferBufferCreateInfo(
                    SdlGpuTransferBufferUsage.Upload,
                    capacity)),
            "SDL GPU geometry transfer-buffer creation");
        if (slot.TransferBuffer != 0)
        {
            retiredTransferBuffers.Add(slot.TransferBuffer);
        }
        slot.TransferBuffer = created;
        slot.TransferCapacity = capacity;
        slot.TransferOffset = 0;
    }

    private static uint GrowCapacity(uint current, uint required)
    {
        uint next = Math.Max(current, InitialCapacity);
        while (next < required)
        {
            next = checked(next * 2);
        }
        return next;
    }

    internal static uint AlignOffset(uint offset) =>
        checked((offset + BufferOffsetAlignment - 1) & ~(BufferOffsetAlignment - 1));

    private nint RequireHandle(nint handle, string operation) =>
        handle != 0 ? handle : throw SdlApiError.Create(api, operation);

    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(disposed, this);

    private static unsafe void CopyToUnmanaged(
        ReadOnlySpan<byte> source,
        nint destination)
    {
        source.CopyTo(new Span<byte>(destination.ToPointer(), source.Length));
    }

    private sealed class FrameSlot
    {
        public nint VertexBuffer;
        public nint IndexBuffer;
        public nint TransferBuffer;
        public uint VertexCapacity;
        public uint IndexCapacity;
        public uint TransferCapacity;
        public uint VertexOffset;
        public uint IndexOffset;
        public uint TransferOffset;
        public void ResetOffsets()
        {
            VertexOffset = 0;
            IndexOffset = 0;
            TransferOffset = 0;
        }
    }
}

internal readonly record struct SdlGpuGeometryBinding(
    nint VertexBuffer,
    nint IndexBuffer,
    uint VertexOffset,
    uint IndexOffset);

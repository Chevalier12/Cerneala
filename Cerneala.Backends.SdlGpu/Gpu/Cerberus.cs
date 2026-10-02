using System.Runtime.InteropServices;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Platforms.Sdl3;

namespace Cerneala.Backends.SdlGpu;

internal sealed class Cerberus
{
    private const int InitialBatchCapacity = 256;
    private const int InitialVertexCapacity = InitialBatchCapacity * 4;
    private const int InitialIndexCapacity = InitialBatchCapacity * 6;
    private SdlGpuVertex[] vertices = new SdlGpuVertex[InitialVertexCapacity];
    private SdlGpuImageDomainVertex[] imageDomainVertices = [];
    private byte[] packedVertices = [];
    private int[] indices = new int[InitialIndexCapacity];
    private CerberusGpuDraw[] draws = new CerberusGpuDraw[InitialBatchCapacity];
    private SdlGpuRenderTarget? target;
    private int vertexCount;
    private int imageDomainVertexCount;
    private int indexCount;
    private int drawCount;
    private int submissionCount;
    private int mergedSubmissionCount;

    internal SdlGpuRenderTarget Target => target ??
        throw new InvalidOperationException("SDL_GPU batching requires an active target.");

    public void Begin(SdlGpuRenderTarget nextTarget)
    {
        ArgumentNullException.ThrowIfNull(nextTarget);
        if (vertexCount != 0 || imageDomainVertexCount != 0 ||
            indexCount != 0 || drawCount != 0)
        {
            throw new InvalidOperationException(
                "An SDL_GPU batch target cannot change before its queued geometry is flushed.");
        }
        target = nextTarget;
    }

    public void Add(CerberusBatch batch)
    {
        if (batch.Indices.Length == 0 || batch.Vertices.Length == 0)
        {
            return;
        }
        Span<SdlGpuVertex> destination = Allocate(
            batch.Vertices.Length,
            batch.Indices,
            CerberusBatchKey.From(batch));
        batch.Vertices.AsSpan().CopyTo(destination);
    }

    public Span<SdlGpuVertex> Allocate(
        int nextVertexCount,
        ReadOnlySpan<int> sourceIndices,
        CerberusBatchKey key)
    {
        if (nextVertexCount <= 0 || sourceIndices.IsEmpty)
        {
            return Span<SdlGpuVertex>.Empty;
        }
        if (key.PointClampImageDomain)
        {
            throw new ArgumentException(
                "Image-domain draws require image-domain vertices.", nameof(key));
        }

        int vertexStart = Reserve(nextVertexCount, sourceIndices, key);
        return vertices.AsSpan(vertexStart, nextVertexCount);
    }

    public Span<SdlGpuImageDomainVertex> AllocateImageDomain(
        int nextVertexCount,
        ReadOnlySpan<int> sourceIndices,
        CerberusBatchKey key)
    {
        if (nextVertexCount <= 0 || sourceIndices.IsEmpty)
        {
            return Span<SdlGpuImageDomainVertex>.Empty;
        }
        if (!key.PointClampImageDomain ||
            key.Sampling != DrawSamplingMode.Point ||
            key.AddressMode != DrawAddressMode.Clamp ||
            key.PrismWorkingColorProfile is not null)
        {
            throw new ArgumentException(
                "Image-domain vertices require Point+Clamp image-draw provenance.", nameof(key));
        }

        int vertexStart = Reserve(nextVertexCount, sourceIndices, key);
        return imageDomainVertices.AsSpan(vertexStart, nextVertexCount);
    }

    private int Reserve(
        int nextVertexCount,
        ReadOnlySpan<int> sourceIndices,
        CerberusBatchKey key)
    {
        bool imageDomain = key.PointClampImageDomain;
        int vertexStart = imageDomain ? imageDomainVertexCount : vertexCount;
        int indexStart = indexCount;
        int requiredVertexCount = checked(vertexStart + nextVertexCount);
        int requiredIndexCount = checked(indexCount + sourceIndices.Length);
        if (imageDomain)
        {
            EnsureCapacity(ref imageDomainVertices, requiredVertexCount);
        }
        else
        {
            EnsureCapacity(ref vertices, requiredVertexCount);
        }
        EnsureCapacity(ref indices, requiredIndexCount);
        submissionCount = checked(submissionCount + 1);

        int drawVertexOffset;
        if (drawCount > 0 && draws[drawCount - 1].Key.CanMerge(key))
        {
            mergedSubmissionCount = checked(mergedSubmissionCount + 1);
            int last = drawCount - 1;
            CerberusGpuDraw draw = draws[last];
            drawVertexOffset = draw.VertexOffset;
            draws[last] = draw with
            {
                IndexCount = checked(draw.IndexCount + sourceIndices.Length)
            };
        }
        else
        {
            EnsureCapacity(ref draws, checked(drawCount + 1));
            drawVertexOffset = vertexStart;
            draws[drawCount++] = new CerberusGpuDraw(
                key,
                vertexStart,
                indexStart,
                sourceIndices.Length);
        }

        int relativeVertexOffset = vertexStart - drawVertexOffset;
        for (int index = 0; index < sourceIndices.Length; index++)
        {
            indices[indexStart + index] = checked(
                sourceIndices[index] + relativeVertexOffset);
        }
        if (imageDomain)
        {
            imageDomainVertexCount = requiredVertexCount;
        }
        else
        {
            vertexCount = requiredVertexCount;
        }
        indexCount = requiredIndexCount;
        return vertexStart;
    }

    public CerberusFlushMetrics Flush(CerberusExecutionContext context)
    {
        if (drawCount == 0)
        {
            return default;
        }

        try
        {
            SdlGpuRenderTarget activeTarget = target ??
                throw new InvalidOperationException("SDL_GPU batching requires an active target.");
            SdlGpuWindowGraphicsSession session = context.Session;
            int ordinaryVertexBytes =
                MemoryMarshal.AsBytes(vertices.AsSpan(0, vertexCount)).Length;
            int imageDomainVertexBytes =
                MemoryMarshal.AsBytes(imageDomainVertices.AsSpan(0, imageDomainVertexCount)).Length;
            SdlGpuGeometryBinding geometry = UploadGeometry(
                session, ordinaryVertexBytes, imageDomainVertexBytes);
            return EmitDraws(context, activeTarget, geometry,
                ordinaryVertexBytes, imageDomainVertexBytes);
        }
        finally
        {
            Reset();
        }
    }

    private CerberusFlushMetrics EmitDraws(
        CerberusExecutionContext context,
        SdlGpuRenderTarget activeTarget,
        SdlGpuGeometryBinding geometry,
        int ordinaryVertexBytes,
        int imageDomainVertexBytes)
    {
        // The upload has resumed the render pass. Bindings are local to this emission.
        SdlGpuWindowGraphicsSession session = context.Session;
        ISdlApi api = session.Api;
        nint renderPass = session.ActiveRenderPass;
        api.BindGpuIndexBuffer(renderPass, new SdlGpuBufferBinding(
            geometry.IndexBuffer,
            geometry.IndexOffset));
        Span<float> viewport = stackalloc float[4]
        {
            activeTarget.PixelWidth,
            activeTarget.PixelHeight,
            0,
            0
        };
        api.PushGpuVertexUniformData(
            session.ActiveCommandBuffer,
            0,
            MemoryMarshal.AsBytes(viewport));
        nint currentPipeline = 0;
        nint currentTexture = 0;
        nint currentSampler = 0;
        uint currentVertexBinding = 0;
        SdlRect currentScissor = default;
        byte currentStencilReference = 0;
        bool hasScissor = false;
        bool hasStencilReference = false;
        bool hasVertexBinding = false;
        int pipelineBindCount = 0;
        int samplerBindCount = 0;
        int scissorSetCount = 0;
        int stencilReferenceSetCount = 0;
        Span<float> presentationUniform = stackalloc float[4];
        for (int drawIndex = 0; drawIndex < drawCount; drawIndex++)
        {
            CerberusGpuDraw draw = draws[drawIndex];
            CerberusBatchKey key = draw.Key;
            nint pipeline = context.Resources.GetPipeline(
                activeTarget.ColorFormat,
                activeTarget.SampleCount,
                key.Topology,
                key.BlendMode,
                key.StencilMode,
                key.ColorWriteMask,
                key.AlphaMask,
                key.PrismWorkingColorProfile.HasValue,
                key.PointClampImageDomain);
            uint vertexBinding = checked(geometry.VertexOffset +
                (key.PointClampImageDomain ? checked((uint)ordinaryVertexBytes) : 0));
            if (!hasVertexBinding || vertexBinding != currentVertexBinding)
            {
                api.BindGpuVertexBuffer(renderPass, 0, new SdlGpuBufferBinding(
                    geometry.VertexBuffer,
                    vertexBinding));
                currentVertexBinding = vertexBinding;
                hasVertexBinding = true;
            }
            nint sampler = context.Resources.GetSampler(key.Sampling, key.AddressMode);
            bool pipelineChanged = pipeline != currentPipeline;
            if (pipelineChanged)
            {
                api.BindGpuGraphicsPipeline(renderPass, pipeline);
                currentPipeline = pipeline;
                pipelineBindCount = checked(pipelineBindCount + 1);
            }
            if (pipelineChanged || key.Texture != currentTexture || sampler != currentSampler)
            {
                api.BindGpuFragmentSampler(
                    renderPass,
                    0,
                    new SdlGpuTextureSamplerBinding(key.Texture, sampler));
                currentTexture = key.Texture;
                currentSampler = sampler;
                samplerBindCount = checked(samplerBindCount + 1);
            }
            if (key.PrismWorkingColorProfile is PrismColorProfile profile)
            {
                presentationUniform.Clear();
                presentationUniform[0] = SdlGpuPrismKernelSelector.ForPresentation(profile);
                api.PushGpuFragmentUniformData(
                    session.ActiveCommandBuffer,
                    0,
                    MemoryMarshal.AsBytes(presentationUniform));
            }
            if (!hasScissor || key.Scissor != currentScissor)
            {
                api.SetGpuScissor(renderPass, key.Scissor);
                currentScissor = key.Scissor;
                hasScissor = true;
                scissorSetCount = checked(scissorSetCount + 1);
            }
            if (!hasStencilReference || key.StencilReference != currentStencilReference)
            {
                api.SetGpuStencilReference(renderPass, key.StencilReference);
                currentStencilReference = key.StencilReference;
                hasStencilReference = true;
                stencilReferenceSetCount = checked(stencilReferenceSetCount + 1);
            }
            api.DrawGpuIndexedPrimitives(
                renderPass,
                checked((uint)draw.IndexCount),
                checked((uint)draw.IndexOffset),
                draw.VertexOffset);
        }
        return new CerberusFlushMetrics(
            submissionCount,
            mergedSubmissionCount,
            checked(vertexCount + imageDomainVertexCount),
            indexCount,
            checked(ordinaryVertexBytes + imageDomainVertexBytes),
            MemoryMarshal.AsBytes(indices.AsSpan(0, indexCount)).Length,
            drawCount,
            pipelineBindCount,
            samplerBindCount,
            scissorSetCount,
            stencilReferenceSetCount);
    }

    public void Discard()
    {
        if (vertexCount != 0 || imageDomainVertexCount != 0 ||
            indexCount != 0 || drawCount != 0 || target is not null)
        {
            Reset();
        }
    }

    private void Reset()
    {
        vertexCount = 0;
        imageDomainVertexCount = 0;
        indexCount = 0;
        drawCount = 0;
        submissionCount = 0;
        mergedSubmissionCount = 0;
        target = null;
    }

    private SdlGpuGeometryBinding UploadGeometry(
        SdlGpuWindowGraphicsSession session,
        int ordinaryVertexBytes,
        int imageDomainVertexBytes)
    {
        ReadOnlySpan<int> queuedIndices = indices.AsSpan(0, indexCount);
        if (imageDomainVertexCount == 0)
        {
            return session.GeometryUploadArena.UploadGeometry<SdlGpuVertex>(
                session, vertices.AsSpan(0, vertexCount), queuedIndices);
        }
        if (vertexCount == 0)
        {
            return session.GeometryUploadArena.UploadGeometry<SdlGpuImageDomainVertex>(
                session, imageDomainVertices.AsSpan(0, imageDomainVertexCount), queuedIndices);
        }

        int totalBytes = checked(ordinaryVertexBytes + imageDomainVertexBytes);
        EnsureCapacity(ref packedVertices, totalBytes);
        MemoryMarshal.AsBytes(vertices.AsSpan(0, vertexCount)).CopyTo(packedVertices);
        MemoryMarshal.AsBytes(imageDomainVertices.AsSpan(0, imageDomainVertexCount))
            .CopyTo(packedVertices.AsSpan(ordinaryVertexBytes));
        return session.GeometryUploadArena.UploadGeometryBytes(
            session, packedVertices.AsSpan(0, totalBytes), queuedIndices);
    }

    private static void EnsureCapacity<T>(ref T[] storage, int required)
    {
        if (required > storage.Length)
        {
            Array.Resize(ref storage, ExpandedCapacity(storage.Length, required));
        }
    }

    private static int ExpandedCapacity(int current, int required)
    {
        int expanded = checked(current + (current / 2));
        return Math.Max(expanded, required);
    }

    private readonly record struct CerberusGpuDraw(
        CerberusBatchKey Key,
        int VertexOffset,
        int IndexOffset,
        int IndexCount);
}

internal readonly record struct CerberusExecutionContext(
    SdlGpuWindowGraphicsSession Session,
    SdlGpuDrawingResources Resources);

internal readonly record struct CerberusFlushMetrics(
    int SubmissionCount,
    int MergedSubmissionCount,
    int VertexCount,
    int IndexCount,
    int VertexBytes,
    int IndexBytes,
    int DrawCallCount,
    int PipelineBindCount,
    int SamplerBindCount,
    int ScissorSetCount,
    int StencilReferenceSetCount);

internal readonly record struct CerberusBatchKey(
    DrawPrimitiveTopology Topology,
    nint Texture,
    DrawSamplingMode Sampling,
    DrawAddressMode AddressMode,
    DrawBlendMode BlendMode,
    SdlGpuStencilMode StencilMode,
    byte StencilReference,
    SdlRect Scissor,
    SdlGpuColorWriteMask ColorWriteMask,
    bool AlphaMask = false,
    PrismColorProfile? PrismWorkingColorProfile = null,
    bool PointClampImageDomain = false)
{
    public static CerberusBatchKey From(CerberusBatch batch) => new(
        batch.Topology,
        batch.Texture,
        batch.Sampling,
        batch.AddressMode,
        batch.BlendMode,
        batch.StencilMode,
        batch.StencilReference,
        batch.Scissor,
        batch.ColorWriteMask);

    public bool CanMerge(CerberusBatchKey other) =>
        Topology == DrawPrimitiveTopology.TriangleList &&
        this == other;
}

internal sealed record CerberusBatch(
    SdlGpuVertex[] Vertices,
    int[] Indices,
    DrawPrimitiveTopology Topology,
    nint Texture,
    DrawSamplingMode Sampling,
    DrawAddressMode AddressMode,
    DrawBlendMode BlendMode,
    SdlGpuStencilMode StencilMode,
    byte StencilReference,
    SdlRect Scissor,
    SdlGpuColorWriteMask ColorWriteMask = SdlGpuColorWriteMask.All);

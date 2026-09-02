using System.Runtime.InteropServices;
using Cerneala.Drawing;
using Cerneala.Platforms.Sdl3;

namespace Cerneala.Backends.SdlGpu;

internal sealed class Cerberus
{
    private const int InitialBatchCapacity = 256;
    private const int InitialVertexCapacity = InitialBatchCapacity * 4;
    private const int InitialIndexCapacity = InitialBatchCapacity * 6;
    private SdlGpuVertex[] vertices = new SdlGpuVertex[InitialVertexCapacity];
    private int[] indices = new int[InitialIndexCapacity];
    private CerberusGpuDraw[] draws = new CerberusGpuDraw[InitialBatchCapacity];
    private SdlGpuRenderTarget? target;
    private int vertexCount;
    private int indexCount;
    private int drawCount;
    private int submissionCount;
    private int mergedSubmissionCount;

    public void Begin(SdlGpuRenderTarget nextTarget)
    {
        ArgumentNullException.ThrowIfNull(nextTarget);
        if (vertexCount != 0 || indexCount != 0 || drawCount != 0)
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

        int vertexStart = vertexCount;
        int indexStart = indexCount;
        int requiredVertexCount = checked(vertexCount + nextVertexCount);
        int requiredIndexCount = checked(indexCount + sourceIndices.Length);
        EnsureVertexCapacity(requiredVertexCount);
        EnsureIndexCapacity(requiredIndexCount);
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
            EnsureDrawCapacity(checked(drawCount + 1));
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
        vertexCount = requiredVertexCount;
        indexCount = requiredIndexCount;
        return vertices.AsSpan(vertexStart, nextVertexCount);
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
            SdlGpuGeometryBinding geometry = session.GeometryUploadArena.UploadGeometry(
                session,
                vertices.AsSpan(0, vertexCount),
                indices.AsSpan(0, indexCount));
            ISdlApi api = session.Api;
            nint renderPass = session.ActiveRenderPass;
            api.BindGpuVertexBuffer(renderPass, 0, new SdlGpuBufferBinding(
                geometry.VertexBuffer,
                geometry.VertexOffset));
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
            SdlRect currentScissor = default;
            byte currentStencilReference = 0;
            bool hasScissor = false;
            bool hasStencilReference = false;
            int pipelineBindCount = 0;
            int samplerBindCount = 0;
            int scissorSetCount = 0;
            int stencilReferenceSetCount = 0;
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
                    key.ColorWriteMask);
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
                vertexCount,
                indexCount,
                MemoryMarshal.AsBytes(vertices.AsSpan(0, vertexCount)).Length,
                MemoryMarshal.AsBytes(indices.AsSpan(0, indexCount)).Length,
                drawCount,
                pipelineBindCount,
                samplerBindCount,
                scissorSetCount,
                stencilReferenceSetCount);
        }
        finally
        {
            Reset();
        }
    }

    public void Discard()
    {
        if (vertexCount != 0 || indexCount != 0 || drawCount != 0 || target is not null)
        {
            Reset();
        }
    }

    private void Reset()
    {
        vertexCount = 0;
        indexCount = 0;
        drawCount = 0;
        submissionCount = 0;
        mergedSubmissionCount = 0;
        target = null;
    }

    private void EnsureVertexCapacity(int required)
    {
        if (required > vertices.Length)
        {
            Array.Resize(ref vertices, ExpandedCapacity(vertices.Length, required));
        }
    }

    private void EnsureIndexCapacity(int required)
    {
        if (required > indices.Length)
        {
            Array.Resize(ref indices, ExpandedCapacity(indices.Length, required));
        }
    }

    private void EnsureDrawCapacity(int required)
    {
        if (required > draws.Length)
        {
            Array.Resize(ref draws, ExpandedCapacity(draws.Length, required));
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
    SdlGpuColorWriteMask ColorWriteMask)
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
        other.Topology == Topology &&
        Texture == other.Texture &&
        Sampling == other.Sampling &&
        AddressMode == other.AddressMode &&
        BlendMode == other.BlendMode &&
        StencilMode == other.StencilMode &&
        StencilReference == other.StencilReference &&
        Scissor == other.Scissor &&
        ColorWriteMask == other.ColorWriteMask;
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

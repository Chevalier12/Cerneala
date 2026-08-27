using System.Diagnostics;
using System.Runtime.InteropServices;
using Cerneala.Platforms.Sdl3;

namespace Cerneala.Backends.SdlGpu;

internal sealed partial class SdlGpuDrawingBackend
{
    private sealed class Cerberus(SdlGpuDrawingBackend owner)
    {
        private const int InitialBatchCapacity = 256;
        private const int InitialVertexCapacity = InitialBatchCapacity * 4;
        private const int InitialIndexCapacity = InitialBatchCapacity * 6;
        private SdlGpuVertex[] vertices = new SdlGpuVertex[InitialVertexCapacity];
        private int[] indices = new int[InitialIndexCapacity];
        private GpuDraw[] draws = new GpuDraw[InitialBatchCapacity];
        private SdlGpuRenderTarget? target;
        private int vertexCount;
        private int indexCount;
        private int drawCount;

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

        public void Add(CpuDrawBatch batch)
        {
            if (batch.Indices.Length == 0 || batch.Vertices.Length == 0)
            {
                return;
            }
            Span<SdlGpuVertex> destination = Allocate(
                batch.Vertices.Length,
                batch.Indices,
                BatchKey.From(batch));
            batch.Vertices.AsSpan().CopyTo(destination);
        }

        public Span<SdlGpuVertex> Allocate(
            int nextVertexCount,
            IReadOnlyList<int> sourceIndices,
            BatchKey key)
        {
            if (nextVertexCount <= 0 || sourceIndices.Count == 0)
            {
                return Span<SdlGpuVertex>.Empty;
            }

            int vertexStart = vertexCount;
            int indexStart = indexCount;
            int requiredVertexCount = checked(vertexCount + nextVertexCount);
            int requiredIndexCount = checked(indexCount + sourceIndices.Count);
            EnsureVertexCapacity(requiredVertexCount);
            EnsureIndexCapacity(requiredIndexCount);

            int drawVertexOffset;
            if (drawCount > 0 && draws[drawCount - 1].Key.CanMerge(key))
            {
                int last = drawCount - 1;
                GpuDraw draw = draws[last];
                drawVertexOffset = draw.VertexOffset;
                draws[last] = draw with
                {
                    IndexCount = checked(draw.IndexCount + sourceIndices.Count)
                };
            }
            else
            {
                EnsureDrawCapacity(checked(drawCount + 1));
                drawVertexOffset = vertexStart;
                draws[drawCount++] = new GpuDraw(
                    key,
                    vertexStart,
                    indexStart,
                    sourceIndices.Count);
            }

            int relativeVertexOffset = vertexStart - drawVertexOffset;
            for (int index = 0; index < sourceIndices.Count; index++)
            {
                indices[indexStart + index] = checked(
                    sourceIndices[index] + relativeVertexOffset);
            }
            vertexCount = requiredVertexCount;
            indexCount = requiredIndexCount;
            return vertices.AsSpan(vertexStart, nextVertexCount);
        }

        public void Flush()
        {
            owner.FlushPendingTextAtlasUploads();
            if (drawCount == 0)
            {
                return;
            }
            SdlGpuRenderTarget activeTarget = target ??
                throw new InvalidOperationException("SDL_GPU batching requires an active target.");
            try
            {
                SdlGpuGeometryBinding geometry = owner.resources.UploadGeometry(
                    owner.session,
                    vertices.AsSpan(0, vertexCount),
                    indices.AsSpan(0, indexCount));
                ISdlApi api = owner.session.Api;
                nint renderPass = owner.session.ActiveRenderPass;
                api.BindGpuVertexBuffer(
                    renderPass,
                    0,
                    new SdlGpuBufferBinding(geometry.VertexBuffer));
                api.BindGpuIndexBuffer(
                    renderPass,
                    new SdlGpuBufferBinding(geometry.IndexBuffer));
                Span<float> viewport = stackalloc float[4]
                {
                    activeTarget.PixelWidth,
                    activeTarget.PixelHeight,
                    0,
                    0
                };
                api.PushGpuVertexUniformData(
                    owner.session.ActiveCommandBuffer,
                    0,
                    MemoryMarshal.AsBytes(viewport));
                for (int drawIndex = 0; drawIndex < drawCount; drawIndex++)
                {
                    GpuDraw draw = draws[drawIndex];
                    BatchKey key = draw.Key;
                    nint pipeline = owner.resources.GetPipeline(
                        activeTarget.ColorFormat,
                        activeTarget.SampleCount,
                        key.Topology,
                        key.BlendMode,
                        key.StencilMode,
                        key.ColorWriteMask);
                    nint sampler = owner.resources.GetSampler(
                        key.Sampling,
                        key.AddressMode);
                    api.BindGpuGraphicsPipeline(renderPass, pipeline);
                    api.BindGpuFragmentSampler(
                        renderPass,
                        0,
                        new SdlGpuTextureSamplerBinding(key.Texture, sampler));
                    api.SetGpuScissor(renderPass, key.Scissor);
                    api.SetGpuStencilReference(renderPass, key.StencilReference);
                    api.DrawGpuIndexedPrimitives(
                        renderPass,
                        checked((uint)draw.IndexCount),
                        checked((uint)draw.IndexOffset),
                        draw.VertexOffset);
                }
            }
            finally
            {
                Reset();
            }
        }

        public void Discard()
        {
            if (vertexCount == 0 && indexCount == 0 && drawCount == 0 && target is null)
            {
                return;
            }
            Reset();
        }

        private void Reset()
        {
            long cleanupStarted = Stopwatch.GetTimestamp();
            try
            {
                vertexCount = 0;
                indexCount = 0;
                drawCount = 0;
                target = null;
            }
            finally
            {
                owner.cleanupTime += Stopwatch.GetElapsedTime(cleanupStarted);
            }
        }

        private void EnsureVertexCapacity(int required)
        {
            if (required <= vertices.Length)
            {
                return;
            }
            Array.Resize(ref vertices, ExpandedCapacity(vertices.Length, required));
        }

        private void EnsureIndexCapacity(int required)
        {
            if (required <= indices.Length)
            {
                return;
            }
            Array.Resize(ref indices, ExpandedCapacity(indices.Length, required));
        }

        private void EnsureDrawCapacity(int required)
        {
            if (required <= draws.Length)
            {
                return;
            }
            Array.Resize(ref draws, ExpandedCapacity(draws.Length, required));
        }

        private static int ExpandedCapacity(int current, int required)
        {
            int expanded = checked(current + (current / 2));
            return Math.Max(expanded, required);
        }

        private readonly record struct GpuDraw(
            BatchKey Key,
            int VertexOffset,
            int IndexOffset,
            int IndexCount);
    }
}

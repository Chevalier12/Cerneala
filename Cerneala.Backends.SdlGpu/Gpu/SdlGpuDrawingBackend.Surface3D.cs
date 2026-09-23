using System.Numerics;
using System.Runtime.InteropServices;
using Cerneala.Drawing;
using Cerneala.Platforms.Sdl3;

namespace Cerneala.Backends.SdlGpu;

internal sealed partial class SdlGpuDrawingBackend
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly record struct Surface3DVertex(
        Vector3 Start, Vector3 End, Vector4 Color, Vector2 Corner, Vector2 Parameters);

    [StructLayout(LayoutKind.Sequential)]
    internal readonly record struct Surface3DUniforms(
        Matrix4x4 Model, Matrix4x4 View, Matrix4x4 Projection, Vector4 Viewport);

    private sealed class SdlGpuRenderSurface3DExecutor : IDisposable, ISdlGpuCommandBufferParticipant
    {
        private static readonly Vector2[] LineCorners =
            [new(0, -1), new(1, -1), new(1, 1), new(0, 1)];
        private static readonly Vector2[] MarkerCorners =
            [new(-1, -1), new(1, -1), new(1, 1), new(-1, 1)];
        private readonly SdlGpuDrawingBackend backend;
        private readonly SdlGpuWindowGraphicsSession session;
        private readonly SdlGpuDrawingResources resources;
        private readonly SdlGpuRenderSurface3DDiagnostics diagnostics;
        private readonly Dictionary<IRenderSurface3DSource, SurfaceState> surfaces =
            new(ReferenceEqualityComparer.Instance);
        private bool disposed;

        internal SdlGpuRenderSurface3DExecutor(
            SdlGpuDrawingBackend backend,
            SdlGpuWindowGraphicsSession session,
            SdlGpuDrawingResources resources,
            SdlGpuRenderSurface3DDiagnostics diagnostics)
        {
            this.backend = backend;
            this.session = session;
            this.resources = resources;
            this.diagnostics = diagnostics;
        }

        internal void AddSurface(
            DrawCommand command, RenderState state,
            SdlGpuRenderTarget parentTarget, Cerberus parentBatches)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            IRenderSurface3DSource source = command.RenderSurface3D ??
                throw new InvalidOperationException("RenderSurface3D requires a frame-producing source.");
            // Retained commands may outlive the final subscriber. They must
            // not acquire a fresh target after the control retired its owner.
            if (!source.HasDrawSubscribers || command.RenderSurface3DResourceEpoch != source.ResourceEpoch ||
                command.Rect.Width == 0 || command.Rect.Height == 0)
            {
                // RenderRange flushed the preceding batch before dispatching
                // this command. Even a skipped surface must rebind that batch.
                parentBatches.Begin(parentTarget);
                return;
            }
            int width = Math.Max(1, checked((int)MathF.Ceiling(command.Rect.Width * backend.CoordinateScale)));
            int height = Math.Max(1, checked((int)MathF.Ceiling(command.Rect.Height * backend.CoordinateScale)));
            SurfaceKey key = new(source.FrameVersion, command.RetainedVersion,
                command.Rect.Width, command.Rect.Height,
                backend.CoordinateScale, width, height, parentTarget.ColorFormat);
            if (!surfaces.TryGetValue(source, out SurfaceState? surface))
            {
                surface = new SurfaceState(this, source, resources, diagnostics);
                surfaces.Add(source, surface);
                source.SetBackendState(this, surface);
            }
            if (surface.Target is null || surface.Target.PixelWidth != width || surface.Target.PixelHeight != height ||
                surface.Target.ColorFormat != parentTarget.ColorFormat)
            {
                if (surface.Target is not null)
                {
                    surface.RetireTarget();
                }
                SdlGpuSampleCount samples = SelectSampleCount(parentTarget.ColorFormat);
                surface.Target = resources.CreateRenderTarget(width, height, parentTarget.ColorFormat, samples);
                surface.SubmittedKey = null;
                surface.PendingToken = null;
                diagnostics.RecordTargetCreated();
            }

            SdlGpuCommandBufferToken token = session.ActiveCommandBufferToken;
            resources.PinRenderTarget(session, token, surface.Target);
            // The command is a captured immutable presentation snapshot. A
            // callback may invalidate the live source while that same command
            // is replayed for capture; consume the new generation next frame.
            bool pendingMatch = surface.PendingToken == token &&
                surface.PendingKey == key with { Version = surface.PendingKey.Version } &&
                (key.Version == surface.PendingKey.Version || key.Version == surface.PendingReplayVersion);
            bool submittedMatch = surface.PendingToken is null &&
                surface.SubmittedKey == key;
            if (!pendingMatch && !submittedMatch)
            {
                RenderSurface3DRecording recording = source.RecordFrame(
                    new DrawRect(0, 0, command.Rect.Width, command.Rect.Height),
                    backend.CoordinateScale);
                diagnostics.RecordRecording();
                if (recording.PixelWidth != width || recording.PixelHeight != height ||
                    recording.Generation != key.Version)
                {
                    throw new InvalidOperationException("The RenderSurface3D recording does not match its target or generation.");
                }
                long postRecordVersion = source.FrameVersion;
                ObjectDisposedException.ThrowIf(surface.IsDisposed, surface);
                try
                {
                    // A failed render may already have changed this texture. The
                    // previous submitted generation must not survive that write.
                    surface.SubmittedKey = null;
                    surface.PendingToken = null;
                    session.BeginRenderTarget(surface.Target, recording.ClearColor, SdlGpuLoadOp.Clear);
                    diagnostics.RecordPass();
                    RenderPrimitives(recording, surface.Target);
                    ObjectDisposedException.ThrowIf(surface.IsDisposed, surface);
                    surface.PendingToken = token;
                    surface.PendingKey = key;
                    surface.PendingReplayVersion = postRecordVersion;
                    session.RegisterCommandBufferParticipant(this);
                }
                finally
                {
                    session.BeginRenderTarget(parentTarget, Color.Transparent, SdlGpuLoadOp.Load);
                }
            }
            else
            {
                session.BeginRenderTarget(parentTarget, Color.Transparent, SdlGpuLoadOp.Load);
            }

            parentBatches.Begin(parentTarget);
            AddQuad(parentBatches, command.Rect, new DrawRect(0, 0, 1, 1),
                command.Color, state.Transform, state.Opacity,
                CreateBatchKey(DrawPrimitiveTopology.TriangleList,
                    surface.Target.SampleTexture, DrawSamplingMode.Linear,
                    DrawAddressMode.Clamp, state));
        }

        private SdlGpuSampleCount SelectSampleCount(SdlGpuTextureFormat colorFormat)
        {
            SdlGpuSampleCount[] candidates =
                [SdlGpuSampleCount.Eight, SdlGpuSampleCount.Four,
                 SdlGpuSampleCount.Two];
            foreach (SdlGpuSampleCount candidate in candidates)
            {
                if (session.Api.GpuTextureSupportsSampleCount(session.Device, colorFormat, candidate) &&
                    session.Api.GpuTextureSupportsSampleCount(session.Device,
                        SdlGpuTextureFormat.D24UnormS8Uint, candidate))
                {
                    return candidate;
                }
            }
            throw new NotSupportedException(
                $"SDL GPU RenderSurface3D requires at least two common color/depth samples for {colorFormat} and D24UnormS8Uint.");
        }

        private void RenderPrimitives(RenderSurface3DRecording recording, SdlGpuRenderTarget target)
        {
            IReadOnlyList<DrawPrimitive3D> primitives = recording.Primitives;
            if (primitives.Count == 0) return;
            Surface3DVertex[] vertices = new Surface3DVertex[checked(primitives.Count * 4)];
            int[] indices = new int[checked(primitives.Count * 6)];
            for (int primitiveIndex = 0; primitiveIndex < primitives.Count; primitiveIndex++)
            {
                DrawPrimitive3D primitive = primitives[primitiveIndex];
                Vector2[] corners = primitive.Kind == DrawPrimitive3DKind.Line ? LineCorners : MarkerCorners;
                Vector4 color = new(primitive.Color.R / 255f, primitive.Color.G / 255f,
                    primitive.Color.B / 255f, 1);
                Vector2 parameters = new(primitive.Size * recording.RasterScale,
                    primitive.Kind == DrawPrimitive3DKind.Marker ? 1 : 0);
                int vertexStart = primitiveIndex * 4;
                int indexStart = primitiveIndex * 6;
                for (int corner = 0; corner < 4; corner++)
                {
                    vertices[vertexStart + corner] = new(
                        primitive.Start, primitive.End, color, corners[corner], parameters);
                }
                indices[indexStart] = vertexStart;
                indices[indexStart + 1] = vertexStart + 1;
                indices[indexStart + 2] = vertexStart + 2;
                indices[indexStart + 3] = vertexStart;
                indices[indexStart + 4] = vertexStart + 2;
                indices[indexStart + 5] = vertexStart + 3;
            }

            SdlGpuGeometryBinding binding = session.GeometryUploadArena.UploadGeometry<Surface3DVertex>(
                session, vertices, indices);
            diagnostics.RecordUpload(MemoryMarshal.AsBytes(vertices.AsSpan()).Length +
                MemoryMarshal.AsBytes(indices.AsSpan()).Length);
            ISdlApi api = session.Api;
            nint pass = session.ActiveRenderPass;
            nint pipeline = resources.Surface3DResources.GetPipeline(
                target.ColorFormat, SdlGpuTextureFormat.D24UnormS8Uint, target.SampleCount);
            api.BindGpuGraphicsPipeline(pass, pipeline);
            api.BindGpuVertexBuffer(pass, 0, new SdlGpuBufferBinding(binding.VertexBuffer, binding.VertexOffset));
            api.BindGpuIndexBuffer(pass, new SdlGpuBufferBinding(binding.IndexBuffer, binding.IndexOffset));
            api.SetGpuScissor(pass, new SdlRect(0, 0, target.PixelWidth, target.PixelHeight));
            for (int primitiveIndex = 0; primitiveIndex < primitives.Count; primitiveIndex++)
            {
                Surface3DUniforms uniform = new(primitives[primitiveIndex].Model,
                    recording.ViewMatrix, recording.ProjectionMatrix,
                    new Vector4(target.PixelWidth, target.PixelHeight,
                        1 << (int)target.SampleCount, 0));
                api.PushGpuVertexUniformData(session.ActiveCommandBuffer, 0,
                    MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref uniform, 1)));
                api.DrawGpuIndexedPrimitives(pass, 6, checked((uint)(primitiveIndex * 6)), 0);
                diagnostics.RecordDraw();
            }
        }

        public void OnCommandBufferSubmitted(SdlGpuCommandBufferToken token)
        {
            foreach (SurfaceState surface in surfaces.Values)
            {
                if (surface.PendingToken != token) continue;
                surface.SubmittedKey = surface.PendingKey;
                surface.PendingToken = null;
            }
        }

        public void OnCommandBufferAbandoned(SdlGpuCommandBufferToken token)
        {
            foreach (SurfaceState surface in surfaces.Values)
            {
                if (surface.PendingToken != token) continue;
                surface.SubmittedKey = null;
                surface.PendingToken = null;
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (SurfaceState surface in surfaces.Values.ToArray())
            {
                surface.Dispose();
            }
            surfaces.Clear();
        }

        private void RemoveState(IRenderSurface3DSource source, SurfaceState state)
        {
            if (surfaces.TryGetValue(source, out SurfaceState? current) && ReferenceEquals(current, state))
                surfaces.Remove(source);
        }

        private readonly record struct SurfaceKey(long Version, long CommandVersion,
            float LogicalWidth, float LogicalHeight,
            float Scale, int PixelWidth, int PixelHeight, SdlGpuTextureFormat Format);

        private sealed class SurfaceState : IRenderSurface3DBackendState
        {
            private readonly SdlGpuRenderSurface3DExecutor owner;
            private readonly SdlGpuDrawingResources resources;
            private readonly SdlGpuRenderSurface3DDiagnostics diagnostics;

            internal SurfaceState(SdlGpuRenderSurface3DExecutor owner, IRenderSurface3DSource source,
                SdlGpuDrawingResources resources, SdlGpuRenderSurface3DDiagnostics diagnostics)
            {
                this.owner = owner;
                Source = source;
                this.resources = resources;
                this.diagnostics = diagnostics;
            }

            internal IRenderSurface3DSource Source { get; }
            internal SdlGpuRenderTarget? Target;
            internal SurfaceKey? SubmittedKey;
            internal SdlGpuCommandBufferToken? PendingToken;
            internal SurfaceKey PendingKey;
            internal long PendingReplayVersion;
            internal bool IsDisposed { get; private set; }

            internal void RetireTarget()
            {
                if (Target is null) return;
                resources.RetireRenderTarget(Target);
                Target = null;
                diagnostics.RecordTargetRetired();
            }

            public void Dispose()
            {
                if (IsDisposed) return;
                IsDisposed = true;
                SubmittedKey = null;
                PendingToken = null;
                RetireTarget();
                owner.RemoveState(Source, this);
                Source.SetBackendState(owner, null);
            }
        }
    }
}

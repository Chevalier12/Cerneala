using System.Diagnostics;
using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Paths;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Text;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Hosting;

namespace Cerneala.Backends.SdlGpu;

internal sealed partial class SdlGpuDrawingBackend
{
    private void AddRenderSurface(
        DrawCommand command,
        RenderState state,
        SdlGpuRenderTarget parentTarget,
        Cerberus parentBatches)
    {
        IRenderSurface2DFrameSource source = command.RenderSurface as IRenderSurface2DFrameSource ??
            throw new InvalidOperationException(
                "RenderSurface2D requires a frame-producing source.");
        (int width, int height) = RenderSurface2DGeometry.GetPixelSize(
            command.Rect.Width, command.Rect.Height, CoordinateScale);
        IRenderSurface2DBackendState? backendState = source.GetBackendState(resources);
        SdlGpuRenderSurfaceStateCache? surfaceCache =
            backendState as SdlGpuRenderSurfaceStateCache;
        if (surfaceCache is null)
        {
            backendState?.Dispose();
            surfaceCache = new SdlGpuRenderSurfaceStateCache();
            source.SetBackendState(resources, surfaceCache);
        }
        surfaceCache.Attach(this);
        SdlGpuRenderSurfaceState? surface = surfaceCache.Get(session);
        if (surface is null || surface.PixelWidth != width || surface.PixelHeight != height)
        {
            surface = new SdlGpuRenderSurfaceState(
                resources,
                resources.CreateRenderTarget(
                    width,
                    height,
                    parentTarget.ColorFormat,
                    // Surface edge coverage is independent of window MSAA,
                    // including single-sample design-preview windows.
                    session.SelectSampleCount(parentTarget.ColorFormat, SdlGpuSampleCount.Eight)),
                new SdlGpuPrismExecutor(session, this));
            surfaceCache.Set(session, surface);
        }

        SdlGpuCommandBufferToken token = session.ActiveCommandBufferToken;
        if (surface.GetFrameVersion(token) != source.FrameVersion)
        {
            // A surface records local pixels, not the hosting window's DIPs.
            // Keep that coordinate contract through text, clips and Prism, then
            // restore the host scale before compositing the surface into it.
            float parentCoordinateScale = CoordinateScale;
            float parentThreadScale = threadScale;
            CoordinateScale = 1;
            threadScale = 1;
            try
            {
                RenderSurfaceFrame(source, surface, parentBatches);
                surface.SetPendingFrameVersion(session, token, source.FrameVersion);
            }
            finally
            {
                CoordinateScale = parentCoordinateScale;
                threadScale = parentThreadScale;
                session.BeginRenderTarget(
                    parentTarget,
                    Color.Transparent,
                    SdlGpuLoadOp.Load);
            }
        }

        parentBatches.Begin(parentTarget);
        AddQuad(
            parentBatches,
            command.Rect,
            new DrawRect(0, 0, 1, 1),
            command.Color,
            state.Transform,
            state.Opacity,
            CreateBatchKey(
                DrawPrimitiveTopology.TriangleList,
                surface.Target.SampleTexture,
                DrawSamplingMode.Linear,
                DrawAddressMode.Clamp,
                state));
    }

    private void RenderSurfaceFrame(
        IRenderSurface2DFrameSource source,
        SdlGpuRenderSurfaceState surface,
        Cerberus surfaceBatches)
    {
        surface.Commands.Clear();
        source.RecordFrame(
            surface.Commands,
            new DrawRect(0, 0, surface.PixelWidth, surface.PixelHeight));
        RenderRecordedSurfaceFrame(surface, source.ClearColor, surfaceBatches);
    }

    private bool RenderRecordedSurfaceFrame(
        SdlGpuRenderSurfaceState surface,
        Color clearColor,
        Cerberus surfaceBatches,
        bool requireFullReplay = false)
    {
        SdlGpuCommandBufferToken token = session.ActiveCommandBufferToken;
        IReadOnlyList<DrawCommandStateEntry>? retainedEntries = surface.GetRetainedEntries(token);
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(
            surface.Commands, retainedEntries, surface.RentEntryBuffer());
        if (analysis.Scopes.IsDefaultOrEmpty)
        {
            surface.PrismExecutor.ProcessInvalidations(analysis, surface.PrismCacheInvalidations);
        }
        SdlRect? damage = ResolveSurfaceDamage(surface, analysis.StateAnalysis, clearColor, token);
        if (damage is null)
        {
            return false;
        }
        DrawingFrameContext frameContext = new(
            analysis,
            backdropLease: null,
            backdropSourceToken: default,
            surface.PrismCacheInvalidations);
        SdlRect bounds = new(0, 0, surface.PixelWidth, surface.PixelHeight);
        if (requireFullReplay)
        {
            damage = bounds;
        }
        bool fullReplay = damage.Value == bounds;
        // A failed replay must not leave a partially changed texture eligible
        // for reuse on a later attempt.
        surface.InvalidatePending(session, token);
        session.BeginRenderTarget(surface.Target, clearColor,
            fullReplay ? SdlGpuLoadOp.Clear : SdlGpuLoadOp.Load);
        if (!analysis.Scopes.IsDefaultOrEmpty)
        {
            surface.PrismExecutor.Execute(surface.Commands, frameContext, surface.Target);
            CountPrismExecution(surface.PrismExecutor.Diagnostics);
        }
        else
        {
            RenderState state = RenderState.Create(surface.Target, CoordinateScale);
            CommandRangeState rangeState = new(surface.Target, state);
            surfaceBatches.Begin(surface.Target);
            if (fullReplay)
            {
                RenderRange(surface.Commands, 0, surface.Commands.Count,
                    analysis.StateAnalysis, rangeState, surfaceBatches);
            }
            else
            {
                // Partial replay is restricted to independent commands. A
                // changed state/Prism scope conservatively replays the target.
                state.Scissors[0] = damage.Value;
                state.Blends[0] = DrawBlendMode.Opaque;
                AddFillRectangle(DrawCommand.FillRectangle(
                    new DrawRect(damage.Value.X, damage.Value.Y, damage.Value.Width, damage.Value.Height),
                    clearColor), state, surfaceBatches);
                state.Blends[0] = DrawBlendMode.Normal;
                for (int index = 0; index < surface.Commands.Count; index++)
                {
                    SdlRect commandBounds = SurfaceCommandBounds(analysis.StateAnalysis.Entries[index], bounds);
                    SdlRect intersection = IntersectScissor(commandBounds, damage.Value);
                    if (intersection.Width > 0 && intersection.Height > 0)
                    {
                        RenderRange(surface.Commands, index, index + 1,
                            analysis.StateAnalysis, rangeState, surfaceBatches);
                    }
                }
            }
            FlushBatches();
            EnsureCompositingScopesClosed(rangeState);
        }
        surface.PublishPending(
            session,
            token,
            analysis.StateAnalysis.Entries,
            clearColor,
            analysis.StateAnalysis.Buffer);
        return true;
    }

    private sealed class SdlGpuRenderSurfaceStateCache : IRenderSurface2DBackendState
    {
        private readonly Dictionary<SdlGpuWindowGraphicsSession, SdlGpuRenderSurfaceState> surfaces =
            new(ReferenceEqualityComparer.Instance);
        private readonly HashSet<SdlGpuDrawingBackend> owners =
            new(ReferenceEqualityComparer.Instance);
        private bool disposed;

        public void Attach(SdlGpuDrawingBackend owner)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (owners.Add(owner))
            {
                owner.renderSurfaceStateCaches.Add(this);
            }
        }

        public SdlGpuRenderSurfaceState? Get(SdlGpuWindowGraphicsSession session)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return surfaces.GetValueOrDefault(session);
        }

        public void Set(
            SdlGpuWindowGraphicsSession session,
            SdlGpuRenderSurfaceState surface)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (surfaces.Remove(session, out SdlGpuRenderSurfaceState? previous))
            {
                previous.Dispose();
            }
            surfaces.Add(session, surface);
        }

        public void Remove(
            SdlGpuDrawingBackend owner,
            SdlGpuWindowGraphicsSession session)
        {
            owners.Remove(owner);
            owner.renderSurfaceStateCaches.Remove(this);
            if (!disposed && surfaces.Remove(session, out SdlGpuRenderSurfaceState? surface))
            {
                surface.Dispose();
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            foreach (SdlGpuDrawingBackend owner in owners)
            {
                owner.renderSurfaceStateCaches.Remove(this);
            }
            owners.Clear();
            foreach (SdlGpuRenderSurfaceState surface in surfaces.Values)
            {
                surface.Dispose();
            }
            surfaces.Clear();
        }
    }

    private sealed record SdlGpuRenderSurfaceState(
        SdlGpuDrawingResources Resources,
        SdlGpuRenderTarget Target,
        SdlGpuPrismExecutor PrismExecutor) :
        IDisposable,
        ISdlGpuCommandBufferParticipant
    {
        private bool disposed;
        private long submittedFrameVersion = long.MinValue;
        private IReadOnlyList<DrawCommandStateEntry>? submittedRetainedEntries;
        private Color submittedClearColor;
        private SdlGpuCommandBufferToken pendingToken;
        private long pendingFrameVersion = long.MinValue;
        private IReadOnlyList<DrawCommandStateEntry>? pendingRetainedEntries;
        private Color pendingClearColor;
        // Arrays backing this surface's analyzed entries. One retained by
        // neither the submitted nor the pending frame is refilled, not reallocated.
        private DrawCommandStateEntry[]? submittedEntryBuffer;
        private DrawCommandStateEntry[]? pendingEntryBuffer;
        private readonly List<DrawCommandStateEntry[]> entryBuffers = new(3);
        private bool pendingInvalidated;

        public int PixelWidth => Target.PixelWidth;
        public int PixelHeight => Target.PixelHeight;
        public DrawCommandList Commands { get; } = new();
        public PrismCacheInvalidationQueue PrismCacheInvalidations { get; } = new();
        public long GetFrameVersion(SdlGpuCommandBufferToken token) =>
            pendingToken == token && !pendingInvalidated
                ? pendingFrameVersion
                : submittedFrameVersion;

        public IReadOnlyList<DrawCommandStateEntry>? GetRetainedEntries(
            SdlGpuCommandBufferToken token) =>
            pendingToken == token && !pendingInvalidated
                ? pendingRetainedEntries
                : submittedRetainedEntries;

        public Color GetRetainedClearColor(SdlGpuCommandBufferToken token) =>
            pendingToken == token && !pendingInvalidated
                ? pendingClearColor
                : submittedClearColor;

        public bool RequiresFullReplay(SdlGpuCommandBufferToken token) =>
            pendingToken == token && pendingInvalidated;

        public void SetPendingFrameVersion(
            SdlGpuWindowGraphicsSession session,
            SdlGpuCommandBufferToken token,
            long frameVersion)
        {
            EnsurePending(session, token);
            pendingFrameVersion = frameVersion;
        }

        // Never null: an empty buffer still marks the analysis as recycled, so a
        // replacement is sized with room to grow.
        public DrawCommandStateEntry[] RentEntryBuffer()
        {
            foreach (DrawCommandStateEntry[] buffer in entryBuffers)
            {
                if (!ReferenceEquals(buffer, submittedEntryBuffer) && !ReferenceEquals(buffer, pendingEntryBuffer))
                {
                    return buffer;
                }
            }
            return [];
        }

        public void PublishPending(
            SdlGpuWindowGraphicsSession session,
            SdlGpuCommandBufferToken token,
            IReadOnlyList<DrawCommandStateEntry> retainedEntries,
            Color clearColor,
            DrawCommandStateEntry[]? entryBuffer = null)
        {
            EnsurePending(session, token);
            // A replay invalidates pending state before publication. Revalidated
            // entries still own their array even when that pending link was cleared.
            entryBuffer ??= (retainedEntries as DrawCommandStateEntries)?.Array;
            pendingEntryBuffer = entryBuffer;
            if (entryBuffer is { Length: > 0 } && !entryBuffers.Contains(entryBuffer))
            {
                if (entryBuffers.Count == entryBuffers.Capacity)
                {
                    int free = entryBuffers.FindIndex(buffer =>
                        !ReferenceEquals(buffer, submittedEntryBuffer) && !ReferenceEquals(buffer, pendingEntryBuffer));
                    if (free >= 0) { entryBuffers.RemoveAt(free); }
                }
                if (entryBuffers.Count < entryBuffers.Capacity) { entryBuffers.Add(entryBuffer); }
            }
            pendingRetainedEntries = retainedEntries;
            pendingClearColor = clearColor;
            pendingInvalidated = false;
        }

        public void InvalidatePending(
            SdlGpuWindowGraphicsSession session,
            SdlGpuCommandBufferToken token)
        {
            EnsurePending(session, token);
            pendingRetainedEntries = null;
            pendingEntryBuffer = null;
            pendingFrameVersion = long.MinValue;
            pendingInvalidated = true;
        }

        public void OnCommandBufferSubmitted(SdlGpuCommandBufferToken token)
        {
            if (disposed || pendingToken != token)
            {
                return;
            }

            if (pendingInvalidated)
            {
                submittedFrameVersion = long.MinValue;
                submittedRetainedEntries = null;
                submittedEntryBuffer = null;
            }
            else
            {
                submittedFrameVersion = pendingFrameVersion;
                submittedRetainedEntries = pendingRetainedEntries;
                submittedEntryBuffer = pendingEntryBuffer;
                submittedClearColor = pendingClearColor;
            }
            ClearPending();
        }

        public void OnCommandBufferAbandoned(SdlGpuCommandBufferToken token)
        {
            if (pendingToken == token)
            {
                ClearPending();
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            ClearPending();
            PrismExecutor.Dispose();
            Resources.RetireRenderTarget(Target);
        }

        private void EnsurePending(
            SdlGpuWindowGraphicsSession session,
            SdlGpuCommandBufferToken token)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (pendingToken != token)
            {
                pendingToken = token;
                pendingFrameVersion = submittedFrameVersion;
                pendingRetainedEntries = submittedRetainedEntries;
                pendingEntryBuffer = submittedEntryBuffer;
                pendingClearColor = submittedClearColor;
                pendingInvalidated = false;
                session.RegisterCommandBufferParticipant(this);
            }
        }

        private void ClearPending()
        {
            pendingToken = default;
            pendingFrameVersion = long.MinValue;
            pendingRetainedEntries = null;
            pendingEntryBuffer = null;
            pendingClearColor = default;
            pendingInvalidated = false;
        }
    }

}

using System.Diagnostics;
using Cerneala.Drawing.MonoGame.Prism.Kernels;
using Cerneala.Drawing.MonoGame.Prism.Surfaces;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame.Prism.Execution;

internal sealed class PrismGraphExecutor : IDisposable
{
    private static readonly Vector2 FullUvScale = Vector2.One;
    private static readonly Vector2 ZeroUvOffset = Vector2.Zero;

    private readonly GraphicsDevice graphicsDevice;
    private readonly PrismSurfacePool surfacePool;
    private readonly PrismKernelRegistry kernels;
    private readonly PrismExecutionDiagnostics diagnostics;
    private PrismSurfaceKey[] surfaceKeys = [];
    private bool[] bypassedScopes = [];
    private int[] captureSteps = [];
    private int[] captureCommandIndices = [];
    private bool[] initializedCaptures = [];
    private bool disposed;

    public PrismGraphExecutor(
        GraphicsDevice graphicsDevice,
        PrismExecutionDiagnostics? diagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ObjectDisposedException.ThrowIf(
            graphicsDevice.IsDisposed,
            graphicsDevice);

        this.graphicsDevice = graphicsDevice;
        this.diagnostics =
            diagnostics ?? new PrismExecutionDiagnostics();
        surfacePool = new PrismSurfacePool(graphicsDevice);
        try
        {
            kernels = new PrismKernelRegistry(graphicsDevice);
        }
        catch
        {
            surfacePool.Dispose();
            throw;
        }
    }

    public PrismExecutionDiagnostics Diagnostics => diagnostics;

    public PrismSurfacePool SurfacePool => surfacePool;

    public PrismKernelRegistry Kernels => kernels;

    public void Execute(
        DrawCommandList commands,
        PrismFrameAnalysis analysis,
        PrismGraphExecutionPlan plan,
        IPrismCommandRenderer renderer,
        Viewport hostViewport,
        IBackdropFrameLease? backdropLease)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(renderer);
        analysis.EnsureCurrent(commands);
        if (!ReferenceEquals(
            graphicsDevice,
            renderer.GraphicsDevice))
        {
            throw new ArgumentException(
                "The Prism renderer must use the executor GraphicsDevice.",
                nameof(renderer));
        }
        if (hostViewport.Width <= 0 || hostViewport.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hostViewport),
                "A Prism execution viewport must have positive dimensions.");
        }

        PrismGraph graph = plan.OptimizedGraph;
        long submitStarted = Stopwatch.GetTimestamp();
        long createdBefore = surfacePool.CreatedSurfaceCount;
        long reusedBefore = surfacePool.ReusedSurfaceCount;
        diagnostics.BeginExecution(
            analysis,
            plan,
            checked(plan.ExecutionOrder.Length + graph.Scopes.Length));
        try
        {
            if (plan.ExecutionOrder.IsEmpty)
            {
                RenderRawRange(renderer, commands, 0, commands.Count);
                return;
            }

            PrepareFrameBuffers(plan, graph, hostViewport);
            int hostCommandIndex =
                RenderHostPrelude(renderer, commands, graph);

            try
            {
                using PrismSurfaceFrame frame =
                    surfacePool.BeginFrame(plan);
                for (int step = 0;
                    step < plan.ExecutionOrder.Length;
                    step++)
                {
                    frame.AdvanceToStep(step, surfaceKeys);
                    diagnostics.ObserveLiveSurfaces(
                        surfacePool.ActiveLeaseCount);
                    PrismGraphNode node =
                        graph.GetNode(plan.ExecutionOrder[step]);
                    RenderNode(
                        renderer,
                        commands,
                        plan,
                        graph,
                        frame,
                        step,
                        node,
                        backdropLease);
                    diagnostics.RecordGraphPass(node);
                    PresentCompletedNestedScopes(
                        renderer,
                        commands,
                        plan,
                        graph,
                        frame,
                        step,
                        node);
                    PresentCompletedRootScopes(
                        renderer,
                        commands,
                        plan,
                        graph,
                        frame,
                        step,
                        node,
                        hostViewport,
                        ref hostCommandIndex);
                }
            }
            catch (PrismSurfaceAllocationException exception)
            {
                diagnostics.Record(
                    null,
                    -1,
                    PrismFallbackReason.SurfaceAllocationFailed,
                    exception.Message);
                renderer.EndBatch();
                renderer.RestoreHostTarget();
                renderer.BeginCommandBatch();
                RenderRawRange(
                    renderer,
                    commands,
                    hostCommandIndex,
                    commands.Count);
                return;
            }

            renderer.EndBatch();
            renderer.RestoreHostTarget();
            renderer.BeginCommandBatch();
            RenderRawRange(
                renderer,
                commands,
                hostCommandIndex,
                commands.Count);
        }
        finally
        {
            diagnostics.CompleteExecution(
                surfacePool.CreatedSurfaceCount - createdBefore,
                surfacePool.ReusedSurfaceCount - reusedBefore,
                Stopwatch.GetElapsedTime(submitStarted));
        }
    }

    public void Reset()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        surfacePool.Reset();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        kernels.Dispose();
        surfacePool.Dispose();
        disposed = true;
    }

    private void PrepareFrameBuffers(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        Viewport hostViewport)
    {
        if (surfaceKeys.Length < plan.ExecutionOrder.Length)
        {
            Array.Resize(
                ref surfaceKeys,
                plan.ExecutionOrder.Length);
        }

        int requiredScopeSlots = 0;
        for (int index = 0; index < graph.Scopes.Length; index++)
        {
            requiredScopeSlots = Math.Max(
                requiredScopeSlots,
                graph.Scopes[index].AnalysisScopeIndex + 1);
        }
        if (bypassedScopes.Length < requiredScopeSlots)
        {
            Array.Resize(
                ref bypassedScopes,
                requiredScopeSlots);
        }
        if (captureSteps.Length < requiredScopeSlots)
        {
            Array.Resize(ref captureSteps, requiredScopeSlots);
            Array.Resize(
                ref captureCommandIndices,
                requiredScopeSlots);
            Array.Resize(
                ref initializedCaptures,
                requiredScopeSlots);
        }
        Array.Clear(bypassedScopes);
        Array.Clear(initializedCaptures);
        Array.Fill(
            captureSteps,
            -1,
            0,
            requiredScopeSlots);

        for (int index = 0;
            index < plan.ExecutionOrder.Length;
            index++)
        {
            PrismGraphNode node =
                graph.GetNode(plan.ExecutionOrder[index]);
            PrismGraphScope scope =
                FindScope(graph, node.AnalysisScopeIndex);
            if (node.Kind == PrismGraphNodeKind.ControlCapture)
            {
                captureSteps[node.AnalysisScopeIndex] = index;
                captureCommandIndices[node.AnalysisScopeIndex] =
                    scope.BeginCommandIndex + 1;
            }
            PrismColorProfile profile =
                node.ColorProfile ??
                scope.CompositionSettings.WorkingColorProfile;
            surfaceKeys[index] = new PrismSurfaceKey(
                hostViewport.Width,
                hostViewport.Height,
                SurfaceFormat.Color,
                0,
                profile);
        }
    }

    private int RenderHostPrelude(
        IPrismCommandRenderer renderer,
        DrawCommandList commands,
        PrismGraph graph)
    {
        int firstRootBegin = commands.Count;
        for (int index = 0; index < graph.Scopes.Length; index++)
        {
            PrismGraphScope scope = graph.Scopes[index];
            if (scope.Depth == 0)
            {
                firstRootBegin = Math.Min(
                    firstRootBegin,
                    scope.BeginCommandIndex);
            }
        }

        RenderRawRange(
            renderer,
            commands,
            0,
            firstRootBegin);
        return firstRootBegin;
    }

    private void RenderNode(
        IPrismCommandRenderer renderer,
        DrawCommandList commands,
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismSurfaceFrame frame,
        int step,
        PrismGraphNode node,
        IBackdropFrameLease? backdropLease)
    {
        RenderTarget2D target = frame.GetSurface(step);
        switch (node.Kind)
        {
            case PrismGraphNodeKind.ControlCapture:
                CaptureControl(
                    renderer,
                    commands,
                    graph,
                    target,
                    node);
                break;
            case PrismGraphNodeKind.BackdropInput:
                ClearSurface(
                    renderer,
                    target,
                    Microsoft.Xna.Framework.Color.Transparent);
                RecordFallback(
                    node,
                    backdropLease is null
                        ? PrismFallbackReason.MissingBackdrop
                        : PrismFallbackReason.UnsupportedCapability,
                    backdropLease is null
                        ? "The frame did not provide a backdrop lease."
                        : "The backdrop lease does not expose a MonoGame texture.");
                break;
            case PrismGraphNodeKind.ColorConversion:
                RenderColorConversion(
                    renderer,
                    plan,
                    graph,
                    frame,
                    step,
                    target,
                    node);
                break;
            case PrismGraphNodeKind.Layer:
            case PrismGraphNodeKind.Group:
                RenderCopyInput(
                    renderer,
                    plan,
                    graph,
                    frame,
                    step,
                    target,
                    node,
                    1f);
                break;
            case PrismGraphNodeKind.Filter:
            case PrismGraphNodeKind.Style:
                RecordFallback(
                    node,
                    PrismFallbackReason.MissingKernel,
                    node.DiagnosticName);
                RenderCopyInput(
                    renderer,
                    plan,
                    graph,
                    frame,
                    step,
                    target,
                    node,
                    1f);
                break;
            case PrismGraphNodeKind.Mask:
                ClearSurface(
                    renderer,
                    target,
                    Microsoft.Xna.Framework.Color.White);
                RecordFallback(
                    node,
                    PrismFallbackReason.UnsupportedCapability,
                    "Mask resource sampling is not available in the fundamental registry.");
                break;
            case PrismGraphNodeKind.Fill:
            case PrismGraphNodeKind.Opacity:
                RenderCopyInput(
                    renderer,
                    plan,
                    graph,
                    frame,
                    step,
                    target,
                    node,
                    Math.Clamp(node.Amount ?? 1f, 0f, 1f));
                break;
            case PrismGraphNodeKind.ClipToBelow:
                RenderTwoInputKernel(
                    renderer,
                    plan,
                    graph,
                    frame,
                    step,
                    target,
                    node,
                    PrismGraphEdgeKind.Content,
                    PrismGraphEdgeKind.ClipBaseAlpha,
                    kernels.ClipAlpha,
                    1f);
                break;
            case PrismGraphNodeKind.Composite:
            case PrismGraphNodeKind.PassThroughComposite:
                RenderComposite(
                    renderer,
                    plan,
                    graph,
                    frame,
                    step,
                    target,
                    node);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported Prism graph node kind '{node.Kind}'.");
        }
    }

    private void CaptureControl(
        IPrismCommandRenderer renderer,
        DrawCommandList commands,
        PrismGraph graph,
        RenderTarget2D target,
        PrismGraphNode node)
    {
        PrismGraphScope scope =
            FindScope(graph, node.AnalysisScopeIndex);
        int scopeIndex = scope.AnalysisScopeIndex;

        renderer.EndBatch();
        graphicsDevice.SetRenderTarget(target);
        if (!initializedCaptures[scopeIndex])
        {
            graphicsDevice.Clear(
                Microsoft.Xna.Framework.Color.Transparent);
            initializedCaptures[scopeIndex] = true;
        }

        renderer.BeginCommandBatch();
        RenderRawRange(
            renderer,
            commands,
            captureCommandIndices[scopeIndex],
            scope.EndCommandIndex);
        captureCommandIndices[scopeIndex] =
            scope.EndCommandIndex;
        renderer.EndBatch();
    }

    private void RenderColorConversion(
        IPrismCommandRenderer renderer,
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismSurfaceFrame frame,
        int step,
        RenderTarget2D target,
        PrismGraphNode node)
    {
        if (node.ColorProfile is not PrismColorProfile profile ||
            !kernels.TryGetColorConversionKernel(
                profile,
                out PrismKernel kernel))
        {
            RecordFallback(
                node,
                PrismFallbackReason.InvalidColorProfile,
                node.DiagnosticName);
            RenderCopyInput(
                renderer,
                plan,
                graph,
                frame,
                step,
                target,
                node,
                1f);
            return;
        }

        RenderSingleInputKernel(
            renderer,
            plan,
            graph,
            frame,
            step,
            target,
            node,
            kernel,
            1f);
    }

    private void RenderComposite(
        IPrismCommandRenderer renderer,
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismSurfaceFrame frame,
        int step,
        RenderTarget2D target,
        PrismGraphNode node)
    {
        int maskIndex = FindInputIndex(
            plan,
            graph,
            node.Id,
            PrismGraphEdgeKind.MaskAlpha);
        if (maskIndex >= 0)
        {
            RenderTwoInputKernel(
                renderer,
                plan,
                graph,
                frame,
                step,
                target,
                node,
                PrismGraphEdgeKind.Content,
                PrismGraphEdgeKind.MaskAlpha,
                kernels.MaskAlpha,
                1f);
            return;
        }

        int foregroundIndex = FindInputIndex(
            plan,
            graph,
            node.Id,
            PrismGraphEdgeKind.CompositeForeground);
        if (foregroundIndex < 0)
        {
            foregroundIndex = FindInputIndex(
                plan,
                graph,
                node.Id,
                PrismGraphEdgeKind.Content);
        }
        int backgroundIndex = FindInputIndex(
            plan,
            graph,
            node.Id,
            PrismGraphEdgeKind.CompositeBackground);

        if (foregroundIndex < 0)
        {
            ClearSurface(
                renderer,
                target,
                Microsoft.Xna.Framework.Color.Transparent);
            return;
        }
        if (backgroundIndex < 0)
        {
            RenderKernel(
                renderer,
                target,
                frame.GetSurface(foregroundIndex),
                frame.GetSurface(foregroundIndex),
                kernels.Copy,
                1f);
            return;
        }

        PrismBlendMode blendMode =
            node.BlendMode ?? PrismBlendMode.Normal;
        if (!kernels.TryGetBlendKernel(
            blendMode,
            out PrismKernel kernel))
        {
            RecordFallback(
                node,
                PrismFallbackReason.MissingKernel,
                node.DiagnosticName);
            RenderKernel(
                renderer,
                target,
                frame.GetSurface(foregroundIndex),
                frame.GetSurface(foregroundIndex),
                kernels.Copy,
                1f);
            return;
        }

        RenderKernel(
            renderer,
            target,
            frame.GetSurface(foregroundIndex),
            frame.GetSurface(backgroundIndex),
            kernel,
            1f);
    }

    private void RenderCopyInput(
        IPrismCommandRenderer renderer,
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismSurfaceFrame frame,
        int step,
        RenderTarget2D target,
        PrismGraphNode node,
        float opacity)
    {
        RenderSingleInputKernel(
            renderer,
            plan,
            graph,
            frame,
            step,
            target,
            node,
            kernels.Copy,
            opacity);
    }

    private void RenderSingleInputKernel(
        IPrismCommandRenderer renderer,
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismSurfaceFrame frame,
        int step,
        RenderTarget2D target,
        PrismGraphNode node,
        PrismKernel kernel,
        float opacity)
    {
        int sourceIndex =
            FindAnyInputIndex(plan, graph, node.Id);
        if (sourceIndex < 0)
        {
            ClearSurface(
                renderer,
                target,
                Microsoft.Xna.Framework.Color.Transparent);
            return;
        }

        RenderKernel(
            renderer,
            target,
            frame.GetSurface(sourceIndex),
            frame.GetSurface(sourceIndex),
            kernel,
            opacity);
    }

    private void RenderTwoInputKernel(
        IPrismCommandRenderer renderer,
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismSurfaceFrame frame,
        int step,
        RenderTarget2D target,
        PrismGraphNode node,
        PrismGraphEdgeKind sourceKind,
        PrismGraphEdgeKind secondaryKind,
        PrismKernel kernel,
        float opacity)
    {
        int sourceIndex = FindInputIndex(
            plan,
            graph,
            node.Id,
            sourceKind);
        int secondaryIndex = FindInputIndex(
            plan,
            graph,
            node.Id,
            secondaryKind);
        if (sourceIndex < 0 || secondaryIndex < 0)
        {
            RenderCopyInput(
                renderer,
                plan,
                graph,
                frame,
                step,
                target,
                node,
                opacity);
            return;
        }

        RenderKernel(
            renderer,
            target,
            frame.GetSurface(sourceIndex),
            frame.GetSurface(secondaryIndex),
            kernel,
            opacity);
    }

    private void RenderKernel(
        IPrismCommandRenderer renderer,
        RenderTarget2D target,
        Texture2D source,
        Texture2D secondary,
        PrismKernel kernel,
        float opacity)
    {
        renderer.EndBatch();
        graphicsDevice.SetRenderTarget(target);
        graphicsDevice.Clear(
            Microsoft.Xna.Framework.Color.Transparent);

        PrismKernelParameters parameters = new(
            secondary,
            opacity,
            new Vector2(
                1f / target.Width,
                1f / target.Height),
            FullUvScale,
            ZeroUvOffset);
        kernels.Bind(kernel, in parameters);
        renderer.BeginKernelBatch(
            kernels.Effect,
            BlendState.Opaque);
        renderer.DrawFullscreen(
            source,
            new Rectangle(0, 0, target.Width, target.Height));
        renderer.EndBatch();
    }

    private void ClearSurface(
        IPrismCommandRenderer renderer,
        RenderTarget2D target,
        Microsoft.Xna.Framework.Color color)
    {
        renderer.EndBatch();
        graphicsDevice.SetRenderTarget(target);
        graphicsDevice.Clear(color);
    }

    private void PresentCompletedNestedScopes(
        IPrismCommandRenderer renderer,
        DrawCommandList commands,
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismSurfaceFrame frame,
        int step,
        PrismGraphNode node)
    {
        for (int index = 0; index < graph.Scopes.Length; index++)
        {
            PrismGraphScope scope = graph.Scopes[index];
            if (scope.ParentScopeIndex is not int parentScopeIndex ||
                scope.Output is not PrismGraphNodeId output ||
                output != node.Id)
            {
                continue;
            }

            PrismGraphScope parentScope =
                FindScope(graph, parentScopeIndex);
            int parentCaptureStep =
                captureSteps[parentScopeIndex];
            if (parentCaptureStep < 0)
            {
                throw new InvalidOperationException(
                    $"Parent Prism scope {parentScopeIndex} has no control capture.");
            }

            RenderTarget2D parentTarget =
                frame.GetSurface(parentCaptureStep);
            renderer.EndBatch();
            graphicsDevice.SetRenderTarget(parentTarget);
            if (!initializedCaptures[parentScopeIndex])
            {
                graphicsDevice.Clear(
                    Microsoft.Xna.Framework.Color.Transparent);
                initializedCaptures[parentScopeIndex] = true;
            }

            renderer.BeginCommandBatch();
            RenderRawRange(
                renderer,
                commands,
                captureCommandIndices[parentScopeIndex],
                scope.BeginCommandIndex);

            PrismKernel presentKernel =
                GetPresentKernel(scope, node);
            if (IsScopeBypassed(scope.AnalysisScopeIndex))
            {
                RenderRawRange(
                    renderer,
                    commands,
                    scope.BeginCommandIndex + 1,
                    scope.EndCommandIndex);
                renderer.EndBatch();
            }
            else
            {
                renderer.EndBatch();
                RenderTarget2D source = frame.GetSurface(step);
                DrawPresentation(
                    renderer,
                    source,
                    parentTarget,
                    presentKernel,
                    new Rectangle(
                        0,
                        0,
                        parentTarget.Width,
                        parentTarget.Height));
                diagnostics.RecordPresentation(
                    PrismExecutionPassKind.NestedPresent,
                    node.Id,
                    scope.AnalysisScopeIndex);
            }

            captureCommandIndices[parentScopeIndex] =
                scope.EndCommandIndex + 1;
        }
    }

    private void PresentCompletedRootScopes(
        IPrismCommandRenderer renderer,
        DrawCommandList commands,
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismSurfaceFrame frame,
        int step,
        PrismGraphNode node,
        Viewport hostViewport,
        ref int hostCommandIndex)
    {
        for (int index = 0; index < graph.Scopes.Length; index++)
        {
            PrismGraphScope scope = graph.Scopes[index];
            if (scope.Depth != 0 ||
                scope.Output is not PrismGraphNodeId output ||
                output != node.Id)
            {
                continue;
            }

            renderer.EndBatch();
            renderer.RestoreHostTarget();
            renderer.BeginCommandBatch();
            RenderRawRange(
                renderer,
                commands,
                hostCommandIndex,
                scope.BeginCommandIndex);

            PrismKernel presentKernel =
                GetPresentKernel(scope, node);
            if (IsScopeBypassed(scope.AnalysisScopeIndex))
            {
                RenderRawRange(
                    renderer,
                    commands,
                    scope.BeginCommandIndex + 1,
                    scope.EndCommandIndex);
            }
            else
            {
                renderer.EndBatch();
                RenderTarget2D source = frame.GetSurface(step);
                DrawPresentation(
                    renderer,
                    source,
                    target: null,
                    presentKernel,
                    new Rectangle(
                        hostViewport.X,
                        hostViewport.Y,
                        hostViewport.Width,
                        hostViewport.Height));
                diagnostics.RecordPresentation(
                    PrismExecutionPassKind.RootPresent,
                    node.Id,
                    scope.AnalysisScopeIndex);
                renderer.BeginCommandBatch();
            }

            hostCommandIndex = scope.EndCommandIndex + 1;
            int nextRootBegin =
                FindNextRootBegin(graph, hostCommandIndex, commands.Count);
            RenderRawRange(
                renderer,
                commands,
                hostCommandIndex,
                nextRootBegin);
            hostCommandIndex = nextRootBegin;
        }
    }

    private void DrawPresentation(
        IPrismCommandRenderer renderer,
        RenderTarget2D source,
        RenderTarget2D? target,
        PrismKernel kernel,
        Rectangle destination)
    {
        if (target is not null)
        {
            graphicsDevice.SetRenderTarget(target);
        }

        PrismKernelParameters parameters = new(
            source,
            1f,
            new Vector2(
                1f / source.Width,
                1f / source.Height),
            FullUvScale,
            ZeroUvOffset);
        kernels.Bind(kernel, in parameters);
        renderer.BeginKernelBatch(
            kernels.Effect,
            BlendState.AlphaBlend);
        renderer.DrawFullscreen(source, destination);
        renderer.EndBatch();
    }

    private PrismKernel GetPresentKernel(
        PrismGraphScope scope,
        PrismGraphNode node)
    {
        return scope.CompositionSettings.WorkingColorProfile switch
        {
            PrismColorProfile.LinearSrgb => kernels.LinearToSrgb,
            PrismColorProfile.Srgb => kernels.Present,
            _ => RecordInvalidPresentProfile(scope, node)
        };
    }

    private PrismKernel RecordInvalidPresentProfile(
        PrismGraphScope scope,
        PrismGraphNode node)
    {
        RecordFallback(
            node,
            PrismFallbackReason.InvalidColorProfile,
            node.DiagnosticName);
        return kernels.Present;
    }

    private PrismFallbackAction RecordFallback(
        PrismGraphNode node,
        PrismFallbackReason reason,
        string detail)
    {
        PrismFallbackAction action = diagnostics.Record(
            node.Id,
            node.AnalysisScopeIndex,
            reason,
            detail);
        if (action == PrismFallbackAction.BypassComposition &&
            (uint)node.AnalysisScopeIndex <
                (uint)bypassedScopes.Length)
        {
            bypassedScopes[node.AnalysisScopeIndex] = true;
        }
        return action;
    }

    private bool IsScopeBypassed(int scopeIndex)
    {
        return (uint)scopeIndex < (uint)bypassedScopes.Length &&
            bypassedScopes[scopeIndex];
    }

    private static int FindInputIndex(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNodeId target,
        PrismGraphEdgeKind kind)
    {
        for (int edgeIndex = 0;
            edgeIndex < graph.Edges.Length;
            edgeIndex++)
        {
            PrismGraphEdge edge = graph.Edges[edgeIndex];
            if (edge.Target == target && edge.Kind == kind)
            {
                return FindExecutionIndex(plan, edge.Source);
            }
        }

        return -1;
    }

    private static int FindAnyInputIndex(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNodeId target)
    {
        for (int edgeIndex = 0;
            edgeIndex < graph.Edges.Length;
            edgeIndex++)
        {
            PrismGraphEdge edge = graph.Edges[edgeIndex];
            if (edge.Target == target)
            {
                return FindExecutionIndex(plan, edge.Source);
            }
        }

        return -1;
    }

    private static int FindExecutionIndex(
        PrismGraphExecutionPlan plan,
        PrismGraphNodeId nodeId)
    {
        for (int index = 0;
            index < plan.ExecutionOrder.Length;
            index++)
        {
            if (plan.ExecutionOrder[index] == nodeId)
            {
                return index;
            }
        }

        throw new InvalidOperationException(
            $"Prism execution node '{nodeId}' is not in the optimized plan.");
    }

    private static PrismGraphScope FindScope(
        PrismGraph graph,
        int analysisScopeIndex)
    {
        for (int index = 0; index < graph.Scopes.Length; index++)
        {
            if (graph.Scopes[index].AnalysisScopeIndex ==
                analysisScopeIndex)
            {
                return graph.Scopes[index];
            }
        }

        throw new InvalidOperationException(
            $"Prism graph scope '{analysisScopeIndex}' does not exist.");
    }

    private static int FindNextRootBegin(
        PrismGraph graph,
        int commandIndex,
        int fallback)
    {
        int result = fallback;
        for (int index = 0; index < graph.Scopes.Length; index++)
        {
            PrismGraphScope scope = graph.Scopes[index];
            if (scope.Depth == 0 &&
                scope.BeginCommandIndex >= commandIndex)
            {
                result = Math.Min(result, scope.BeginCommandIndex);
            }
        }
        return result;
    }

    private static void RenderRawRange(
        IPrismCommandRenderer renderer,
        DrawCommandList commands,
        int start,
        int end)
    {
        for (int index = start; index < end; index++)
        {
            DrawCommand command = commands[index];
            if (command.Kind is DrawCommandKind.BeginPrism or
                DrawCommandKind.EndPrism)
            {
                continue;
            }
            renderer.RenderCommand(command);
        }
    }
}

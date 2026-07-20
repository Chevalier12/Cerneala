using System.Diagnostics;
using Cerneala.Drawing.MonoGame.Prism.Kernels;
using Cerneala.Drawing.MonoGame.Prism.Surfaces;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Blending;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Prism.Styles;
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
                SurfaceFormat.HalfVector4,
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
            case PrismGraphNodeKind.Style:
                RenderStyle(
                    renderer,
                    plan,
                    graph,
                    frame,
                    step,
                    target,
                    node);
                break;
            case PrismGraphNodeKind.Mask:
                RenderMask(
                    renderer,
                    plan,
                    graph,
                    frame,
                    step,
                    target,
                    node);
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

    private void RenderMask(
        IPrismCommandRenderer renderer,
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismSurfaceFrame frame,
        int step,
        RenderTarget2D target,
        PrismGraphNode node)
    {
        if (node.MaskPass is null or PrismMaskPass.Extract)
        {
            RenderMaskExtract(renderer, graph, target, node);
            return;
        }

        int sourceIndex = FindAnyInputIndex(plan, graph, node.Id);
        if (sourceIndex < 0)
        {
            RenderOpaqueMaskFallback(
                renderer,
                target,
                node,
                PrismFallbackReason.MissingKernel,
                "A mask feather pass has no scalar input.");
            return;
        }

        PrismGraphScope scope =
            FindScope(graph, node.AnalysisScopeIndex);
        float transformScale = MathF.Max(
            MathF.Sqrt(
                (scope.EffectiveTransform.M11 *
                    scope.EffectiveTransform.M11) +
                (scope.EffectiveTransform.M12 *
                    scope.EffectiveTransform.M12)),
            MathF.Sqrt(
                (scope.EffectiveTransform.M21 *
                    scope.EffectiveTransform.M21) +
                (scope.EffectiveTransform.M22 *
                    scope.EffectiveTransform.M22)));
        float radius =
            (node.Feather ?? 0) *
            transformScale *
            scope.PixelScale;
        if (!float.IsFinite(radius))
        {
            RenderOpaqueMaskFallback(
                renderer,
                target,
                node,
                PrismFallbackReason.UnsupportedCapability,
                "The transformed mask feather radius is not finite.");
            return;
        }

        Vector2 featherStep =
            node.MaskPass == PrismMaskPass.FeatherHorizontal
                ? new Vector2(radius / target.Width, 0)
                : new Vector2(0, radius / target.Height);
        float density =
            node.MaskPass == PrismMaskPass.FeatherVertical
                ? node.Density ?? 1
                : 1;
        PrismMaskKernelSettings settings = new(
            Channel: 0,
            Density: density,
            Invert: 0,
            UvRowX: new Vector3(1, 0, 0),
            UvRowY: new Vector3(0, 1, 0),
            FeatherStep: featherStep);
        Texture2D source = frame.GetSurface(sourceIndex);
        RenderKernel(
            renderer,
            target,
            source,
            source,
            kernels.MaskFeather,
            1f,
            maskSettings: settings);
    }

    private void RenderStyle(
        IPrismCommandRenderer renderer,
        PrismGraphExecutionPlan executionPlan,
        PrismGraph graph,
        PrismSurfaceFrame frame,
        int step,
        RenderTarget2D target,
        PrismGraphNode node)
    {
        if (node.Style is not PrismStyleId style ||
            !kernels.TryGetStyleKernel(
                style,
                out PrismKernel kernel))
        {
            RecordFallback(
                node,
                PrismFallbackReason.MissingKernel,
                node.DiagnosticName);
            RenderCopyInput(
                renderer,
                executionPlan,
                graph,
                frame,
                step,
                target,
                node,
                1f);
            return;
        }

        int contentIndex = FindInputIndex(
            executionPlan,
            graph,
            node.Id,
            PrismGraphEdgeKind.Content);
        int sourceIndex = FindInputIndex(
            executionPlan,
            graph,
            node.Id,
            PrismGraphEdgeKind.StyleSource);
        if (contentIndex < 0 || sourceIndex < 0)
        {
            RecordFallback(
                node,
                PrismFallbackReason.MissingKernel,
                "A layer-style pass has no content or prepared source.");
            RenderCopyInput(
                renderer,
                executionPlan,
                graph,
                frame,
                step,
                target,
                node,
                1f);
            return;
        }

        PrismGraphScope scope =
            FindScope(graph, node.AnalysisScopeIndex);
        PrismStylePlan stylePlan =
            PrismStylePlanner.Create(node, scope);
        Texture2D styleTexture =
            frame.GetSurface(sourceIndex);
        bool resourceAvailable = false;
        if (stylePlan.ResourceEnabled)
        {
            if (stylePlan.Resource.Value <= 0 ||
                !scope.Resources.TryGetImage(
                    stylePlan.Resource,
                    out IDrawImage image))
            {
                if (stylePlan.ResourceRequired)
                {
                    RecordFallback(
                        node,
                        PrismFallbackReason.MissingResource,
                        $"Style resource '{stylePlan.Resource}' is not available.");
                    RenderCopyInput(
                        renderer,
                        executionPlan,
                        graph,
                        frame,
                        step,
                        target,
                        node,
                        1f);
                    return;
                }
            }
            else if (image is not MonoGameImage monoGameImage ||
                monoGameImage.Texture.IsDisposed ||
                !ReferenceEquals(
                    monoGameImage.Texture.GraphicsDevice,
                    graphicsDevice))
            {
                RecordFallback(
                    node,
                    PrismFallbackReason.UnsupportedCapability,
                    "The style resource is not a live MonoGame texture owned by this graphics device.");
                RenderCopyInput(
                    renderer,
                    executionPlan,
                    graph,
                    frame,
                    step,
                    target,
                    node,
                    1f);
                return;
            }
            else
            {
                styleTexture = monoGameImage.Texture;
                resourceAvailable = true;
            }
        }

        PrismStyleSamplingGeometry geometry =
            PrismStylePlanner.ResolveSamplingGeometry(
                stylePlan,
                scope);
        PrismStyleKernelSettings settings = new(
            styleTexture,
            ToVector4(stylePlan.PrimaryColor),
            ToVector4(stylePlan.SecondaryColor),
            new Vector4(
                geometry.Offset.X / target.Width,
                geometry.Offset.Y / target.Height,
                geometry.Size,
                geometry.Spread),
            new Vector4(
                stylePlan.Angle * (MathF.PI / 180f),
                stylePlan.Altitude * (MathF.PI / 180f),
                stylePlan.Depth,
                geometry.Soften),
            new Vector4(
                stylePlan.Opacity,
                stylePlan.SecondaryOpacity,
                stylePlan.Noise,
                stylePlan.Jitter),
            new Vector4(
                stylePlan.Scale,
                stylePlan.TextureDepth,
                stylePlan.Offset.X,
                stylePlan.Offset.Y),
            new Vector4(
                stylePlan.Kind,
                (int)stylePlan.BlendMode,
                (int)stylePlan.SecondaryBlendMode,
                (int)stylePlan.PaintKind),
            new Vector4(
                stylePlan.Contour,
                stylePlan.DetailContour,
                stylePlan.Technique,
                stylePlan.Position),
            new Vector4(
                stylePlan.Origin,
                stylePlan.Direction,
                stylePlan.GradientMethod,
                stylePlan.GradientStyle),
            new Vector4(
                stylePlan.BevelStyle,
                (int)stylePlan.Flags,
                stylePlan.Range,
                0),
            resourceAvailable);
        RenderKernel(
            renderer,
            target,
            frame.GetSurface(contentIndex),
            frame.GetSurface(sourceIndex),
            kernel,
            1f,
            styleSettings: settings);
    }

    private void RenderMaskExtract(
        IPrismCommandRenderer renderer,
        PrismGraph graph,
        RenderTarget2D target,
        PrismGraphNode node)
    {
        PrismGraphScope scope =
            FindScope(graph, node.AnalysisScopeIndex);
        if (node.Resource is not UI.Prism.Definitions.PrismResourceId
                resource ||
            !scope.Resources.TryGetImage(resource, out IDrawImage image))
        {
            RenderOpaqueMaskFallback(
                renderer,
                target,
                node,
                PrismFallbackReason.MissingResource,
                $"Mask resource '{node.Resource?.ToString() ?? "<none>"}' is not available.");
            return;
        }
        if (image is not MonoGameImage maskImage ||
            maskImage.Texture.IsDisposed ||
            !ReferenceEquals(
                maskImage.Texture.GraphicsDevice,
                graphicsDevice))
        {
            RenderOpaqueMaskFallback(
                renderer,
                target,
                node,
                PrismFallbackReason.UnsupportedCapability,
                "The mask resource is not a live MonoGame texture owned by this graphics device.");
            return;
        }

        DrawRect bounds = scope.ControlBounds;
        if (bounds.Width <= 0 ||
            bounds.Height <= 0 ||
            !System.Numerics.Matrix3x2.Invert(
                scope.EffectiveTransform,
                out System.Numerics.Matrix3x2 inverse))
        {
            RenderOpaqueMaskFallback(
                renderer,
                target,
                node,
                PrismFallbackReason.UnsupportedCapability,
                "The mask mapping requires non-empty bounds and an invertible transform.");
            return;
        }

        float pixelScale = scope.PixelScale;
        Vector3 uvRowX = new(
            inverse.M11 / (pixelScale * bounds.Width),
            inverse.M21 / (pixelScale * bounds.Width),
            (inverse.M31 - bounds.X) / bounds.Width);
        Vector3 uvRowY = new(
            inverse.M12 / (pixelScale * bounds.Height),
            inverse.M22 / (pixelScale * bounds.Height),
            (inverse.M32 - bounds.Y) / bounds.Height);
        float density = (node.Feather ?? 0) > 0
            ? 1
            : node.Density ?? 1;
        PrismMaskKernelSettings settings = new(
            Channel: (float)(
                node.MaskChannel ??
                UI.Prism.Definitions.PrismMaskChannel.Alpha),
            Density: density,
            Invert: node.Invert == true ? 1 : 0,
            UvRowX: uvRowX,
            UvRowY: uvRowY,
            FeatherStep: Vector2.Zero);
        RenderKernel(
            renderer,
            target,
            maskImage.Texture,
            maskImage.Texture,
            kernels.MaskExtract,
            1f,
            maskSettings: settings);
    }

    private void RenderOpaqueMaskFallback(
        IPrismCommandRenderer renderer,
        RenderTarget2D target,
        PrismGraphNode node,
        PrismFallbackReason reason,
        string detail)
    {
        ClearSurface(
            renderer,
            target,
            Microsoft.Xna.Framework.Color.White);
        RecordFallback(node, reason, detail);
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
            backgroundIndex >= 0
                ? frame.GetSurface(backgroundIndex)
                : frame.GetSurface(foregroundIndex),
            kernel,
            1f,
            node.LayerSettings,
            node.DefinitionNodeId?.Value ?? 0,
            backgroundIndex >= 0);
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
        float opacity,
        PrismGraphLayerSettings? layerSettings = null,
        int layerIdentity = 0,
        bool backgroundAvailable = true,
        PrismMaskKernelSettings? maskSettings = null,
        PrismStyleKernelSettings? styleSettings = null)
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
            ZeroUvOffset)
        {
            BackgroundAvailable =
                backgroundAvailable ? 1 : 0
        };
        if (layerSettings is PrismGraphLayerSettings settings)
        {
            parameters = parameters with
            {
                BlendChannels = ResolveBlendChannels(
                    settings.BlendChannels),
                KnockoutMode = (float)settings.Knockout,
                BlendIfChannel =
                    (float)settings.BlendIfChannel,
                ThisLayerRange = ResolveBlendRange(
                    settings.ThisLayerRange),
                UnderlyingRange = ResolveBlendRange(
                    settings.UnderlyingRange),
                DissolveSeed =
                    PrismBlendMath.NormalizeDissolveSeed(
                        settings.DissolveSeed,
                        layerIdentity)
            };
        }
        if (maskSettings is PrismMaskKernelSettings mask)
        {
            parameters = parameters with
            {
                MaskChannel = mask.Channel,
                MaskDensity = mask.Density,
                MaskInvert = mask.Invert,
                MaskUvRowX = mask.UvRowX,
                MaskUvRowY = mask.UvRowY,
                MaskFeatherStep = mask.FeatherStep
            };
        }
        if (styleSettings is PrismStyleKernelSettings style)
        {
            parameters = parameters with
            {
                StyleTexture = style.Texture,
                StyleColor = style.Color,
                StyleSecondaryColor =
                    style.SecondaryColor,
                StyleGeometry0 = style.Geometry0,
                StyleGeometry1 = style.Geometry1,
                StyleOptions0 = style.Options0,
                StyleOptions1 = style.Options1,
                StyleModes0 = style.Modes0,
                StyleModes1 = style.Modes1,
                StyleModes2 = style.Modes2,
                StyleModes3 = style.Modes3,
                StyleResourceAvailable =
                    style.ResourceAvailable ? 1 : 0
            };
        }
        kernels.Bind(kernel, in parameters);
        renderer.BeginKernelBatch(
            kernels.Effect,
            BlendState.Opaque);
        renderer.DrawFullscreen(
            source,
            new Rectangle(0, 0, target.Width, target.Height));
        renderer.EndBatch();
    }

    private static Vector4 ResolveBlendChannels(
        UI.Prism.Runtime.PrismBlendChannels channels)
    {
        return new Vector4(
            (channels &
                UI.Prism.Runtime.PrismBlendChannels.Red) != 0
                ? 1
                : 0,
            (channels &
                UI.Prism.Runtime.PrismBlendChannels.Green) != 0
                ? 1
                : 0,
            (channels &
                UI.Prism.Runtime.PrismBlendChannels.Blue) != 0
                ? 1
                : 0,
            (channels &
                UI.Prism.Runtime.PrismBlendChannels.Alpha) != 0
                ? 1
                : 0);
    }

    private static Vector4 ToVector4(
        System.Numerics.Vector4 value) =>
        new(value.X, value.Y, value.Z, value.W);

    private static Vector4 ResolveBlendRange(
        UI.Prism.Runtime.PrismBlendRange range)
    {
        return new Vector4(
            range.BlackStart,
            range.BlackEnd,
            range.WhiteStart,
            range.WhiteEnd);
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
        if (kernels.TryGetPresentKernel(
            scope.CompositionSettings.WorkingColorProfile,
            out PrismKernel kernel))
        {
            return kernel;
        }

        return RecordInvalidPresentProfile(scope, node);
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

    private readonly record struct PrismMaskKernelSettings(
        float Channel,
        float Density,
        float Invert,
        Vector3 UvRowX,
        Vector3 UvRowY,
        Vector2 FeatherStep);

    private readonly record struct PrismStyleKernelSettings(
        Texture2D Texture,
        Vector4 Color,
        Vector4 SecondaryColor,
        Vector4 Geometry0,
        Vector4 Geometry1,
        Vector4 Options0,
        Vector4 Options1,
        Vector4 Modes0,
        Vector4 Modes1,
        Vector4 Modes2,
        Vector4 Modes3,
        bool ResourceAvailable);
}

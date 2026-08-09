using Cerneala.Drawing.MonoGame.Prism.Kernels;
using Cerneala.Drawing.MonoGame.Prism.Surfaces;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame.Prism.Execution;

internal sealed class PrismGraphPresentation
{
    private static readonly Vector2 FullUvScale = Vector2.One;
    private static readonly Vector2 ZeroUvOffset = Vector2.Zero;

    private readonly GraphicsDevice graphicsDevice;
    private readonly PrismKernelRegistry kernels;
    private readonly PrismExecutionDiagnostics diagnostics;
    private readonly PrismColorProfile hostColorProfile;
    private readonly PrismGraphExecutionCache executionCache;
    private readonly PrismGraphFallbackTracker fallbackTracker;
    private int[] captureSteps = [];
    private int[] captureCommandIndices = [];
    private bool[] initializedCaptures = [];

    public PrismGraphPresentation(
        GraphicsDevice graphicsDevice,
        PrismKernelRegistry kernels,
        PrismExecutionDiagnostics diagnostics,
        PrismColorProfile hostColorProfile,
        PrismGraphExecutionCache executionCache,
        PrismGraphFallbackTracker fallbackTracker)
    {
        this.graphicsDevice = graphicsDevice;
        this.kernels = kernels;
        this.diagnostics = diagnostics;
        this.hostColorProfile = hostColorProfile;
        this.executionCache = executionCache;
        this.fallbackTracker = fallbackTracker;
    }

    public void Prepare(
        PrismGraphExecutionPlan plan,
        PrismGraph graph)
    {
        int requiredScopeSlots = 0;
        for (int index = 0; index < graph.Scopes.Length; index++)
        {
            requiredScopeSlots = Math.Max(
                requiredScopeSlots,
                graph.Scopes[index].AnalysisScopeIndex + 1);
        }
        if (captureSteps.Length < requiredScopeSlots)
        {
            Array.Resize(ref captureSteps, requiredScopeSlots);
            Array.Resize(ref captureCommandIndices, requiredScopeSlots);
            Array.Resize(ref initializedCaptures, requiredScopeSlots);
        }
        Array.Clear(initializedCaptures);
        Array.Fill(captureSteps, -1, 0, requiredScopeSlots);
        fallbackTracker.Prepare(requiredScopeSlots);

        for (int index = 0; index < plan.ExecutionOrder.Length; index++)
        {
            PrismGraphNode node = graph.GetNode(plan.ExecutionOrder[index]);
            if (node.Kind != PrismGraphNodeKind.ControlCapture)
            {
                continue;
            }

            PrismGraphScope scope =
                FindScope(graph, node.AnalysisScopeIndex);
            captureSteps[node.AnalysisScopeIndex] = index;
            captureCommandIndices[node.AnalysisScopeIndex] =
                scope.BeginCommandIndex + 1;
        }
    }

    public void CaptureControl(
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
        captureCommandIndices[scopeIndex] = scope.EndCommandIndex;
        renderer.EndBatch();
    }

    public void PresentCompletedNestedScopes(
        IPrismCommandRenderer renderer,
        DrawCommandList commands,
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
            int parentCaptureStep = captureSteps[parentScopeIndex];
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

            PrismKernel presentKernel = GetPresentKernel(scope, node);
            if (fallbackTracker.IsScopeBypassed(
                    scope.AnalysisScopeIndex))
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
                RenderTarget2D source =
                    executionCache.GetExecutionSurface(frame, step);
                DrawPresentation(
                    renderer,
                    source,
                    parentTarget,
                    presentKernel,
                    scope.CompositionSettings.WorkingColorProfile,
                    new Rectangle(
                        0,
                        0,
                        parentTarget.Width,
                        parentTarget.Height));
                diagnostics.RecordPresentation(
                    PrismExecutionPassKind.NestedPresent,
                    node,
                    scope.AnalysisScopeIndex);
            }

            captureCommandIndices[parentScopeIndex] =
                scope.EndCommandIndex + 1;
        }
    }

    public void PresentCompletedRootScopes(
        IPrismCommandRenderer renderer,
        DrawCommandList commands,
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

            PrismKernel presentKernel = GetPresentKernel(scope, node);
            if (fallbackTracker.IsScopeBypassed(
                    scope.AnalysisScopeIndex))
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
                RenderTarget2D source =
                    executionCache.GetExecutionSurface(frame, step);
                DrawPresentation(
                    renderer,
                    source,
                    target: null,
                    presentKernel,
                    scope.CompositionSettings.WorkingColorProfile,
                    new Rectangle(
                        hostViewport.X,
                        hostViewport.Y,
                        hostViewport.Width,
                        hostViewport.Height));
                diagnostics.RecordPresentation(
                    PrismExecutionPassKind.RootPresent,
                    node,
                    scope.AnalysisScopeIndex);
                renderer.BeginCommandBatch();
            }

            hostCommandIndex = scope.EndCommandIndex + 1;
            int nextRootBegin = FindNextRootBegin(
                graph,
                hostCommandIndex,
                commands.Count);
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
        PrismColorProfile sourceProfile,
        Rectangle destination)
    {
        if (target is not null)
        {
            graphicsDevice.SetRenderTarget(target);
        }

        PrismKernelParameters parameters = new(
            source,
            1f,
            new Vector2(1f / source.Width, 1f / source.Height),
            FullUvScale,
            ZeroUvOffset)
        {
            FilterHeader = new Vector4(
                (float)sourceProfile,
                (float)hostColorProfile,
                0,
                0)
        };
        kernels.Bind(kernel, in parameters);
        renderer.BeginKernelBatch(
            kernels.Effect,
            BlendState.AlphaBlend,
            SamplerState.LinearClamp);
        renderer.DrawFullscreen(source, destination);
        renderer.EndBatch();
    }

    private PrismKernel GetPresentKernel(
        PrismGraphScope scope,
        PrismGraphNode node)
    {
        if (hostColorProfile == PrismColorProfile.Srgb &&
            kernels.TryGetPresentKernel(
                scope.CompositionSettings.WorkingColorProfile,
                out PrismKernel sRgbKernel))
        {
            return sRgbKernel;
        }
        if (Enum.IsDefined(
                scope.CompositionSettings.WorkingColorProfile) &&
            Enum.IsDefined(hostColorProfile))
        {
            return kernels.BackdropColorConversion;
        }

        fallbackTracker.Record(
            node,
            PrismFallbackReason.InvalidColorProfile,
            node.DiagnosticName);
        return kernels.Present;
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

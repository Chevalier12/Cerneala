using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Blending;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Prism.Styles;
using Cerneala.Drawing.Prism.Surfaces;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Backends.SdlGpu;

internal sealed partial class SdlGpuPrismExecutor
{
    private static float ResolveMaskRadius(PrismGraphNode node, PrismGraphScope scope)
    {
        float scale = MathF.Max(
            new Vector2(scope.EffectiveTransform.M11, scope.EffectiveTransform.M12).Length(),
            new Vector2(scope.EffectiveTransform.M21, scope.EffectiveTransform.M22).Length());
        return (node.Feather ?? 0) * scale * scope.PixelScale;
    }

    private bool ResolveScopeUvMapping(
        PrismGraphScope scope,
        out Vector3 rowX,
        out Vector3 rowY)
    {
        DrawRect bounds = scope.ControlBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0 ||
            !Matrix3x2.Invert(scope.EffectiveTransform, out Matrix3x2 inverse))
        {
            rowX = Vector3.Zero;
            rowY = Vector3.Zero;
            return false;
        }
        // Effect DPI describes kernel parameters. Scene commands can already be
        // in pixels, so raster positions use the destination's coordinate scale.
        float targetScale = drawingBackend.CoordinateScale;
        rowX = new Vector3(
            inverse.M11 / (targetScale * bounds.Width),
            inverse.M21 / (targetScale * bounds.Width),
            ((executionOriginPixelX * inverse.M11 +
                executionOriginPixelY * inverse.M21) / targetScale +
                inverse.M31 - bounds.X) / bounds.Width);
        rowY = new Vector3(
            inverse.M12 / (targetScale * bounds.Height),
            inverse.M22 / (targetScale * bounds.Height),
            ((executionOriginPixelX * inverse.M12 +
                executionOriginPixelY * inverse.M22) / targetScale +
                inverse.M32 - bounds.Y) / bounds.Height);
        return true;
    }

    private static SdlRect ResolveBackdropDestination(
        DrawRect bounds,
        float pixelScale,
        SdlGpuRenderTarget target,
        int originPixelX,
        int originPixelY)
    {
        int left = (int)Math.Clamp(
            MathF.Floor(bounds.X * pixelScale) - originPixelX,
            0,
            target.PixelWidth);
        int top = (int)Math.Clamp(
            MathF.Floor(bounds.Y * pixelScale) - originPixelY,
            0,
            target.PixelHeight);
        int right = (int)Math.Clamp(
            MathF.Ceiling(bounds.Right * pixelScale) - originPixelX,
            0,
            target.PixelWidth);
        int bottom = (int)Math.Clamp(
            MathF.Ceiling(bounds.Bottom * pixelScale) - originPixelY,
            0,
            target.PixelHeight);
        return new SdlRect(
            left,
            top,
            Math.Max(0, right - left),
            Math.Max(0, bottom - top));
    }

    private SdlRect? ResolvePresentationClip(
        PrismGraphExecutionPlan plan,
        PrismGraphScope scope,
        PrismGraphNode node,
        SdlGpuRenderTarget target,
        int originPixelX = 0,
        int originPixelY = 0)
    {
        PrismGraphNodePlan nodePlan = plan.GetNodePlan(node.Id);
        if (nodePlan.BoundsStatus == PrismGraphBoundsStatus.Unknown)
        {
            return null;
        }

        // Both bounds must be in host coordinates; ControlBounds is local.
        DrawRect bounds = UnionBounds(nodePlan.Bounds, scope.Bounds);
        // RenderSurface2D commands already use surface pixels. The owner's
        // effect DPI must not scale their host-space positions a second time.
        float pixelScale = drawingBackend.CoordinateScale;
        int left = (int)Math.Clamp(
            MathF.Floor(bounds.X * pixelScale) - PresentationSamplingOutset - originPixelX,
            0,
            target.PixelWidth);
        int top = (int)Math.Clamp(
            MathF.Floor(bounds.Y * pixelScale) - PresentationSamplingOutset - originPixelY,
            0,
            target.PixelHeight);
        int right = (int)Math.Clamp(
            MathF.Ceiling(bounds.Right * pixelScale) + PresentationSamplingOutset - originPixelX,
            0,
            target.PixelWidth);
        int bottom = (int)Math.Clamp(
            MathF.Ceiling(bounds.Bottom * pixelScale) + PresentationSamplingOutset - originPixelY,
            0,
            target.PixelHeight);
        return new SdlRect(
            left,
            top,
            Math.Max(0, right - left),
            Math.Max(0, bottom - top));
    }

    private void ResolveExecutionExtent(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        SdlGpuRenderTarget hostTarget)
    {
        if (graph.Nodes.Any(RequiresHostCoordinates))
        {
            executionOriginPixelX = 0;
            executionOriginPixelY = 0;
            executionPixelWidth = hostTarget.PixelWidth;
            executionPixelHeight = hostTarget.PixelHeight;
            return;
        }

        int left = hostTarget.PixelWidth - 1;
        int top = hostTarget.PixelHeight - 1;
        int right = hostTarget.PixelWidth;
        int bottom = hostTarget.PixelHeight;
        bool hasKnownOutput = false;
        foreach (PrismGraphScope scope in graph.Scopes)
        {
            if (scope.Output is not PrismGraphNodeId output)
            {
                continue;
            }

            PrismGraphNodePlan outputPlan = plan.GetNodePlan(output);
            if (outputPlan.BoundsStatus == PrismGraphBoundsStatus.Unknown)
            {
                executionOriginPixelX = 0;
                executionOriginPixelY = 0;
                executionPixelWidth = hostTarget.PixelWidth;
                executionPixelHeight = hostTarget.PixelHeight;
                return;
            }

            DrawRect bounds = UnionBounds(outputPlan.Bounds, scope.Bounds);
            float pixelScale = drawingBackend.CoordinateScale;
            int scopeLeft =
                (int)MathF.Floor(bounds.X * pixelScale) -
                PresentationSamplingOutset;
            int scopeTop =
                (int)MathF.Floor(bounds.Y * pixelScale) -
                PresentationSamplingOutset;
            int scopeRight =
                (int)MathF.Ceiling(bounds.Right * pixelScale) +
                PresentationSamplingOutset;
            int scopeBottom =
                (int)MathF.Ceiling(bounds.Bottom * pixelScale) +
                PresentationSamplingOutset;
            if (!hasKnownOutput)
            {
                left = scopeLeft;
                top = scopeTop;
                right = scopeRight;
                bottom = scopeBottom;
            }
            else
            {
                left = Math.Min(left, scopeLeft);
                top = Math.Min(top, scopeTop);
                right = Math.Max(right, scopeRight);
                bottom = Math.Max(bottom, scopeBottom);
            }
            hasKnownOutput = true;
        }

        if (!hasKnownOutput)
        {
            executionOriginPixelX = 0;
            executionOriginPixelY = 0;
            executionPixelWidth = hostTarget.PixelWidth;
            executionPixelHeight = hostTarget.PixelHeight;
            return;
        }

        SetExecutionExtent(AlignExecutionExtent(left, top, right, bottom, hostTarget));
    }

    private void ResolveScopeExecutionExtents(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        SdlGpuRenderTarget hostTarget,
        PrismFrameAnalysis analysis)
    {
        scopeExecutionExtents.Clear();
        inputReferenceExtents.Clear();
        foreach (PrismAnalyzedScope analyzed in analysis.Scopes)
        {
            if (analyzed.Scope.InputBounds is null) { continue; }
            DrawRect reference = DrawCommandStateAnalyzer.TransformBounds(
                analyzed.Scope.ControlBounds, analyzed.EffectiveTransform);
            inputReferenceExtents.Add(analyzed.ScopeIndex, CreateInputExtent(reference, outset: 0));
        }
        hostCoordinateScopes.Clear();
        foreach (PrismGraphNode node in graph.Nodes)
        {
            if (RequiresHostCoordinates(node))
            {
                hostCoordinateScopes.Add(node.AnalysisScopeIndex);
            }
        }
        foreach (PrismGraphScope scope in graph.Scopes)
        {
            PrismRasterExtent extent = new(0, 0, hostTarget.PixelWidth, hostTarget.PixelHeight);
            if (inputReferenceExtents.ContainsKey(scope.AnalysisScopeIndex) && scope.Output is PrismGraphNodeId inputOutput)
            {
                PrismGraphNodePlan outputPlan = plan.GetNodePlan(inputOutput);
                if (outputPlan.BoundsStatus == PrismGraphBoundsStatus.Unknown)
                {
                    throw new InvalidOperationException("A complete scene Prism input requires finite execution bounds.");
                }
                // Input pixels may lie outside the destination. Only presentation
                // is clipped to the host; capture and analysis must retain them.
                extent = CreateInputExtent(UnionBounds(outputPlan.Bounds, scope.Bounds),
                    ResolveInputSamplingOutset(graph, scope));
            }
            else if (!hostCoordinateScopes.Contains(scope.AnalysisScopeIndex) &&
                scope.Output is PrismGraphNodeId output)
            {
                PrismGraphNodePlan outputPlan = plan.GetNodePlan(output);
                if (outputPlan.BoundsStatus != PrismGraphBoundsStatus.Unknown)
                {
                    DrawRect bounds = UnionBounds(outputPlan.Bounds, scope.Bounds);
                    float scale = drawingBackend.CoordinateScale;
                    extent = AlignExecutionExtent(
                        (int)MathF.Floor(bounds.X * scale) - PresentationSamplingOutset,
                        (int)MathF.Floor(bounds.Y * scale) - PresentationSamplingOutset,
                        (int)MathF.Ceiling(bounds.Right * scale) + PresentationSamplingOutset,
                        (int)MathF.Ceiling(bounds.Bottom * scale) + PresentationSamplingOutset,
                        hostTarget);
                }
            }
            scopeExecutionExtents.Add(scope.AnalysisScopeIndex, extent);
        }
    }

    private PrismRasterExtent GetReferenceExtent(int scopeIndex) =>
        inputReferenceExtents.TryGetValue(scopeIndex, out PrismRasterExtent input) ? input : referenceExtent;

    private static int ResolveInputSamplingOutset(PrismGraph graph, PrismGraphScope scope)
    {
        float maskRadius = 0;
        foreach (PrismGraphNode node in graph.Nodes)
        {
            if (node.AnalysisScopeIndex == scope.AnalysisScopeIndex && node.Kind == PrismGraphNodeKind.Mask &&
                node.MaskPass is PrismMaskPass.FeatherHorizontal or PrismMaskPass.FeatherVertical)
            {
                maskRadius = MathF.Max(maskRadius, ResolveMaskRadius(node, scope));
            }
        }
        // Feathering reads the independent mask outside the selected scene input.
        // Keep that finite raster halo, not extra scene payloads or draw commands.
        double outset = Math.Ceiling(maskRadius) + PresentationSamplingOutset;
        if (!double.IsFinite(outset) || outset > int.MaxValue)
        {
            throw new InvalidOperationException("The scene Prism mask exceeds representable raster dimensions.");
        }
        return (int)outset;
    }

    private PrismRasterExtent CreateInputExtent(DrawRect bounds, int outset)
    {
        double scale = drawingBackend.CoordinateScale;
        double left = Math.Floor(bounds.X * scale) - outset;
        double top = Math.Floor(bounds.Y * scale) - outset;
        double right = Math.Ceiling(bounds.Right * scale) + outset;
        double bottom = Math.Ceiling(bounds.Bottom * scale) + outset;
        double width = Math.Max(1, right - left), height = Math.Max(1, bottom - top);
        if (!double.IsFinite(left) || !double.IsFinite(top) || !double.IsFinite(width) || !double.IsFinite(height) ||
            left < int.MinValue || left > int.MaxValue || top < int.MinValue || top > int.MaxValue ||
            width > int.MaxValue || height > int.MaxValue)
        {
            throw new InvalidOperationException("The scene Prism input exceeds representable raster dimensions.");
        }
        return new((int)left, (int)top, (int)width, (int)height);
    }

    private static bool RequiresHostCoordinates(PrismGraphNode node) =>
        node.Filter is PrismFilterId filter &&
        (PrismNeighborhoodPlanner.RequiresStableHostCoordinates(filter) ||
            PrismResamplingPlanner.RequiresStableHostCoordinates(filter) ||
            PrismCatalogFilterPlanner.RequiresStableHostCoordinates(filter));

    private void SetExecutionExtent(PrismRasterExtent extent)
    {
        executionOriginPixelX = extent.X;
        executionOriginPixelY = extent.Y;
        executionPixelWidth = extent.Width;
        executionPixelHeight = extent.Height;
    }

    private static PrismRasterExtent AlignExecutionExtent(
        int left, int top, int right, int bottom, SdlGpuRenderTarget hostTarget)
    {
        int clampedLeft = Math.Clamp(left, 0, hostTarget.PixelWidth - 1);
        int clampedTop = Math.Clamp(top, 0, hostTarget.PixelHeight - 1);
        int clampedRight = Math.Clamp(
            right,
            clampedLeft + 1,
            hostTarget.PixelWidth);
        int clampedBottom = Math.Clamp(
            bottom,
            clampedTop + 1,
            hostTarget.PixelHeight);
        int originX = AlignDown(clampedLeft, ExecutionSurfaceTileSize);
        int originY = AlignDown(clampedTop, ExecutionSurfaceTileSize);
        int alignedRight = AlignUp(
            clampedRight,
            ExecutionSurfaceTileSize,
            hostTarget.PixelWidth);
        int alignedBottom = AlignUp(
            clampedBottom,
            ExecutionSurfaceTileSize,
            hostTarget.PixelHeight);
        return new(originX, originY, alignedRight - originX, alignedBottom - originY);
    }

    private static int AlignDown(int value, int alignment) =>
        value - (value % alignment);

    private static int AlignUp(int value, int alignment, int maximum)
    {
        int remainder = value % alignment;
        return remainder == 0
            ? value
            : (int)Math.Min(
                (long)value + alignment - remainder,
                maximum);
    }

    private static DrawRect UnionBounds(DrawRect first, DrawRect second)
    {
        if (first.Width <= 0 || first.Height <= 0)
        {
            return second;
        }
        if (second.Width <= 0 || second.Height <= 0)
        {
            return first;
        }

        float left = MathF.Min(first.X, second.X);
        float top = MathF.Min(first.Y, second.Y);
        float right = MathF.Max(first.Right, second.Right);
        float bottom = MathF.Max(first.Bottom, second.Bottom);
        return new DrawRect(left, top, right - left, bottom - top);
    }
}

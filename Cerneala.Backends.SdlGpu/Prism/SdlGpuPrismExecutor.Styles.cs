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
    private void RenderStyle(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNode node,
        SdlGpuRenderTarget target,
        PrismRasterPass? auxiliary = null)
    {
        PrismGraphNode owner = auxiliary is PrismRasterPass pass ? graph.GetNode(pass.Owner) : node;
        int contentIndex = FindInputIndex(plan, graph, node.Id, PrismGraphEdgeKind.Content);
        int sourceIndex = FindInputIndex(plan, graph, node.Id, PrismGraphEdgeKind.StyleSource);
        if (contentIndex < 0 || sourceIndex < 0 || owner.Style is not PrismStyleId style)
        {
            RenderSingleInput(plan, graph, node, target, 0, 1);
            return;
        }
        PrismGraphScope scope = FindScope(graph, node.AnalysisScopeIndex);
        PrismStylePlan stylePlan = rasterPlan.Styles[owner.Id];
        nint content = GetSurface(contentIndex).SampleTexture;
        nint source = GetSurface(sourceIndex).SampleTexture;
        int backdropIndex = FindInputIndex(
            plan,
            graph,
            node.Id,
            PrismGraphEdgeKind.CompositeBackground);
        nint backdrop = backdropIndex >= 0
            ? GetSurface(backdropIndex).SampleTexture
            : source;
        nint styleTexture = source;
        bool resourceAvailable = false;
        if (style == PrismStyleId.GradientOverlay)
        {
            PrismGradientMapResource gradient = PrismGradientOverlayStyle.DefaultGradient;
            long identity = 0;
            long version = 0;
            if (stylePlan.ResourceEnabled &&
                !scope.Resources.TryGetGradientMap(
                    stylePlan.Resource,
                    out gradient,
                    out identity,
                    out version))
            {
                diagnostics.Record(
                    node.Id,
                    node.AnalysisScopeIndex,
                    PrismFallbackReason.MissingResource,
                    $"Gradient resource '{stylePlan.Resource}' is not available.");
                RenderKernel(target, content, content, 0, 1, node);
                return;
            }

            styleTexture = deviceResources.GetGradientOverlayTexture(
                session,
                stylePlan.Resource,
                gradient,
                identity,
                version,
                (PrismGradientInterpolation)stylePlan.GradientMethod,
                scope.CompositionSettings.WorkingColorProfile);
            resourceAvailable = true;
        }
        else if (stylePlan.ResourceEnabled)
        {
            if (!TryResolveImage(
                    scope,
                    node,
                    stylePlan.Resource,
                    stylePlan.ResourceRequired,
                    source,
                    out nint resolved,
                    out resourceAvailable) &&
                stylePlan.ResourceRequired)
            {
                RenderKernel(target, content, content, 0, 1, node);
                return;
            }
            styleTexture = resolved;
        }
        PrismStyleSamplingGeometry geometry = PrismStylePlanner.ResolveSamplingGeometry(stylePlan, scope);
        ResolveScopeUvMapping(scope, out Vector3 rowX, out Vector3 rowY);
        bool alignGradientWithLayer =
            (stylePlan.Flags & PrismStyleFlags.AlignWithLayer) != 0;
        Vector2 gradientOffset = alignGradientWithLayer
            ? new Vector2(
                stylePlan.Offset.X / MathF.Max(scope.ControlBounds.Width, 1),
                stylePlan.Offset.Y / MathF.Max(scope.ControlBounds.Height, 1))
            : new Vector2(
                (stylePlan.Offset.X * scope.PixelScale) / currentReferenceExtent.Width,
                (stylePlan.Offset.Y * scope.PixelScale) / currentReferenceExtent.Height);
        float gradientAspect = alignGradientWithLayer
            ? scope.ControlBounds.Width / MathF.Max(scope.ControlBounds.Height, 1)
            : currentReferenceExtent.Width / (float)currentReferenceExtent.Height;
        nint maskTexture = auxiliary is not null
            ? content
            : FindOptionalInput(plan, graph, node, PrismGraphEdgeKind.PreparedInput, source);
        int kernel = auxiliary?.Kind switch
        {
            PrismRasterPassKind.BevelHeight => 87,
            PrismRasterPassKind.BevelLighting => 88,
            null => 82,
            _ => throw new InvalidOperationException("Only bevel auxiliary passes use the style bindings.")
        };
        nint rasterSource = auxiliary?.Kind == PrismRasterPassKind.BevelHeight ? source : content;
        PrepareBaseUniforms(rasterSource, source, kernel, 1);
        ConfigureStyle(
            stylePlan,
            style == PrismStyleId.GradientOverlay,
            geometry,
            rowX,
            rowY,
            styleTexture,
            maskTexture,
            source,
            backdrop,
            resourceAvailable,
            backdropIndex >= 0,
            gradientAspect,
            gradientOffset);
        RenderPrepared(target, rasterSource, source);
    }

    private void ConfigureStyle(
        PrismStylePlan stylePlan,
        bool gradientOverlay,
        PrismStyleSamplingGeometry geometry,
        Vector3 rowX,
        Vector3 rowY,
        nint styleTexture,
        nint maskTexture,
        nint source,
        nint backdrop,
        bool resourceAvailable,
        bool backdropAvailable,
        float gradientAspect,
        Vector2 gradientOffset)
    {
        textures[4] = styleTexture;
        textures[5] = maskTexture;
        textures[8] = backdrop;
        textures[9] = deviceResources.GetGradientDitherTexture(session);
        textures[11] = maskTexture;
        textures[12] = source;
        uniforms[10] = stylePlan.PrimaryColor;
        uniforms[11] = stylePlan.SecondaryColor;
        uniforms[12] = new Vector4(
            geometry.Offset.X / Math.Max(executionPixelWidth, 1),
            geometry.Offset.Y / Math.Max(executionPixelHeight, 1),
            geometry.Size,
            geometry.Spread);
        uniforms[13] = new Vector4(
            stylePlan.Angle * MathF.PI / 180,
            stylePlan.Altitude * MathF.PI / 180,
            stylePlan.Depth,
            geometry.Soften);
        uniforms[14] = new Vector4(stylePlan.Opacity, stylePlan.SecondaryOpacity,
            stylePlan.Noise, stylePlan.Jitter);
        uniforms[15] = new Vector4(
            stylePlan.Scale,
            gradientOverlay
                ? gradientAspect
                : stylePlan.TextureDepth,
            gradientOverlay
                ? gradientOffset.X
                : stylePlan.Offset.X,
            gradientOverlay
                ? gradientOffset.Y
                : stylePlan.Offset.Y);
        uniforms[16] = new Vector4(
            stylePlan.Kind,
            SdlGpuPrismKernelSelector.ResolveBlendMode(stylePlan.BlendMode),
            SdlGpuPrismKernelSelector.ResolveBlendMode(stylePlan.SecondaryBlendMode),
            (int)stylePlan.PaintKind);
        uniforms[17] = new Vector4(stylePlan.Contour, stylePlan.DetailContour,
            stylePlan.Technique, stylePlan.Position);
        uniforms[18] = new Vector4(stylePlan.Origin, stylePlan.Direction,
            stylePlan.GradientMethod, stylePlan.GradientStyle);
        uniforms[19] = new Vector4(stylePlan.BevelStyle, (int)stylePlan.Flags,
            stylePlan.Range, 0);
        uniforms[20] = new Vector4(rowX, 0);
        uniforms[21] = new Vector4(rowY, 0);
        uniforms[22] = new Vector4(
            resourceAvailable ? 1 : 0,
            backdropAvailable ? 1 : 0,
            0,
            0);
    }

    private void RenderStyleDistanceFloodPass(
        SdlGpuRenderTarget target,
        nint source,
        int jump)
    {
        PrepareBaseUniforms(source, source, 86, 1);
        uniforms[6] = new Vector4(1, 0, 0, 0);
        uniforms[7] = new Vector4(1, 0, 0, 0);
        uniforms[8] = new Vector4(0, 1, 0, 0);
        uniforms[9] = new Vector4(
            jump / (float)target.PixelWidth,
            jump / (float)target.PixelHeight,
            0,
            0);
        RenderPrepared(target, source, source);
    }

    private void RenderStyleMaskPass(
        SdlGpuRenderTarget target,
        nint source,
        int kernelId,
        float radius,
        bool horizontal)
    {
        PrepareBaseUniforms(source, source, kernelId, 1);
        textures[12] = source;
        uniforms[6] = new Vector4(1, 0, radius, 0);
        uniforms[7] = new Vector4(1, 0, 0, 0);
        uniforms[8] = new Vector4(0, 1, 0, 0);
        uniforms[9] = horizontal
            ? new Vector4(1f / target.PixelWidth, 0, 0, 0)
            : new Vector4(0, 1f / target.PixelHeight, 0, 0);
        RenderPrepared(target, source, source);
    }
}

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
    private int RenderHostPrelude(
        DrawCommandList commands,
        PrismGraph graph,
        DrawCommandStateAnalysis analysis,
        SdlGpuRenderTarget hostTarget,
        SdlGpuDrawingBackend.CommandRangeState hostState)
    {
        int firstRoot = commands.Count;
        foreach (PrismGraphScope scope in graph.Scopes)
        {
            if (scope.Depth == 0)
            {
                firstRoot = Math.Min(firstRoot, scope.BeginCommandIndex);
            }
        }
        drawingBackend.RenderCommandRange(
            commands,
            0,
            firstRoot,
            analysis,
            hostTarget,
            childSurfaces: null,
            hostState);
        return firstRoot;
    }

    private void RenderNode(
        DrawCommandList commands,
        DrawCommandStateAnalysis stateAnalysis,
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        int step,
        PrismGraphNode node,
        SdlGpuRenderTarget target,
        IBackdropFrameLease? backdropLease)
    {
        switch (node.Kind)
        {
            case PrismGraphNodeKind.RasterAuxiliary:
                RenderRasterAuxiliary(plan, graph, node, target);
                return;
            case PrismGraphNodeKind.ControlCapture:
                RenderControlCapture(commands, stateAnalysis, plan, graph, node, target);
                return;
            case PrismGraphNodeKind.BackdropInput:
                RenderBackdropInput(graph, node, target, backdropLease);
                return;
            case PrismGraphNodeKind.Filter:
                RenderFilter(plan, graph, node, target);
                return;
            case PrismGraphNodeKind.Style:
                RenderStyle(plan, graph, node, target);
                return;
            case PrismGraphNodeKind.Mask:
                RenderMask(plan, graph, node, target);
                return;
            case PrismGraphNodeKind.Composite:
            case PrismGraphNodeKind.PassThroughComposite:
                RenderComposite(plan, graph, node, target);
                return;
            case PrismGraphNodeKind.ClipToBelow:
                RenderTwoInput(plan, graph, node, target, PrismGraphEdgeKind.Content,
                    PrismGraphEdgeKind.ClipBaseAlpha, 43);
                return;
            case PrismGraphNodeKind.BackdropCrop:
                RenderBackdropCrop(plan, graph, node, target);
                return;
            case PrismGraphNodeKind.ColorConversion:
                RenderColorConversion(plan, graph, node, target);
                return;
            case PrismGraphNodeKind.Fill:
            case PrismGraphNodeKind.Opacity:
                RenderSingleInput(plan, graph, node, target, 0,
                    Math.Clamp(node.Amount ?? 1, 0, 1));
                return;
            case PrismGraphNodeKind.Layer:
            case PrismGraphNodeKind.Group:
                RenderSingleInput(plan, graph, node, target, 0, 1);
                return;
            default:
                Clear(target, Color.Transparent);
                return;
        }
    }

    private static SdlGpuTextureFormat ResolveSurfaceFormat(PrismRasterSurfaceFormat format) =>
        format switch
        {
            PrismRasterSurfaceFormat.Rgba16Float => SdlGpuTextureFormat.R16G16B16A16Float,
            PrismRasterSurfaceFormat.Rgba32Float => SdlGpuTextureFormat.R32G32B32A32Float,
            PrismRasterSurfaceFormat.R32Float => SdlGpuTextureFormat.R32Float,
            PrismRasterSurfaceFormat.Rgba8Unorm => SdlGpuTextureFormat.R8G8B8A8Unorm,
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

    private void RenderRasterAuxiliary(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNode node,
        SdlGpuRenderTarget target)
    {
        PrismRasterPass pass = rasterPlan.AuxiliaryPasses[node.Id];
        nint source = GetSurface(FindInputIndex(plan, graph, node.Id,
            PrismGraphEdgeKind.Content)).SampleTexture;
        switch (pass.Kind)
        {
            case PrismRasterPassKind.ThresholdCdf:
                PrepareBaseUniforms(source, source, 4, 1);
                uniforms[23] = new Vector4(0,
                    (int)FindScope(graph, node.AnalysisScopeIndex).CompositionSettings.WorkingColorProfile, 0, 0);
                RenderPrepared(target, source, source);
                return;
            case PrismRasterPassKind.ThresholdSelection:
                PrepareBaseUniforms(source, source, 6, 1);
                uniforms[23] = new Vector4(pass.RadiusOrJump, 0, 0, 0);
                RenderPrepared(target, source, source);
                return;
            case PrismRasterPassKind.ShadowSpread:
            case PrismRasterPassKind.ShadowBlur:
                RenderStyleMaskPass(target, source,
                    pass.Kind == PrismRasterPassKind.ShadowSpread ? 83 : 84,
                    pass.RadiusOrJump, pass.Horizontal);
                return;
            case PrismRasterPassKind.DistanceSeed:
                PrepareBaseUniforms(source, source, 85, 1);
                textures[12] = source;
                uniforms[16] = new Vector4(rasterPlan.Styles[pass.Owner].Kind, 0, 0, 0);
                RenderPrepared(target, source, source);
                return;
            case PrismRasterPassKind.DistanceFlood:
                RenderStyleDistanceFloodPass(target, source, checked((int)pass.RadiusOrJump));
                return;
            case PrismRasterPassKind.BevelHeight:
            case PrismRasterPassKind.BevelLighting:
                RenderStyle(plan, graph, node, target, pass);
                return;
            default:
                throw new InvalidOperationException($"Unsupported shared raster pass '{pass.Kind}'.");
        }
    }

    private void RenderControlCapture(
        DrawCommandList commands,
        DrawCommandStateAnalysis analysis,
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNode node,
        SdlGpuRenderTarget target)
    {
        PrismGraphScope scope = FindScope(graph, node.AnalysisScopeIndex);
        childPresentationSurfaces.Clear();
        try
        {
            foreach (PrismGraphScope child in graph.Scopes)
            {
                if (child.ParentScopeIndex == scope.AnalysisScopeIndex &&
                    child.Output is PrismGraphNodeId output)
                {
                    int index = plan.GetExecutionIndex(output);
                    if (surfaces.TryGetValue(index, out SdlGpuPrismSurfaceLease childLease))
                    {
                        PrismGraphNode childOutput = graph.GetNode(output);
                        PrismRasterExtent childExtent = scopeExecutionExtents[child.AnalysisScopeIndex];
                        childPresentationSurfaces.Add(
                            child.BeginCommandIndex,
                            new SdlGpuPrismPresentationSurface(
                                childLease.Target,
                                ResolvePresentationClip(
                                    plan,
                                    child,
                                    childOutput,
                                    target,
                                    executionOriginPixelX,
                                    executionOriginPixelY),
                                child.CompositionSettings.WorkingColorProfile,
                                new DrawRect(
                                    (childExtent.X - executionOriginPixelX) / drawingBackend.CoordinateScale,
                                    (childExtent.Y - executionOriginPixelY) / drawingBackend.CoordinateScale,
                                    childLease.Target.PixelWidth / drawingBackend.CoordinateScale,
                                    childLease.Target.PixelHeight / drawingBackend.CoordinateScale)));
                        diagnostics.RecordPresentation(
                            PrismExecutionPassKind.NestedPresent,
                            childOutput,
                            child.AnalysisScopeIndex);
                    }
                }
            }

            session.BeginRenderTarget(target, Color.Transparent, SdlGpuLoadOp.Clear);
            drawingBackend.RenderCommandRange(
                commands,
                scope.BeginCommandIndex + 1,
                scope.EndCommandIndex,
                analysis,
                target,
                childPresentationSurfaces,
                logicalOrigin: new Vector2(
                    executionOriginPixelX / drawingBackend.CoordinateScale,
                    executionOriginPixelY / drawingBackend.CoordinateScale),
                isolateCompositingState: true,
                captureClip: (scope.ControlBounds, scope.EffectiveTransform));
        }
        finally
        {
            childPresentationSurfaces.Clear();
        }
    }

    private void RenderBackdropInput(
        PrismGraph graph,
        PrismGraphNode node,
        SdlGpuRenderTarget target,
        IBackdropFrameLease? backdropLease)
    {
        if (backdropLease is not ISdlGpuBackdropFrameLease lease)
        {
            Clear(target, Color.Transparent);
            diagnostics.Record(
                node.Id,
                node.AnalysisScopeIndex,
                backdropLease is null
                    ? PrismFallbackReason.MissingBackdrop
                    : PrismFallbackReason.UnsupportedCapability,
                "The active backdrop lease does not expose an SDL_GPU texture.");
            return;
        }
        try
        {
            nint texture = lease.Texture;
            BackdropFrameMetadata metadata = lease.Metadata;
            Matrix3x2 transform = metadata.CoordinateTransform;
            float pixelScale = FindScope(graph, node.AnalysisScopeIndex).PixelScale;
            // Map the provider raster exactly once into the bounded execution space.
            // Later graph crops must address this snapshot, not the provider's dimensions.
            PrepareBaseUniforms(texture, texture, 1, 1);
            uniforms[0] = new Vector4(1, 1f / metadata.PixelWidth, 1f / metadata.PixelHeight, executionOriginPixelX);
            uniforms[6] = new Vector4(1, (float)metadata.AlphaMode, 0, 0);
            uniforms[7] = new Vector4(
                transform.M11 / (pixelScale * metadata.PixelWidth),
                transform.M21 / (pixelScale * metadata.PixelWidth),
                ((executionOriginPixelX * transform.M11 + executionOriginPixelY * transform.M21) / pixelScale +
                    transform.M31) / metadata.PixelWidth, 0);
            uniforms[8] = new Vector4(
                transform.M12 / (pixelScale * metadata.PixelHeight),
                transform.M22 / (pixelScale * metadata.PixelHeight),
                ((executionOriginPixelX * transform.M12 + executionOriginPixelY * transform.M22) / pixelScale +
                    transform.M32) / metadata.PixelHeight, 0);
            RenderPrepared(target, texture, texture);
        }
        catch (Exception exception) when (exception is ObjectDisposedException or InvalidOperationException)
        {
            Clear(target, Color.Transparent);
            diagnostics.Record(node.Id, node.AnalysisScopeIndex,
                PrismFallbackReason.UnsupportedCapability, exception.Message);
        }
    }

    private void RenderBackdropCrop(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNode node,
        SdlGpuRenderTarget target)
    {
        int sourceIndex = FindAnyInputIndex(plan, graph, node.Id);
        if (sourceIndex < 0 ||
            node.BackdropSourceBounds is not DrawRect sourceBounds ||
            sourceBounds.Width <= 0 ||
            sourceBounds.Height <= 0)
        {
            Clear(target, Color.Transparent);
            return;
        }

        PrismGraphScope scope = FindScope(graph, node.AnalysisScopeIndex);
        BackdropFrameMetadata? metadata = FindBackdropMetadata(plan, graph, node.Id);
        if (metadata is null)
        {
            Clear(target, Color.Transparent);
            diagnostics.Record(
                node.Id,
                node.AnalysisScopeIndex,
                PrismFallbackReason.MissingBackdrop,
                "The backdrop crop has no raster metadata.");
            return;
        }

        DrawRect backdropBounds = scope.Output is PrismGraphNodeId output
            ? plan.GetNodePlan(output).Bounds
            : scope.Bounds;
        SdlRect destination = ResolveBackdropDestination(
            backdropBounds,
            scope.PixelScale,
            target,
            executionOriginPixelX,
            executionOriginPixelY);
        if (destination.Width <= 0 || destination.Height <= 0)
        {
            Clear(target, Color.Transparent);
            return;
        }

        SdlGpuRenderTarget snapshot = GetSurface(sourceIndex);
        nint source = snapshot.SampleTexture;
        PrepareBaseUniforms(source, source, 1, 1);
        uniforms[0] = new Vector4(
            1,
            1f / snapshot.PixelWidth,
            1f / snapshot.PixelHeight,
            executionOriginPixelX);
        uniforms[6] = new Vector4(
            1,
            (float)BackdropAlphaMode.Premultiplied,
            0,
            0);
        uniforms[7] = new Vector4(1f / snapshot.PixelWidth, 0, 0, 0);
        uniforms[8] = new Vector4(0, 1f / snapshot.PixelHeight, 0, 0);
        RenderPrepared(target, source, source, destination);
    }

    private void RenderColorConversion(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNode node,
        SdlGpuRenderTarget target)
    {
        if (node.BackdropMetadata is null)
        {
            if (node.ColorProfile is PrismColorProfile profile && PrismEnumValidation.IsDefined(profile))
            {
                RenderSingleInput(
                    plan,
                    graph,
                    node,
                    target,
                    SdlGpuPrismKernelSelector.ForInputColorProfile(profile),
                    1);
                return;
            }

            diagnostics.Record(
                node.Id,
                node.AnalysisScopeIndex,
                PrismFallbackReason.InvalidColorProfile,
                node.DiagnosticName);
            RenderSingleInput(plan, graph, node, target, 0, 1);
            return;
        }

        PrismColorProfile sourceProfile = node.BackdropMetadata.Value.ColorProfile;
        if (node.ColorProfile is not PrismColorProfile targetProfile ||
            !PrismEnumValidation.IsDefined(sourceProfile) ||
            !PrismEnumValidation.IsDefined(targetProfile))
        {
            diagnostics.Record(
                node.Id,
                node.AnalysisScopeIndex,
                PrismFallbackReason.InvalidColorProfile,
                node.DiagnosticName);
            RenderSingleInput(plan, graph, node, target, 0, 1);
            return;
        }

        int sourceIndex = FindAnyInputIndex(plan, graph, node.Id);
        if (sourceIndex < 0)
        {
            Clear(target, Color.Transparent);
            return;
        }

        nint source = GetSurface(sourceIndex).SampleTexture;
        PrepareBaseUniforms(source, source, 2, 1);
        uniforms[23] = new Vector4(
            (float)sourceProfile,
            (float)targetProfile,
            0,
            0);
        RenderPrepared(target, source, source);
    }

    private void RenderMask(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNode node,
        SdlGpuRenderTarget target)
    {
        if (node.MaskPass is null or PrismMaskPass.Extract)
        {
            PrismGraphScope scope = FindScope(graph, node.AnalysisScopeIndex);
            if (node.Resource is not PrismResourceId id ||
                !TryResolveImage(scope, node, id, required: true, 0, out nint texture, out _))
            {
                Clear(target, Color.White);
                return;
            }
            PrepareBaseUniforms(texture, texture, 41, 1);
            uniforms[6] = new Vector4(
                1,
                (float)(node.MaskChannel ?? PrismMaskChannel.Alpha),
                (node.Feather ?? 0) > 0 ? 1 : node.Density ?? 1,
                node.Invert == true ? 1 : 0);
            ResolveScopeUvMapping(scope, out Vector3 rowX, out Vector3 rowY);
            uniforms[7] = new Vector4(rowX, 0);
            uniforms[8] = new Vector4(rowY, 0);
            RenderPrepared(target, texture, texture);
            return;
        }
        int sourceIndex = FindAnyInputIndex(plan, graph, node.Id);
        if (sourceIndex < 0)
        {
            Clear(target, Color.White);
            return;
        }
        nint source = GetSurface(sourceIndex).SampleTexture;
        PrismGraphScope owner = FindScope(graph, node.AnalysisScopeIndex);
        float radius = ResolveMaskRadius(node, owner);
        PrepareBaseUniforms(source, source, 42, 1);
        uniforms[6] = new Vector4(1, 0,
            node.MaskPass == PrismMaskPass.FeatherVertical ? node.Density ?? 1 : 1, 0);
        uniforms[9] = node.MaskPass == PrismMaskPass.FeatherHorizontal
            ? new Vector4(radius / target.PixelWidth, 0, 0, 0)
            : new Vector4(0, radius / target.PixelHeight, 0, 0);
        RenderPrepared(target, source, source);
    }

    private void RenderComposite(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNode node,
        SdlGpuRenderTarget target)
    {
        int maskIndex = FindInputIndex(plan, graph, node.Id, PrismGraphEdgeKind.MaskAlpha);
        if (maskIndex >= 0)
        {
            RenderTwoInput(plan, graph, node, target, PrismGraphEdgeKind.Content,
                PrismGraphEdgeKind.MaskAlpha, 40);
            return;
        }
        int foreground = FindInputIndex(plan, graph, node.Id, PrismGraphEdgeKind.CompositeForeground);
        if (foreground < 0)
        {
            foreground = FindInputIndex(plan, graph, node.Id, PrismGraphEdgeKind.Content);
        }
        if (foreground < 0)
        {
            Clear(target, Color.Transparent);
            return;
        }
        int background = FindInputIndex(plan, graph, node.Id, PrismGraphEdgeKind.CompositeBackground);
        nint source = GetSurface(foreground).SampleTexture;
        nint secondary = background >= 0 ? GetSurface(background).SampleTexture : source;
        PrepareBaseUniforms(source, secondary, SdlGpuPrismKernelSelector.ForNode(node), 1);
        Vector4 maskControl = uniforms[6];
        maskControl.X = background >= 0 ? 1 : 0;
        uniforms[6] = maskControl;
        if (node.LayerSettings is PrismGraphLayerSettings settings)
        {
            uniforms[2] = ResolveBlendChannels(settings.BlendChannels);
            uniforms[3] = new Vector4((int)settings.Knockout, 0,
                (int)settings.BlendIfChannel,
                PrismBlendMath.NormalizeDissolveSeed(
                    settings.DissolveSeed,
                    node.DefinitionNodeId?.Value ?? 0));
            uniforms[4] = ResolveBlendRange(settings.ThisLayerRange);
            uniforms[5] = ResolveBlendRange(settings.UnderlyingRange);
        }
        int knockoutBackdrop = FindInputIndex(plan, graph, node.Id, PrismGraphEdgeKind.KnockoutBackdrop);
        int knockoutShape = FindInputIndex(plan, graph, node.Id, PrismGraphEdgeKind.KnockoutShape);
        if (knockoutBackdrop >= 0)
        {
            textures[2] = GetSurface(knockoutBackdrop).SampleTexture;
            Vector4 blendControl = uniforms[3];
            blendControl.Y = 1;
            uniforms[3] = blendControl;
        }
        if (knockoutShape >= 0)
        {
            textures[3] = GetSurface(knockoutShape).SampleTexture;
        }
        RenderPrepared(target, source, secondary);
    }

    private void RenderSingleInput(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNode node,
        SdlGpuRenderTarget target,
        int kernelId,
        float opacity)
    {
        int input = FindAnyInputIndex(plan, graph, node.Id);
        if (input < 0)
        {
            Clear(target, Color.Transparent);
            return;
        }
        nint source = GetSurface(input).SampleTexture;
        RenderKernel(target, source, source, kernelId, opacity, node);
    }

    private void RenderTwoInput(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNode node,
        SdlGpuRenderTarget target,
        PrismGraphEdgeKind sourceKind,
        PrismGraphEdgeKind secondaryKind,
        int kernelId)
    {
        int sourceIndex = FindInputIndex(plan, graph, node.Id, sourceKind);
        int secondaryIndex = FindInputIndex(plan, graph, node.Id, secondaryKind);
        if (sourceIndex < 0 || secondaryIndex < 0)
        {
            RenderSingleInput(plan, graph, node, target, 0, 1);
            return;
        }
        nint source = GetSurface(sourceIndex).SampleTexture;
        nint secondary = GetSurface(secondaryIndex).SampleTexture;
        RenderKernel(target, source, secondary, kernelId, 1, node);
    }

    private void RenderKernel(
        SdlGpuRenderTarget target,
        nint source,
        nint secondary,
        int kernelId,
        float opacity,
        PrismGraphNode? node)
    {
        try
        {
            PrepareBaseUniforms(source, secondary, kernelId, opacity);
            RenderPrepared(target, source, secondary);
        }
        catch (Exception exception)
        {
            if (node is null)
            {
                throw;
            }
            diagnostics.Record(node.Id, node.AnalysisScopeIndex,
                PrismFallbackReason.ShaderUnavailable, exception.Message);
            if (kernelId == 0)
            {
                throw;
            }
            PrepareBaseUniforms(source, source, 0, opacity);
            RenderPrepared(target, source, source);
        }
    }

    private void PrepareBaseUniforms(nint source, nint secondary, int kernelId, float opacity)
    {
        uniforms.Reset();
        Array.Fill(textures, deviceResources.GetWhiteTexture(session));
        textures[0] = source != 0 ? source : textures[0];
        textures[1] = secondary != 0 ? secondary : textures[0];
        uniforms[0] = new Vector4(opacity,
            1f / Math.Max(executionPixelWidth, 1),
            1f / Math.Max(executionPixelHeight, 1),
            executionOriginPixelX);
        uniforms[1] = new Vector4(1, 1, 0, 0);
        uniforms[34] = new Vector4(
            executionPixelWidth,
            executionPixelHeight,
            kernelId,
            executionOriginPixelY);
        uniforms[59] = new Vector4(currentReferenceExtent.X, currentReferenceExtent.Y,
            currentReferenceExtent.Width, currentReferenceExtent.Height);
    }

    private void RenderPrepared(
        SdlGpuRenderTarget target,
        nint source,
        nint secondary,
        SdlRect? destination = null)
    {
        textures[0] = source;
        textures[1] = secondary;
        if (textures.Contains(target.SampleTexture))
        {
            throw new InvalidOperationException(
                "An SDL_GPU Prism render pass cannot sample from its active color target.");
        }
        session.BeginRenderTarget(target, Color.Transparent, SdlGpuLoadOp.Clear);
        SdlRect destinationRect = destination ?? new SdlRect(
            0,
            0,
            target.PixelWidth,
            target.PixelHeight);
        ISdlApi api = session.Api;
        nint pass = session.ActiveRenderPass;
        api.BindGpuGraphicsPipeline(pass, deviceResources.GetPipeline(target.ColorFormat));
        Span<float> viewport =
        [
            target.PixelWidth,
            target.PixelHeight,
            0,
            0,
            destinationRect.X,
            destinationRect.Y,
            destinationRect.Width,
            destinationRect.Height
        ];
        api.PushGpuVertexUniformData(session.ActiveCommandBuffer, 0,
            MemoryMarshal.AsBytes(viewport));
        api.PushGpuFragmentUniformData(session.ActiveCommandBuffer, 0, uniforms.Pack());
        for (int slot = 0; slot < textures.Length; slot++)
        {
            nint sampler = GetSamplerForSlot(slot);
            api.BindGpuFragmentSampler(pass, checked((uint)slot),
                new SdlGpuTextureSamplerBinding(textures[slot], sampler));
        }
        api.SetGpuScissor(pass, destinationRect);
        api.SetGpuStencilReference(pass, 0);
        api.DrawGpuPrimitives(pass, 3, 0);
    }

    private nint GetSamplerForSlot(int slot) => slot switch
    {
        0 when uniforms[34].Z == 8 &&
            uniforms[23].X == (int)PrismResamplingOperation.Transform =>
            session.DrawingResources.GetSampler(
                DrawSamplingMode.Linear,
                DrawAddressMode.Clamp,
                anisotropic: true),
        4 => session.DrawingResources.GetSampler(
            DrawSamplingMode.Linear,
            DrawAddressMode.Wrap),
        7 or 9 => session.DrawingResources.GetSampler(
            DrawSamplingMode.Point,
            DrawAddressMode.Wrap),
        10 or 11 or 13 or 14 => session.DrawingResources.GetSampler(
            DrawSamplingMode.Point,
            DrawAddressMode.Clamp),
        _ => session.DrawingResources.GetSampler(
            DrawSamplingMode.Linear,
            DrawAddressMode.Clamp)
    };

    private void Clear(SdlGpuRenderTarget target, Color color) =>
        session.BeginRenderTarget(target, color, SdlGpuLoadOp.Clear);

    private void PresentCompletedRoots(
        DrawCommandList commands,
        DrawCommandStateAnalysis analysis,
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        int step,
        PrismGraphNode node,
        SdlGpuRenderTarget hostTarget,
        SdlGpuDrawingBackend.CommandRangeState hostState,
        ref int hostCommandIndex)
    {
        int previousBeginCommandIndex = -1;
        while (TryFindNextCompletedRoot(
            graph,
            node.Id,
            previousBeginCommandIndex,
            out PrismGraphScope scope))
        {
            SdlGpuRenderTarget source = surfaces[step].Target;
            SdlGpuRenderTarget presentationTarget = hostState.Target;
            SdlRect? presentationClip = ResolvePresentationClip(
                plan,
                scope,
                node,
                presentationTarget);
            drawingBackend.DrawPrismTexture(
                source.SampleTexture,
                presentationTarget,
                presentationClip,
                new DrawRect(
                    executionOriginPixelX / drawingBackend.CoordinateScale,
                    executionOriginPixelY / drawingBackend.CoordinateScale,
                    source.PixelWidth / drawingBackend.CoordinateScale,
                    source.PixelHeight / drawingBackend.CoordinateScale),
                presentationState: hostState,
                workingColorProfile: scope.CompositionSettings.WorkingColorProfile);
            diagnostics.RecordPresentation(
                PrismExecutionPassKind.RootPresent,
                node,
                scope.AnalysisScopeIndex);
            hostCommandIndex = scope.EndCommandIndex + 1;
            int nextRoot = commands.Count;
            foreach (PrismGraphScope candidate in graph.Scopes)
            {
                if (candidate.Depth == 0 &&
                    candidate.BeginCommandIndex > scope.BeginCommandIndex)
                {
                    nextRoot = Math.Min(nextRoot, candidate.BeginCommandIndex);
                }
            }
            drawingBackend.RenderCommandRange(
                commands,
                hostCommandIndex,
                nextRoot,
                analysis,
                hostTarget,
                childSurfaces: null,
                hostState);
            hostCommandIndex = nextRoot;
            previousBeginCommandIndex = scope.BeginCommandIndex;
        }
    }

    private static bool TryFindNextCompletedRoot(
        PrismGraph graph,
        PrismGraphNodeId output,
        int previousBeginCommandIndex,
        out PrismGraphScope result)
    {
        result = default;
        bool found = false;
        foreach (PrismGraphScope scope in graph.Scopes)
        {
            if (scope.Depth != 0 ||
                scope.Output != output ||
                scope.BeginCommandIndex <= previousBeginCommandIndex ||
                found && scope.BeginCommandIndex >= result.BeginCommandIndex)
            {
                continue;
            }
            result = scope;
            found = true;
        }
        return found;
    }

    private bool TryResolveImage(
        PrismGraphScope scope,
        PrismGraphNode node,
        PrismResourceId id,
        bool required,
        nint fallback,
        out nint texture,
        out bool available)
    {
        if (id.Value > 0 && scope.Resources.TryGetImage(id, out IDrawImage image))
        {
            if (image is SdlGpuImage sdlImage)
            {
                texture = session.DrawingResources.GetOrCreateTexture(
                    session,
                    sdlImage,
                    sdlImage.Width,
                    sdlImage.Height,
                    sdlImage.RgbaPixels.Span).Handle;
                available = true;
                return true;
            }
            diagnostics.Record(node.Id, node.AnalysisScopeIndex,
                PrismFallbackReason.UnsupportedCapability,
                "The Prism image resource is not owned by SDL_GPU.");
            texture = fallback;
            available = false;
            return false;
        }
        texture = fallback;
        available = false;
        if (required)
        {
            diagnostics.Record(node.Id, node.AnalysisScopeIndex,
                PrismFallbackReason.MissingResource,
                $"Prism resource '{id}' is not available.");
            return false;
        }
        return true;
    }

    private static BackdropFrameMetadata? FindBackdropMetadata(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNodeId cropNodeId)
    {
        foreach (PrismGraphEdge edge in graph.Edges)
        {
            if (edge.Source != cropNodeId)
            {
                continue;
            }

            PrismGraphNode target = graph.GetNode(edge.Target);
            if (target.Kind == PrismGraphNodeKind.ColorConversion &&
                plan.GetExecutionIndex(target.Id) >= 0)
            {
                return target.BackdropMetadata;
            }
        }

        return null;
    }

    private static Vector4 ResolveBlendChannels(PrismBlendChannels channels) => new(
        (channels & PrismBlendChannels.Red) != 0 ? 1 : 0,
        (channels & PrismBlendChannels.Green) != 0 ? 1 : 0,
        (channels & PrismBlendChannels.Blue) != 0 ? 1 : 0,
        (channels & PrismBlendChannels.Alpha) != 0 ? 1 : 0);

    private static Vector4 ResolveBlendRange(PrismBlendRange range) =>
        new(range.BlackStart, range.BlackEnd, range.WhiteStart, range.WhiteEnd);
}

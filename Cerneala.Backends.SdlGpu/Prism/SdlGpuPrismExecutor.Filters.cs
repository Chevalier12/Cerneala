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
    private void RenderFilter(
        PrismGraphExecutionPlan plan,
        PrismGraph graph,
        PrismGraphNode node,
        SdlGpuRenderTarget target)
    {
        int sourceIndex = FindInputIndex(plan, graph, node.Id, PrismGraphEdgeKind.Content);
        if (sourceIndex < 0 || node.Filter is not PrismFilterId filter)
        {
            Clear(target, Color.Transparent);
            return;
        }
        nint source = GetSurface(sourceIndex).SampleTexture;
        PrismGraphScope scope = FindScope(graph, node.AnalysisScopeIndex);
        int kernelId = SdlGpuPrismKernelSelector.ForNode(node);
        PrepareBaseUniforms(source, source, kernelId, Math.Clamp(node.Amount ?? 1, 0, 1));

        if (node.NeighborhoodPlan is PrismNeighborhoodPlan neighborhood)
        {
            PrismNeighborhoodPass pass = neighborhood.Passes[node.NeighborhoodPassIndex];
            ResolveFilterResource(scope, node, neighborhood.Resource,
                neighborhood.ResourceRequired, source, out nint resource, out bool available);
            textures[1] = resource;
            textures[6] = FindOptionalInput(plan, graph, node, PrismGraphEdgeKind.FilterOriginal, source);
            uniforms[23] = new Vector4((int)neighborhood.Operation,
                (int)scope.CompositionSettings.WorkingColorProfile, (int)pass.Kind,
                available ? 1 : 0);
            SetFilterOptions(neighborhood.Options0, neighborhood.Options1,
                neighborhood.Options2, neighborhood.Options3);
            uniforms[33] = new Vector4(
                pass.RadiusX,
                pass.RadiusY,
                pass.SampleCount,
                SdlGpuPrismKernelSelector.ResolveBlendMode(neighborhood.BlendMode));
        }
        else if (node.ResamplingPlan is PrismResamplingPlan resampling)
        {
            PrismResamplingPass pass = resampling.Passes[node.ResamplingPassIndex];
            ResolveFilterResource(scope, node, resampling.PrimaryResource,
                resampling.PrimaryResourceRequired, source, out nint primary, out bool available);
            ResolveFilterResource(scope, node, resampling.AuxiliaryResource,
                resampling.AuxiliaryResourceRequired, source, out nint auxiliary, out bool auxAvailable);
            textures[1] = primary;
            textures[6] = FindOptionalInput(plan, graph, node, PrismGraphEdgeKind.FilterOriginal, auxiliary);
            uniforms[23] = new Vector4((int)resampling.Operation,
                (int)scope.CompositionSettings.WorkingColorProfile, (int)pass.Kind,
                available ? 1 : 0);
            SetFilterOptions(resampling.Options0, resampling.Options1, resampling.Options2,
                resampling.Options3, resampling.Options4, resampling.Options5);
            uniforms[30] = new Vector4(auxAvailable ? 1 : 0, 0, 0, 0);
            uniforms[33] = new Vector4(
                0,
                0,
                0,
                SdlGpuPrismKernelSelector.ResolveBlendMode(resampling.BlendMode));
        }
        else if (node.CatalogFilterPlan is PrismCatalogFilterPlan catalog)
        {
            PrismCatalogFilterPass pass = catalog.Passes[node.CatalogFilterPassIndex];
            ResolveFilterResource(scope, node, catalog.PrimaryResource,
                catalog.PrimaryResourceRequired, source, out nint primary, out bool available);
            ResolveFilterResource(scope, node, catalog.AuxiliaryResource,
                catalog.AuxiliaryResourceRequired, source, out nint auxiliary, out bool auxAvailable);
            textures[1] = primary;
            bool usesWaveNoise = filter is
                PrismFilterId.Clouds or
                PrismFilterId.DifferenceClouds;
            bool usesSpatter = filter == PrismFilterId.Spatter;
            bool usesBlueNoisePoints = usesSpatter ||
                filter == PrismFilterId.SprayedStrokes;
            nint filterAuxiliary = PrismCatalogFilterPlanner.RequiresOriginalInput(filter, pass)
                ? FindOptionalInput(plan, graph, node, PrismGraphEdgeKind.FilterOriginal, source)
                : usesWaveNoise
                    ? deviceResources.GetWaveNoiseTexture(session, catalog.WaveNoiseTable)
                    : usesBlueNoisePoints
                        ? deviceResources.GetSpatterPointTexture(session)
                        : auxiliary;
            textures[6] = filterAuxiliary;
            textures[10] = auxiliary;
            textures[13] = usesWaveNoise ? filterAuxiliary : textures[13];
            textures[14] = usesBlueNoisePoints ? filterAuxiliary : textures[14];
            uniforms[23] = new Vector4((int)catalog.Filter,
                (int)scope.CompositionSettings.WorkingColorProfile,
                (int)catalog.Primitive, (available ? 1 : 0) + (auxAvailable ? 2 : 0));
            SetFilterOptions(catalog.Options0, catalog.Options1, catalog.Options2,
                usesWaveNoise
                    ? PackSeed(catalog.WaveNoiseSeed)
                    : usesSpatter
                        ? PackSeed(catalog.SpatterSeed)
                        : catalog.Options3,
                catalog.Options4, catalog.Options5, catalog.Options6,
                catalog.Options7, catalog.Options8);
            uniforms[33] = new Vector4(
                usesWaveNoise ? catalog.WaveNoiseTable.Normalization : pass.RadiusX,
                pass.RadiusY,
                (int)pass.Kind + (pass.Iteration * 4),
                SdlGpuPrismKernelSelector.ResolveBlendMode(catalog.BlendMode));
        }
        else if (PrismAdjustmentPlanner.IsSupported(filter))
        {
            PrismAdjustmentPlan adjustment = rasterPlan.Adjustments[node.Id];
            ResolveFilterResource(scope, node, adjustment.Resource,
                adjustment.ResourceRequired, source, out nint resource, out bool available);
            if (filter == PrismFilterId.Threshold)
            {
                resource = GetSurface(FindInputIndex(plan, graph, node.Id,
                    PrismGraphEdgeKind.PreparedInput)).SampleTexture;
            }
            textures[1] = resource;
            uniforms[23] = new Vector4(
                (int)adjustment.Operation,
                (int)scope.CompositionSettings.WorkingColorProfile,
                SdlGpuPrismKernelSelector.ResolveBlendMode(adjustment.BlendMode),
                0);
            SetFilterOptions(adjustment.Parameters0, adjustment.Parameters1,
                adjustment.Parameters2, adjustment.Parameters3, adjustment.Parameters4,
                adjustment.Parameters5, adjustment.Parameters6, adjustment.Parameters7,
                adjustment.Parameters8, adjustment.Parameters9);
            if (adjustment.ResourceRequired && !available)
            {
                RenderKernel(target, source, source, 0, 1, node);
                return;
            }
            if (filter == PrismFilterId.ColorLookup && available &&
                scope.Resources.TryGetImage(adjustment.Resource, out IDrawImage lookup))
            {
                int level = (int)Math.Round(Math.Cbrt(lookup.Width));
                if (lookup.Width != lookup.Height || level < 2 ||
                    (long)level * level * level != lookup.Width)
                {
                    diagnostics.Record(node.Id, node.AnalysisScopeIndex,
                        PrismFallbackReason.UnsupportedCapability,
                        "ColorLookup requires a square Hald LUT whose side is level cubed (level >= 2).");
                    RenderKernel(target, source, source, 0, 1, node);
                    return;
                }
                Vector4 header = uniforms[23];
                header.W = level * level;
                uniforms[23] = header;
                Vector4 control = uniforms[34];
                control.X = lookup.Width;
                control.Y = lookup.Height;
                uniforms[34] = control;
            }
        }
        else
        {
            diagnostics.Record(node.Id, node.AnalysisScopeIndex,
                PrismFallbackReason.MissingKernel, node.DiagnosticName);
            RenderKernel(target, source, source, 0, 1, node);
            return;
        }
        RenderPrepared(target, source, textures[1]);
    }

    private bool ResolveFilterResource(
        PrismGraphScope scope,
        PrismGraphNode node,
        PrismResourceId id,
        bool required,
        nint source,
        out nint texture,
        out bool available)
    {
        if (node.Filter != PrismFilterId.Curves)
        {
            return TryResolveImage(scope, node, id, required, source, out texture, out available);
        }
        if (id.Value > 0 && scope.Resources.TryGetCurves(id,
            out PrismCurvesResource resource, out long identity, out long version))
        {
            texture = deviceResources.GetCurvesTexture(session, id, resource, identity, version);
            available = true;
            Vector4 control = uniforms[34];
            control.X = PrismCurveLut.SampleCount;
            control.Y = 1;
            uniforms[34] = control;
            return true;
        }
        texture = source;
        available = false;
        if (required)
        {
            diagnostics.Record(node.Id, node.AnalysisScopeIndex,
                PrismFallbackReason.MissingResource,
                $"Prism curves resource '{id}' is not available.");
        }
        return !required;
    }

    private void SetFilterOptions(params Vector4[] options)
    {
        for (int index = 0; index < Math.Min(options.Length, 10); index++)
        {
            uniforms[24 + index] = options[index];
        }
    }

    private static Vector4 PackSeed(uint seed) =>
        new(seed & 0xffffu, seed >> 16, 0, 0);
}

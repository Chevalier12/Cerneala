using Cerneala.Drawing.Prism.Blending;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;

namespace Cerneala.Backends.SdlGpu;

internal static class SdlGpuPrismKernelSelector
{
    public static int ForNode(PrismGraphNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return node.Kind switch
        {
            PrismGraphNodeKind.BackdropCrop => 1,
            PrismGraphNodeKind.ColorConversion => ResolveColorConversion(node),
            PrismGraphNodeKind.Filter => ResolveFilter(node),
            PrismGraphNodeKind.Style => 82,
            PrismGraphNodeKind.Mask => node.MaskPass is null or PrismMaskPass.Extract ? 41 : 42,
            PrismGraphNodeKind.ClipToBelow => 43,
            PrismGraphNodeKind.Composite or PrismGraphNodeKind.PassThroughComposite =>
                44 + ResolveBlendMode(node.BlendMode ?? PrismBlendMode.Normal),
            _ => 0
        };
    }

    public static int ResolveBlendMode(PrismBlendMode blendMode) =>
        PrismBlendMath.ToShaderMode(blendMode);

    public static int ResolveCatalogFilter(PrismFilterId filter) => filter switch
    {
        PrismFilterId.DryBrush => 10,
        PrismFilterId.Underpainting => 11,
        PrismFilterId.Watercolor => 12,
        PrismFilterId.WaterPaper => 13,
        PrismFilterId.Wind => 14,
        PrismFilterId.SumiE => 15,
        PrismFilterId.ChalkCharcoal => 16,
        PrismFilterId.ColoredPencil => 17,
        PrismFilterId.Fresco => 18,
        PrismFilterId.Cutout => 19,
        PrismFilterId.PosterEdges => 20,
        PrismFilterId.AccentedEdges or
            PrismFilterId.DarkStrokes or
            PrismFilterId.InkOutlines => 21,
        PrismFilterId.GlowingEdges => 22,
        PrismFilterId.TraceContour => 23,
        PrismFilterId.Chrome => 24,
        PrismFilterId.NotePaper => 25,
        PrismFilterId.Photocopy or
            PrismFilterId.Stamp or
            PrismFilterId.TornEdges => 26,
        PrismFilterId.Reticulation => 27,
        PrismFilterId.StainedGlass => 28,
        PrismFilterId.Craquelure => 29,
        PrismFilterId.Texturizer => 30,
        PrismFilterId.Grain => 31,
        PrismFilterId.MosaicTiles => 32,
        PrismFilterId.Patchwork => 33,
        PrismFilterId.Clouds or PrismFilterId.DifferenceClouds => 34,
        PrismFilterId.Spatter => 35,
        PrismFilterId.SprayedStrokes => 36,
        PrismFilterId.ColorHalftone => 37,
        PrismFilterId.Facet => 38,
        PrismFilterId.LightingEffects => 39,
        PrismFilterId.BasRelief => 20,
        PrismFilterId.Charcoal => 93,
        PrismFilterId.ConteCrayon => 94,
        PrismFilterId.GraphicPen => 95,
        PrismFilterId.Plaster => 96,
        PrismFilterId.Deinterlace => 97,
        _ => 9
    };

    public static int ForPresentation(PrismColorProfile sourceProfile)
    {
        return sourceProfile switch
        {
            PrismColorProfile.LinearSrgb => 77,
            PrismColorProfile.Srgb => 78,
            PrismColorProfile.LinearDisplayP3 => 79,
            PrismColorProfile.DisplayP3 => 80,
            PrismColorProfile.ScRgb => 81,
            _ => throw new ArgumentOutOfRangeException(
                nameof(sourceProfile),
                sourceProfile,
                "The Prism color profile has no SDL_GPU presentation kernel.")
        };
    }

    public static int ForInputColorProfile(PrismColorProfile targetProfile) =>
        targetProfile switch
        {
            PrismColorProfile.LinearSrgb => 72,
            PrismColorProfile.Srgb => 73,
            PrismColorProfile.LinearDisplayP3 => 74,
            PrismColorProfile.DisplayP3 => 75,
            PrismColorProfile.ScRgb => 76,
            _ => throw new ArgumentOutOfRangeException(
                nameof(targetProfile),
                targetProfile,
                "The Prism color profile has no SDL_GPU input kernel.")
        };

    private static int ResolveColorConversion(PrismGraphNode node)
    {
        if (node.BackdropMetadata is not null)
        {
            return 2;
        }
        return ForInputColorProfile(
            node.ColorProfile ?? PrismColorProfile.LinearSrgb);
    }

    private static int ResolveFilter(PrismGraphNode node)
    {
        if (node.NeighborhoodPlan is not null)
        {
            return 7;
        }
        if (node.ResamplingPlan is not null)
        {
            return 8;
        }
        if (node.CatalogFilterPlan is { } catalog)
        {
            return ResolveCatalogFilterPass(
                catalog.Filter,
                catalog.Passes[node.CatalogFilterPassIndex].Iteration);
        }
        return 3;
    }

    internal static int ResolveCatalogFilterPass(
        PrismFilterId filter,
        int iteration)
    {
        if (filter is not
                PrismFilterId.Charcoal and not
                PrismFilterId.ConteCrayon and not
                PrismFilterId.GraphicPen)
        {
            return ResolveCatalogFilter(filter);
        }

        return iteration switch
        {
            0 => 89,
            <= 3 => 90,
            4 => 91,
            5 => 92,
            _ => filter switch
            {
                PrismFilterId.ConteCrayon => 94,
                PrismFilterId.GraphicPen => 95,
                _ => 93
            }
        };
    }
}

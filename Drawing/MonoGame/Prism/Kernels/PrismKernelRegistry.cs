using Cerneala.Drawing.MonoGame.Prism.Shaders;
using Cerneala.Drawing.Prism.Blending;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Filters;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame.Prism.Kernels;

internal readonly record struct PrismKernelParameters(
    Texture2D SecondaryTexture,
    float Opacity,
    Vector2 PixelSize,
    Vector2 UvScale,
    Vector2 UvOffset)
{
    public Texture2D? SourceTexture { get; init; }

    public Vector4 BlendChannels { get; init; } =
        Vector4.One;

    public float KnockoutMode { get; init; }

    public Texture2D? KnockoutBackdropTexture { get; init; }

    public Texture2D? KnockoutShapeTexture { get; init; }

    public float KnockoutBackdropAvailable { get; init; } = 1;

    public float BlendIfChannel { get; init; }

    public Vector4 ThisLayerRange { get; init; } =
        new(0, 0, 1, 1);

    public Vector4 UnderlyingRange { get; init; } =
        new(0, 0, 1, 1);

    public float DissolveSeed { get; init; }

    public float BackgroundAvailable { get; init; } = 1;

    public float MaskChannel { get; init; }

    public float MaskDensity { get; init; } = 1;

    public float MaskInvert { get; init; }

    public Vector3 MaskUvRowX { get; init; } =
        new(1, 0, 0);

    public Vector3 MaskUvRowY { get; init; } =
        new(0, 1, 0);

    public Vector2 MaskFeatherStep { get; init; }

    public Texture2D? StyleTexture { get; init; }

    public Texture2D? StyleMaskTexture { get; init; }

    public Texture2D? StyleBackdropTexture { get; init; }

    public Vector4 StyleColor { get; init; } =
        Vector4.One;

    public Vector4 StyleSecondaryColor { get; init; } =
        Vector4.One;

    public Vector4 StyleGeometry0 { get; init; }

    public Vector4 StyleGeometry1 { get; init; }

    public Vector4 StyleOptions0 { get; init; }

    public Vector4 StyleOptions1 { get; init; }

    public Vector4 StyleModes0 { get; init; }

    public Vector4 StyleModes1 { get; init; }

    public Vector4 StyleModes2 { get; init; }

    public Vector4 StyleModes3 { get; init; }

    public Vector3 StyleBoundsUvRowX { get; init; } =
        new(1, 0, 0);

    public Vector3 StyleBoundsUvRowY { get; init; } =
        new(0, 1, 0);

    public float StyleResourceAvailable { get; init; }

    public float StyleBackdropAvailable { get; init; }

    public Vector4 FilterHeader { get; init; }

    public Vector4 FilterOptions0 { get; init; }

    public Vector4 FilterOptions1 { get; init; }

    public Vector4 FilterOptions2 { get; init; }

    public Vector4 FilterOptions3 { get; init; }

    public Vector4 FilterOptions4 { get; init; }

    public Vector4 FilterOptions5 { get; init; }

    public Vector4 FilterOptions6 { get; init; }

    public Vector4 FilterOptions7 { get; init; }

    public Vector4 FilterOptions8 { get; init; }

    public Vector4 FilterOptions9 { get; init; }

    public Vector2 FilterTextureSize { get; init; } =
        Vector2.One;

    public Texture2D? FilterAuxiliaryTexture { get; init; }

    public float FilterLightCount { get; init; }

    public Vector4[]? FilterLights { get; init; }
}

internal sealed class PrismKernelRegistry : IDisposable
{
    public const long ShaderPackageVersion = 56;

    private const string CatalogOwnerPrefix =
        "PrismKernelRegistry/";
    private const string FilterOwnerPrefix =
        "PrismKernelRegistry/";

    private readonly Effect effect;
    private readonly Effect styleEffect;
    private readonly Effect charcoalEffect;
    private readonly Effect conteCrayonEffect;
    private readonly Effect graphicPenEffect;
    private readonly Effect plasterEffect;
    private readonly Effect deinterlaceEffect;
    private Effect activeEffect;
    private readonly EffectParameter sourceTextureParameter;
    private readonly EffectParameter secondaryTextureParameter;
    private readonly EffectParameter opacityParameter;
    private readonly EffectParameter pixelSizeParameter;
    private readonly EffectParameter uvScaleParameter;
    private readonly EffectParameter uvOffsetParameter;
    private readonly EffectParameter blendChannelsParameter;
    private readonly EffectParameter knockoutModeParameter;
    private readonly EffectParameter knockoutBackdropTextureParameter;
    private readonly EffectParameter knockoutShapeTextureParameter;
    private readonly EffectParameter knockoutBackdropAvailableParameter;
    private readonly EffectParameter blendIfChannelParameter;
    private readonly EffectParameter thisLayerRangeParameter;
    private readonly EffectParameter underlyingRangeParameter;
    private readonly EffectParameter dissolveSeedParameter;
    private readonly EffectParameter dissolveThresholdTextureParameter;
    private readonly EffectParameter backgroundAvailableParameter;
    private readonly EffectParameter maskChannelParameter;
    private readonly EffectParameter maskDensityParameter;
    private readonly EffectParameter maskInvertParameter;
    private readonly EffectParameter maskUvRowXParameter;
    private readonly EffectParameter maskUvRowYParameter;
    private readonly EffectParameter maskFeatherStepParameter;
    private readonly EffectParameter styleSourceTextureParameter;
    private readonly EffectParameter styleSecondaryTextureParameter;
    private readonly EffectParameter styleOpacityParameter;
    private readonly EffectParameter stylePixelSizeParameter;
    private readonly EffectParameter styleUvScaleParameter;
    private readonly EffectParameter styleUvOffsetParameter;
    private readonly EffectParameter styleMaskDensityParameter;
    private readonly EffectParameter styleMaskFeatherStepParameter;
    private readonly EffectParameter styleTextureParameter;
    private readonly EffectParameter styleMaskTextureParameter;
    private readonly EffectParameter styleBackdropTextureParameter;
    private readonly EffectParameter styleColorParameter;
    private readonly EffectParameter styleSecondaryColorParameter;
    private readonly EffectParameter styleGeometry0Parameter;
    private readonly EffectParameter styleGeometry1Parameter;
    private readonly EffectParameter styleOptions0Parameter;
    private readonly EffectParameter styleOptions1Parameter;
    private readonly EffectParameter styleModes0Parameter;
    private readonly EffectParameter styleModes1Parameter;
    private readonly EffectParameter styleModes2Parameter;
    private readonly EffectParameter styleModes3Parameter;
    private readonly EffectParameter styleBoundsUvRowXParameter;
    private readonly EffectParameter styleBoundsUvRowYParameter;
    private readonly EffectParameter styleResourceAvailableParameter;
    private readonly EffectParameter styleBackdropAvailableParameter;
    private readonly EffectParameter styleFilterAuxiliaryTextureParameter;
    private readonly EffectParameter filterHeaderParameter;
    private readonly EffectParameter filterOptions0Parameter;
    private readonly EffectParameter filterOptions1Parameter;
    private readonly EffectParameter filterOptions2Parameter;
    private readonly EffectParameter filterOptions3Parameter;
    private readonly EffectParameter filterOptions4Parameter;
    private readonly EffectParameter filterOptions5Parameter;
    private readonly EffectParameter filterOptions6Parameter;
    private readonly EffectParameter filterOptions7Parameter;
    private readonly EffectParameter filterOptions8Parameter;
    private readonly EffectParameter filterOptions9Parameter;
    private readonly EffectParameter filterTextureSizeParameter;
    private readonly EffectParameter filterAuxiliaryTextureParameter;
    private readonly EffectParameter filterLightCountParameter;
    private readonly EffectParameter filterLightsParameter;
    private readonly EffectParameter charcoalSourceTextureParameter;
    private readonly EffectParameter charcoalOpacityParameter;
    private readonly EffectParameter charcoalPixelSizeParameter;
    private readonly EffectParameter charcoalUvScaleParameter;
    private readonly EffectParameter charcoalUvOffsetParameter;
    private readonly EffectParameter charcoalFilterHeaderParameter;
    private readonly EffectParameter charcoalFilterOptions0Parameter;
    private readonly EffectParameter charcoalFilterOptions1Parameter;
    private readonly EffectParameter charcoalFilterOptions2Parameter;
    private readonly EffectParameter charcoalFilterOptions3Parameter;
    private readonly EffectParameter charcoalFilterOptions4Parameter;
    private readonly EffectParameter charcoalFilterOptions5Parameter;
    private readonly EffectParameter charcoalFilterOptions6Parameter;
    private readonly EffectParameter charcoalFilterOptions8Parameter;
    private readonly EffectParameter charcoalFilterOptions9Parameter;
    private readonly EffectParameter charcoalFilterAuxiliaryTextureParameter;
    private readonly EffectParameter conteCrayonSourceTextureParameter;
    private readonly EffectParameter conteCrayonOpacityParameter;
    private readonly EffectParameter conteCrayonPixelSizeParameter;
    private readonly EffectParameter conteCrayonUvScaleParameter;
    private readonly EffectParameter conteCrayonUvOffsetParameter;
    private readonly EffectParameter conteCrayonFilterHeaderParameter;
    private readonly EffectParameter conteCrayonFilterOptions0Parameter;
    private readonly EffectParameter conteCrayonFilterOptions1Parameter;
    private readonly EffectParameter conteCrayonFilterOptions2Parameter;
    private readonly EffectParameter conteCrayonFilterOptions3Parameter;
    private readonly EffectParameter conteCrayonFilterOptions4Parameter;
    private readonly EffectParameter conteCrayonFilterOptions5Parameter;
    private readonly EffectParameter conteCrayonFilterOptions6Parameter;
    private readonly EffectParameter conteCrayonFilterOptions7Parameter;
    private readonly EffectParameter conteCrayonFilterOptions9Parameter;
    private readonly EffectParameter conteCrayonFilterAuxiliaryTextureParameter;
    private readonly EffectParameter graphicPenOpacityParameter;
    private readonly EffectParameter graphicPenSourceTextureParameter;
    private readonly EffectParameter graphicPenPixelSizeParameter;
    private readonly EffectParameter graphicPenUvScaleParameter;
    private readonly EffectParameter graphicPenUvOffsetParameter;
    private readonly EffectParameter graphicPenFilterHeaderParameter;
    private readonly EffectParameter graphicPenFilterOptions0Parameter;
    private readonly EffectParameter graphicPenFilterOptions1Parameter;
    private readonly EffectParameter graphicPenFilterOptions2Parameter;
    private readonly EffectParameter graphicPenFilterOptions3Parameter;
    private readonly EffectParameter graphicPenFilterOptions4Parameter;
    private readonly EffectParameter graphicPenFilterOptions9Parameter;
    private readonly EffectParameter graphicPenFilterAuxiliaryTextureParameter;
    private readonly EffectParameter plasterSourceTextureParameter;
    private readonly EffectParameter plasterOpacityParameter;
    private readonly EffectParameter plasterPixelSizeParameter;
    private readonly EffectParameter plasterUvScaleParameter;
    private readonly EffectParameter plasterUvOffsetParameter;
    private readonly EffectParameter plasterFilterHeaderParameter;
    private readonly EffectParameter plasterFilterOptions0Parameter;
    private readonly EffectParameter plasterFilterOptions1Parameter;
    private readonly EffectParameter plasterFilterOptions3Parameter;
    private readonly EffectParameter plasterFilterOptions4Parameter;
    private readonly EffectParameter plasterFilterOptions5Parameter;
    private readonly EffectParameter plasterFilterOptions6Parameter;
    private readonly EffectParameter plasterFilterOptions9Parameter;
    private readonly EffectParameter plasterFilterAuxiliaryTextureParameter;
    private readonly EffectParameter deinterlaceSourceTextureParameter;
    private readonly EffectParameter deinterlaceOpacityParameter;
    private readonly EffectParameter deinterlacePixelSizeParameter;
    private readonly EffectParameter deinterlaceUvScaleParameter;
    private readonly EffectParameter deinterlaceUvOffsetParameter;
    private readonly EffectParameter deinterlaceFilterHeaderParameter;
    private readonly EffectParameter deinterlaceFilterOptions0Parameter;
    private readonly EffectParameter deinterlaceFilterOptions1Parameter;
    private readonly EffectParameter deinterlaceFilterOptions9Parameter;
    private readonly EffectParameter deinterlaceTextureSizeParameter;
    private readonly PrismKernel copy;
    private readonly PrismKernel maskExtract;
    private readonly PrismKernel maskFeather;
    private readonly PrismKernel maskAlpha;
    private readonly PrismKernel clipAlpha;
    private readonly PrismKernel styleDilate;
    private readonly PrismKernel styleGaussian;
    private readonly PrismKernel strokeDistanceSeed;
    private readonly PrismKernel strokeDistanceFlood;
    private readonly PrismKernel bevelHeight;
    private readonly PrismKernel bevelLighting;
    private readonly PrismKernel layerStyle;
    private readonly PrismKernel adjustmentFilter;
    private readonly PrismKernel levelsCdf;
    private readonly PrismKernel levelsRange;
    private readonly PrismKernel thresholdRange;
    private readonly PrismKernel neighborhoodFilter;
    private readonly PrismKernel resamplingFilter;
    private readonly PrismKernel catalogFilter;
    private readonly PrismKernel coloredPencilFilter;
    private readonly PrismKernel frescoFilter;
    private readonly PrismKernel cutoutFilter;
    private readonly PrismKernel dryBrushFilter;
    private readonly PrismKernel underpaintingFilter;
    private readonly PrismKernel watercolorFilter;
    private readonly PrismKernel waterPaperFilter;
    private readonly PrismKernel windFilter;
    private readonly PrismKernel sumiEFilter;
    private readonly PrismKernel chalkCharcoalFilter;
    private readonly PrismKernel charcoalFilter;
    private readonly PrismKernel conteCrayonFilter;
    private readonly PrismKernel graphicPenFilter;
    private readonly PrismKernel charcoalInitialEtfFilter;
    private readonly PrismKernel charcoalRefineEtfFilter;
    private readonly PrismKernel charcoalNormalDogFilter;
    private readonly PrismKernel charcoalFlowDogFilter;
    private readonly PrismKernel accentedEdgesFilter;
    private readonly PrismKernel glowingEdgesFilter;
    private readonly PrismKernel traceContourFilter;
    private readonly PrismKernel basReliefFilter;
    private readonly PrismKernel posterEdgesFilter;
    private readonly PrismKernel chromeFilter;
    private readonly PrismKernel notePaperFilter;
    private readonly PrismKernel plasterFilter;
    private readonly PrismKernel photocopyFilter;
    private readonly PrismKernel craquelureFilter;
    private readonly PrismKernel texturizerFilter;
    private readonly PrismKernel grainFilter;
    private readonly PrismKernel mosaicTilesFilter;
    private readonly PrismKernel patchworkFilter;
    private readonly PrismKernel reticulationFilter;
    private readonly PrismKernel stainedGlassFilter;
    private readonly PrismKernel deinterlaceFilter;
    private readonly PrismKernel waveNoiseFilter;
    private readonly PrismKernel spatterFilter;
    private readonly PrismKernel sprayedStrokesFilter;
    private readonly PrismKernel colorHalftoneFilter;
    private readonly PrismKernel facetFilter;
    private readonly PrismKernel lightingEffectsFilter;
    private readonly PrismKernel backdropCrop;
    private readonly PrismKernel backdropColorConversion;
    private readonly Dictionary<PrismBlendMode, PrismKernel>
        blendKernels = [];
    private readonly Dictionary<PrismFilterId, PrismKernel>
        filterKernels = [];
    private readonly Dictionary<PrismColorProfile, PrismKernel>
        inputColorConversions = [];
    private readonly Dictionary<PrismColorProfile, PrismKernel>
        outputColorConversions = [];
    private readonly Texture2D dissolveThresholdTexture;
    private bool disposed;

    public PrismKernelRegistry(GraphicsDevice graphicsDevice)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ObjectDisposedException.ThrowIf(graphicsDevice.IsDisposed, graphicsDevice);

        ValidateFundamentalCatalogBindings();

        try
        {
            effect = PrismShaderResources.CreateEffect(
                graphicsDevice,
                PrismShaderId.CopyComposite);
            try
            {
                deinterlaceEffect = PrismShaderResources.CreateEffect(
                    graphicsDevice,
                    PrismShaderId.Deinterlace);
            }
            catch
            {
                effect.Dispose();
                throw;
            }
            try
            {
                charcoalEffect = PrismShaderResources.CreateEffect(
                    graphicsDevice,
                    PrismShaderId.Charcoal);
            }
            catch
            {
                deinterlaceEffect.Dispose();
                effect.Dispose();
                throw;
            }
            try
            {
                graphicPenEffect = PrismShaderResources.CreateEffect(
                    graphicsDevice,
                    PrismShaderId.GraphicPen);
            }
            catch
            {
                charcoalEffect.Dispose();
                deinterlaceEffect.Dispose();
                effect.Dispose();
                throw;
            }
            try
            {
                conteCrayonEffect = PrismShaderResources.CreateEffect(
                    graphicsDevice,
                    PrismShaderId.ConteCrayon);
            }
            catch
            {
                graphicPenEffect.Dispose();
                charcoalEffect.Dispose();
                deinterlaceEffect.Dispose();
                effect.Dispose();
                throw;
            }
            try
            {
                plasterEffect = PrismShaderResources.CreateEffect(
                    graphicsDevice,
                    PrismShaderId.Plaster);
            }
            catch
            {
                conteCrayonEffect.Dispose();
                graphicPenEffect.Dispose();
                charcoalEffect.Dispose();
                deinterlaceEffect.Dispose();
                effect.Dispose();
                throw;
            }
            try
            {
                styleEffect = PrismShaderResources.CreateEffect(
                    graphicsDevice,
                    PrismShaderId.Styles);
            }
            catch
            {
                plasterEffect.Dispose();
                conteCrayonEffect.Dispose();
                graphicPenEffect.Dispose();
                charcoalEffect.Dispose();
                deinterlaceEffect.Dispose();
                effect.Dispose();
                throw;
            }
        }
        catch (Exception exception) when (
            exception is InvalidOperationException or
                ArgumentException or
                BadImageFormatException)
        {
            throw new PrismShaderUnavailableException(
                "The embedded Prism shader package could not be loaded.",
                exception);
        }

        activeEffect = effect;

        sourceTextureParameter = GetParameter("SpriteTexture");
        secondaryTextureParameter = GetParameter("SecondaryTexture");
        opacityParameter = GetParameter("Opacity");
        pixelSizeParameter = GetParameter("PixelSize");
        uvScaleParameter = GetParameter("UvScale");
        uvOffsetParameter = GetParameter("UvOffset");
        blendChannelsParameter = GetParameter("BlendChannels");
        knockoutModeParameter = GetParameter("KnockoutMode");
        knockoutBackdropTextureParameter =
            GetParameter("KnockoutBackdropTexture");
        knockoutShapeTextureParameter =
            GetParameter("KnockoutShapeTexture");
        knockoutBackdropAvailableParameter =
            GetParameter("KnockoutBackdropAvailable");
        blendIfChannelParameter = GetParameter("BlendIfChannel");
        thisLayerRangeParameter = GetParameter("ThisLayerRange");
        underlyingRangeParameter = GetParameter("UnderlyingRange");
        dissolveSeedParameter = GetParameter("DissolveSeed");
        dissolveThresholdTextureParameter =
            GetParameter("DissolveThresholdTexture");
        backgroundAvailableParameter =
            GetParameter("BackgroundAvailable");
        maskChannelParameter = GetParameter("MaskChannel");
        maskDensityParameter = GetParameter("MaskDensity");
        maskInvertParameter = GetParameter("MaskInvert");
        maskUvRowXParameter = GetParameter("MaskUvRowX");
        maskUvRowYParameter = GetParameter("MaskUvRowY");
        maskFeatherStepParameter =
            GetParameter("MaskFeatherStep");
        styleSourceTextureParameter =
            GetStyleParameter("SpriteTexture");
        styleSecondaryTextureParameter =
            GetStyleParameter("SecondaryTexture");
        styleOpacityParameter = GetStyleParameter("Opacity");
        stylePixelSizeParameter = GetStyleParameter("PixelSize");
        styleUvScaleParameter = GetStyleParameter("UvScale");
        styleUvOffsetParameter = GetStyleParameter("UvOffset");
        styleMaskDensityParameter =
            GetStyleParameter("MaskDensity");
        styleMaskFeatherStepParameter =
            GetStyleParameter("MaskFeatherStep");
        styleTextureParameter = GetStyleParameter("StyleTexture");
        styleMaskTextureParameter =
            GetStyleParameter("StyleMaskTexture");
        styleBackdropTextureParameter =
            GetStyleParameter("StyleBackdropTexture");
        styleColorParameter = GetStyleParameter("StyleColor");
        styleSecondaryColorParameter =
            GetStyleParameter("StyleSecondaryColor");
        styleGeometry0Parameter =
            GetStyleParameter("StyleGeometry0");
        styleGeometry1Parameter =
            GetStyleParameter("StyleGeometry1");
        styleOptions0Parameter =
            GetStyleParameter("StyleOptions0");
        styleOptions1Parameter =
            GetStyleParameter("StyleOptions1");
        styleModes0Parameter = GetStyleParameter("StyleModes0");
        styleModes1Parameter = GetStyleParameter("StyleModes1");
        styleModes2Parameter = GetStyleParameter("StyleModes2");
        styleModes3Parameter = GetStyleParameter("StyleModes3");
        styleBoundsUvRowXParameter =
            GetStyleParameter("StyleBoundsUvRowX");
        styleBoundsUvRowYParameter =
            GetStyleParameter("StyleBoundsUvRowY");
        styleResourceAvailableParameter =
            GetStyleParameter("StyleResourceAvailable");
        styleBackdropAvailableParameter =
            GetStyleParameter("StyleBackdropAvailable");
        styleFilterAuxiliaryTextureParameter =
            GetStyleParameter("FilterAuxiliaryTexture");
        filterHeaderParameter =
            GetParameter("FilterHeader");
        filterOptions0Parameter =
            GetParameter("FilterOptions0");
        filterOptions1Parameter =
            GetParameter("FilterOptions1");
        filterOptions2Parameter =
            GetParameter("FilterOptions2");
        filterOptions3Parameter =
            GetParameter("FilterOptions3");
        filterOptions4Parameter =
            GetParameter("FilterOptions4");
        filterOptions5Parameter =
            GetParameter("FilterOptions5");
        filterOptions6Parameter =
            GetParameter("FilterOptions6");
        filterOptions7Parameter =
            GetParameter("FilterOptions7");
        filterOptions8Parameter =
            GetParameter("FilterOptions8");
        filterOptions9Parameter =
            GetParameter("FilterOptions9");
        filterTextureSizeParameter =
            GetParameter("FilterTextureSize");
        filterAuxiliaryTextureParameter =
            GetParameter("FilterAuxiliaryTexture");
        filterLightCountParameter =
            GetParameter("FilterLightCount");
        filterLightsParameter =
            GetParameter("FilterLights");
        charcoalSourceTextureParameter =
            GetCharcoalParameter("SpriteTexture");
        charcoalOpacityParameter =
            GetCharcoalParameter("Opacity");
        charcoalPixelSizeParameter =
            GetCharcoalParameter("PixelSize");
        charcoalUvScaleParameter =
            GetCharcoalParameter("UvScale");
        charcoalUvOffsetParameter =
            GetCharcoalParameter("UvOffset");
        charcoalFilterHeaderParameter =
            GetCharcoalParameter("FilterHeader");
        charcoalFilterOptions0Parameter =
            GetCharcoalParameter("FilterOptions0");
        charcoalFilterOptions1Parameter =
            GetCharcoalParameter("FilterOptions1");
        charcoalFilterOptions2Parameter =
            GetCharcoalParameter("FilterOptions2");
        charcoalFilterOptions3Parameter =
            GetCharcoalParameter("FilterOptions3");
        charcoalFilterOptions4Parameter =
            GetCharcoalParameter("FilterOptions4");
        charcoalFilterOptions5Parameter =
            GetCharcoalParameter("FilterOptions5");
        charcoalFilterOptions6Parameter =
            GetCharcoalParameter("FilterOptions6");
        charcoalFilterOptions8Parameter =
            GetCharcoalParameter("FilterOptions8");
        charcoalFilterOptions9Parameter =
            GetCharcoalParameter("FilterOptions9");
        charcoalFilterAuxiliaryTextureParameter =
            GetCharcoalParameter("FilterAuxiliaryTexture");
        conteCrayonSourceTextureParameter =
            GetConteCrayonParameter("SpriteTexture");
        conteCrayonOpacityParameter =
            GetConteCrayonParameter("Opacity");
        conteCrayonPixelSizeParameter =
            GetConteCrayonParameter("PixelSize");
        conteCrayonUvScaleParameter =
            GetConteCrayonParameter("UvScale");
        conteCrayonUvOffsetParameter =
            GetConteCrayonParameter("UvOffset");
        conteCrayonFilterHeaderParameter =
            GetConteCrayonParameter("FilterHeader");
        conteCrayonFilterOptions0Parameter =
            GetConteCrayonParameter("FilterOptions0");
        conteCrayonFilterOptions1Parameter =
            GetConteCrayonParameter("FilterOptions1");
        conteCrayonFilterOptions2Parameter =
            GetConteCrayonParameter("FilterOptions2");
        conteCrayonFilterOptions3Parameter =
            GetConteCrayonParameter("FilterOptions3");
        conteCrayonFilterOptions4Parameter =
            GetConteCrayonParameter("FilterOptions4");
        conteCrayonFilterOptions5Parameter =
            GetConteCrayonParameter("FilterOptions5");
        conteCrayonFilterOptions6Parameter =
            GetConteCrayonParameter("FilterOptions6");
        conteCrayonFilterOptions7Parameter =
            GetConteCrayonParameter("FilterOptions7");
        conteCrayonFilterOptions9Parameter =
            GetConteCrayonParameter("FilterOptions9");
        conteCrayonFilterAuxiliaryTextureParameter =
            GetConteCrayonParameter("FilterAuxiliaryTexture");
        graphicPenOpacityParameter =
            GetGraphicPenParameter("Opacity");
        graphicPenSourceTextureParameter =
            GetGraphicPenParameter("SpriteTexture");
        graphicPenPixelSizeParameter =
            GetGraphicPenParameter("PixelSize");
        graphicPenUvScaleParameter =
            GetGraphicPenParameter("UvScale");
        graphicPenUvOffsetParameter =
            GetGraphicPenParameter("UvOffset");
        graphicPenFilterHeaderParameter =
            GetGraphicPenParameter("FilterHeader");
        graphicPenFilterOptions0Parameter =
            GetGraphicPenParameter("FilterOptions0");
        graphicPenFilterOptions1Parameter =
            GetGraphicPenParameter("FilterOptions1");
        graphicPenFilterOptions2Parameter =
            GetGraphicPenParameter("FilterOptions2");
        graphicPenFilterOptions3Parameter =
            GetGraphicPenParameter("FilterOptions3");
        graphicPenFilterOptions4Parameter =
            GetGraphicPenParameter("FilterOptions4");
        graphicPenFilterOptions9Parameter =
            GetGraphicPenParameter("FilterOptions9");
        graphicPenFilterAuxiliaryTextureParameter =
            GetGraphicPenParameter("FilterAuxiliaryTexture");
        plasterSourceTextureParameter =
            GetPlasterParameter("SpriteTexture");
        plasterOpacityParameter =
            GetPlasterParameter("Opacity");
        plasterPixelSizeParameter =
            GetPlasterParameter("PixelSize");
        plasterUvScaleParameter =
            GetPlasterParameter("UvScale");
        plasterUvOffsetParameter =
            GetPlasterParameter("UvOffset");
        plasterFilterHeaderParameter =
            GetPlasterParameter("FilterHeader");
        plasterFilterOptions0Parameter =
            GetPlasterParameter("FilterOptions0");
        plasterFilterOptions1Parameter =
            GetPlasterParameter("FilterOptions1");
        plasterFilterOptions3Parameter =
            GetPlasterParameter("FilterOptions3");
        plasterFilterOptions4Parameter =
            GetPlasterParameter("FilterOptions4");
        plasterFilterOptions5Parameter =
            GetPlasterParameter("FilterOptions5");
        plasterFilterOptions6Parameter =
            GetPlasterParameter("FilterOptions6");
        plasterFilterOptions9Parameter =
            GetPlasterParameter("FilterOptions9");
        plasterFilterAuxiliaryTextureParameter =
            GetPlasterParameter("FilterAuxiliaryTexture");
        deinterlaceSourceTextureParameter =
            GetDeinterlaceParameter("SpriteTexture");
        deinterlaceOpacityParameter =
            GetDeinterlaceParameter("Opacity");
        deinterlacePixelSizeParameter =
            GetDeinterlaceParameter("PixelSize");
        deinterlaceUvScaleParameter =
            GetDeinterlaceParameter("UvScale");
        deinterlaceUvOffsetParameter =
            GetDeinterlaceParameter("UvOffset");
        deinterlaceFilterHeaderParameter =
            GetDeinterlaceParameter("FilterHeader");
        deinterlaceFilterOptions0Parameter =
            GetDeinterlaceParameter("FilterOptions0");
        deinterlaceFilterOptions1Parameter =
            GetDeinterlaceParameter("FilterOptions1");
        deinterlaceFilterOptions9Parameter =
            GetDeinterlaceParameter("FilterOptions9");
        deinterlaceTextureSizeParameter =
            GetDeinterlaceParameter("FilterTextureSize");

        copy = CreateKernel(
            PrismKernelKind.Copy,
            "CopyComposite");
        maskExtract = CreateKernel(
            PrismKernelKind.MaskExtract,
            "MaskExtract");
        maskFeather = CreateKernel(
            PrismKernelKind.MaskFeather,
            "MaskFeather");
        maskAlpha = CreateKernel(
            PrismKernelKind.MaskAlpha,
            "MaskAlpha");
        clipAlpha = CreateKernel(
            PrismKernelKind.ClipAlpha,
            "ClipAlpha");
        styleDilate = CreateKernel(
            styleEffect,
            PrismKernelKind.StyleDilate,
            "StyleDilate",
            "Styles");
        styleGaussian = CreateKernel(
            styleEffect,
            PrismKernelKind.StyleGaussian,
            "StyleGaussian",
            "Styles");
        strokeDistanceSeed = CreateKernel(
            styleEffect,
            PrismKernelKind.StrokeDistanceSeed,
            "StrokeDistanceSeed",
            "Styles");
        strokeDistanceFlood = CreateKernel(
            styleEffect,
            PrismKernelKind.StrokeDistanceFlood,
            "StrokeDistanceFlood",
            "Styles");
        bevelHeight = CreateKernel(
            styleEffect,
            PrismKernelKind.BevelHeight,
            "BevelHeight",
            "Styles");
        bevelLighting = CreateKernel(
            styleEffect,
            PrismKernelKind.BevelLighting,
            "BevelLighting",
            "Styles");
        layerStyle = CreateKernel(
            styleEffect,
            PrismKernelKind.LayerStyle,
            "LayerStyle",
            "Styles");
        adjustmentFilter = CreateKernel(
            PrismKernelKind.AdjustmentFilter,
            "AdjustmentFilter");
        levelsCdf = CreateKernel(
            PrismKernelKind.LevelsCdf,
            "LevelsCdf");
        levelsRange = CreateKernel(
            PrismKernelKind.LevelsRange,
            "LevelsRange");
        thresholdRange = CreateKernel(
            PrismKernelKind.ThresholdRange,
            "ThresholdRange");
        neighborhoodFilter = CreateKernel(
            PrismKernelKind.NeighborhoodFilter,
            "NeighborhoodFilter");
        resamplingFilter = CreateKernel(
            PrismKernelKind.ResamplingFilter,
            "ResamplingFilter");
        catalogFilter = CreateKernel(
            PrismKernelKind.CatalogFilter,
            "CatalogFilter");
        coloredPencilFilter = CreateKernel(
            PrismKernelKind.ColoredPencilFilter,
            "ColoredPencilFilter");
        frescoFilter = CreateKernel(
            PrismKernelKind.FrescoFilter,
            "FrescoFilter");
        cutoutFilter = CreateKernel(
            PrismKernelKind.CutoutFilter,
            "CutoutFilter");
        dryBrushFilter = CreateKernel(
            PrismKernelKind.CatalogFilter,
            "DryBrushFilter");
        underpaintingFilter = CreateKernel(
            PrismKernelKind.CatalogFilter,
            "UnderpaintingFilter");
        watercolorFilter = CreateKernel(
            PrismKernelKind.CatalogFilter,
            "WatercolorFilter");
        waterPaperFilter = CreateKernel(
            PrismKernelKind.CatalogFilter,
            "WaterPaperFilter");
        windFilter = CreateKernel(
            PrismKernelKind.CatalogFilter,
            "WindFilter");
        sumiEFilter = CreateKernel(
            PrismKernelKind.CatalogFilter,
            "SumiEFilter");
        chalkCharcoalFilter = CreateKernel(
            PrismKernelKind.CatalogFilter,
            "ChalkCharcoalFilter");
        charcoalFilter = CreateKernel(
            charcoalEffect,
            PrismKernelKind.CatalogFilter,
            "CharcoalFilter",
            "Charcoal");
        conteCrayonFilter = CreateKernel(
            conteCrayonEffect,
            PrismKernelKind.CatalogFilter,
            "ConteCrayonFilter",
            "ConteCrayon");
        graphicPenFilter = CreateKernel(
            graphicPenEffect,
            PrismKernelKind.CatalogFilter,
            "GraphicPenFilter",
            "GraphicPen");
        charcoalInitialEtfFilter = CreateKernel(
            charcoalEffect,
            PrismKernelKind.CatalogFilter,
            "CharcoalInitialEtfFilter",
            "Charcoal");
        charcoalRefineEtfFilter = CreateKernel(
            charcoalEffect,
            PrismKernelKind.CatalogFilter,
            "CharcoalRefineEtfFilter",
            "Charcoal");
        charcoalNormalDogFilter = CreateKernel(
            charcoalEffect,
            PrismKernelKind.CatalogFilter,
            "CharcoalNormalDogFilter",
            "Charcoal");
        charcoalFlowDogFilter = CreateKernel(
            charcoalEffect,
            PrismKernelKind.CatalogFilter,
            "CharcoalFlowDogFilter",
            "Charcoal");
        accentedEdgesFilter = CreateKernel(
            PrismKernelKind.CatalogFilter,
            "AccentedEdgesFilter");
        glowingEdgesFilter = CreateKernel(
            PrismKernelKind.CatalogFilter,
            "GlowingEdgesFilter");
        traceContourFilter = CreateKernel(
            PrismKernelKind.CatalogFilter,
            "TraceContourFilter");
        basReliefFilter = CreateKernel(
            PrismKernelKind.CatalogFilter,
            "BasReliefFilter");
        posterEdgesFilter = CreateKernel(
            PrismKernelKind.CatalogFilter,
            "PosterEdgesFilter");
        chromeFilter = CreateKernel(
            PrismKernelKind.CatalogFilter,
            "ChromeFilter");
        notePaperFilter = CreateKernel(
            PrismKernelKind.CatalogFilter,
            "NotePaperFilter");
        plasterFilter = CreateKernel(
            plasterEffect,
            PrismKernelKind.CatalogFilter,
            "PlasterFilter",
            "Plaster");
        photocopyFilter = CreateKernel(
            PrismKernelKind.CatalogFilter,
            "PhotocopyFilter");
        craquelureFilter = CreateKernel(
            PrismKernelKind.CatalogFilter,
            "CraquelureFilter");
        texturizerFilter = CreateKernel(
            PrismKernelKind.CatalogFilter,
            "TexturizerFilter");
        grainFilter = CreateKernel(
            PrismKernelKind.CatalogFilter,
            "GrainFilter");
        mosaicTilesFilter = CreateKernel(
            PrismKernelKind.CatalogFilter,
            "MosaicTilesFilter");
        patchworkFilter = CreateKernel(
            PrismKernelKind.CatalogFilter,
            "PatchworkFilter");
        reticulationFilter = CreateKernel(
            PrismKernelKind.CatalogFilter,
            "ReticulationFilter");
        stainedGlassFilter = CreateKernel(
            PrismKernelKind.CatalogFilter,
            "StainedGlassFilter");
        deinterlaceFilter = CreateKernel(
            deinterlaceEffect,
            PrismKernelKind.CatalogFilter,
            "DeinterlaceFilter",
            "Deinterlace");
        waveNoiseFilter = CreateKernel(
            PrismKernelKind.CatalogFilter,
            "WaveNoiseFilter");
        spatterFilter = CreateKernel(
            PrismKernelKind.CatalogFilter,
            "SpatterFilter");
        sprayedStrokesFilter = CreateKernel(
            PrismKernelKind.CatalogFilter,
            "SprayedStrokesFilter");
        colorHalftoneFilter = CreateKernel(
            PrismKernelKind.ColorHalftoneFilter,
            "ColorHalftoneFilter");
        facetFilter = CreateKernel(
            PrismKernelKind.FacetFilter,
            "FacetFilter");
        lightingEffectsFilter = CreateKernel(
            PrismKernelKind.LightingEffectsFilter,
            "LightingEffectsFilter");
        backdropCrop = CreateKernel(
            PrismKernelKind.BackdropCrop,
            "BackdropCrop");
        backdropColorConversion = CreateKernel(
            PrismKernelKind.BackdropColorConversion,
            "BackdropColorConversion");
        foreach (PrismBlendMode blendMode in
            Enum.GetValues<PrismBlendMode>())
        {
            blendKernels.Add(
                blendMode,
                CreateKernel(
                    PrismKernelKind.Blend,
                    $"{blendMode}Blend"));
        }
        foreach (PrismColorProfile profile in
            Enum.GetValues<PrismColorProfile>())
        {
            string symbol = profile.ToString();
            inputColorConversions.Add(
                profile,
                CreateKernel(
                    PrismKernelKind.InputColorConversion,
                    $"InputTo{symbol}"));
            outputColorConversions.Add(
                profile,
                CreateKernel(
                    PrismKernelKind.OutputColorConversion,
                    $"{symbol}ToOutput"));
        }
        RegisterCatalogFilters();
        dissolveThresholdTexture =
            CreateDissolveThresholdTexture(graphicsDevice);
    }

    public Effect Effect => activeEffect;

    public PrismKernel Copy => copy;

    public PrismKernel MaskExtract => maskExtract;

    public PrismKernel MaskFeather => maskFeather;

    public PrismKernel MaskAlpha => maskAlpha;

    public PrismKernel ClipAlpha => clipAlpha;

    public PrismKernel StyleDilate => styleDilate;

    public PrismKernel StyleGaussian => styleGaussian;

    public PrismKernel StrokeDistanceSeed => strokeDistanceSeed;

    public PrismKernel StrokeDistanceFlood => strokeDistanceFlood;

    public PrismKernel BevelHeight => bevelHeight;

    public PrismKernel BevelLighting => bevelLighting;

    public PrismKernel LayerStyle => layerStyle;

    public PrismKernel AdjustmentFilter => adjustmentFilter;

    public PrismKernel LevelsCdf => levelsCdf;

    public PrismKernel LevelsRange => levelsRange;

    public PrismKernel ThresholdRange => thresholdRange;

    public PrismKernel NeighborhoodFilter => neighborhoodFilter;

    public PrismKernel ResamplingFilter => resamplingFilter;

    public PrismKernel CatalogFilter => catalogFilter;

    public PrismKernel ColoredPencilFilter =>
        coloredPencilFilter;

    public PrismKernel FrescoFilter => frescoFilter;

    public PrismKernel CutoutFilter => cutoutFilter;

    public PrismKernel DryBrushFilter => dryBrushFilter;

    public PrismKernel UnderpaintingFilter => underpaintingFilter;

    public PrismKernel WatercolorFilter => watercolorFilter;

    public PrismKernel WaterPaperFilter => waterPaperFilter;

    public PrismKernel WindFilter => windFilter;

    public PrismKernel SumiEFilter => sumiEFilter;

    public PrismKernel ChromeFilter => chromeFilter;

    public PrismKernel NotePaperFilter => notePaperFilter;

    public PrismKernel PlasterFilter => plasterFilter;

    public PrismKernel PhotocopyFilter => photocopyFilter;

    public PrismKernel CraquelureFilter => craquelureFilter;

    public PrismKernel TexturizerFilter => texturizerFilter;

    public PrismKernel GrainFilter => grainFilter;

    public PrismKernel MosaicTilesFilter => mosaicTilesFilter;

    public PrismKernel PatchworkFilter => patchworkFilter;

    public PrismKernel ReticulationFilter => reticulationFilter;

    public PrismKernel StainedGlassFilter => stainedGlassFilter;

    public PrismKernel ChalkCharcoalFilter => chalkCharcoalFilter;

    public PrismKernel CharcoalFilter => charcoalFilter;

    public PrismKernel ConteCrayonFilter => conteCrayonFilter;

    public PrismKernel GraphicPenFilter => graphicPenFilter;

    public PrismKernel ResolveCatalogFilterPassKernel(
        PrismFilterId filter,
        int iteration)
    {
        if (filter is not
                PrismFilterId.Charcoal and not
                PrismFilterId.ConteCrayon and not
                PrismFilterId.GraphicPen)
        {
            return filterKernels[filter];
        }

        return iteration switch
        {
            0 => charcoalInitialEtfFilter,
            <= 3 => charcoalRefineEtfFilter,
            4 => charcoalNormalDogFilter,
            5 => charcoalFlowDogFilter,
            _ => filter switch
            {
                PrismFilterId.ConteCrayon => conteCrayonFilter,
                PrismFilterId.GraphicPen => graphicPenFilter,
                _ => charcoalFilter
            }
        };
    }

    public PrismKernel AccentedEdgesFilter => accentedEdgesFilter;

    public PrismKernel GlowingEdgesFilter => glowingEdgesFilter;

    public PrismKernel TraceContourFilter => traceContourFilter;

    public PrismKernel BasReliefFilter => basReliefFilter;

    public PrismKernel PosterEdgesFilter => posterEdgesFilter;

    public PrismKernel DeinterlaceFilter => deinterlaceFilter;

    public PrismKernel WaveNoiseFilter => waveNoiseFilter;

    public PrismKernel SpatterFilter => spatterFilter;

    public PrismKernel SprayedStrokesFilter => sprayedStrokesFilter;

    public PrismKernel ColorHalftoneFilter => colorHalftoneFilter;

    public PrismKernel FacetFilter => facetFilter;

    public PrismKernel LightingEffectsFilter => lightingEffectsFilter;

    public PrismKernel BackdropCrop => backdropCrop;

    public PrismKernel BackdropColorConversion =>
        backdropColorConversion;

    public PrismKernel Present =>
        outputColorConversions[PrismColorProfile.Srgb];

    public bool TryGetBlendKernel(
        PrismBlendMode blendMode,
        out PrismKernel kernel)
    {
        return blendKernels.TryGetValue(blendMode, out kernel);
    }

    public bool TryGetColorConversionKernel(
        PrismColorProfile targetProfile,
        out PrismKernel kernel)
    {
        return inputColorConversions.TryGetValue(
            targetProfile,
            out kernel);
    }

    public bool TryGetPresentKernel(
        PrismColorProfile sourceProfile,
        out PrismKernel kernel)
    {
        return outputColorConversions.TryGetValue(
            sourceProfile,
            out kernel);
    }

    public bool TryGetStyleKernel(
        PrismStyleId style,
        out PrismKernel kernel)
    {
        if (Enum.IsDefined(style))
        {
            kernel = layerStyle;
            return true;
        }

        kernel = default;
        return false;
    }

    public bool TryGetFilterKernel(
        PrismFilterId filter,
        out PrismKernel kernel) =>
        filterKernels.TryGetValue(filter, out kernel);

    public bool IsFundamentalCatalogEntryRegistered(
        string kind,
        string symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        return kind switch
        {
            "blend-mode" =>
                IsRegisteredBlendMode(symbol),
            "color-profile" =>
                IsRegisteredColorProfile(symbol),
            "sampling" =>
                symbol == "Linear",
            "style" =>
                IsRegisteredStyle(symbol),
            "filter" =>
                IsRegisteredFilter(symbol),
            _ => false
        };
    }

    public void Bind(
        PrismKernel kernel,
        in PrismKernelParameters parameters)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(parameters.SecondaryTexture);

        if (IsStyleKernel(kernel))
        {
            BindStyle(kernel, parameters);
            return;
        }
        if (kernel == deinterlaceFilter)
        {
            BindDeinterlace(parameters);
            return;
        }
        if (kernel == conteCrayonFilter)
        {
            BindConteCrayon(parameters);
            return;
        }
        if (kernel == graphicPenFilter)
        {
            BindGraphicPen(parameters);
            return;
        }
        if (kernel == plasterFilter)
        {
            BindPlaster(parameters);
            return;
        }
        if (IsCharcoalKernel(kernel))
        {
            BindCharcoal(kernel, parameters);
            return;
        }

        activeEffect = effect;
        effect.CurrentTechnique = kernel.Technique;
        sourceTextureParameter.SetValue(
            parameters.SourceTexture ?? parameters.SecondaryTexture);
        secondaryTextureParameter.SetValue(parameters.SecondaryTexture);
        opacityParameter.SetValue(parameters.Opacity);
        pixelSizeParameter.SetValue(parameters.PixelSize);
        uvScaleParameter.SetValue(parameters.UvScale);
        uvOffsetParameter.SetValue(parameters.UvOffset);
        blendChannelsParameter.SetValue(parameters.BlendChannels);
        knockoutModeParameter.SetValue(parameters.KnockoutMode);
        knockoutBackdropTextureParameter.SetValue(
            parameters.KnockoutBackdropTexture ??
                parameters.SecondaryTexture);
        knockoutShapeTextureParameter.SetValue(
            parameters.KnockoutShapeTexture ??
                parameters.SourceTexture ??
                parameters.SecondaryTexture);
        knockoutBackdropAvailableParameter.SetValue(
            parameters.KnockoutBackdropAvailable);
        blendIfChannelParameter.SetValue(parameters.BlendIfChannel);
        thisLayerRangeParameter.SetValue(parameters.ThisLayerRange);
        underlyingRangeParameter.SetValue(
            parameters.UnderlyingRange);
        dissolveSeedParameter.SetValue(parameters.DissolveSeed);
        dissolveThresholdTextureParameter.SetValue(
            dissolveThresholdTexture);
        backgroundAvailableParameter.SetValue(
            parameters.BackgroundAvailable);
        maskChannelParameter.SetValue(parameters.MaskChannel);
        maskDensityParameter.SetValue(parameters.MaskDensity);
        maskInvertParameter.SetValue(parameters.MaskInvert);
        maskUvRowXParameter.SetValue(parameters.MaskUvRowX);
        maskUvRowYParameter.SetValue(parameters.MaskUvRowY);
        maskFeatherStepParameter.SetValue(
            parameters.MaskFeatherStep);
        filterHeaderParameter.SetValue(
            parameters.FilterHeader);
        filterOptions0Parameter.SetValue(
            parameters.FilterOptions0);
        filterOptions1Parameter.SetValue(
            parameters.FilterOptions1);
        filterOptions2Parameter.SetValue(
            parameters.FilterOptions2);
        filterOptions3Parameter.SetValue(
            parameters.FilterOptions3);
        filterOptions4Parameter.SetValue(
            parameters.FilterOptions4);
        filterOptions5Parameter.SetValue(
            parameters.FilterOptions5);
        filterOptions6Parameter.SetValue(
            parameters.FilterOptions6);
        filterOptions7Parameter.SetValue(
            parameters.FilterOptions7);
        filterOptions8Parameter.SetValue(
            parameters.FilterOptions8);
        filterOptions9Parameter.SetValue(
            parameters.FilterOptions9);
        filterTextureSizeParameter.SetValue(
            parameters.FilterTextureSize);
        filterAuxiliaryTextureParameter.SetValue(
            parameters.FilterAuxiliaryTexture ??
            parameters.SecondaryTexture);
        filterLightCountParameter.SetValue(
            parameters.FilterLightCount);
        if (parameters.FilterLights is not null)
        {
            filterLightsParameter.SetValue(
                parameters.FilterLights);
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        effect.Dispose();
        styleEffect.Dispose();
        charcoalEffect.Dispose();
        conteCrayonEffect.Dispose();
        graphicPenEffect.Dispose();
        plasterEffect.Dispose();
        deinterlaceEffect.Dispose();
        dissolveThresholdTexture.Dispose();
        disposed = true;
    }

    private void BindStyle(
        PrismKernel kernel,
        in PrismKernelParameters parameters)
    {
        activeEffect = styleEffect;
        styleEffect.CurrentTechnique = kernel.Technique;
        styleSourceTextureParameter.SetValue(
            parameters.SourceTexture ?? parameters.SecondaryTexture);
        styleSecondaryTextureParameter.SetValue(
            parameters.SecondaryTexture);
        styleOpacityParameter.SetValue(parameters.Opacity);
        stylePixelSizeParameter.SetValue(parameters.PixelSize);
        styleUvScaleParameter.SetValue(parameters.UvScale);
        styleUvOffsetParameter.SetValue(parameters.UvOffset);
        styleMaskDensityParameter.SetValue(parameters.MaskDensity);
        styleMaskFeatherStepParameter.SetValue(
            parameters.MaskFeatherStep);
        styleTextureParameter.SetValue(
            parameters.StyleTexture ?? parameters.SecondaryTexture);
        styleMaskTextureParameter.SetValue(
            parameters.StyleMaskTexture ?? parameters.SecondaryTexture);
        styleBackdropTextureParameter.SetValue(
            parameters.StyleBackdropTexture ?? parameters.SecondaryTexture);
        styleColorParameter.SetValue(parameters.StyleColor);
        styleSecondaryColorParameter.SetValue(
            parameters.StyleSecondaryColor);
        styleGeometry0Parameter.SetValue(parameters.StyleGeometry0);
        styleGeometry1Parameter.SetValue(parameters.StyleGeometry1);
        styleOptions0Parameter.SetValue(parameters.StyleOptions0);
        styleOptions1Parameter.SetValue(parameters.StyleOptions1);
        Vector4 styleModes0 = parameters.StyleModes0;
        if (kernel == layerStyle)
        {
            styleModes0.Y = PrismBlendMath.ToShaderMode(
                (PrismBlendMode)(int)styleModes0.Y);
            styleModes0.Z = PrismBlendMath.ToShaderMode(
                (PrismBlendMode)(int)styleModes0.Z);
        }
        styleModes0Parameter.SetValue(styleModes0);
        styleModes1Parameter.SetValue(parameters.StyleModes1);
        styleModes2Parameter.SetValue(parameters.StyleModes2);
        styleModes3Parameter.SetValue(parameters.StyleModes3);
        styleBoundsUvRowXParameter.SetValue(
            parameters.StyleBoundsUvRowX);
        styleBoundsUvRowYParameter.SetValue(
            parameters.StyleBoundsUvRowY);
        styleResourceAvailableParameter.SetValue(
            parameters.StyleResourceAvailable);
        styleBackdropAvailableParameter.SetValue(
            parameters.StyleBackdropAvailable);
        styleFilterAuxiliaryTextureParameter.SetValue(
            parameters.FilterAuxiliaryTexture ??
                parameters.SecondaryTexture);
    }

    private void BindDeinterlace(
        in PrismKernelParameters parameters)
    {
        activeEffect = deinterlaceEffect;
        deinterlaceEffect.CurrentTechnique =
            deinterlaceFilter.Technique;
        deinterlaceSourceTextureParameter.SetValue(
            parameters.SourceTexture ?? parameters.SecondaryTexture);
        deinterlaceOpacityParameter.SetValue(
            parameters.Opacity);
        deinterlacePixelSizeParameter.SetValue(
            parameters.PixelSize);
        deinterlaceUvScaleParameter.SetValue(
            parameters.UvScale);
        deinterlaceUvOffsetParameter.SetValue(
            parameters.UvOffset);
        deinterlaceFilterHeaderParameter.SetValue(
            parameters.FilterHeader);
        deinterlaceFilterOptions0Parameter.SetValue(
            parameters.FilterOptions0);
        deinterlaceFilterOptions1Parameter.SetValue(
            parameters.FilterOptions1);
        deinterlaceFilterOptions9Parameter.SetValue(
            parameters.FilterOptions9);
        deinterlaceTextureSizeParameter.SetValue(
            parameters.FilterTextureSize);
    }

    private void BindCharcoal(
        PrismKernel kernel,
        in PrismKernelParameters parameters)
    {
        activeEffect = charcoalEffect;
        charcoalEffect.CurrentTechnique = kernel.Technique;
        charcoalSourceTextureParameter.SetValue(
            parameters.SourceTexture ?? parameters.SecondaryTexture);
        charcoalOpacityParameter.SetValue(parameters.Opacity);
        charcoalPixelSizeParameter.SetValue(parameters.PixelSize);
        charcoalUvScaleParameter.SetValue(parameters.UvScale);
        charcoalUvOffsetParameter.SetValue(parameters.UvOffset);
        charcoalFilterHeaderParameter.SetValue(parameters.FilterHeader);
        charcoalFilterOptions0Parameter.SetValue(parameters.FilterOptions0);
        charcoalFilterOptions1Parameter.SetValue(parameters.FilterOptions1);
        charcoalFilterOptions2Parameter.SetValue(parameters.FilterOptions2);
        charcoalFilterOptions3Parameter.SetValue(parameters.FilterOptions3);
        charcoalFilterOptions4Parameter.SetValue(parameters.FilterOptions4);
        charcoalFilterOptions5Parameter.SetValue(parameters.FilterOptions5);
        charcoalFilterOptions6Parameter.SetValue(parameters.FilterOptions6);
        charcoalFilterOptions8Parameter.SetValue(parameters.FilterOptions8);
        charcoalFilterOptions9Parameter.SetValue(parameters.FilterOptions9);
        charcoalFilterAuxiliaryTextureParameter.SetValue(
            parameters.FilterAuxiliaryTexture ?? parameters.SecondaryTexture);
    }

    private void BindConteCrayon(
        in PrismKernelParameters parameters)
    {
        activeEffect = conteCrayonEffect;
        conteCrayonEffect.CurrentTechnique = conteCrayonFilter.Technique;
        conteCrayonSourceTextureParameter.SetValue(
            parameters.SourceTexture ?? parameters.SecondaryTexture);
        conteCrayonOpacityParameter.SetValue(parameters.Opacity);
        conteCrayonPixelSizeParameter.SetValue(parameters.PixelSize);
        conteCrayonUvScaleParameter.SetValue(parameters.UvScale);
        conteCrayonUvOffsetParameter.SetValue(parameters.UvOffset);
        conteCrayonFilterHeaderParameter.SetValue(parameters.FilterHeader);
        conteCrayonFilterOptions0Parameter.SetValue(parameters.FilterOptions0);
        conteCrayonFilterOptions1Parameter.SetValue(parameters.FilterOptions1);
        conteCrayonFilterOptions2Parameter.SetValue(parameters.FilterOptions2);
        conteCrayonFilterOptions3Parameter.SetValue(parameters.FilterOptions3);
        conteCrayonFilterOptions4Parameter.SetValue(parameters.FilterOptions4);
        conteCrayonFilterOptions5Parameter.SetValue(parameters.FilterOptions5);
        conteCrayonFilterOptions6Parameter.SetValue(parameters.FilterOptions6);
        conteCrayonFilterOptions7Parameter.SetValue(parameters.FilterOptions7);
        conteCrayonFilterOptions9Parameter.SetValue(parameters.FilterOptions9);
        conteCrayonFilterAuxiliaryTextureParameter.SetValue(
            parameters.FilterAuxiliaryTexture ?? parameters.SecondaryTexture);
    }

    private void BindGraphicPen(
        in PrismKernelParameters parameters)
    {
        activeEffect = graphicPenEffect;
        graphicPenEffect.CurrentTechnique = graphicPenFilter.Technique;
        graphicPenOpacityParameter.SetValue(parameters.Opacity);
        graphicPenSourceTextureParameter.SetValue(
            parameters.SourceTexture ?? parameters.SecondaryTexture);
        graphicPenPixelSizeParameter.SetValue(parameters.PixelSize);
        graphicPenUvScaleParameter.SetValue(parameters.UvScale);
        graphicPenUvOffsetParameter.SetValue(parameters.UvOffset);
        graphicPenFilterHeaderParameter.SetValue(parameters.FilterHeader);
        graphicPenFilterOptions0Parameter.SetValue(parameters.FilterOptions0);
        graphicPenFilterOptions1Parameter.SetValue(parameters.FilterOptions1);
        graphicPenFilterOptions2Parameter.SetValue(parameters.FilterOptions2);
        graphicPenFilterOptions3Parameter.SetValue(parameters.FilterOptions3);
        graphicPenFilterOptions4Parameter.SetValue(parameters.FilterOptions4);
        graphicPenFilterOptions9Parameter.SetValue(parameters.FilterOptions9);
        graphicPenFilterAuxiliaryTextureParameter.SetValue(
            parameters.FilterAuxiliaryTexture ?? parameters.SecondaryTexture);
    }

    private void BindPlaster(
        in PrismKernelParameters parameters)
    {
        activeEffect = plasterEffect;
        plasterEffect.CurrentTechnique = plasterFilter.Technique;
        plasterSourceTextureParameter.SetValue(
            parameters.SourceTexture ?? parameters.SecondaryTexture);
        plasterOpacityParameter.SetValue(parameters.Opacity);
        plasterPixelSizeParameter.SetValue(parameters.PixelSize);
        plasterUvScaleParameter.SetValue(parameters.UvScale);
        plasterUvOffsetParameter.SetValue(parameters.UvOffset);
        plasterFilterHeaderParameter.SetValue(parameters.FilterHeader);
        plasterFilterOptions0Parameter.SetValue(parameters.FilterOptions0);
        plasterFilterOptions1Parameter.SetValue(parameters.FilterOptions1);
        plasterFilterOptions3Parameter.SetValue(parameters.FilterOptions3);
        plasterFilterOptions4Parameter.SetValue(parameters.FilterOptions4);
        plasterFilterOptions5Parameter.SetValue(parameters.FilterOptions5);
        plasterFilterOptions6Parameter.SetValue(parameters.FilterOptions6);
        plasterFilterOptions9Parameter.SetValue(parameters.FilterOptions9);
        plasterFilterAuxiliaryTextureParameter.SetValue(
            parameters.FilterAuxiliaryTexture ?? parameters.SecondaryTexture);
    }

    private bool IsCharcoalKernel(PrismKernel kernel) =>
        kernel == charcoalFilter ||
        kernel == charcoalInitialEtfFilter ||
        kernel == charcoalRefineEtfFilter ||
        kernel == charcoalNormalDogFilter ||
        kernel == charcoalFlowDogFilter;

    private bool IsStyleKernel(PrismKernel kernel) =>
        kernel == styleDilate ||
        kernel == styleGaussian ||
        kernel == strokeDistanceSeed ||
        kernel == strokeDistanceFlood ||
        kernel == bevelHeight ||
        kernel == bevelLighting ||
        kernel == layerStyle;

    private PrismKernel CreateKernel(
        PrismKernelKind kind,
        string techniqueName)
    {
        EffectTechnique? technique = effect.Techniques[techniqueName];
        return technique is null
            ? throw new PrismShaderUnavailableException(
                $"The Prism shader package does not contain technique '{techniqueName}'.")
            : new PrismKernel(kind, technique);
    }

    private static Texture2D CreateDissolveThresholdTexture(
        GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(
            graphicsDevice,
            PrismDissolveBlend.ThresholdSize,
            PrismDissolveBlend.ThresholdSize,
            false,
            SurfaceFormat.Alpha8);
        try
        {
            texture.SetData(PrismDissolveBlend.Thresholds.ToArray());
            return texture;
        }
        catch
        {
            texture.Dispose();
            throw;
        }
    }

    private EffectParameter GetParameter(string name)
    {
        EffectParameter? parameter = effect.Parameters[name];
        return parameter ??
            throw new PrismShaderUnavailableException(
                $"The Prism shader package does not contain parameter '{name}'.");
    }

    private EffectParameter GetDeinterlaceParameter(string name)
    {
        EffectParameter? parameter =
            deinterlaceEffect.Parameters[name];
        return parameter ??
            throw new PrismShaderUnavailableException(
                $"The Deinterlace shader package does not contain parameter '{name}'.");
    }

    private EffectParameter GetStyleParameter(string name)
    {
        EffectParameter? parameter = styleEffect.Parameters[name];
        return parameter ??
            throw new PrismShaderUnavailableException(
                $"The Styles shader package does not contain parameter '{name}'.");
    }

    private EffectParameter GetCharcoalParameter(string name)
    {
        EffectParameter? parameter = charcoalEffect.Parameters[name];
        return parameter ??
            throw new PrismShaderUnavailableException(
                $"The Charcoal shader package does not contain parameter '{name}'.");
    }

    private EffectParameter GetConteCrayonParameter(string name)
    {
        EffectParameter? parameter = conteCrayonEffect.Parameters[name];
        return parameter ??
            throw new PrismShaderUnavailableException(
                $"The ConteCrayon shader package does not contain parameter '{name}'.");
    }

    private EffectParameter GetGraphicPenParameter(string name)
    {
        EffectParameter? parameter = graphicPenEffect.Parameters[name];
        return parameter ??
            throw new PrismShaderUnavailableException(
                $"The GraphicPen shader package does not contain parameter '{name}'.");
    }

    private EffectParameter GetPlasterParameter(string name)
    {
        EffectParameter? parameter = plasterEffect.Parameters[name];
        return parameter ??
            throw new PrismShaderUnavailableException(
                $"The Plaster shader package does not contain parameter '{name}'.");
    }

    private static PrismKernel CreateKernel(
        Effect owner,
        PrismKernelKind kind,
        string techniqueName,
        string packageName)
    {
        EffectTechnique? technique =
            owner.Techniques[techniqueName];
        return technique is null
            ? throw new PrismShaderUnavailableException(
                $"The {packageName} shader package does not contain technique '{techniqueName}'.")
            : new PrismKernel(kind, technique);
    }

    private static void ValidateFundamentalCatalogBindings()
    {
        foreach (PrismBlendMode blendMode in
            Enum.GetValues<PrismBlendMode>())
        {
            ValidateCatalogBinding(
                "blend-mode",
                blendMode.ToString());
        }
        foreach (PrismColorProfile profile in
            Enum.GetValues<PrismColorProfile>())
        {
            ValidateCatalogBinding(
                "color-profile",
                profile.ToString());
        }
        ValidateCatalogBinding("sampling", "Linear");
        foreach (PrismStyleId style in
            Enum.GetValues<PrismStyleId>())
        {
            ValidateCatalogBinding("style", style.ToString());
        }
    }

    private bool IsRegisteredColorProfile(string symbol)
    {
        return Enum.TryParse(
                symbol,
                ignoreCase: false,
                out PrismColorProfile profile) &&
            string.Equals(
                profile.ToString(),
                symbol,
                StringComparison.Ordinal) &&
            inputColorConversions.ContainsKey(profile) &&
            outputColorConversions.ContainsKey(profile);
    }

    private bool IsRegisteredBlendMode(string symbol)
    {
        return Enum.TryParse(
                symbol,
                ignoreCase: false,
                out PrismBlendMode blendMode) &&
            string.Equals(
                blendMode.ToString(),
                symbol,
                StringComparison.Ordinal) &&
            blendKernels.ContainsKey(blendMode);
    }

    private bool IsRegisteredStyle(string symbol)
    {
        return Enum.TryParse(
                symbol,
                ignoreCase: false,
                out PrismStyleId style) &&
            string.Equals(
                style.ToString(),
                symbol,
                StringComparison.Ordinal) &&
            TryGetStyleKernel(style, out PrismKernel kernel) &&
            kernel == layerStyle;
    }

    private bool IsRegisteredFilter(
        string symbol)
    {
        if (!Enum.TryParse(
                symbol,
                ignoreCase: false,
                out PrismFilterId filter) ||
            !string.Equals(
                filter.ToString(),
                symbol,
                StringComparison.Ordinal) ||
            !filterKernels.TryGetValue(
                filter,
                out PrismKernel kernel))
        {
            return false;
        }

        return PrismAdjustmentPlanner.IsSupported(filter)
            ? kernel == adjustmentFilter
            : PrismNeighborhoodPlanner.IsSupported(filter)
                ? kernel == neighborhoodFilter
                : PrismResamplingPlanner.IsSupported(filter)
                    ? kernel == resamplingFilter
                    : PrismCatalogFilterPlanner.IsSupported(filter) &&
                        kernel == ResolveCatalogFilterKernel(filter);
    }

    private void RegisterCatalogFilters()
    {
        foreach (PrismCatalogEntryDescriptor entry in
            PrismCatalogGenerated.Entries)
        {
            if (entry.Kind != "filter")
            {
                continue;
            }
            if (!Enum.TryParse(
                    entry.Symbol,
                    ignoreCase: false,
                    out PrismFilterId filter) ||
                !string.Equals(
                    filter.ToString(),
                    entry.Symbol,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Catalog filter '{entry.Id}' has no generated filter id.");
            }

            PrismKernel kernel;
            if (PrismAdjustmentPlanner.IsSupported(filter))
            {
                kernel = adjustmentFilter;
            }
            else if (PrismNeighborhoodPlanner.IsSupported(filter))
            {
                kernel = neighborhoodFilter;
            }
            else if (PrismResamplingPlanner.IsSupported(filter))
            {
                kernel = resamplingFilter;
            }
            else if (PrismCatalogFilterPlanner.IsSupported(filter))
            {
                kernel = ResolveCatalogFilterKernel(filter);
            }
            else
            {
                continue;
            }

            string expectedOwner =
                FilterOwnerPrefix + entry.Symbol;
            if (!string.Equals(
                entry.Coverage.Kernel,
                expectedOwner,
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Catalog filter '{entry.Id}' is assigned to " +
                    $"'{entry.Coverage.Kernel}', not '{expectedOwner}'.");
            }
            filterKernels.Add(filter, kernel);
        }
    }

    private PrismKernel ResolveCatalogFilterKernel(
        PrismFilterId filter) =>
        filter switch
        {
            PrismFilterId.Deinterlace =>
                deinterlaceFilter,
            PrismFilterId.Clouds or
                PrismFilterId.DifferenceClouds =>
                waveNoiseFilter,
            PrismFilterId.Spatter =>
                spatterFilter,
            PrismFilterId.SprayedStrokes =>
                sprayedStrokesFilter,
            PrismFilterId.ColorHalftone =>
                colorHalftoneFilter,
            PrismFilterId.ColoredPencil =>
                coloredPencilFilter,
            PrismFilterId.Cutout =>
                cutoutFilter,
            PrismFilterId.DryBrush =>
                dryBrushFilter,
            PrismFilterId.Underpainting =>
                underpaintingFilter,
            PrismFilterId.Watercolor =>
                watercolorFilter,
            PrismFilterId.WaterPaper =>
                waterPaperFilter,
            PrismFilterId.Wind =>
                windFilter,
            PrismFilterId.SumiE =>
                sumiEFilter,
            PrismFilterId.ChalkCharcoal =>
                chalkCharcoalFilter,
            PrismFilterId.Charcoal =>
                charcoalFilter,
            PrismFilterId.ConteCrayon =>
                conteCrayonFilter,
            PrismFilterId.GraphicPen =>
                graphicPenFilter,
            PrismFilterId.Facet =>
                facetFilter,
            PrismFilterId.Fresco =>
                frescoFilter,
            PrismFilterId.LightingEffects =>
                lightingEffectsFilter,
            PrismFilterId.AccentedEdges or
                PrismFilterId.DarkStrokes or
                PrismFilterId.InkOutlines =>
                accentedEdgesFilter,
            PrismFilterId.GlowingEdges =>
                glowingEdgesFilter,
            PrismFilterId.TraceContour =>
                traceContourFilter,
            PrismFilterId.BasRelief =>
                basReliefFilter,
            PrismFilterId.PosterEdges =>
                posterEdgesFilter,
            PrismFilterId.Chrome =>
                chromeFilter,
            PrismFilterId.NotePaper =>
                notePaperFilter,
            PrismFilterId.Plaster =>
                plasterFilter,
            PrismFilterId.Photocopy or
                PrismFilterId.Stamp or
                PrismFilterId.TornEdges =>
                photocopyFilter,
            PrismFilterId.Craquelure =>
                craquelureFilter,
            PrismFilterId.Texturizer =>
                texturizerFilter,
            PrismFilterId.Grain =>
                grainFilter,
            PrismFilterId.MosaicTiles =>
                mosaicTilesFilter,
            PrismFilterId.Patchwork =>
                patchworkFilter,
            PrismFilterId.Reticulation =>
                reticulationFilter,
            PrismFilterId.StainedGlass =>
                stainedGlassFilter,
            _ =>
                catalogFilter
        };

    private static void ValidateCatalogBinding(
        string kind,
        string symbol)
    {
        string expectedOwner = CatalogOwnerPrefix + symbol;
        foreach (PrismCatalogEntryDescriptor entry in
            PrismCatalogGenerated.Entries)
        {
            if (entry.Kind != kind || entry.Symbol != symbol)
            {
                continue;
            }

            if (!string.Equals(
                entry.Coverage.Kernel,
                expectedOwner,
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Catalog entry '{entry.Id}' is assigned to " +
                    $"'{entry.Coverage.Kernel}', not '{expectedOwner}'.");
            }

            return;
        }

        throw new InvalidOperationException(
            $"The Prism catalog does not contain fundamental " +
            $"kernel '{kind}:{symbol}'.");
    }
}

internal readonly record struct PrismKernel(
    PrismKernelKind Kind,
    EffectTechnique Technique);

internal sealed class PrismShaderUnavailableException :
    InvalidOperationException
{
    public PrismShaderUnavailableException(string message)
        : base(message)
    {
    }

    public PrismShaderUnavailableException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}

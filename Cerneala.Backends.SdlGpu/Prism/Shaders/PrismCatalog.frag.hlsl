#define PRISM_DEINTERLACE_EFFECT
#define CERNEALA_SDL_GPU

struct VertexShaderOutput
{
    float4 Position : SV_Position;
    float4 Color : TEXCOORD0;
    float2 TextureCoordinates : TEXCOORD1;
};

cbuffer PrismFragmentUniforms : register(b0, space3)
{
    float4 PrismFrame;
    float4 PrismUv;
    float4 BlendChannels;
    float4 PrismBlendControl;
    float4 ThisLayerRange;
    float4 UnderlyingRange;
    float4 PrismMaskControl;
    float4 PrismMaskUvRowX;
    float4 PrismMaskUvRowY;
    float4 PrismMaskFeather;
    float4 StyleColor;
    float4 StyleSecondaryColor;
    float4 StyleGeometry0;
    float4 StyleGeometry1;
    float4 StyleOptions0;
    float4 StyleOptions1;
    float4 StyleModes0;
    float4 StyleModes1;
    float4 StyleModes2;
    float4 StyleModes3;
    float4 PrismStyleBoundsUvRowX;
    float4 PrismStyleBoundsUvRowY;
    float4 PrismStyleControl;
    float4 FilterHeader;
    float4 FilterOptions0;
    float4 FilterOptions1;
    float4 FilterOptions2;
    float4 FilterOptions3;
    float4 FilterOptions4;
    float4 FilterOptions5;
    float4 FilterOptions6;
    float4 FilterOptions7;
    float4 FilterOptions8;
    float4 FilterOptions9;
    float4 PrismFilterControl;
    float4 FilterLights[24];
};

#define Opacity PrismFrame.x
#define PixelSize PrismFrame.yz
#define UvScale PrismUv.xy
#define UvOffset PrismUv.zw
#define KnockoutMode PrismBlendControl.x
#define KnockoutBackdropAvailable PrismBlendControl.y
#define BlendIfChannel PrismBlendControl.z
#define DissolveSeed PrismBlendControl.w
#define BackgroundAvailable PrismMaskControl.x
#define MaskChannel PrismMaskControl.y
#define MaskDensity PrismMaskControl.z
#define MaskInvert PrismMaskControl.w
#define MaskUvRowX PrismMaskUvRowX.xyz
#define MaskUvRowY PrismMaskUvRowY.xyz
#define MaskFeatherStep PrismMaskFeather.xy
#define StyleBoundsUvRowX PrismStyleBoundsUvRowX.xyz
#define StyleBoundsUvRowY PrismStyleBoundsUvRowY.xyz
#define StyleResourceAvailable PrismStyleControl.x
#define StyleBackdropAvailable PrismStyleControl.y
#define FilterLightCount PrismStyleControl.z
#define FilterTextureSize PrismFilterControl.xy
#define PrismKernelId ((uint)(PrismFilterControl.z + 0.5))

Texture2D SpriteTexture : register(t0, space2);
SamplerState SpriteTextureState : register(s0, space2);
Texture2D SecondaryTexture : register(t1, space2);
SamplerState SecondaryTextureState : register(s1, space2);
Texture2D KnockoutBackdropTexture : register(t2, space2);
SamplerState KnockoutBackdropState : register(s2, space2);
Texture2D KnockoutShapeTexture : register(t3, space2);
SamplerState KnockoutShapeState : register(s3, space2);
Texture2D StyleTexture : register(t4, space2);
SamplerState StyleTextureState : register(s4, space2);
Texture2D StyleMaskTexture : register(t5, space2);
SamplerState StyleMaskState : register(s5, space2);
Texture2D FilterAuxiliaryTexture : register(t6, space2);
SamplerState FilterAuxiliaryState : register(s6, space2);
Texture2D DissolveThresholdTexture : register(t7, space2);
SamplerState DissolveThresholdState : register(s7, space2);
Texture2D StyleBackdropTexture : register(t8, space2);
SamplerState StyleBackdropState : register(s8, space2);
Texture2D GradientDitherTexture : register(t9, space2);
SamplerState GradientDitherState : register(s9, space2);
Texture2D DisplacementMapTexture : register(t10, space2);
SamplerState DisplacementMapState : register(s10, space2);
Texture2D StyleDistanceTexture : register(t11, space2);
SamplerState StyleDistanceState : register(s11, space2);
Texture2D StyleMaskSourceTexture : register(t12, space2);
SamplerState StyleMaskSourceState : register(s12, space2);
Texture2D WaveNoiseTableTexture : register(t13, space2);
SamplerState WaveNoiseTableState : register(s13, space2);
Texture2D SpatterPointTexture : register(t14, space2);
SamplerState SpatterPointState : register(s14, space2);

#define SpriteTextureSampler SpriteTexture, SpriteTextureState
#define SecondaryTextureSampler SecondaryTexture, SecondaryTextureState
#define KnockoutBackdropSampler KnockoutBackdropTexture, KnockoutBackdropState
#define KnockoutShapeSampler KnockoutShapeTexture, KnockoutShapeState
#define StyleTextureSampler StyleTexture, StyleTextureState
#define StyleMaskTextureSampler StyleMaskTexture, StyleMaskState
#define FilterAuxiliaryTextureSampler FilterAuxiliaryTexture, FilterAuxiliaryState
#define DissolveThresholdSampler DissolveThresholdTexture, DissolveThresholdState
#define StyleBackdropTextureSampler StyleBackdropTexture, StyleBackdropState
#define GradientDitherSampler GradientDitherTexture, GradientDitherState
#define DisplacementMapSampler DisplacementMapTexture, DisplacementMapState
#define StyleDistanceTextureSampler StyleDistanceTexture, StyleDistanceState
#define StyleMaskSourceSampler StyleMaskSourceTexture, StyleMaskSourceState
#define WaveNoiseTableSampler WaveNoiseTableTexture, WaveNoiseTableState
#define SpatterPointSampler SpatterPointTexture, SpatterPointState
#define PolarTextureSampler SpriteTextureSampler
#define NeonPyramidSampler SpriteTextureSampler
#define AccentedEdgesOriginalSampler FilterAuxiliaryTextureSampler
#define ChalkCharcoalOriginalSampler FilterAuxiliaryTextureSampler
#define CharcoalOriginalSampler FilterAuxiliaryTextureSampler
#define ChromeOriginalSampler FilterAuxiliaryTextureSampler
#define ColoredPencilOriginalSampler FilterAuxiliaryTextureSampler
#define ConteCrayonOriginalSampler FilterAuxiliaryTextureSampler
#define FrescoOriginalSampler FilterAuxiliaryTextureSampler
#define GlowingEdgesOriginalSampler FilterAuxiliaryTextureSampler
#define NotePaperOriginalSampler FilterAuxiliaryTextureSampler
#define PhotocopyOriginalSampler FilterAuxiliaryTextureSampler
#define PlasterOriginalSampler FilterAuxiliaryTextureSampler
#define PosterEdgesOriginalSampler FilterAuxiliaryTextureSampler
#define StainedGlassSeedSampler SpriteTextureSampler
#define StainedGlassOriginalSampler FilterAuxiliaryTextureSampler
#define SumiEOriginalSampler FilterAuxiliaryTextureSampler
#define TexturizerTextureSampler SecondaryTextureSampler
#define WatercolorOriginalSampler FilterAuxiliaryTextureSampler
#define WaterPaperOriginalSampler FilterAuxiliaryTextureSampler

#define CernealaSample(textureName, samplerName, coordinates) \
    textureName.Sample(samplerName, coordinates)
#define CernealaSampleLevel(textureName, samplerName, coordinates) \
    textureName.SampleLevel(samplerName, (coordinates).xy, (coordinates).w)
#define tex2D(binding, coordinates) CernealaSample(binding, coordinates)
#define tex2Dlod(binding, coordinates) CernealaSampleLevel(binding, coordinates)

float2 ResolveUv(VertexShaderOutput input)
{
    float2 uv = (input.TextureCoordinates * UvScale) + UvOffset;
    return clamp(uv, PixelSize * 0.5, 1.0 - (PixelSize * 0.5));
}

float4 SampleSource(VertexShaderOutput input)
{
    return tex2D(SpriteTextureSampler, ResolveUv(input));
}

float4 SampleSecondary(VertexShaderOutput input)
{
    return tex2D(SecondaryTextureSampler, ResolveUv(input));
}

float4 SampleKnockoutBackdrop(VertexShaderOutput input)
{
    return tex2D(KnockoutBackdropSampler, ResolveUv(input));
}

float SampleKnockoutShape(VertexShaderOutput input)
{
    return tex2D(KnockoutShapeSampler, ResolveUv(input)).a;
}

#include "../../../Drawing/Prism/Shaders/Hlsl/AllKernels.hlsl"

float4 PrismFinalizeCatalog(
    VertexShaderOutput input,
    float4 source,
    float4 filtered,
    int profile)
{
    filtered.a = saturate(filtered.a);
    filtered.rgb = clamp(filtered.rgb, 0.0, filtered.a);
    int blendMode = (int)(FilterOptions9.w + 0.5);
    float3 blendedStraight = EvaluateBlendMode(
        blendMode,
        saturate(Unpremultiply(source)),
        saturate(Unpremultiply(filtered)));
    float4 blended = float4(blendedStraight * filtered.a, filtered.a);
    float4 result = lerp(source, blended, saturate(Opacity));
    return LinearSrgbAssociatedToWorking(result, profile) * input.Color;
}

float4 CharcoalInitialEtfSdl(VertexShaderOutput input)
{
    int profile = (int)(FilterHeader.y + 0.5);
    return CharcoalInitialEtf(ResolveUv(input), profile) * input.Color;
}

float4 CharcoalRefineEtfSdl(VertexShaderOutput input)
{
    return CharcoalRefineEtf(ResolveUv(input)) * input.Color;
}

float4 CharcoalNormalDogSdl(VertexShaderOutput input)
{
    int profile = (int)(FilterHeader.y + 0.5);
    return CharcoalNormalDog(ResolveUv(input), profile) * input.Color;
}

float4 CharcoalFlowDogSdl(VertexShaderOutput input)
{
    return CharcoalFlowDog(ResolveUv(input)) * input.Color;
}

float4 CharcoalSdl(VertexShaderOutput input)
{
    int profile = (int)(FilterHeader.y + 0.5);
    float2 uv = ResolveUv(input);
    float4 original = CharcoalOriginal(uv, profile);
    return PrismFinalizeCatalog(
        input,
        original,
        CharcoalComposite(uv, original),
        profile);
}

float4 ConteCrayonSdl(VertexShaderOutput input)
{
    int profile = (int)(FilterHeader.y + 0.5);
    float2 uv = ResolveUv(input);
    float4 original = ConteCrayonOriginal(uv, profile);
    return PrismFinalizeCatalog(
        input,
        original,
        ConteCrayonComposite(uv, original),
        profile);
}

float4 GraphicPenSdl(VertexShaderOutput input)
{
    int profile = (int)(FilterHeader.y + 0.5);
    float2 uv = ResolveUv(input);
    float4 original = CharcoalOriginal(uv, profile);
    return PrismFinalizeCatalog(
        input,
        original,
        GraphicPenComposite(uv, original),
        profile);
}

float4 PlasterSdl(VertexShaderOutput input)
{
    int profile = (int)(FilterHeader.y + 0.5);
    int passIndex = (int)(FilterOptions9.z / 4.0);
    float2 uv = ResolveUv(input);
    if (passIndex == 0)
    {
        return PlasterHorizontalMoments(uv, profile) * input.Color;
    }
    if (passIndex == 1)
    {
        return PlasterVerticalCoefficients(uv) * input.Color;
    }
    if (passIndex == 2)
    {
        return PlasterHorizontalCoefficients(uv) * input.Color;
    }
    if (passIndex == 3)
    {
        return PlasterReconstructHeight(uv, profile) * input.Color;
    }

    float4 original = PlasterOriginal(uv, profile);
    return PrismFinalizeCatalog(
        input,
        original,
        PlasterComposite(uv, original),
        profile);
}

float4 DeinterlaceSdl(VertexShaderOutput input)
{
    int profile = (int)(FilterHeader.y + 0.5);
    float4 source = WorkingAssociatedToLinearSrgb(
        SampleSource(input),
        profile);
    return PrismFinalizeCatalog(
        input,
        source,
        CatalogDeinterlace(ResolveUv(input), source, profile),
        profile);
}

float4 main(VertexShaderOutput input) : SV_Target0
{
    switch (PrismKernelId)
    {
        case 0: return CopyCompositePixelShader(input);
        case 1: return BackdropCropPixelShader(input);
        case 2: return BackdropColorConversionPixelShader(input);
        case 3: return AdjustmentFilterPixelShader(input);
        case 4: return LevelsCdfPixelShader(input);
        case 5: return LevelsRangePixelShader(input);
        case 6: return ThresholdRangePixelShader(input);
        case 7: return NeighborhoodFilterPixelShader(input);
        case 8: return ResamplingFilterPixelShader(input);
        case 9: return CatalogFilterPixelShader(input);
        case 10: return DryBrushFilterPixelShader(input);
        case 11: return UnderpaintingFilterPixelShader(input);
        case 12: return WatercolorFilterPixelShader(input);
        case 13: return WaterPaperFilterPixelShader(input);
        case 14: return WindFilterPixelShader(input);
        case 15: return SumiEFilterPixelShader(input);
        case 16: return ChalkCharcoalFilterPixelShader(input);
        case 17: return ColoredPencilFilterPixelShader(input);
        case 18: return FrescoFilterPixelShader(input);
        case 19: return CutoutFilterPixelShader(input);
        case 20: return PosterEdgesFilterPixelShader(input);
        case 21: return AccentedEdgesFilterPixelShader(input);
        case 22: return GlowingEdgesFilterPixelShader(input);
        case 23: return TraceContourFilterPixelShader(input);
        case 24: return ChromeFilterPixelShader(input);
        case 25: return NotePaperFilterPixelShader(input);
        case 26: return PhotocopyFilterPixelShader(input);
        case 27: return ReticulationFilterPixelShader(input);
        case 28: return StainedGlassFilterPixelShader(input);
        case 29: return CraquelureFilterPixelShader(input);
        case 30: return TexturizerFilterPixelShader(input);
        case 31: return GrainFilterPixelShader(input);
        case 32: return MosaicTilesFilterPixelShader(input);
        case 33: return PatchworkFilterPixelShader(input);
        case 34: return WaveNoiseFilterPixelShader(input);
        case 35: return SpatterFilterPixelShader(input);
        case 36: return SprayedStrokesFilterPixelShader(input);
        case 37: return ColorHalftoneFilterPixelShader(input);
        case 38: return FacetFilterPixelShader(input);
        case 39: return LightingEffectsFilterPixelShader(input);
        case 40: return MaskAlphaPixelShader(input);
        case 41: return MaskExtractPixelShader(input);
        case 42: return MaskFeatherPixelShader(input);
        case 43: return ClipAlphaPixelShader(input);
        case 44: return NormalBlendPixelShader(input);
        case 45: return DissolveBlendPixelShader(input);
        case 46: return DarkenBlendPixelShader(input);
        case 47: return MultiplyBlendPixelShader(input);
        case 48: return ColorBurnBlendPixelShader(input);
        case 49: return LinearBurnBlendPixelShader(input);
        case 50: return DarkerColorBlendPixelShader(input);
        case 51: return LightenBlendPixelShader(input);
        case 52: return ScreenBlendPixelShader(input);
        case 53: return ColorDodgeBlendPixelShader(input);
        case 54: return LinearDodgeBlendPixelShader(input);
        case 55: return LighterColorBlendPixelShader(input);
        case 56: return OverlayBlendPixelShader(input);
        case 57: return SoftLightBlendPixelShader(input);
        case 58: return HardLightBlendPixelShader(input);
        case 59: return VividLightBlendPixelShader(input);
        case 60: return LinearLightBlendPixelShader(input);
        case 61: return PinLightBlendPixelShader(input);
        case 62: return HardMixBlendPixelShader(input);
        case 63: return DifferenceBlendPixelShader(input);
        case 64: return ExclusionBlendPixelShader(input);
        case 65: return SubtractBlendPixelShader(input);
        case 66: return DivideBlendPixelShader(input);
        case 67: return HueBlendPixelShader(input);
        case 68: return SaturationBlendPixelShader(input);
        case 69: return ColorBlendPixelShader(input);
        case 70: return LuminosityBlendPixelShader(input);
        case 71: return PassThroughBlendPixelShader(input);
        case 72: return InputToLinearSrgbPixelShader(input);
        case 73: return InputToSrgbPixelShader(input);
        case 74: return InputToLinearDisplayP3PixelShader(input);
        case 75: return InputToDisplayP3PixelShader(input);
        case 76: return InputToScRgbPixelShader(input);
        case 77: return LinearSrgbToOutputPixelShader(input);
        case 78: return SrgbToOutputPixelShader(input);
        case 79: return LinearDisplayP3ToOutputPixelShader(input);
        case 80: return DisplayP3ToOutputPixelShader(input);
        case 81: return ScRgbToOutputPixelShader(input);
        case 82: return LayerStylePixelShader(input);
        case 83: return StyleDilatePixelShader(input);
        case 84: return StyleGaussianPixelShader(input);
        case 85: return StyleDistanceSeedPixelShader(input);
        case 86: return StyleDistanceFloodPixelShader(input);
        case 87: return BevelHeightPixelShader(input);
        case 88: return BevelLightingPixelShader(input);
        case 89: return CharcoalInitialEtfSdl(input);
        case 90: return CharcoalRefineEtfSdl(input);
        case 91: return CharcoalNormalDogSdl(input);
        case 92: return CharcoalFlowDogSdl(input);
        case 93: return CharcoalSdl(input);
        case 94: return ConteCrayonSdl(input);
        case 95: return GraphicPenSdl(input);
        case 96: return PlasterSdl(input);
        case 97: return DeinterlaceSdl(input);
        default: return CopyCompositePixelShader(input);
    }
}

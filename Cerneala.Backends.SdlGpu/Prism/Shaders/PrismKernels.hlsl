// Shared body of the SDL_GPU Prism kernel fragment shaders. Each Prism*.frag.hlsl
// entry point that includes this file switches over its own subset of the kernel ids,
// because the D3D12 WARP JIT crashes on one shader holding every kernel (issue #72).
// SdlGpuPrismKernelSelector.ForShader selects the shader that holds a kernel.
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
    float4 PrismReferenceRaster;
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
    float2 uv = ResolveUv(input);
    int profile = (int)(FilterHeader.y + 0.5);
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
// A separable, integer-factor box reduction of coverage. Partial trailing
// cells include transparent padding rather than stretching the source domain.
// Bound the loop by the actual source extent, including when k exceeds it.
float4 StyleMaskBoxPixelShader(VertexShaderOutput input)
{
    float factor = FilterOptions0.z;
    bool horizontal = FilterOptions0.w > 0.5;
    float2 axis = horizontal ? float2(1.0, 0.0) : float2(0.0, 1.0);
    float2 sourcePosition = floor(input.Position.xy) * (1.0 + axis * (factor - 1.0));
    float remaining = horizontal
        ? FilterOptions0.x - sourcePosition.x
        : FilterOptions0.y - sourcePosition.y;
    int count = (int)min(factor, max(remaining, 0.0));
    float coverage = 0.0;
    [loop]
    for (int offset = 0; offset < count; offset++)
    {
        coverage += StyleMaskSourceTexture.Load(int3(sourcePosition + axis * offset, 0)).a;
    }
    coverage /= factor;
    return float4(coverage, coverage, coverage, coverage);
}

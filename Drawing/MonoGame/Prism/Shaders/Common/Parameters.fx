Texture2D SpriteTexture;
Texture2D SecondaryTexture;
Texture2D KnockoutBackdropTexture;
Texture2D KnockoutShapeTexture;
Texture2D StyleTexture;
Texture2D StyleMaskTexture;
Texture2D StyleBackdropTexture;
Texture2D FilterAuxiliaryTexture;
Texture2D DissolveThresholdTexture;
float Opacity;
float2 PixelSize;
float2 UvScale;
float2 UvOffset;
float4 BlendChannels;
float KnockoutMode;
float KnockoutBackdropAvailable;
float BlendIfChannel;
float4 ThisLayerRange;
float4 UnderlyingRange;
float DissolveSeed;
float BackgroundAvailable;
float MaskChannel;
float MaskDensity;
float MaskInvert;
float3 MaskUvRowX;
float3 MaskUvRowY;
float2 MaskFeatherStep;
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
float3 StyleBoundsUvRowX;
float3 StyleBoundsUvRowY;
float StyleResourceAvailable;
float StyleBackdropAvailable;
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
float2 FilterTextureSize;
float FilterLightCount;
float4 FilterLights[24];

sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
};

sampler2D SecondaryTextureSampler = sampler_state
{
    Texture = <SecondaryTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D KnockoutBackdropSampler = sampler_state
{
    Texture = <KnockoutBackdropTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D KnockoutShapeSampler = sampler_state
{
    Texture = <KnockoutShapeTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D DisplacementMapSampler = sampler_state
{
    Texture = <SecondaryTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D StyleTextureSampler = sampler_state
{
    Texture = <StyleTexture>;
    AddressU = Wrap;
    AddressV = Wrap;
};

sampler2D StyleMaskTextureSampler = sampler_state
{
    Texture = <StyleMaskTexture>;
    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D StyleDistanceTextureSampler = sampler_state
{
    Texture = <StyleMaskTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D StyleMaskSourceSampler = sampler_state
{
    Texture = <SpriteTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D FilterAuxiliaryTextureSampler = sampler_state
{
    Texture = <FilterAuxiliaryTexture>;
};

sampler2D StyleBackdropTextureSampler = sampler_state
{
    Texture = <StyleBackdropTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D GradientDitherSampler = sampler_state
{
    Texture = <FilterAuxiliaryTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;
    AddressU = Wrap;
    AddressV = Wrap;
};

sampler2D DissolveThresholdSampler = sampler_state
{
    Texture = <DissolveThresholdTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;
    AddressU = Wrap;
    AddressV = Wrap;
};

sampler2D WaveNoiseTableSampler = sampler_state
{
    Texture = <FilterAuxiliaryTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D SpatterPointSampler = sampler_state
{
    Texture = <FilterAuxiliaryTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

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


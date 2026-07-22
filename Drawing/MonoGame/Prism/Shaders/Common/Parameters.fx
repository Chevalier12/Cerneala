Texture2D SpriteTexture;
Texture2D SecondaryTexture;
Texture2D StyleTexture;
Texture2D StyleMaskTexture;
Texture2D FilterAuxiliaryTexture;
float Opacity;
float2 PixelSize;
float2 UvScale;
float2 UvOffset;
float4 BlendChannels;
float KnockoutMode;
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

sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
};

sampler2D SecondaryTextureSampler = sampler_state
{
    Texture = <SecondaryTexture>;
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


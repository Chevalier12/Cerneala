struct VertexShaderOutput
{
    float4 Position : SV_Position;
    float4 Color : TEXCOORD0;
    float2 TextureCoordinates : TEXCOORD1;
};

cbuffer PrismFragmentUniforms : register(b0, space3)
{
    float Opacity;
    float2 PixelSize;
    float Padding0;
    float2 UvScale;
    float2 UvOffset;
};

Texture2D SpriteTexture : register(t0, space2);
SamplerState SpriteTextureSampler : register(s0, space2);

float2 ResolveUv(VertexShaderOutput input)
{
    float2 uv = (input.TextureCoordinates * UvScale) + UvOffset;
    return clamp(uv, PixelSize * 0.5, 1.0 - (PixelSize * 0.5));
}

float4 SampleSource(VertexShaderOutput input)
{
    return SpriteTexture.Sample(SpriteTextureSampler, ResolveUv(input));
}

#include "../../../Drawing/Prism/Shaders/Hlsl/Composition/Copy.hlsl"

float4 main(VertexShaderOutput input) : SV_Target0
{
    return CopyCompositePixelShader(input);
}

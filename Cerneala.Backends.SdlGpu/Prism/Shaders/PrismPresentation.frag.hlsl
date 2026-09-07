// Convert the retained working surface during its final blended draw. The
// conversion is shared with the catalog; no intermediate presentation target.
struct VertexShaderOutput
{
    float4 Position : SV_Position;
    float2 TextureCoordinate : TEXCOORD0;
    float4 Color : TEXCOORD1;
};

Texture2D DrawingTexture : register(t0, space2);
SamplerState DrawingSampler : register(s0, space2);
cbuffer PresentationUniforms : register(b0, space3)
{
    float4 Presentation;
};
static const float Opacity = 1.0;

float4 SampleSource(VertexShaderOutput input)
{
    return DrawingTexture.Sample(DrawingSampler, input.TextureCoordinate);
}

#include "../../../Drawing/Prism/Shaders/Hlsl/Color/Common.hlsl"
#include "../../../Drawing/Prism/Shaders/Hlsl/Color/LinearSrgb.hlsl"
#include "../../../Drawing/Prism/Shaders/Hlsl/Color/Srgb.hlsl"
#include "../../../Drawing/Prism/Shaders/Hlsl/Color/LinearDisplayP3.hlsl"
#include "../../../Drawing/Prism/Shaders/Hlsl/Color/DisplayP3.hlsl"
#include "../../../Drawing/Prism/Shaders/Hlsl/Color/ScRgb.hlsl"

float4 main(VertexShaderOutput input) : SV_Target0
{
    switch ((int)Presentation.x)
    {
        case 78: return SrgbToOutputPixelShader(input);
        case 79: return LinearDisplayP3ToOutputPixelShader(input);
        case 80: return DisplayP3ToOutputPixelShader(input);
        case 81: return ScRgbToOutputPixelShader(input);
        default: return LinearSrgbToOutputPixelShader(input);
    }
}

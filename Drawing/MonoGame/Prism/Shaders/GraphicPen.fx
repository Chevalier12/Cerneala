#define CERNEALA_MONOGAME_SPECIALIZED
#include "Common/Parameters.fx"
#include "Common/AllBlends.fx"
#include "Common/AllColor.fx"
#include "../../../Prism/Shaders/Hlsl/Filters/Catalog/Common.hlsl"
#include "../../../Prism/Shaders/Hlsl/Filters/Catalog/Charcoal.hlsl"
#include "../../../Prism/Shaders/Hlsl/Filters/Catalog/GraphicPen.hlsl"

float4 GraphicPenFinalize(
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

float4 GraphicPenFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    float2 uv = ResolveUv(input);
    float4 original = CharcoalOriginal(uv, profile);
    return GraphicPenFinalize(
        input,
        original,
        GraphicPenComposite(uv, original),
        profile);
}

technique GraphicPenFilter
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 GraphicPenFilterPixelShader();
    }
}

#include "Common/Parameters.fx"
#include "Blends/All.fx"
#include "Color/All.fx"
#include "Filters/Catalog/Common.fx"
#include "Filters/Catalog/ConteCrayon.fx"

float4 ConteCrayonFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    float2 uv = ResolveUv(input);
    float4 original = ConteCrayonOriginal(uv, profile);
    float4 filtered = ConteCrayonComposite(uv, original);
    filtered.a = saturate(filtered.a);
    filtered.rgb = clamp(filtered.rgb, 0.0, filtered.a);
    int blendMode = (int)(FilterOptions9.w + 0.5);
    float3 blendedStraight = EvaluateBlendMode(
        blendMode,
        saturate(Unpremultiply(original)),
        saturate(Unpremultiply(filtered)));
    float4 blended = float4(blendedStraight * filtered.a, filtered.a);
    float4 result = lerp(original, blended, saturate(Opacity));
    return LinearSrgbAssociatedToWorking(result, profile) * input.Color;
}

technique ConteCrayonFilter
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 ConteCrayonFilterPixelShader();
    }
}

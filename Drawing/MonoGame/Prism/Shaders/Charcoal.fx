#include "Common/Parameters.fx"
#include "Blends/All.fx"
#include "Color/All.fx"
#include "Filters/Catalog/Common.fx"
#include "Filters/Catalog/Charcoal.fx"

float4 CharcoalFinalize(
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

float4 CharcoalInitialEtfFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    return CharcoalInitialEtf(ResolveUv(input), profile) * input.Color;
}

float4 CharcoalRefineEtfFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    return CharcoalRefineEtf(ResolveUv(input)) * input.Color;
}

float4 CharcoalNormalDogFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    return CharcoalNormalDog(ResolveUv(input), profile) * input.Color;
}

float4 CharcoalFlowDogFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    return CharcoalFlowDog(ResolveUv(input)) * input.Color;
}

float4 CharcoalFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    float2 uv = ResolveUv(input);
    float4 original = CharcoalOriginal(uv, profile);
    return CharcoalFinalize(
        input,
        original,
        CharcoalComposite(uv, original),
        profile);
}

technique CharcoalInitialEtfFilter
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 CharcoalInitialEtfFilterPixelShader();
    }
}

technique CharcoalRefineEtfFilter
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 CharcoalRefineEtfFilterPixelShader();
    }
}

technique CharcoalNormalDogFilter
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 CharcoalNormalDogFilterPixelShader();
    }
}

technique CharcoalFlowDogFilter
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 CharcoalFlowDogFilterPixelShader();
    }
}

technique CharcoalFilter
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 CharcoalFilterPixelShader();
    }
}

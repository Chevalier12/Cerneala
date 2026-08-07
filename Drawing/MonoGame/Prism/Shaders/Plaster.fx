#include "Common/Parameters.fx"
#include "Blends/All.fx"
#include "Color/All.fx"
#include "Filters/Catalog/Common.fx"
#include "Filters/Catalog/Plaster.fx"

float4 PlasterFinalize(
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

float4 PlasterFilterPixelShader(
    VertexShaderOutput input) : COLOR0
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
    return PlasterFinalize(
        input,
        original,
        PlasterComposite(uv, original),
        profile);
}

technique PlasterFilter
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 PlasterFilterPixelShader();
    }
}

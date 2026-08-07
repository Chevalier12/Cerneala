#define PRISM_DEINTERLACE_EFFECT

#include "Common/Parameters.fx"
#include "Blends/All.fx"
#include "Color/All.fx"
#include "Filters/Catalog/Common.fx"
#include "Filters/Catalog/Video.fx"

float4 DeinterlaceFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    float4 source = WorkingAssociatedToLinearSrgb(
        SampleSource(input),
        profile);
    float4 filtered = CatalogDeinterlace(
        ResolveUv(input),
        source,
        profile);
    filtered.a = saturate(filtered.a);
    filtered.rgb = clamp(
        filtered.rgb,
        0.0,
        filtered.a);
    int blendMode =
        (int)(FilterOptions9.w + 0.5);
    float3 blendedStraight = EvaluateBlendMode(
        blendMode,
        saturate(Unpremultiply(source)),
        saturate(Unpremultiply(filtered)));
    float4 blended = float4(
        blendedStraight * filtered.a,
        filtered.a);
    float4 result = lerp(
        source,
        blended,
        saturate(Opacity));
    return LinearSrgbAssociatedToWorking(
        result,
        profile) * input.Color;
}

technique DeinterlaceFilter
{
    pass Pass0
    {
        PixelShader = compile ps_4_0 DeinterlaceFilterPixelShader();
    }
}

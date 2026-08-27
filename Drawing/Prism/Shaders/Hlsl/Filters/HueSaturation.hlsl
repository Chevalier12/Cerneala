float3 ApplyHueSaturation(float3 color, VertexShaderOutput input)
{
    float3 hsl = AdjustmentLinearSrgbToOkhsl(color);
    float weight = AdjustmentHueWeight(
        hsl.x,
        (int)(FilterOptions0.x + 0.5));
    if (FilterOptions1.x > 0.5)
    {
        float targetHue = frac((FilterOptions0.y / 360.0) + 1.0);
        hsl.x = frac(
            hsl.x +
            (AdjustmentShortestHueDelta(hsl.x, targetHue) * weight) +
            1.0);
        hsl.y = saturate(
            hsl.y +
            ((0.5 + (FilterOptions0.z * 0.5) - hsl.y) * weight));
        hsl.z = saturate(hsl.z + (FilterOptions0.w * weight));
    }
    else
    {
        hsl.x = frac(
            hsl.x +
            ((FilterOptions0.y / 360.0) * weight) +
            1.0);
        hsl.y = saturate(
            hsl.y * (1.0 + (FilterOptions0.z * weight)));
        hsl.z = saturate(hsl.z + (FilterOptions0.w * weight));
    }
    return AdjustmentOkhslToLinearSrgb(hsl);
}

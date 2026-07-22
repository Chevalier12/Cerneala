float3 CatalogRotateHue(float3 color, float degrees)
{
    float angle = degrees * 0.01745329252;
    float3 axis = normalize(float3(1.0, 1.0, 1.0));
    return
        (color * cos(angle)) +
        (cross(axis, color) * sin(angle)) +
        (axis *
            dot(axis, color) *
            (1.0 - cos(angle)));
}

float4 CatalogColor(
    float4 source,
    int filterId)
{
    float3 straight =
        saturate(Unpremultiply(source));
    if (filterId == 119)
    {
        float3 inverted = 1.0 - straight;
        straight = lerp(
            straight,
            inverted,
            step(FilterOptions0.x, straight));
    }
    else if (filterId == 132)
    {
        straight += FilterOptions0.x;
        straight =
            (straight - 0.5) *
            FilterOptions2.x +
            0.5;
        straight *= exp2(FilterOptions3.x);
        float luminance = dot(
            straight,
            float3(0.2126, 0.7152, 0.0722));
        straight = lerp(
            luminance,
            straight,
            FilterOptions6.x);
        straight = CatalogRotateHue(
            straight,
            FilterOptions4.x);
        straight += float3(
            FilterOptions7.x,
            0.0,
            -FilterOptions7.x);
        straight = lerp(
            straight,
            FilterOptions8.rgb,
            FilterOptions8.a);
    }
    else
    {
        float signature = frac(
            dot(
                FilterOptions1.xy,
                float2(
                    0.00006103515625,
                    0.00390625)));
        straight += float3(
            signature - 0.5,
            0.5 - signature,
            (signature - 0.5) * 0.5) * 0.1;
    }
    return float4(saturate(straight) * source.a, source.a);
}


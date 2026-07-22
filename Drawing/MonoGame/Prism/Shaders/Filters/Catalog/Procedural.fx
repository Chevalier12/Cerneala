float4 CatalogProcedural(
    float2 uv,
    float4 source,
    int filterId,
    int profile)
{
    float2 pixel = uv / PixelSize;
    float packedPass = floor(FilterOptions9.z / 4.0);
    float seed = CatalogSeed() +
        (packedPass * 4099.0);
    float noise = NeighborhoodHash(pixel, seed);
    if (filterId == 70 || filterId == 71)
    {
        float4 background = FilterOptions0;
        float4 foreground = FilterOptions1;
        float3 pattern = lerp(
            background.rgb,
            foreground.rgb,
            noise);
        if (filterId == 71)
        {
            pattern = abs(
                saturate(Unpremultiply(source)) -
                pattern);
        }
        return float4(pattern * source.a, source.a);
    }
    if (filterId == 72)
    {
        float fibers = saturate(
            0.5 +
            ((NeighborhoodHash(
                    float2(
                        pixel.x * 0.25,
                        pixel.y),
                    seed) -
                0.5) *
                max(FilterOptions3.x, 1.0)));
        return float4(
            lerp(
                FilterOptions0.rgb,
                FilterOptions1.rgb,
                fibers) * source.a,
            source.a);
    }
    if (filterId == 73)
    {
        float2 center = FilterOptions1.xy;
        float flare = pow(
            saturate(1.0 - (distance(uv, center) * 3.0)),
            2.0) *
            max(FilterOptions0.x, 0.0);
        float3 straight = saturate(
            Unpremultiply(source) +
            float3(
                flare,
                flare * 0.75,
                flare * 0.35));
        return float4(straight * source.a, source.a);
    }
    if (filterId == 74)
    {
        float4 light = tex2D(
            SecondaryTextureSampler,
            uv);
        float4 textureSample =
            FilterHeader.w >= 2.0
                ? tex2D(
                    FilterAuxiliaryTextureSampler,
                    uv)
                : 1.0;
        float intensity = saturate(
            FilterOptions0.x +
            dot(light.rgb, float3(0.2126, 0.7152, 0.0722)) *
            (0.5 +
                (0.5 *
                    dot(
                        textureSample.rgb,
                        float3(0.2126, 0.7152, 0.0722)))));
        return float4(
            saturate(Unpremultiply(source) * intensity) *
                source.a,
            source.a);
    }

    float angle = noise * 6.28318530718;
    float2 offset = float2(cos(angle), sin(angle)) *
        FilterOptions9.xy *
        PixelSize;
    return CatalogLinearSample(uv + offset, profile);
}


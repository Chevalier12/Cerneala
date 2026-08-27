


#ifndef CERNEALA_SDL_GPU
sampler2D WatercolorOriginalSampler = sampler_state
{
    Texture = <FilterAuxiliaryTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};
#endif

float4 WatercolorOriginal(float2 uv, int profile)
{
    return WorkingAssociatedToLinearSrgb(
        tex2D(
            WatercolorOriginalSampler,
            clamp(
                uv,
                PixelSize * 0.5,
                1.0 - (PixelSize * 0.5))),
        profile);
}

float4 WatercolorMeanShift(float2 uv, int profile)
{
    float4 center = CatalogLinearSample(uv, profile);
    if (center.a <= 0.000001)
    {
        return 0.0;
    }

    float detail = saturate(FilterOptions0.x / 16.0);
    float rangeSigma = 0.30 - (0.24 * detail);
    float rangeDivisor = 2.0 * rangeSigma * rangeSigma;
    float radius = max(
        max(FilterOptions9.x, FilterOptions9.y),
        1.0);
    float3 centerColor = saturate(Unpremultiply(center));
    float3 colorTotal = 0.0;
    float weightTotal = 0.0;
    [loop]
    for (int offsetY = -3; offsetY <= 3; offsetY++)
    {
        [loop]
        for (int offsetX = -3; offsetX <= 3; offsetX++)
        {
            float2 offset =
                float2(offsetX, offsetY) *
                radius * PixelSize / 3.0;
            float4 sample = CatalogLinearSample(
                uv + offset,
                profile);
            float3 sampleColor = saturate(
                Unpremultiply(sample));
            float3 difference = sampleColor - centerColor;
            float spatialDistance =
                (offsetX * offsetX) +
                (offsetY * offsetY);
            float weight =
                exp(-spatialDistance / 6.0) *
                exp(-dot(difference, difference) / rangeDivisor) *
                exp(-abs(sample.a - center.a) * 8.0) *
                step(0.000001, sample.a);
            colorTotal += sampleColor * weight;
            weightTotal += weight;
        }
    }

    float3 shifted = weightTotal <= 0.000001
        ? centerColor
        : colorTotal / weightTotal;
    return float4(saturate(shifted) * center.a, center.a);
}

float4 WatercolorMorphology(
    float2 uv,
    int profile,
    bool erode)
{
    float4 center = CatalogLinearSample(uv, profile);
    if (center.a <= 0.000001)
    {
        return 0.0;
    }

    float radius = max(
        max(FilterOptions9.x, FilterOptions9.y),
        1.0);
    float3 selected = erode ? 1.0 : 0.0;
    float found = 0.0;
    [loop]
    for (int offsetY = -2; offsetY <= 2; offsetY++)
    {
        [loop]
        for (int offsetX = -2; offsetX <= 2; offsetX++)
        {
            if ((offsetX * offsetX) +
                    (offsetY * offsetY) <=
                4)
            {
                float2 offset =
                    float2(offsetX, offsetY) *
                    radius * PixelSize / 2.0;
                float4 sample = CatalogLinearSample(
                    uv + offset,
                    profile);
                if (sample.a > 0.000001 &&
                    abs(sample.a - center.a) <= 0.25)
                {
                    float3 color = saturate(
                        Unpremultiply(sample));
                    selected = erode
                        ? min(selected, color)
                        : max(selected, color);
                    found = 1.0;
                }
            }
        }
    }

    selected = found > 0.0
        ? selected
        : saturate(Unpremultiply(center));
    return float4(selected * center.a, center.a);
}

float WatercolorValueNoise(float2 coordinate, uint seed)
{
    int2 cell = (int2)floor(coordinate);
    float2 blend = frac(coordinate);
    blend = blend * blend * (3.0 - (2.0 * blend));
    float top = lerp(
        DryBrushHash(cell, seed),
        DryBrushHash(cell + int2(1, 0), seed),
        blend.x);
    float bottom = lerp(
        DryBrushHash(cell + int2(0, 1), seed),
        DryBrushHash(cell + int2(1, 1), seed),
        blend.x);
    return lerp(top, bottom, blend.y);
}

float WatercolorPaperHeight(float2 pixel)
{
    float fine = WatercolorValueNoise(
        pixel / 2.5,
        0x51ed270bu);
    float coarse = WatercolorValueNoise(
        pixel / 13.0,
        0x8321ca5du);
    float fiberX = 0.5 +
        (0.5 * cos((pixel.x + (coarse * 2.0)) * 2.1));
    float fiberY = 0.5 +
        (0.5 * cos((pixel.y - (fine * 2.0)) * 2.35));
    return saturate(
        (0.46 * fine) +
        (0.28 * coarse) +
        (0.13 * fiberX) +
        (0.13 * fiberY));
}

float3 WatercolorPigmentDensity(
    float3 color,
    float density)
{
    return saturate(
        color -
        ((color - (color * color)) * (density - 1.0)));
}

float3 WatercolorStraightSample(float2 uv, int profile)
{
    return saturate(Unpremultiply(
        CatalogLinearSample(uv, profile)));
}

float4 WatercolorComposite(float2 uv, int profile)
{
    float4 original = WatercolorOriginal(uv, profile);
    if (original.a <= 0.000001)
    {
        return 0.0;
    }

    float textureStrength = saturate(FilterOptions2.x / 10.0);
    float2 pixel = uv / PixelSize;
    float paper = WatercolorPaperHeight(pixel);
    float paperHorizontal =
        WatercolorPaperHeight(pixel + float2(1.0, 0.0)) -
        WatercolorPaperHeight(pixel - float2(1.0, 0.0));
    float paperVertical =
        WatercolorPaperHeight(pixel + float2(0.0, 1.0)) -
        WatercolorPaperHeight(pixel - float2(0.0, 1.0));
    float2 warpedUv = uv +
        (float2(paperHorizontal, paperVertical) *
            PixelSize * textureStrength * 0.45);
    float3 color = WatercolorStraightSample(warpedUv, profile);

    float radius = max(
        max(FilterOptions9.x, FilterOptions9.y),
        1.0);
    float2 horizontal = float2(PixelSize.x * radius, 0.0);
    float2 vertical = float2(0.0, PixelSize.y * radius);
    float3 edgeDelta =
        abs(
            WatercolorStraightSample(
                warpedUv - horizontal,
                profile) -
            WatercolorStraightSample(
                warpedUv + horizontal,
                profile)) +
        abs(
            WatercolorStraightSample(
                warpedUv - vertical,
                profile) -
            WatercolorStraightSample(
                warpedUv + vertical,
                profile));
    float edge = saturate(dot(edgeDelta, 1.0 / 6.0));
    float shadow = clamp(FilterOptions1.x, 0.0, 4.0);
    color = WatercolorPigmentDensity(
        color,
        1.0 + (edge * shadow * 0.85));

    if (textureStrength > 0.0)
    {
        float turbulence = WatercolorValueNoise(
            pixel / 32.0,
            0x9a4e21d3u);
        float dispersion =
            (0.65 * WatercolorValueNoise(
                pixel / 4.0,
                0x68bc21ebu)) +
            (0.35 * WatercolorValueNoise(
                pixel / 1.75,
                0x2e5be93du));
        color = WatercolorPigmentDensity(
            color,
            1.0 +
                ((turbulence - 0.5) *
                    0.28 * textureStrength));
        color = WatercolorPigmentDensity(
            color,
            1.0 +
                ((dispersion - 0.5) *
                    0.20 * textureStrength));
        color = WatercolorPigmentDensity(
            color,
            1.0 +
                ((paper - 0.5) *
                    0.34 * textureStrength));

        float dryThreshold = 0.72 -
            (0.12 * textureStrength);
        float dryGap =
            smoothstep(
                dryThreshold,
                dryThreshold + 0.14,
                paper) *
            textureStrength *
            (0.25 +
                (0.4 * dot(
                    color,
                    float3(0.2126, 0.7152, 0.0722))));
        color = lerp(color, 1.0, dryGap);
    }

    return float4(saturate(color) * original.a, original.a);
}

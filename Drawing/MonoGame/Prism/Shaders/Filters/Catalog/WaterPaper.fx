


sampler2D WaterPaperOriginalSampler = sampler_state
{
    Texture = <FilterAuxiliaryTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

float4 WaterPaperOriginal(float2 uv, int profile)
{
    return WorkingAssociatedToLinearSrgb(
        tex2D(
            WaterPaperOriginalSampler,
            clamp(uv, PixelSize * 0.5, 1.0 - (PixelSize * 0.5))),
        profile);
}

uint WaterPaperSeed()
{
    uint low = (uint)FilterOptions3.x;
    uint high = (uint)FilterOptions3.y;
    return (low & 0xffffu) | (high << 16);
}

float WaterPaperHash(int2 cell, uint seed)
{
    uint value =
        ((uint)cell.x * 0x9e3779b9u) ^
        ((uint)cell.y * 0x85ebca6bu) ^
        (seed * 0xc2b2ae35u);
    value ^= value >> 16;
    value *= 0x7feb352du;
    value ^= value >> 15;
    value *= 0x846ca68bu;
    value ^= value >> 16;
    return (value & 0x00ffffffu) / 16777215.0;
}

float WaterPaperValueNoise(float2 position, uint seed)
{
    int2 cell = (int2)floor(position);
    float2 blend = frac(position);
    blend = blend * blend * (3.0 - (2.0 * blend));
    float top = lerp(
        WaterPaperHash(cell, seed),
        WaterPaperHash(cell + int2(1, 0), seed),
        blend.x);
    float bottom = lerp(
        WaterPaperHash(cell + int2(0, 1), seed),
        WaterPaperHash(cell + int2(1, 1), seed),
        blend.x);
    return lerp(top, bottom, blend.y);
}

float WaterPaperSubstrate(float2 pixel)
{
    float fiberLength = clamp(FilterOptions2.x, 1.0, 96.0);
    uint seed = WaterPaperSeed();
    float angle = WaterPaperHash(int2(0, 0), seed + 0x51ed270bu) *
        3.14159265359;
    float cosine = cos(angle);
    float sine = sin(angle);
    float along = (cosine * pixel.x) + (sine * pixel.y);
    float across = (-sine * pixel.x) + (cosine * pixel.y);
    float width = max(1.0, fiberLength * 0.16);
    float warp = WaterPaperValueNoise(
            float2(
                along / (fiberLength * 1.8),
                across / max(width * 2.5, 1.0)),
            seed + 0x8321ca5du) -
        0.5;
    float primary = WaterPaperValueNoise(
        float2(
            (along + (warp * fiberLength * 0.75)) / fiberLength,
            across / width),
        seed + 0x68bc21ebu);
    float secondary = WaterPaperValueNoise(
        float2(
            ((along * 0.55) - (across * 0.18)) /
                max(fiberLength * 0.55, 1.0),
            ((across * 0.45) + (along * 0.04)) /
                max(width * 0.7, 1.0)),
        seed + 0x2e5be93du);
    float fine = WaterPaperValueNoise(
        pixel / 2.2,
        seed + 0x9a4e21d3u);
    float striation = 0.5 +
        (0.5 * cos(
            ((across / width) + warp) * 6.28318530718 +
            (primary * 1.5)));
    return saturate(
        (0.42 * primary) +
        (0.24 * secondary) +
        (0.2 * striation) +
        (0.14 * fine));
}

float4 WaterPaperPreparePigment(float2 uv, int profile)
{
    float4 center = CatalogLinearSample(uv, profile);
    if (center.a <= 0.000001)
    {
        return 0.0;
    }

    float radius = clamp(
        sqrt(clamp(FilterOptions2.x, 1.0, 96.0)) * 0.75,
        1.0,
        6.0);
    float3 centerColor = saturate(Unpremultiply(center));
    float3 colorTotal = 0.0;
    float weightTotal = 0.0;
    [loop]
    for (int offsetY = -2; offsetY <= 2; offsetY++)
    {
        [loop]
        for (int offsetX = -2; offsetX <= 2; offsetX++)
        {
            float2 offset = float2(offsetX, offsetY) *
                radius * PixelSize / 2.0;
            float4 sample = CatalogLinearSample(uv + offset, profile);
            if (sample.a <= 0.000001)
            {
                continue;
            }

            float3 sampleColor = saturate(Unpremultiply(sample));
            float3 difference = sampleColor - centerColor;
            float spatialDistance =
                (offsetX * offsetX) +
                (offsetY * offsetY);
            float weight =
                exp(-spatialDistance / 5.5) *
                exp(-dot(difference, difference) * 4.0) *
                exp(-abs(sample.a - center.a) * 8.0);
            colorTotal += sampleColor * weight;
            weightTotal += weight;
        }
    }

    float3 bled = weightTotal <= 0.000001
        ? centerColor
        : colorTotal / weightTotal;
    float3 prepared = saturate(lerp(centerColor, bled, 0.72));
    return float4(prepared * center.a, center.a);
}

float3 WaterPaperPigmentDensity(float3 color, float density)
{
    return saturate(
        color - ((color - (color * color)) * (density - 1.0)));
}

float3 WaterPaperStraightSample(float2 uv, int profile)
{
    return saturate(Unpremultiply(CatalogLinearSample(uv, profile)));
}

float4 WaterPaperComposite(float2 uv, int profile)
{
    float4 original = WaterPaperOriginal(uv, profile);
    if (original.a <= 0.000001)
    {
        return 0.0;
    }

    float fiberLength = clamp(FilterOptions2.x, 1.0, 96.0);
    float contrast = saturate(FilterOptions1.x / 100.0);
    float brightnessOffset =
        (clamp(FilterOptions0.x, 0.0, 100.0) - 50.0) / 100.0;
    float contrastGain = lerp(0.65, 1.8, contrast);
    float2 pixel = uv / PixelSize;
    float paper = WaterPaperSubstrate(pixel);
    float horizontal =
        WaterPaperSubstrate(pixel + float2(1.0, 0.0)) -
        WaterPaperSubstrate(pixel - float2(1.0, 0.0));
    float vertical =
        WaterPaperSubstrate(pixel + float2(0.0, 1.0)) -
        WaterPaperSubstrate(pixel - float2(0.0, 1.0));
    float warpStrength = clamp(
        0.35 + (fiberLength * 0.02),
        0.35,
        1.5);
    float2 warpedUv = uv +
        float2(horizontal, vertical) * PixelSize * warpStrength;
    float3 color = WaterPaperStraightSample(warpedUv, profile);

    float edgeStep = clamp(sqrt(fiberLength) * 0.25, 1.0, 3.0);
    float2 horizontalStep = float2(PixelSize.x * edgeStep, 0.0);
    float2 verticalStep = float2(0.0, PixelSize.y * edgeStep);
    float3 edgeDelta =
        abs(
            WaterPaperStraightSample(warpedUv - horizontalStep, profile) -
            WaterPaperStraightSample(warpedUv + horizontalStep, profile)) +
        abs(
            WaterPaperStraightSample(warpedUv - verticalStep, profile) -
            WaterPaperStraightSample(warpedUv + verticalStep, profile));
    float edge = saturate(dot(edgeDelta, 1.0 / 6.0));
    float density = 1.0 +
        ((0.5 - paper) * (0.45 + (contrast * 0.35))) +
        (edge * (0.35 + (contrast * 0.65)));
    color = WaterPaperPigmentDensity(color, density);

    float dryGap = smoothstep(0.68, 0.9, paper) *
        (0.035 + (contrast * 0.12)) *
        (0.35 +
            (dot(color, float3(0.2126, 0.7152, 0.0722)) * 0.65));
    color = lerp(color, 1.0, dryGap);
    color = ((color - 0.5) * contrastGain) +
        0.5 + brightnessOffset;
    return float4(saturate(color) * original.a, original.a);
}

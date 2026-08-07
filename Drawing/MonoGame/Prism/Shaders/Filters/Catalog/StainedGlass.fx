




sampler2D StainedGlassSeedSampler = sampler_state
{
    Texture = <SpriteTexture>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D StainedGlassOriginalSampler = sampler_state
{
    Texture = <FilterAuxiliaryTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

uint StainedGlassHash(int2 cell, uint seed)
{
    uint value =
        (uint(cell.x) * 0x9e3779b9u) ^
        (uint(cell.y) * 0x85ebca6bu) ^
        seed;
    value ^= value >> 16;
    value *= 0x7feb352du;
    value ^= value >> 15;
    value *= 0x846ca68bu;
    value ^= value >> 16;
    return value;
}

float StainedGlassRandom(int2 cell, uint seed)
{
    return float(StainedGlassHash(cell, seed) & 0x00ffffffu) /
        16777215.0;
}

uint StainedGlassSeedValue()
{
    return (uint(FilterOptions4.x) & 0xffffu) |
        ((uint(FilterOptions4.y) & 0xffffu) << 16);
}

bool StainedGlassCellIntersects(int2 cell, float cellSize)
{
    float2 minimum = float2(cell) * cellSize;
    float2 maximum = minimum + cellSize;
    float2 dimensions = 1.0 / PixelSize;
    return minimum.x < dimensions.x &&
        maximum.x > 0.0 &&
        minimum.y < dimensions.y &&
        maximum.y > 0.0;
}

float2 StainedGlassFeature(
    int2 cell,
    float cellSize,
    uint seed)
{
    float2 feature = (float2(cell) + 0.15 +
        (0.7 * float2(
            StainedGlassRandom(cell, seed ^ 0x13579bdfu),
            StainedGlassRandom(cell, seed ^ 0x2468ace0u)))) *
        cellSize;
    return clamp(feature, 0.0, (1.0 / PixelSize) - 1.0);
}

bool StainedGlassBefore(float2 candidate, float2 current)
{
    return current.x < 0.0 ||
        candidate.y < current.y ||
        (candidate.y == current.y && candidate.x < current.x);
}

float4 StainedGlassSeedPass(float2 uv)
{
    float cellSize = clamp(FilterOptions2.x, 2.0, 16384.0);
    uint seed = StainedGlassSeedValue();
    float2 pixel = (uv / PixelSize) - 0.5;
    int2 pixelIndex = (int2)floor(pixel + 0.5);
    int2 baseCell = (int2)floor(pixel / cellSize);
    float2 best = -1.0;

    [unroll]
    for (int offsetY = -1; offsetY <= 1; offsetY++)
    {
        [unroll]
        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            int2 cell = baseCell + int2(offsetX, offsetY);
            if (!StainedGlassCellIntersects(cell, cellSize))
            {
                continue;
            }

            float2 feature = StainedGlassFeature(
                cell,
                cellSize,
                seed);
            int2 seedPixel = (int2)floor(feature + 0.5);
            if (all(seedPixel == pixelIndex) &&
                StainedGlassBefore(feature, best))
            {
                best = feature;
            }
        }
    }

    return best.x < 0.0
        ? float4(-1.0, -1.0, 0.0, 0.0)
        : float4((best + 0.5) * PixelSize, 1.0, 1.0);
}

float4 StainedGlassFloodPass(float2 uv)
{
    float jump = max(FilterOptions9.x, 1.0);
    float2 pixel = (uv / PixelSize) - 0.5;
    float2 dimensions = 1.0 / PixelSize;
    float4 best = float4(-1.0, -1.0, 0.0, 0.0);
    float bestDistance = 3.402823466e+38;

    [unroll]
    for (int offsetY = -1; offsetY <= 1; offsetY++)
    {
        [unroll]
        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            float2 samplePixel = pixel +
                (float2(offsetX, offsetY) * jump);
            if (any(samplePixel < 0.0) ||
                any(samplePixel >= dimensions))
            {
                continue;
            }

            float2 sampleUv = (samplePixel + 0.5) * PixelSize;
            float4 candidate = tex2D(
                StainedGlassSeedSampler,
                sampleUv);
            if (candidate.z < 0.5)
            {
                continue;
            }
            float2 candidatePixel =
                (candidate.xy / PixelSize) - 0.5;
            float2 delta = pixel - candidatePixel;
            float distanceSquared = dot(delta, delta);
            bool tied = abs(distanceSquared - bestDistance) <= 0.0001;
            if (distanceSquared < bestDistance ||
                (tied && StainedGlassBefore(candidatePixel,
                    (best.xy / PixelSize) - 0.5)))
            {
                bestDistance = distanceSquared;
                best = candidate;
            }
        }
    }
    return best;
}

float StainedGlassDifferentSeed(
    float2 uv,
    float2 seedUv,
    float2 offset)
{
    float2 sampleUv = clamp(
        uv + (offset * PixelSize),
        PixelSize * 0.5,
        1.0 - (PixelSize * 0.5));
    float4 other = tex2D(StainedGlassSeedSampler, sampleUv);
    if (other.z < 0.5)
    {
        return 0.0;
    }
    float2 difference = (other.xy - seedUv) / PixelSize;
    return dot(difference, difference) > 0.0625
        ? 1.0
        : 0.0;
}

float4 StainedGlassComposite(
    float2 uv,
    int profile,
    float4 original)
{
    float4 label = tex2D(StainedGlassSeedSampler, uv);
    if (label.z < 0.5 || original.a <= 0.000001)
    {
        return original;
    }

    float4 sampled = WorkingAssociatedToLinearSrgb(
        tex2D(StainedGlassOriginalSampler, label.xy),
        profile);
    float3 straight = sampled.a <= 0.000001
        ? saturate(Unpremultiply(original))
        : saturate(Unpremultiply(sampled));
    float cellSize = clamp(FilterOptions2.x, 2.0, 16384.0);
    float lightIntensity = clamp(FilterOptions3.x, 0.0, 10.0);
    float2 pixel = (uv / PixelSize) - 0.5;
    float2 seedPixel = (label.xy / PixelSize) - 0.5;
    float2 local = (pixel - seedPixel) / cellSize;
    float facet = dot(
        local,
        normalize(float2(-0.65, -0.75)));
    float shade = max(
        0.0,
        1.0 + (facet * (lightIntensity / 10.0) * 1.5));
    straight = saturate(straight * shade);

    float thickness = clamp(FilterOptions1.x, 0.0, 1024.0);
    float edge = 0.0;
    if (thickness > 0.0)
    {
        float radius = max(thickness, 1.0);
        const float diagonal = 0.70710678118;
        edge = max(edge, StainedGlassDifferentSeed(
            uv, label.xy, float2(-radius, 0.0)));
        edge = max(edge, StainedGlassDifferentSeed(
            uv, label.xy, float2(radius, 0.0)));
        edge = max(edge, StainedGlassDifferentSeed(
            uv, label.xy, float2(0.0, -radius)));
        edge = max(edge, StainedGlassDifferentSeed(
            uv, label.xy, float2(0.0, radius)));
        float diagonalRadius = radius * diagonal;
        edge = max(edge, StainedGlassDifferentSeed(
            uv, label.xy, float2(-diagonalRadius, -diagonalRadius)));
        edge = max(edge, StainedGlassDifferentSeed(
            uv, label.xy, float2(diagonalRadius, -diagonalRadius)));
        edge = max(edge, StainedGlassDifferentSeed(
            uv, label.xy, float2(-diagonalRadius, diagonalRadius)));
        edge = max(edge, StainedGlassDifferentSeed(
            uv, label.xy, float2(diagonalRadius, diagonalRadius)));
    }

    float4 border = FilterOptions0;
    float3 borderStraight = saturate(Unpremultiply(border));
    straight = lerp(
        straight,
        borderStraight,
        edge * saturate(border.a));
    return float4(saturate(straight) * original.a, original.a);
}

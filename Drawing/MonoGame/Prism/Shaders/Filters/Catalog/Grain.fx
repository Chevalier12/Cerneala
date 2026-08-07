



uint GrainHash(int2 cell, uint seed)
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

float GrainRandom(int2 cell, uint seed)
{
    return float(GrainHash(cell, seed) & 0x00ffffffu) /
        16777215.0;
}

float GrainBooleanCoverage(
    float2 pixel,
    float cellSize,
    float2 radius,
    float softness,
    float probability,
    int type,
    uint seed)
{
    int2 baseCell = (int2)floor(pixel / cellSize);
    float uncovered = 1.0;
    [unroll]
    for (int sample = 0; sample < 18; sample++)
    {
        int candidate = sample % 2;
        int cellIndex = sample / 2;
        int2 cell = baseCell + int2(
            (cellIndex % 3) - 1,
            (cellIndex / 3) - 1);
        uint candidateSeed =
            seed ^ (uint(candidate) * 0x9e3779b9u);
        float accepted = GrainRandom(
            cell,
            candidateSeed ^ 0xa511e9b3u) < probability;
        float2 center = (float2(cell) + float2(
            GrainRandom(cell, candidateSeed ^ 0x63d83595u),
            GrainRandom(cell, candidateSeed ^ 0xb5297a4du))) *
            cellSize;
        float radiusRandom = GrainRandom(
            cell,
            candidateSeed ^ 0x1b56c4e9u);
        float radiusScale = type == 3
            ? 0.45 + (1.35 * radiusRandom * radiusRandom)
            : 0.65 + (0.7 * radiusRandom);
        float2 delta = (pixel - center) / (radius * radiusScale);
        float distance = length(delta);
        float coverage = accepted *
            (1.0 - smoothstep(
                1.0 - softness,
                1.0 + softness,
                distance));
        uncovered *= 1.0 - coverage;
    }

    return 1.0 - uncovered;
}

float4 CatalogGrain(float2 pixel, float4 source)
{
    const float minimumAlpha = 0.000001;
    const float meanRadiusScaleSquared = 1.0408333333;
    if (source.a <= minimumAlpha)
    {
        return 0.0;
    }

    float intensity = saturate(FilterOptions4.x);
    if (intensity <= 0.0)
    {
        return source;
    }

    float contrast = saturate(FilterOptions4.y);
    int type = (int)round(FilterOptions4.z);
    float cellSize = clamp(FilterOptions4.w, 1.0, 256.0);
    float2 radius = clamp(FilterOptions5.xy, 0.1, 96.0);
    float softness = clamp(FilterOptions5.z, 0.01, 0.49);
    float typeGain = clamp(FilterOptions5.w, 0.25, 2.0);
    uint seed =
        (uint(FilterOptions2.x) & 0xffffu) |
        ((uint(FilterOptions2.y) & 0xffffu) << 16);
    float3 straight = saturate(Unpremultiply(source));
    float luminance = dot(straight, float3(0.2126, 0.7152, 0.0722));
    float targetOccupancy = 0.03 + (0.94 * (1.0 - luminance));
    float areaRatio = clamp(
        3.14159265359 * radius.x * radius.y *
            meanRadiusScaleSquared / (cellSize * cellSize),
        0.0001,
        0.95);
    float probability = saturate(
        -log(max(1.0 - targetOccupancy, 0.0001)) /
        (2.0 * areaRatio));
    float expectedOccupancy =
        1.0 - exp(-2.0 * probability * areaRatio);
    float occupancy = GrainBooleanCoverage(
        pixel,
        cellSize,
        radius,
        softness,
        probability,
        type,
        seed);
    float deviation = expectedOccupancy - occupancy;
    float exponent = lerp(1.55, 0.65, contrast);
    float shaped = sign(deviation) * pow(abs(deviation), exponent);
    float amplitude =
        intensity * lerp(0.18, 0.42, contrast) * typeGain;
    float3 output = saturate(straight + (shaped * amplitude));
    return float4(output * source.a, source.a);
}

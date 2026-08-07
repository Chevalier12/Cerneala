

uint PatchworkHash(int2 cell, uint seed)
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

float PatchworkRandom(int2 cell, uint seed)
{
    return float(PatchworkHash(cell, seed) & 0x00ffffffu) /
        16777215.0;
}

float4 CatalogPatchwork(float2 uv, int profile)
{
    const float minimumAlpha = 0.000001;
    float squareSize = max(FilterOptions2.x, 1.0);
    float relief = saturate(FilterOptions0.x);
    uint seed =
        (uint(FilterOptions1.x) & 0xffffu) |
        ((uint(FilterOptions1.y) & 0xffffu) << 16);
    float2 pixel = uv / PixelSize;
    int2 cell = (int2)floor(pixel / squareSize);
    float2 local = pixel - (float2(cell) * squareSize);
    float2 samplePixel = (float2(cell) + 0.5) * squareSize;
    float4 tile = CatalogLinearSample(
        samplePixel * PixelSize,
        profile);
    if (tile.a <= minimumAlpha || relief <= 0.0)
    {
        return tile;
    }

    float depth = (PatchworkRandom(
        cell,
        seed ^ 0xa511e9b3u) * 2.0) - 1.0;
    float2 normalized = ((local / squareSize) * 2.0) - 1.0;
    float edge = smoothstep(
        0.5,
        1.0,
        max(abs(normalized.x), abs(normalized.y)));
    float directional = -(normalized.x + normalized.y) * 0.5;
    float depthSign = depth >= 0.0 ? 1.0 : -1.0;
    float bevelScale = 0.5 + (abs(depth) * 0.5);
    float shade = relief * (
        (depth * 0.18) +
        (directional * edge * depthSign * bevelScale * 0.32));
    float3 straight = saturate(Unpremultiply(tile) + shade);
    return float4(straight * tile.a, tile.a);
}

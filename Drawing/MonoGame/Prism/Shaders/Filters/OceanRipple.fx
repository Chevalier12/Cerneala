float4 ApplyOceanRipple(VertexShaderOutput input, float4 source, int profile)
{
    float2 uv = ResolveUv(input);
    uint seed = (uint)FilterOptions0.z | ((uint)FilterOptions0.w << 16);
    float size = max(FilterOptions0.x, 1.0);
    float2 position = uv / (size * PixelSize);
    float2 firstOctave = OceanWarpVector(position, seed);
    float2 warpedPosition = (position + (firstOctave * 0.75)) * 2.0;
    float2 secondOctave = OceanWarpVector(
        warpedPosition,
        seed ^ 0x85ebca6bu);
    float2 displacement = (firstOctave + (secondOctave * 0.5)) / 1.5;
    return SampleResamplingSource(
        uv + (displacement * FilterOptions0.y * PixelSize),
        profile,
        0,
        0.0);
}

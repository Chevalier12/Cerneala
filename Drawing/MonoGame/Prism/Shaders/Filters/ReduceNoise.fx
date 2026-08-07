float4 ApplyReduceNoise(VertexShaderOutput input, float4 center, int profile)
{
    float2 uv = NeighborhoodUnclampedUv(input);
    int passKind = (int)(FilterHeader.z + 0.5);
    if (passKind == 1 || passKind == 2)
    {
        return SampleDomainTransform(
            uv,
            center,
            passKind == 1 ? float2(1.0, 0.0) : float2(0.0, 1.0),
            (int)(max(FilterOptions9.x, FilterOptions9.y) + 0.5),
            (int)(FilterOptions9.z + 0.5),
            profile);
    }
    if (passKind == 11 || passKind == 12)
    {
        return SampleJpegDeblock(
            uv,
            input.Position.xy,
            center,
            passKind == 11,
            profile);
    }
    if (passKind == 7)
    {
        return RecombineReduceNoise(uv, center, profile);
    }
    return center;
}

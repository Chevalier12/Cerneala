float4 ApplyUnsharpMask(VertexShaderOutput input, float4 center, int profile)
{
    float2 uv = NeighborhoodUnclampedUv(input);
    int passKind = (int)(FilterHeader.z + 0.5);
    if (passKind != 7)
    {
        return SampleIncrementalGaussian(
            uv,
            FilterOptions9.xy,
            (int)(FilterOptions9.z + 0.5),
            0,
            profile,
            FilterOptions0.w);
    }
    return NeighborhoodUnsharpHighBoost(
        SampleNeighborhoodOriginal(uv, profile),
        center,
        FilterOptions0.x,
        FilterOptions0.z);
}

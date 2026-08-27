float4 ApplyFieldBlur(VertexShaderOutput input, float4 center, int profile)
{
    return SampleFieldBlur(
        NeighborhoodUnclampedUv(input),
        FilterOptions9.xy,
        (int)(FilterOptions9.z + 0.5),
        profile);
}

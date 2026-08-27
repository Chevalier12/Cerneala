float4 ApplyAverage(VertexShaderOutput input, float4 center, int profile)
{
    return SampleNeighborhoodAverage3x3(
        NeighborhoodUnclampedUv(input),
        profile);
}

float4 ApplyHighPass(VertexShaderOutput input, float4 center, int profile)
{
    float4 blurred = SampleNeighborhoodDisk(
        NeighborhoodUnclampedUv(input),
        FilterOptions9.xy,
        (int)(FilterOptions9.z + 0.5),
        NeighborhoodEdgeMode(21),
        profile,
        0.0,
        false);
    return float4(
        saturate(0.5 * center.a + center.rgb - blurred.rgb),
        center.a);
}

float4 ApplyShapeBlur(VertexShaderOutput input, float4 center, int profile)
{
    return SampleShapePsf(
        NeighborhoodUnclampedUv(input),
        FilterOptions0.x,
        (int)(FilterOptions9.z + 0.5),
        NeighborhoodEdgeMode(8),
        profile);
}

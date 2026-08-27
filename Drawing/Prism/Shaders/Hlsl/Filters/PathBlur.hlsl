float4 ApplyPathBlur(VertexShaderOutput input, float4 center, int profile)
{
    return SamplePathRk4(
        NeighborhoodUnclampedUv(input),
        input.Position.xy,
        (int)(FilterOptions9.z + 0.5),
        profile);
}

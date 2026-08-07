float4 ApplySmartSharpen(VertexShaderOutput input, float4 center, int profile)
{
    return ApplyRichardsonLucy(
        NeighborhoodUnclampedUv(input),
        center,
        (int)(FilterOptions9.z + 0.5),
        (int)(FilterHeader.z + 0.5),
        profile);
}

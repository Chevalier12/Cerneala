float4 ApplyDespeckleStatePass(
    VertexShaderOutput input,
    int profile,
    int passKind)
{
    return ApplyDespeckleState(
        NeighborhoodUnclampedUv(input),
        profile,
        passKind,
        (int)(FilterOptions9.z + 0.5));
}

float4 ReadDespeckleSource(VertexShaderOutput input, int profile)
{
    return SampleNeighborhoodOriginal(
        NeighborhoodUnclampedUv(input),
        profile);
}

float4 ReadDespeckleResult(VertexShaderOutput input, int profile)
{
    return DecodeDespeckle(
        NeighborhoodUnclampedUv(input),
        profile);
}

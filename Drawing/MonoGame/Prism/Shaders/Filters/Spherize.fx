float4 ApplySpherize(VertexShaderOutput input, float4 source, int profile)
{
    return SampleResamplingSource(
        MapSpherizeCoordinate(ResolveUv(input)),
        profile,
        0,
        0.0);
}

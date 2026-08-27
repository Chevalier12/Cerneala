float4 ApplyWave(VertexShaderOutput input, float4 source, int profile)
{
    return SampleWaveFeline(
        ResolveUv(input),
        profile,
        (int)(FilterOptions2.x + 0.5),
        0.0);
}

float4 ApplyOffset(VertexShaderOutput input, float4 source, int profile)
{
    return SampleResamplingSource(
        ResolveUv(input) - (FilterOptions0.xy * PixelSize),
        profile,
        (int)(FilterOptions0.z + 0.5),
        FilterOptions1);
}

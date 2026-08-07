float4 ApplyLiquify(VertexShaderOutput input, float4 source, int profile)
{
    float2 uv = ResolveUv(input);
    float2 mapped = MapLiquifyCoordinate(uv);
    int edgeMode = (int)(FilterOptions0.z + 0.5);
    float4 bilinear = SampleResamplingSource(
        mapped,
        profile,
        edgeMode,
        0.0);
    float cubicConfidence = LiquifyCubicConfidence(uv);
    if (cubicConfidence <= 0.001)
    {
        return bilinear;
    }
    float4 bicubic = SampleResamplingCubic(
        mapped,
        profile,
        edgeMode,
        bilinear);
    return ClampResamplingAssociated(
        lerp(bilinear, bicubic, cubicConfidence));
}

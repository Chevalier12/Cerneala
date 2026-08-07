float4 ApplyShear(VertexShaderOutput input, float4 source, int profile)
{
    float2 uv = ResolveUv(input);
    float curve = ShearCurve(
        saturate(uv.y),
        (int)(FilterOptions0.y + 0.5));
    uv.x -= FilterOptions0.x * (curve - 0.5);
    return SampleResamplingSource(
        uv,
        profile,
        (int)(FilterOptions0.z + 0.5),
        0.0);
}

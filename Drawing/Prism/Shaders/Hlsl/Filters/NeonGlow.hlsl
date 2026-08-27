float4 ApplyNeonGlow(VertexShaderOutput input, float4 source, int profile)
{
    float2 uv = ResolveUv(input);
    int passKind = (int)(FilterHeader.z + 0.5);
    if (passKind == 4) return NeonGlowEdge(uv, profile, 0.0);
    if (passKind == 5)
    {
        return NeonGlowGaussian(
            uv,
            float2(1.0, 0.0),
            profile,
            0.0);
    }
    if (passKind == 6)
    {
        return NeonGlowGaussian(
            uv,
            float2(0.0, 1.0),
            profile,
            0.0);
    }
    return NeonGlowPyramidComposite(uv, profile);
}

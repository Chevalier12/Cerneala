float4 ApplyTwirl(VertexShaderOutput input, float4 source, int profile)
{
    return SampleTwirlFeline(ResolveUv(input), profile, 0, 0.0);
}

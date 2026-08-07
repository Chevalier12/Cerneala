float DissolveValue(float2 position)
{
    float2 seedOffset = float2(
        fmod(DissolveSeed, 256.0),
        floor(DissolveSeed / 256.0));
    float2 thresholdUv =
        (floor(position) + seedOffset + 0.5) / 256.0;
    float threshold = tex2D(
        DissolveThresholdSampler,
        thresholdUv).a;
    return threshold * (255.0 / 256.0);
}

float4 DissolveBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 1);
}

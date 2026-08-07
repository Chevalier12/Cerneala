float4 ApplyAddNoise(VertexShaderOutput input, float4 center, int profile)
{
    uint2 noisePosition = (uint2)floor(input.Position.xy);
    uint seedLow = (uint)(FilterOptions0.w + 0.5);
    uint seedHigh = (uint)(FilterOptions1.x + 0.5);
    bool gaussian = FilterOptions0.y > 0.5;
    float redNoise = AddNoiseSample(
        noisePosition,
        seedLow,
        seedHigh,
        0u,
        gaussian);
    float greenNoise = FilterOptions0.z > 0.5
        ? redNoise
        : AddNoiseSample(noisePosition, seedLow, seedHigh, 1u, gaussian);
    float blueNoise = FilterOptions0.z > 0.5
        ? redNoise
        : AddNoiseSample(noisePosition, seedLow, seedHigh, 2u, gaussian);
    float3 straight = saturate(
        Unpremultiply(center) +
        (float3(redNoise, greenNoise, blueNoise) * FilterOptions0.x));
    return float4(straight * center.a, center.a);
}

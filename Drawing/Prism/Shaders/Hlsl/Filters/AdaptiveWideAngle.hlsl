float4 ApplyAdaptiveWideAngle(VertexShaderOutput input, float4 source, int profile)
{
    float2 uv = ResolveUv(input);
    float2 focalLength = FilterOptions0.xy;
    float2 principalPoint = FilterOptions0.zw;
    float2 normalized = (uv - principalPoint) / focalLength;
    float radius = length(normalized);
    if (radius < 0.000001)
    {
        return SampleResamplingSource(uv, profile, 1, 0.0);
    }
    float theta = atan(radius);
    float theta2 = theta * theta;
    float theta4 = theta2 * theta2;
    float theta6 = theta4 * theta2;
    float theta8 = theta4 * theta4;
    float distortedTheta = theta *
        (1.0 +
            (FilterOptions1.x * theta2) +
            (FilterOptions1.y * theta4) +
            (FilterOptions1.z * theta6) +
            (FilterOptions1.w * theta8));
    float2 mapped = principalPoint +
        (normalized * (distortedTheta / radius) * focalLength);
    return SampleResamplingSource(mapped, profile, 1, 0.0);
}

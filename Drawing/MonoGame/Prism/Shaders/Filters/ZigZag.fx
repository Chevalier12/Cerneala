float4 ApplyZigZag(VertexShaderOutput input, float4 source, int profile)
{
    float2 uv = ResolveUv(input);
    float2 sourceSize = 1.0 / max(PixelSize, 0.000001);
    float2 center = FilterOptions1.xy;
    float2 centerPixels = center * sourceSize;
    float2 deltaPixels = (uv - center) * sourceSize;
    float radius = length(deltaPixels);
    if (radius < 0.000001)
    {
        return SampleResamplingSource(uv, profile, 0, 0.0);
    }
    float2 cornerDistance = max(
        abs(centerPixels),
        abs(sourceSize - centerPixels));
    float maximumRadius = max(length(cornerDistance), 0.000001);
    float normalizedRadius = saturate(radius / maximumRadius);
    float ridges = clamp(
        FilterOptions0.y,
        1.0,
        max(maximumRadius, 1.0));
    float strength = clamp(FilterOptions0.x, -1.0, 1.0);
    float envelope = sin(3.14159265 * normalizedRadius);
    float oscillation = cos(3.14159265 * ridges * normalizedRadius);
    float maximumDisplacement =
        maximumRadius * 0.85 /
        (3.14159265 * (ridges + 1.0));
    float displacement =
        strength * maximumDisplacement * envelope * oscillation;
    float2 mappedPixels;
    if (FilterOptions0.z < 0.5)
    {
        mappedPixels = (uv * sourceSize) +
            (normalize(float2(1.0, 1.0)) * displacement);
    }
    else if (FilterOptions0.z < 1.5)
    {
        mappedPixels = centerPixels +
            (deltaPixels * ((radius + displacement) / radius));
    }
    else
    {
        float angle = displacement / radius;
        float cosine = cos(angle);
        float sine = sin(angle);
        mappedPixels = centerPixels + float2(
            (deltaPixels.x * cosine) - (deltaPixels.y * sine),
            (deltaPixels.x * sine) + (deltaPixels.y * cosine));
    }
    return SampleResamplingSource(
        mappedPixels / sourceSize,
        profile,
        0,
        0.0);
}

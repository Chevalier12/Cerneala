float4 InkOutlinesComposite(float2 uv, float4 original)
{
    const float epsilon = 0.02;
    const float minimumWeight = 0.000001;
    if (original.a <= minimumWeight)
    {
        return 0.0;
    }

    float darkIntensity = clamp(FilterOptions0.x, 0.0, 50.0);
    float lightIntensity = clamp(FilterOptions1.x, 0.0, 50.0);
    if (darkIntensity <= 0.0 && lightIntensity <= 0.0)
    {
        return original;
    }

    float4 gaussian = AccentedEdgesRawSample(uv);
    float narrowLuminance = gaussian.y <= minimumWeight
        ? 0.0
        : gaussian.x / gaussian.y;
    float extendedLuminance = gaussian.w <= minimumWeight
        ? 0.0
        : gaussian.z / gaussian.w;
    float sharpen = clamp(darkIntensity, 0.0, 64.0);
    float response =
        ((sharpen + 1.0) * narrowLuminance) -
        (sharpen * extendedLuminance);
    float darkStrength = darkIntensity / 50.0;
    float lightStrength = lightIntensity / 50.0;
    float phi = clamp(8.0 + (80.0 * lightStrength), 8.0, 48.0);
    float thresholded = response >= epsilon
        ? 1.0
        : saturate(1.0 + tanh(phi * (response - epsilon)));
    float3 straight = saturate(Unpremultiply(original));
    float inkMask = 1.0 - thresholded;
    float paperMask = thresholded *
        dot(straight, float3(0.2126, 0.7152, 0.0722));
    float3 outlined = lerp(
        straight,
        float3(0.0, 0.0, 0.0),
        darkStrength * inkMask);
    outlined = lerp(
        outlined,
        float3(1.0, 1.0, 1.0),
        lightStrength * paperMask);
    return float4(outlined * original.a, original.a);
}

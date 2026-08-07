float4 DarkStrokesComposite(float2 uv, float4 original)
{
    const float sharpen = 10.0;
    const float minimumWeight = 0.000001;
    if (original.a <= minimumWeight)
    {
        return 0.0;
    }

    float4 gaussian = AccentedEdgesRawSample(uv);
    float narrowLuminance = gaussian.y <= minimumWeight
        ? 0.0
        : gaussian.x / gaussian.y;
    float extendedLuminance = gaussian.w <= minimumWeight
        ? 0.0
        : gaussian.z / gaussian.w;
    float response =
        ((sharpen + 1.0) * narrowLuminance) -
        (sharpen * extendedLuminance);
    float balance = saturate(FilterOptions0.x / 10.0);
    float epsilon = lerp(-0.04, 0.06, balance);
    float thresholded = response >= epsilon
        ? 1.0
        : saturate(1.0 + tanh(24.0 * (response - epsilon)));
    float blackIntensity = saturate(FilterOptions1.x / 10.0);
    float whiteIntensity = saturate(FilterOptions2.x / 10.0);
    float3 straight = saturate(Unpremultiply(original));
    float luminance = dot(straight, float3(0.2126, 0.7152, 0.0722));
    float darkMask = saturate(
        (1.0 - thresholded) + ((1.0 - luminance) * 0.35));
    float lightMask = thresholded * luminance;
    float3 toned = lerp(
        straight,
        float3(0.0, 0.0, 0.0),
        blackIntensity * darkMask);
    toned = lerp(
        toned,
        float3(1.0, 1.0, 1.0),
        whiteIntensity * lightMask);
    return float4(toned * original.a, original.a);
}

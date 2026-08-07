float4 StampComposite(float2 uv, float4 original)
{
    const float sharpen = 35.0;
    const float phi = 10.0;
    const float minimumWeight = 0.000001;
    if (original.a <= minimumWeight)
    {
        return 0.0;
    }

    float4 gaussian = PhotocopyRawSample(uv);
    float narrowLuminance = gaussian.y <= minimumWeight
        ? 0.0
        : gaussian.x / gaussian.y;
    float extendedLuminance = gaussian.w <= minimumWeight
        ? 0.0
        : gaussian.z / gaussian.w;
    float response =
        ((sharpen + 1.0) * narrowLuminance) -
        (sharpen * extendedLuminance);
    float epsilon = saturate(FilterOptions4.w);
    float paper = response >= epsilon
        ? 1.0
        : saturate(1.0 + tanh(phi * (response - epsilon)));
    float3 color = lerp(FilterOptions5.rgb, FilterOptions6.rgb, paper);
    return float4(saturate(color) * original.a, original.a);
}

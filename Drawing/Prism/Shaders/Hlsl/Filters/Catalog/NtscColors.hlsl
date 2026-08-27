float4 CatalogNtscColors(float4 source)
{
    const float pedestal = 0.075;
    const float activeVideoRange = 1.0 - pedestal;
    const float maximumChrominance =
        0.5 / activeVideoRange;
    const float maximumComposite =
        (1.1 - pedestal) / activeVideoRange;
    float3 encoded = pow(
        saturate(Unpremultiply(source)),
        1.0 / 2.2);
    float luminance = dot(
        encoded,
        float3(0.2989, 0.5866, 0.1144));
    float inPhase = dot(
        encoded,
        float3(0.5959, -0.2741, -0.3218));
    float quadrature = dot(
        encoded,
        float3(0.2113, -0.5227, 0.3113));
    float chrominance = sqrt(
        (inPhase * inPhase) +
        (quadrature * quadrature));
    float scale = 1.0;
    if (chrominance > maximumChrominance)
    {
        scale = maximumChrominance / chrominance;
    }
    float compositePeak = luminance + chrominance;
    if (compositePeak > maximumComposite)
    {
        scale = min(
            scale,
            maximumComposite / compositePeak);
    }
    float3 limited = pow(encoded * scale, 2.2);
    float limitedAmount =
        1.0 - step(1.0, scale);
    return lerp(
        source,
        float4(limited * source.a, source.a),
        limitedAmount);
}

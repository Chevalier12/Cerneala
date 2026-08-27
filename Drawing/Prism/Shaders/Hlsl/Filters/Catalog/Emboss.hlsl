float2 CatalogDirectionalReliefGradient(
    float2 uv,
    float2 sampleStep,
    int profile)
{
    float topLeft = CatalogLuminance(
        CatalogLinearSample(uv + float2(-sampleStep.x, -sampleStep.y), profile));
    float top = CatalogLuminance(
        CatalogLinearSample(uv + float2(0.0, -sampleStep.y), profile));
    float topRight = CatalogLuminance(
        CatalogLinearSample(uv + float2(sampleStep.x, -sampleStep.y), profile));
    float left = CatalogLuminance(
        CatalogLinearSample(uv + float2(-sampleStep.x, 0.0), profile));
    float right = CatalogLuminance(
        CatalogLinearSample(uv + float2(sampleStep.x, 0.0), profile));
    float bottomLeft = CatalogLuminance(
        CatalogLinearSample(uv + float2(-sampleStep.x, sampleStep.y), profile));
    float bottom = CatalogLuminance(
        CatalogLinearSample(uv + float2(0.0, sampleStep.y), profile));
    float bottomRight = CatalogLuminance(
        CatalogLinearSample(uv + sampleStep, profile));
    float horizontal = (
        (-3.0 * topLeft) + (3.0 * topRight) -
        (10.0 * left) + (10.0 * right) -
        (3.0 * bottomLeft) + (3.0 * bottomRight)) / 16.0;
    float vertical = (
        (-3.0 * topLeft) - (10.0 * top) - (3.0 * topRight) +
        (3.0 * bottomLeft) + (10.0 * bottom) +
        (3.0 * bottomRight)) / 16.0;
    return float2(horizontal, vertical);
}

float4 CatalogEmboss(float2 uv, float4 source, int profile)
{
    float angle = FilterOptions1.x * 0.01745329252;
    float2 gradient = CatalogDirectionalReliefGradient(
        uv,
        PixelSize * FilterOptions9.xy,
        profile);
    float directionalRelief = dot(
        gradient,
        float2(cos(angle), sin(angle)));
    float value = saturate(0.5 + (directionalRelief * FilterOptions0.x));
    return float4(
        value * source.a,
        value * source.a,
        value * source.a,
        source.a);
}

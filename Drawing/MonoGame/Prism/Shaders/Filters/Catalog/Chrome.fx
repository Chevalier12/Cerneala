sampler2D ChromeOriginalSampler = sampler_state
{
    Texture = <FilterAuxiliaryTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

float2 ChromeClampUv(float2 uv)
{
    return clamp(
        uv,
        PixelSize * 0.5,
        1.0 - (PixelSize * 0.5));
}

float4 ChromeOriginal(float2 uv, int profile)
{
    return WorkingAssociatedToLinearSrgb(
        tex2D(ChromeOriginalSampler, ChromeClampUv(uv)),
        profile);
}

float ChromeGaussianWeight(float offset)
{
    float sigma = clamp(FilterOptions2.x, 0.5, 4.0);
    return exp(
        -(offset * offset) /
        max(2.0 * sigma * sigma, 0.000001));
}

float4 ChromeBlurLuminance(
    float2 uv,
    int profile,
    bool horizontal)
{
    float radius = clamp(FilterOptions2.y, 1.0, 8.0);
    float4 center = CatalogLinearSample(uv, profile);
    if (center.a <= 0.000001)
    {
        return 0.0;
    }

    float weightedLuminance = 0.0;
    float coverageWeight = 0.0;
    [loop]
    for (int offset = -8; offset <= 8; offset++)
    {
        if (abs((float)offset) <= radius)
        {
            float2 delta = horizontal
                ? float2(PixelSize.x * offset, 0.0)
                : float2(0.0, PixelSize.y * offset);
            float4 sample = CatalogLinearSample(
                uv + delta,
                profile);
            float weight = ChromeGaussianWeight(offset) * sample.a;
            weightedLuminance += CatalogLuminance(sample) * weight;
            coverageWeight += weight;
        }
    }

    float luminance = coverageWeight <= 0.000001
        ? 0.0
        : weightedLuminance / coverageWeight;
    return float4(
        luminance * center.a,
        luminance * center.a,
        luminance * center.a,
        center.a);
}

float ChromeHeight(float2 uv, int profile)
{
    return CatalogLuminance(
        CatalogLinearSample(ChromeClampUv(uv), profile));
}

float2 ChromeScharrGradient(float2 uv, int profile)
{
    float2 horizontal = float2(PixelSize.x, 0.0);
    float2 vertical = float2(0.0, PixelSize.y);
    float topLeft = ChromeHeight(uv - horizontal - vertical, profile);
    float top = ChromeHeight(uv - vertical, profile);
    float topRight = ChromeHeight(uv + horizontal - vertical, profile);
    float left = ChromeHeight(uv - horizontal, profile);
    float right = ChromeHeight(uv + horizontal, profile);
    float bottomLeft = ChromeHeight(uv - horizontal + vertical, profile);
    float bottom = ChromeHeight(uv + vertical, profile);
    float bottomRight = ChromeHeight(uv + horizontal + vertical, profile);
    return float2(
        (3.0 * (topRight - topLeft)) +
            (10.0 * (right - left)) +
            (3.0 * (bottomRight - bottomLeft)),
        (3.0 * (bottomLeft - topLeft)) +
            (10.0 * (bottom - top)) +
            (3.0 * (bottomRight - topRight))) / 16.0;
}

float ChromeGaussianLobe(float value, float center, float width)
{
    float normalized =
        (value - center) / max(width, 0.0001);
    return exp(-(normalized * normalized));
}

float ChromeRamp(float value)
{
    float width = clamp(FilterOptions2.w, 0.035, 0.115);
    float broadWidth = width * 1.5;
    float narrowWidth = width * 0.65;
    return saturate(
        0.045 +
        (0.52 * ChromeGaussianLobe(value, 0.12, broadWidth)) +
        (0.92 * ChromeGaussianLobe(value, 0.34, narrowWidth)) +
        (0.18 * ChromeGaussianLobe(value, 0.5, broadWidth)) +
        (0.98 * ChromeGaussianLobe(value, 0.7, narrowWidth)) +
        (0.62 * ChromeGaussianLobe(value, 0.92, width)) -
        (0.28 * ChromeGaussianLobe(value, 0.58, narrowWidth)));
}

float4 ChromeComposite(float2 uv, int profile, float4 original)
{
    if (original.a <= 0.000001)
    {
        return 0.0;
    }

    float detailGain = clamp(FilterOptions2.z, 1.0, 8.5);
    float2 gradient = ChromeScharrGradient(uv, profile);
    float3 normal = normalize(float3(-gradient * detailGain, 1.0));
    float3 reflected = reflect(float3(0.0, 0.0, -1.0), normal);
    float heightValue = ChromeHeight(uv, profile);
    float environmentCoordinate = saturate(
        0.5 +
        (reflected.y * 0.36) +
        (reflected.x * 0.1) +
        ((heightValue - 0.5) * 0.08));
    float chrome = ChromeRamp(environmentCoordinate);
    return float4(
        chrome * original.a,
        chrome * original.a,
        chrome * original.a,
        original.a);
}

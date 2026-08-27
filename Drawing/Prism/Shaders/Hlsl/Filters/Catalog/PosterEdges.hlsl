


#ifndef CERNEALA_SDL_GPU
sampler2D PosterEdgesOriginalSampler = sampler_state
{
    Texture = <FilterAuxiliaryTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};
#endif

float4 PosterEdgesOriginal(float2 uv, int profile)
{
    return WorkingAssociatedToLinearSrgb(
        tex2D(
            PosterEdgesOriginalSampler,
            clamp(
                uv,
                PixelSize * 0.5,
                1.0 - (PixelSize * 0.5))),
        profile);
}

float4 PosterEdgesRawSample(float2 uv)
{
    return tex2D(
        SpriteTextureSampler,
        clamp(
            uv,
            PixelSize * 0.5,
            1.0 - (PixelSize * 0.5)));
}

float4 PosterEdgesBoxBlur(
    float2 uv,
    int profile,
    bool horizontal,
    bool moments)
{
    float radius = clamp(
        horizontal ? FilterOptions9.x : FilterOptions9.y,
        1.0,
        8.0);
    float4 total = 0.0;
    float count = 0.0;
    [loop]
    for (int offset = -8; offset <= 8; offset++)
    {
        if (abs((float)offset) <= radius)
        {
            float2 delta = horizontal
                ? float2(PixelSize.x * offset, 0.0)
                : float2(0.0, PixelSize.y * offset);
            if (moments)
            {
                float4 sample = CatalogLinearSample(
                    uv + delta,
                    profile);
                float luminance = CatalogLuminance(sample);
                total += float4(
                    sample.a,
                    luminance * sample.a,
                    luminance * luminance * sample.a,
                    1.0);
            }
            else
            {
                total += PosterEdgesRawSample(uv + delta);
            }
            count += 1.0;
        }
    }
    return total / max(count, 1.0);
}

float4 PosterEdgesCoefficients(
    float2 uv,
    int profile)
{
    int filterId = (int)(FilterHeader.x + 0.5);
    float epsilon = filterId == 100
        ? 0.0025 * (1.0 + clamp(FilterOptions4.x, 0.0, 15.0))
        : 0.01;
    float4 statistics = PosterEdgesRawSample(uv);
    float4 original = PosterEdgesOriginal(uv, profile);
    if (original.a <= 0.000001 || statistics.x <= 0.000001)
    {
        return float4(0.0, 0.0, 0.0, 1.0);
    }

    float mean = statistics.y / statistics.x;
    float variance = max(
        0.0,
        (statistics.z / statistics.x) -
            (mean * mean));
    float a = variance / (variance + epsilon);
    float b = mean - (a * mean);
    return float4(
        a * original.a,
        b * original.a,
        original.a,
        1.0);
}

float4 PosterEdgesGuidedColor(
    float2 uv,
    int profile)
{
    float4 coefficients = PosterEdgesBoxBlur(
        uv,
        profile,
        false,
        false);
    float4 original = PosterEdgesOriginal(uv, profile);
    if (original.a <= 0.000001)
    {
        return 0.0;
    }

    float3 straight = saturate(Unpremultiply(original));
    float luminance = dot(
        straight,
        float3(0.2126, 0.7152, 0.0722));
    float meanA = coefficients.z <= 0.000001
        ? 0.0
        : coefficients.x / coefficients.z;
    float meanB = coefficients.z <= 0.000001
        ? luminance
        : coefficients.y / coefficients.z;
    float guidedLuminance = saturate(
        (meanA * luminance) + meanB);
    float3 guided = luminance <= 0.000001
        ? guidedLuminance
        : saturate(straight * (guidedLuminance / luminance));
    return float4(guided * original.a, original.a);
}

float PosterEdgesGuidedLuminance(float2 uv)
{
    float4 sample = PosterEdgesRawSample(uv);
    return dot(
        saturate(Unpremultiply(sample)),
        float3(0.2126, 0.7152, 0.0722));
}

float2 GuidedScharrGradient(float2 uv, float radius)
{
    radius = clamp(radius, 1.0, 8.0);
    float2 horizontal = float2(PixelSize.x * radius, 0.0);
    float2 vertical = float2(0.0, PixelSize.y * radius);
    float topLeft = PosterEdgesGuidedLuminance(
        uv - horizontal - vertical);
    float top = PosterEdgesGuidedLuminance(uv - vertical);
    float topRight = PosterEdgesGuidedLuminance(
        uv + horizontal - vertical);
    float left = PosterEdgesGuidedLuminance(uv - horizontal);
    float right = PosterEdgesGuidedLuminance(uv + horizontal);
    float bottomLeft = PosterEdgesGuidedLuminance(
        uv - horizontal + vertical);
    float bottom = PosterEdgesGuidedLuminance(uv + vertical);
    float bottomRight = PosterEdgesGuidedLuminance(
        uv + horizontal + vertical);
    float gradientX =
        (3.0 * (topRight - topLeft)) +
        (10.0 * (right - left)) +
        (3.0 * (bottomRight - bottomLeft));
    float gradientY =
        (3.0 * (bottomLeft - topLeft)) +
        (10.0 * (bottom - top)) +
        (3.0 * (bottomRight - topRight));
    return float2(gradientX, gradientY) / 16.0;
}

float PosterEdgesScharr(float2 uv)
{
    float radius = max(FilterOptions9.x, FilterOptions9.y);
    return saturate(length(GuidedScharrGradient(uv, radius)));
}

float4 PosterEdgesComposite(float2 uv)
{
    float4 guided = PosterEdgesRawSample(uv);
    if (guided.a <= 0.000001)
    {
        return 0.0;
    }

    float levels = clamp(round(FilterOptions2.x), 2.0, 32.0);
    float levelScale = levels - 1.0;
    float3 quantized =
        round(saturate(Unpremultiply(guided)) * levelScale) /
        levelScale;
    float edgeIntensity = clamp(FilterOptions0.x, 0.0, 4.0);
    float ink = saturate(
        PosterEdgesScharr(uv) * edgeIntensity * 2.0);
    return float4(
        saturate(quantized * (1.0 - ink)) * guided.a,
        guided.a);
}

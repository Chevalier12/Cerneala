


#ifndef CERNEALA_SDL_GPU
sampler2D GlowingEdgesOriginalSampler = sampler_state
{
    Texture = <FilterAuxiliaryTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};
#endif

float2 GlowingEdgesClampUv(float2 uv)
{
    return clamp(
        uv,
        PixelSize * 0.5,
        1.0 - (PixelSize * 0.5));
}

float4 GlowingEdgesRawSample(float2 uv)
{
    return tex2D(
        SpriteTextureSampler,
        GlowingEdgesClampUv(uv));
}

float4 GlowingEdgesOriginal(float2 uv, int profile)
{
    return WorkingAssociatedToLinearSrgb(
        tex2D(
            GlowingEdgesOriginalSampler,
            GlowingEdgesClampUv(uv)),
        profile);
}

float4 GlowingEdgesLinearSample(
    float2 uv,
    int profile,
    bool original)
{
    return original
        ? GlowingEdgesOriginal(uv, profile)
        : CatalogLinearSample(uv, profile);
}

float GlowingEdgesLuminance(
    float2 uv,
    int profile,
    bool original)
{
    return CatalogLuminance(
        GlowingEdgesLinearSample(uv, profile, original));
}

float GlowingEdgesScharr(
    float2 uv,
    int profile,
    bool original)
{
    float radius = clamp(FilterOptions3.x, 1.0, 8.0);
    float2 horizontal = float2(PixelSize.x * radius, 0.0);
    float2 vertical = float2(0.0, PixelSize.y * radius);
    float topLeft = GlowingEdgesLuminance(
        uv - horizontal - vertical,
        profile,
        original);
    float top = GlowingEdgesLuminance(
        uv - vertical,
        profile,
        original);
    float topRight = GlowingEdgesLuminance(
        uv + horizontal - vertical,
        profile,
        original);
    float left = GlowingEdgesLuminance(
        uv - horizontal,
        profile,
        original);
    float right = GlowingEdgesLuminance(
        uv + horizontal,
        profile,
        original);
    float bottomLeft = GlowingEdgesLuminance(
        uv - horizontal + vertical,
        profile,
        original);
    float bottom = GlowingEdgesLuminance(
        uv + vertical,
        profile,
        original);
    float bottomRight = GlowingEdgesLuminance(
        uv + horizontal + vertical,
        profile,
        original);
    float gradientX =
        (3.0 * (topRight - topLeft)) +
        (10.0 * (right - left)) +
        (3.0 * (bottomRight - bottomLeft));
    float gradientY =
        (3.0 * (bottomLeft - topLeft)) +
        (10.0 * (bottom - top)) +
        (3.0 * (bottomRight - topRight));
    return saturate(length(float2(gradientX, gradientY)) / 16.0);
}

float GlowingEdgesGaussianWeight(float offset)
{
    float sigma = clamp(FilterOptions3.y, 0.5, 4.0);
    return exp(
        -(offset * offset) /
        max(2.0 * sigma * sigma, 0.000001));
}

float4 GlowingEdgesExtract(float2 uv, int profile)
{
    float alpha = CatalogLinearSample(uv, profile).a;
    float edge = GlowingEdgesScharr(uv, profile, false);
    return float4(edge * alpha, edge * alpha, edge * alpha, alpha);
}

float4 GlowingEdgesGaussian(float2 uv, bool horizontal)
{
    float radius = clamp(FilterOptions3.z, 1.0, 8.0);
    float4 total = 0.0;
    float totalWeight = 0.0;
    [loop]
    for (int offset = -8; offset <= 8; offset++)
    {
        if (abs((float)offset) <= radius)
        {
            float2 delta = horizontal
                ? float2(PixelSize.x * offset, 0.0)
                : float2(0.0, PixelSize.y * offset);
            float weight = GlowingEdgesGaussianWeight(offset);
            total += GlowingEdgesRawSample(uv + delta) * weight;
            totalWeight += weight;
        }
    }

    return total / max(totalWeight, 0.000001);
}

float4 GlowingEdgesHorizontal(float2 uv)
{
    return GlowingEdgesGaussian(uv, true);
}

float4 GlowingEdgesVerticalComposite(
    float2 uv,
    int profile,
    float4 original)
{
    if (original.a <= 0.000001)
    {
        return 0.0;
    }

    float4 bloom = GlowingEdgesGaussian(uv, false);
    float soft = bloom.a <= 0.000001
        ? 0.0
        : bloom.r / bloom.a;
    float crisp = GlowingEdgesScharr(uv, profile, true);
    float brightness = max(FilterOptions1.x, 0.0);
    float intensity = saturate(
        (crisp + (soft * FilterOptions3.w)) *
        brightness *
        0.25);
    float3 glowColor = float3(0.25, 0.6, 1.0);
    return float4(
        glowColor * intensity * original.a,
        original.a);
}

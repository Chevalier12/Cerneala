#ifndef CERNEALA_SDL_GPU
sampler2D NotePaperOriginalSampler = sampler_state
{
    Texture = <FilterAuxiliaryTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};
#endif

float2 NotePaperClampUv(float2 uv)
{
    return clamp(
        uv,
        PixelSize * 0.5,
        1.0 - (PixelSize * 0.5));
}

float4 NotePaperOriginal(float2 uv, int profile)
{
    return WorkingAssociatedToLinearSrgb(
        tex2D(NotePaperOriginalSampler, NotePaperClampUv(uv)),
        profile);
}

float NotePaperGaussianWeight(float offset)
{
    const float sigma = 1.15;
    return exp(
        -(offset * offset) /
        (2.0 * sigma * sigma));
}

float4 NotePaperBlurLuminance(
    float2 uv,
    int profile,
    bool horizontal)
{
    float radius = clamp(max(FilterOptions9.x, FilterOptions9.y), 1.0, 4.0);
    float4 center = CatalogLinearSample(uv, profile);
    if (center.a <= 0.000001)
    {
        return 0.0;
    }

    float weightedLuminance = 0.0;
    float coverageWeight = 0.0;
    [loop]
    for (int offset = -4; offset <= 4; offset++)
    {
        if (abs((float)offset) <= radius)
        {
            float2 delta = horizontal
                ? float2(PixelSize.x * offset, 0.0)
                : float2(0.0, PixelSize.y * offset);
            float4 sample = CatalogLinearSample(uv + delta, profile);
            float weight = NotePaperGaussianWeight(offset) * sample.a;
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

float NotePaperHash(float2 cell)
{
    return frac(
        sin(dot(cell, float2(127.1, 311.7))) *
        43758.5453);
}

float NotePaperValueNoise(float2 coordinate)
{
    float2 cell = floor(coordinate);
    float2 blend = frac(coordinate);
    blend = blend * blend * (3.0 - (2.0 * blend));
    float top = lerp(
        NotePaperHash(cell),
        NotePaperHash(cell + float2(1.0, 0.0)),
        blend.x);
    float bottom = lerp(
        NotePaperHash(cell + float2(0.0, 1.0)),
        NotePaperHash(cell + 1.0),
        blend.x);
    return lerp(top, bottom, blend.y);
}

float NotePaperFractalNoise(float2 coordinate)
{
    return
        (NotePaperValueNoise(coordinate) * 0.625) +
        (NotePaperValueNoise((coordinate * 2.0) + float2(19.1, -7.7)) * 0.25) +
        (NotePaperValueNoise((coordinate * 4.0) + float2(-3.4, 11.3)) * 0.125);
}

float4 NotePaperBuildHeight(float2 uv, int profile)
{
    float4 blurred = NotePaperBlurLuminance(uv, profile, false);
    if (blurred.a <= 0.000001)
    {
        return 0.0;
    }

    float imageBalance = saturate(FilterOptions5.x);
    float graininess = saturate(FilterOptions5.y);
    float threshold = lerp(0.25, 0.75, imageBalance);
    float wavelength = lerp(8.0, 1.6, graininess);
    float2 pixel = uv / PixelSize;
    float grain = NotePaperFractalNoise(pixel / wavelength) - 0.5;
    float tone = CatalogLuminance(blurred) +
        (grain * graininess * 0.28);
    float surface = smoothstep(
        threshold - 0.12,
        threshold + 0.12,
        tone);
    float heightValue = saturate(
        surface + (grain * graininess * 0.16));
    return float4(
        heightValue * blurred.a,
        heightValue * blurred.a,
        heightValue * blurred.a,
        blurred.a);
}

float NotePaperHeight(float2 uv, int profile)
{
    return CatalogLuminance(
        CatalogLinearSample(NotePaperClampUv(uv), profile));
}

float4 NotePaperComposite(float2 uv, int profile, float4 original)
{
    if (original.a <= 0.000001)
    {
        return 0.0;
    }

    float center = NotePaperHeight(uv, profile);
    float horizontal =
        NotePaperHeight(uv + float2(PixelSize.x, 0.0), profile) -
        NotePaperHeight(uv - float2(PixelSize.x, 0.0), profile);
    float vertical =
        NotePaperHeight(uv + float2(0.0, PixelSize.y), profile) -
        NotePaperHeight(uv - float2(0.0, PixelSize.y), profile);
    float relief = saturate(FilterOptions5.z);
    float3 normal = normalize(float3(
        -horizontal * relief * 7.0,
        -vertical * relief * 7.0,
        1.0));
    const float3 lightDirection = float3(
        -0.419758,
        -0.559677,
        0.719585);
    float shade =
        (dot(normal, lightDirection) - lightDirection.z) *
        relief *
        1.8;
    float surface = smoothstep(0.28, 0.72, center);
    float3 color = saturate(
        lerp(FilterOptions0.rgb, FilterOptions1.rgb, surface) +
        shade);
    return float4(color * original.a, original.a);
}

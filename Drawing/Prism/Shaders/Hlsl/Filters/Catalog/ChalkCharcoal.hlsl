



#ifndef CERNEALA_SDL_GPU
sampler2D ChalkCharcoalOriginalSampler = sampler_state
{
    Texture = <FilterAuxiliaryTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};
#endif

float2 ChalkCharcoalClampUv(float2 uv)
{
    return clamp(
        uv,
        PixelSize * 0.5,
        1.0 - (PixelSize * 0.5));
}

float4 ChalkCharcoalRawSample(float2 uv)
{
    return tex2D(
        SpriteTextureSampler,
        ChalkCharcoalClampUv(uv));
}

float4 ChalkCharcoalOriginal(float2 uv, int profile)
{
    return WorkingAssociatedToLinearSrgb(
        tex2D(
            ChalkCharcoalOriginalSampler,
            ChalkCharcoalClampUv(uv)),
        profile);
}

float ChalkCharcoalGaussianWeight(float offset, float sigma)
{
    return exp(
        -(offset * offset) /
        max(2.0 * sigma * sigma, 0.000001));
}

float4 ChalkCharcoalHorizontal(float2 uv, int profile)
{
    float charcoalRadius = clamp(FilterOptions5.z, 1.0, 8.0);
    float chalkRadius = clamp(FilterOptions5.w, charcoalRadius, 8.0);
    float charcoalSigma = clamp(FilterOptions5.x, 0.5, 4.0);
    float chalkSigma = clamp(FilterOptions5.y, charcoalSigma, 6.4);
    float4 total = 0.0;
    float2 totalWeight = 0.0;
    [loop]
    for (int offset = -8; offset <= 8; offset++)
    {
        float absoluteOffset = abs((float)offset);
        if (absoluteOffset <= chalkRadius)
        {
            float4 sample = CatalogLinearSample(
                uv + float2(PixelSize.x * offset, 0.0),
                profile);
            float luminance = CatalogLuminance(sample);
            float chalkWeight = ChalkCharcoalGaussianWeight(
                offset,
                chalkSigma);
            total.zw += float2(luminance * sample.a, sample.a) *
                chalkWeight;
            totalWeight.y += chalkWeight;
            if (absoluteOffset <= charcoalRadius)
            {
                float charcoalWeight = ChalkCharcoalGaussianWeight(
                    offset,
                    charcoalSigma);
                total.xy += float2(luminance * sample.a, sample.a) *
                    charcoalWeight;
                totalWeight.x += charcoalWeight;
            }
        }
    }

    return float4(
        total.xy / max(totalWeight.x, 0.000001),
        total.zw / max(totalWeight.y, 0.000001));
}

float4 ChalkCharcoalVertical(float2 uv)
{
    float charcoalRadius = clamp(FilterOptions5.z, 1.0, 8.0);
    float chalkRadius = clamp(FilterOptions5.w, charcoalRadius, 8.0);
    float charcoalSigma = clamp(FilterOptions5.x, 0.5, 4.0);
    float chalkSigma = clamp(FilterOptions5.y, charcoalSigma, 6.4);
    float4 total = 0.0;
    float2 totalWeight = 0.0;
    [loop]
    for (int offset = -8; offset <= 8; offset++)
    {
        float absoluteOffset = abs((float)offset);
        if (absoluteOffset <= chalkRadius)
        {
            float4 sample = ChalkCharcoalRawSample(
                uv + float2(0.0, PixelSize.y * offset));
            float chalkWeight = ChalkCharcoalGaussianWeight(
                offset,
                chalkSigma);
            total.zw += sample.zw * chalkWeight;
            totalWeight.y += chalkWeight;
            if (absoluteOffset <= charcoalRadius)
            {
                float charcoalWeight = ChalkCharcoalGaussianWeight(
                    offset,
                    charcoalSigma);
                total.xy += sample.xy * charcoalWeight;
                totalWeight.x += charcoalWeight;
            }
        }
    }

    return float4(
        total.xy / max(totalWeight.x, 0.000001),
        total.zw / max(totalWeight.y, 0.000001));
}

float ChalkCharcoalHash(float2 coordinate)
{
    float3 value = frac(float3(coordinate.xyx) * 0.1031);
    value += dot(value, value.yzx + 33.33);
    return frac((value.x + value.y) * value.z);
}

float ChalkCharcoalGrain(float2 pixel)
{
    float fine = ChalkCharcoalHash(pixel + float2(17.0, 43.0));
    float coarse = ChalkCharcoalHash(
        floor(pixel * 0.25) + float2(71.0, 29.0));
    return saturate((0.72 * fine) + (0.28 * coarse));
}

float4 ChalkCharcoalComposite(float2 uv, float4 original)
{
    const float minimumWeight = 0.000001;
    if (original.a <= minimumWeight)
    {
        return 0.0;
    }

    float4 gaussian = ChalkCharcoalRawSample(uv);
    float charcoalLuminance = gaussian.y <= minimumWeight
        ? 0.0
        : gaussian.x / gaussian.y;
    float chalkLuminance = gaussian.w <= minimumWeight
        ? 0.0
        : gaussian.z / gaussian.w;
    float charcoalArea = clamp(FilterOptions2.x, 0.0, 20.0);
    float chalkArea = clamp(FilterOptions1.x, 0.0, 20.0);
    float pressure = saturate(FilterOptions4.x / 10.0);
    float sharpen = lerp(4.0, 16.0, pressure);
    float response =
        ((sharpen + 1.0) * charcoalLuminance) -
        (sharpen * chalkLuminance);
    float epsilon = lerp(0.035, -0.015, pressure);
    float phi = lerp(16.0, 42.0, pressure);
    float thresholded = response >= epsilon
        ? 1.0
        : saturate(1.0 + tanh(phi * (response - epsilon)));
    float edgeMask = 1.0 - thresholded;
    float3 straight = saturate(Unpremultiply(original));
    float luminance = dot(straight, float3(0.2126, 0.7152, 0.0722));
    float darkThreshold = lerp(0.24, 0.72, charcoalArea / 20.0);
    float lightThreshold = lerp(0.84, 0.42, chalkArea / 20.0);
    float darkTone = 1.0 - smoothstep(
        darkThreshold - 0.18,
        darkThreshold + 0.12,
        luminance);
    float lightTone = smoothstep(
        lightThreshold - 0.12,
        lightThreshold + 0.18,
        luminance);
    float2 pixel = floor(uv / PixelSize);
    float grain = ChalkCharcoalGrain(pixel);
    float fiber = 0.5 +
        (0.5 * sin(
            (pixel.x * 1.73) +
            (pixel.y * 0.19) +
            (grain * 3.1)));
    float grainStrength = 0.12 + (0.2 * pressure);
    float centeredGrain = (grain - 0.5) * 2.0;
    float charcoalGrain = saturate(
        0.78 +
        (centeredGrain * grainStrength) -
        ((fiber - 0.5) * 0.18));
    float chalkGrain = saturate(
        0.82 -
        (centeredGrain * grainStrength * 0.8) +
        ((fiber - 0.5) * 0.14));
    float darkMask = saturate(
        max(edgeMask, darkTone * 0.82) *
        charcoalGrain *
        (0.72 + (0.28 * pressure)));
    float lightMask = saturate(
        lightTone *
        chalkGrain *
        (0.68 + (0.32 * pressure)) *
        (1.0 - (darkMask * 0.8)));
    float3 foreground = saturate(FilterOptions3.rgb);
    float3 background = saturate(FilterOptions0.rgb);
    float3 toned = lerp(
        foreground,
        background,
        smoothstep(0.2, 0.8, luminance));
    toned = lerp(toned, foreground, darkMask);
    toned = lerp(toned, background, lightMask);
    return float4(saturate(toned) * original.a, original.a);
}

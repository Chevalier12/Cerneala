




sampler2D SumiEOriginalSampler = sampler_state
{
    Texture = <FilterAuxiliaryTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

float2 SumiEClampUv(float2 uv)
{
    return clamp(
        uv,
        PixelSize * 0.5,
        1.0 - (PixelSize * 0.5));
}

float4 SumiERawSample(float2 uv)
{
    return tex2D(SpriteTextureSampler, SumiEClampUv(uv));
}

float4 SumiEOriginal(float2 uv, int profile)
{
    return WorkingAssociatedToLinearSrgb(
        tex2D(SumiEOriginalSampler, SumiEClampUv(uv)),
        profile);
}

float SumiEGaussianWeight(float offset, float sigma)
{
    return exp(
        -(offset * offset) /
        max(2.0 * sigma * sigma, 0.000001));
}

float SumiEStraightLuminance(float4 sample)
{
    return sample.a <= 0.000001
        ? 0.0
        : dot(
            sample.rgb / sample.a,
            float3(0.2126, 0.7152, 0.0722));
}

float4 SumiEDirectionalWash(float2 uv, int profile)
{
    float4 center = CatalogLinearSample(uv, profile);
    if (center.a <= 0.000001)
    {
        return 0.0;
    }

    float left = SumiEStraightLuminance(
        CatalogLinearSample(uv - float2(PixelSize.x, 0.0), profile));
    float right = SumiEStraightLuminance(
        CatalogLinearSample(uv + float2(PixelSize.x, 0.0), profile));
    float top = SumiEStraightLuminance(
        CatalogLinearSample(uv - float2(0.0, PixelSize.y), profile));
    float bottom = SumiEStraightLuminance(
        CatalogLinearSample(uv + float2(0.0, PixelSize.y), profile));
    float2 gradient = float2(right - left, bottom - top);
    float gradientLength = length(gradient);
    float2 tangent = gradientLength > 0.000001
        ? float2(-gradient.y, gradient.x) / gradientLength
        : float2(1.0, 0.0);
    float2 normal = float2(-tangent.y, tangent.x);

    int radius = (int)clamp(
        ceil(max(FilterOptions9.x, FilterOptions9.y)),
        1.0,
        6.0);
    float radiusSquared = radius * radius;
    float3 sector0 = 0.0;
    float3 sector1 = 0.0;
    float3 sector2 = 0.0;
    float3 sector3 = 0.0;
    [loop]
    for (int offsetY = -6; offsetY <= 6; offsetY++)
    {
        [loop]
        for (int offsetX = -6; offsetX <= 6; offsetX++)
        {
            float distanceSquared =
                (offsetX * offsetX) + (offsetY * offsetY);
            if (abs(offsetX) <= radius &&
                abs(offsetY) <= radius &&
                distanceSquared <= radiusSquared)
            {
                float4 sample = CatalogLinearSample(
                    uv + (float2(offsetX, offsetY) * PixelSize),
                    profile);
                float alphaConfidence = saturate(
                    1.0 - (abs(sample.a - center.a) * 4.0));
                float spatialWeight = exp(
                    -2.0 * distanceSquared /
                    max(radiusSquared, 1.0));
                float weight = sample.a * alphaConfidence * spatialWeight;
                float luminance = SumiEStraightLuminance(sample);
                float3 moment = float3(
                    luminance * weight,
                    luminance * luminance * weight,
                    weight);
                float tangentPosition = dot(
                    float2(offsetX, offsetY),
                    tangent);
                float normalPosition = dot(
                    float2(offsetX, offsetY),
                    normal);
                sector0 += moment *
                    step(tangentPosition, 0.0) *
                    step(normalPosition, 0.0);
                sector1 += moment *
                    step(0.0, tangentPosition) *
                    step(normalPosition, 0.0);
                sector2 += moment *
                    step(0.0, tangentPosition) *
                    step(0.0, normalPosition);
                sector3 += moment *
                    step(tangentPosition, 0.0) *
                    step(0.0, normalPosition);
            }
        }
    }

    float4 sectorWeight = max(
        float4(sector0.z, sector1.z, sector2.z, sector3.z),
        0.000001);
    float4 mean = float4(
        sector0.x,
        sector1.x,
        sector2.x,
        sector3.x) / sectorWeight;
    float4 variance = max(
        abs(
            float4(
                sector0.y,
                sector1.y,
                sector2.y,
                sector3.y) /
            sectorWeight -
            (mean * mean)),
        0.000001);
    float pressure = saturate(FilterOptions1.x / 8.0);
    float sharpness = lerp(2.0, 7.0, pressure);
    float4 confidence = 1.0 /
        (1.0 + pow(400.0 * variance, sharpness));
    float wash = dot(mean, confidence) /
        max(dot(confidence, 1.0), 0.000001);
    wash = lerp(
        SumiEStraightLuminance(center),
        wash,
        0.88);
    return float4(saturate(wash).xxx * center.a, center.a);
}

float4 SumiEHorizontalXDog(float2 uv, int profile)
{
    float narrowRadius = clamp(FilterOptions3.z, 1.0, 8.0);
    float extendedRadius = clamp(FilterOptions3.w, narrowRadius, 8.0);
    float narrowSigma = clamp(FilterOptions3.x, 0.5, 4.0);
    float extendedSigma = clamp(FilterOptions3.y, narrowSigma, 6.4);
    float4 total = 0.0;
    float2 kernelWeight = 0.0;
    [loop]
    for (int offset = -8; offset <= 8; offset++)
    {
        float absoluteOffset = abs((float)offset);
        if (absoluteOffset <= extendedRadius)
        {
            float4 sample = CatalogLinearSample(
                uv + float2(PixelSize.x * offset, 0.0),
                profile);
            float luminance = SumiEStraightLuminance(sample);
            float extendedWeight = SumiEGaussianWeight(
                offset,
                extendedSigma);
            total.zw += float2(luminance * sample.a, sample.a) *
                extendedWeight;
            kernelWeight.y += extendedWeight;
            if (absoluteOffset <= narrowRadius)
            {
                float narrowWeight = SumiEGaussianWeight(
                    offset,
                    narrowSigma);
                total.xy += float2(luminance * sample.a, sample.a) *
                    narrowWeight;
                kernelWeight.x += narrowWeight;
            }
        }
    }

    return float4(
        total.xy / max(kernelWeight.x, 0.000001),
        total.zw / max(kernelWeight.y, 0.000001));
}

float4 SumiEComposite(float2 uv, int profile)
{
    float4 original = SumiEOriginal(uv, profile);
    if (original.a <= 0.000001)
    {
        return 0.0;
    }

    float narrowRadius = clamp(FilterOptions3.z, 1.0, 8.0);
    float extendedRadius = clamp(FilterOptions3.w, narrowRadius, 8.0);
    float narrowSigma = clamp(FilterOptions3.x, 0.5, 4.0);
    float extendedSigma = clamp(FilterOptions3.y, narrowSigma, 6.4);
    float4 gaussian = 0.0;
    float2 kernelWeight = 0.0;
    [loop]
    for (int offset = -8; offset <= 8; offset++)
    {
        float absoluteOffset = abs((float)offset);
        if (absoluteOffset <= extendedRadius)
        {
            float4 sample = SumiERawSample(
                uv + float2(0.0, PixelSize.y * offset));
            float extendedWeight = SumiEGaussianWeight(
                offset,
                extendedSigma);
            gaussian.zw += sample.zw * extendedWeight;
            kernelWeight.y += extendedWeight;
            if (absoluteOffset <= narrowRadius)
            {
                float narrowWeight = SumiEGaussianWeight(
                    offset,
                    narrowSigma);
                gaussian.xy += sample.xy * narrowWeight;
                kernelWeight.x += narrowWeight;
            }
        }
    }
    gaussian = float4(
        gaussian.xy / max(kernelWeight.x, 0.000001),
        gaussian.zw / max(kernelWeight.y, 0.000001));

    float narrowLuminance = gaussian.y <= 0.000001
        ? 0.0
        : gaussian.x / gaussian.y;
    float extendedLuminance = gaussian.w <= 0.000001
        ? 0.0
        : gaussian.z / gaussian.w;
    float pressure = saturate(FilterOptions1.x / 8.0);
    float contrast = clamp(FilterOptions2.x, -3.0, 10.0);
    float sharpen = 4.0 + (24.0 * pressure);
    float response =
        ((sharpen + 1.0) * narrowLuminance) -
        (sharpen * extendedLuminance);
    float epsilon = 0.015 + (0.004 * contrast);
    float phi = clamp(12.0 + (8.0 * contrast), 6.0, 64.0);
    float thresholded = response >= epsilon
        ? 1.0
        : saturate(1.0 + tanh(phi * (response - epsilon)));
    float inkEdge = 1.0 - thresholded;

    float tonalContrast = clamp(1.0 + (0.22 * contrast), 0.25, 3.2);
    float tone = saturate(
        ((narrowLuminance - 0.5) * tonalContrast) + 0.5);
    float quantizedTone = floor((tone * 4.0) + 0.5) / 4.0;
    tone = lerp(tone, quantizedTone, 0.68);
    tone = saturate(
        tone -
        (inkEdge * lerp(0.42, 0.9, pressure)) -
        ((1.0 - narrowLuminance) * pressure * 0.08));

    float2 pixel = uv / PixelSize;
    float fineGrain = DryBrushHash(
        (int2)floor(pixel),
        0x51ed270bu);
    float coarseGrain = DryBrushHash(
        (int2)floor(pixel / 9.0),
        0x8321ca5du);
    float fiber = 0.5 +
        (0.25 * cos((pixel.x + (coarseGrain * 2.0)) * 1.7)) +
        (0.25 * cos((pixel.y - (fineGrain * 2.0)) * 2.15));
    float paper = saturate(
        (0.45 * fineGrain) +
        (0.25 * coarseGrain) +
        (0.30 * fiber));
    float dryGap = smoothstep(0.70, 0.94, paper) *
        (1.0 - (pressure * 0.72)) *
        (1.0 - tone) *
        0.42;
    tone = saturate(
        lerp(tone, 1.0, dryGap) +
        ((paper - 0.5) * 0.055));

    float3 inkColor = float3(0.012, 0.018, 0.017);
    float3 paperColor = float3(0.985, 0.978, 0.948);
    float3 color = lerp(inkColor, paperColor, tone);
    return float4(color * original.a, original.a);
}

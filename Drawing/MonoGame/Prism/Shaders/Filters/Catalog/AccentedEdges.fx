



sampler2D AccentedEdgesOriginalSampler = sampler_state
{
    Texture = <FilterAuxiliaryTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

float2 AccentedEdgesClampUv(float2 uv)
{
    return clamp(
        uv,
        PixelSize * 0.5,
        1.0 - (PixelSize * 0.5));
}

float4 AccentedEdgesRawSample(float2 uv)
{
    return tex2D(
        SpriteTextureSampler,
        AccentedEdgesClampUv(uv));
}

float4 AccentedEdgesOriginal(float2 uv, int profile)
{
    return WorkingAssociatedToLinearSrgb(
        tex2D(
            AccentedEdgesOriginalSampler,
            AccentedEdgesClampUv(uv)),
        profile);
}

float AccentedEdgesGaussianWeight(float offset, float sigma)
{
    return exp(
        -(offset * offset) /
        max(2.0 * sigma * sigma, 0.000001));
}

float4 AccentedEdgesHorizontal(float2 uv, int profile)
{
    float narrowRadius = clamp(FilterOptions3.z, 1.0, 8.0);
    float extendedRadius = clamp(FilterOptions3.w, narrowRadius, 8.0);
    float narrowSigma = clamp(FilterOptions3.x, 0.5, 4.0);
    float extendedSigma = clamp(
        FilterOptions3.y,
        narrowSigma,
        6.4);
    float4 total = 0.0;
    float2 totalWeight = 0.0;
    [loop]
    for (int offset = -8; offset <= 8; offset++)
    {
        float absoluteOffset = abs((float)offset);
        if (absoluteOffset <= extendedRadius)
        {
            float4 sample = CatalogLinearSample(
                uv + float2(PixelSize.x * offset, 0.0),
                profile);
            float luminance = CatalogLuminance(sample);
            float extendedWeight = AccentedEdgesGaussianWeight(
                offset,
                extendedSigma);
            total.zw += float2(luminance * sample.a, sample.a) *
                extendedWeight;
            totalWeight.y += extendedWeight;
            if (absoluteOffset <= narrowRadius)
            {
                float narrowWeight = AccentedEdgesGaussianWeight(
                    offset,
                    narrowSigma);
                total.xy += float2(luminance * sample.a, sample.a) *
                    narrowWeight;
                totalWeight.x += narrowWeight;
            }
        }
    }

    return float4(
        total.xy / max(totalWeight.x, 0.000001),
        total.zw / max(totalWeight.y, 0.000001));
}

float4 AccentedEdgesVertical(float2 uv)
{
    float narrowRadius = clamp(FilterOptions3.z, 1.0, 8.0);
    float extendedRadius = clamp(FilterOptions3.w, narrowRadius, 8.0);
    float narrowSigma = clamp(FilterOptions3.x, 0.5, 4.0);
    float extendedSigma = clamp(
        FilterOptions3.y,
        narrowSigma,
        6.4);
    float4 total = 0.0;
    float2 totalWeight = 0.0;
    [loop]
    for (int offset = -8; offset <= 8; offset++)
    {
        float absoluteOffset = abs((float)offset);
        if (absoluteOffset <= extendedRadius)
        {
            float4 sample = AccentedEdgesRawSample(
                uv + float2(0.0, PixelSize.y * offset));
            float extendedWeight = AccentedEdgesGaussianWeight(
                offset,
                extendedSigma);
            total.zw += sample.zw * extendedWeight;
            totalWeight.y += extendedWeight;
            if (absoluteOffset <= narrowRadius)
            {
                float narrowWeight = AccentedEdgesGaussianWeight(
                    offset,
                    narrowSigma);
                total.xy += sample.xy * narrowWeight;
                totalWeight.x += narrowWeight;
            }
        }
    }

    return float4(
        total.xy / max(totalWeight.x, 0.000001),
        total.zw / max(totalWeight.y, 0.000001));
}

float4 AccentedEdgesComposite(float2 uv, float4 original)
{
    const float epsilon = 0.02;
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
    float smoothness = saturate(FilterOptions2.x / 15.0);
    float phi = lerp(48.0, 6.0, smoothness);
    float thresholded = response >= epsilon
        ? 1.0
        : saturate(1.0 + tanh(phi * (response - epsilon)));
    float accent = 1.0 - thresholded;
    float edgeTone = saturate(FilterOptions0.x / 50.0);
    float3 straight = saturate(Unpremultiply(original));
    float3 accented = lerp(straight, edgeTone, accent);
    return float4(accented * original.a, original.a);
}

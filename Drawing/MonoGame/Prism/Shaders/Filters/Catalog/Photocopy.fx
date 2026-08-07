




sampler2D PhotocopyOriginalSampler = sampler_state
{
    Texture = <FilterAuxiliaryTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

float2 PhotocopyClampUv(float2 uv)
{
    return clamp(uv, PixelSize * 0.5, 1.0 - (PixelSize * 0.5));
}

float4 PhotocopyRawSample(float2 uv)
{
    return tex2D(SpriteTextureSampler, PhotocopyClampUv(uv));
}

float4 PhotocopyOriginal(float2 uv, int profile)
{
    return WorkingAssociatedToLinearSrgb(
        tex2D(PhotocopyOriginalSampler, PhotocopyClampUv(uv)),
        profile);
}

float PhotocopyGaussianWeight(float offset, float sigma)
{
    return exp(
        -(offset * offset) /
        max(2.0 * sigma * sigma, 0.000001));
}

float4 PhotocopyXDogSettings()
{
    int filterId = (int)(FilterHeader.x + 0.5);
    return filterId == 112 ? FilterOptions5 : FilterOptions4;
}

float4 PhotocopyHorizontal(float2 uv, int profile)
{
    float4 settings = PhotocopyXDogSettings();
    float narrowSigma = clamp(settings.x, 0.5, 3.75);
    float extendedSigma = clamp(settings.y, narrowSigma + 0.25, 4.0);
    float narrowRadius = min(ceil(narrowSigma * 3.0), settings.z);
    float extendedRadius = clamp(settings.z, 1.0, 12.0);
    float4 total = 0.0;
    float2 totalWeight = 0.0;
    [loop]
    for (int offset = -12; offset <= 12; offset++)
    {
        float absoluteOffset = abs((float)offset);
        if (absoluteOffset <= extendedRadius)
        {
            float4 sample = CatalogLinearSample(
                uv + float2(PixelSize.x * offset, 0.0),
                profile);
            float luminance = CatalogLuminance(sample);
            float extendedWeight = PhotocopyGaussianWeight(
                offset,
                extendedSigma);
            total.zw += float2(luminance * sample.a, sample.a) *
                extendedWeight;
            totalWeight.y += extendedWeight;
            if (absoluteOffset <= narrowRadius)
            {
                float narrowWeight = PhotocopyGaussianWeight(
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

float4 PhotocopyVertical(float2 uv)
{
    float4 settings = PhotocopyXDogSettings();
    float narrowSigma = clamp(settings.x, 0.5, 3.75);
    float extendedSigma = clamp(settings.y, narrowSigma + 0.25, 4.0);
    float narrowRadius = min(ceil(narrowSigma * 3.0), settings.z);
    float extendedRadius = clamp(settings.z, 1.0, 12.0);
    float4 total = 0.0;
    float2 totalWeight = 0.0;
    [loop]
    for (int offset = -12; offset <= 12; offset++)
    {
        float absoluteOffset = abs((float)offset);
        if (absoluteOffset <= extendedRadius)
        {
            float4 sample = PhotocopyRawSample(
                uv + float2(0.0, PixelSize.y * offset));
            float extendedWeight = PhotocopyGaussianWeight(
                offset,
                extendedSigma);
            total.zw += sample.zw * extendedWeight;
            totalWeight.y += extendedWeight;
            if (absoluteOffset <= narrowRadius)
            {
                float narrowWeight = PhotocopyGaussianWeight(
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

float4 PhotocopyComposite(float2 uv, float4 original)
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

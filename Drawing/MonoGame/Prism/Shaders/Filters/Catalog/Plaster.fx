sampler2D PlasterOriginalSampler = sampler_state
{
    Texture = <FilterAuxiliaryTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};

float2 PlasterClampUv(float2 uv)
{
    return clamp(
        uv,
        PixelSize * 0.5,
        1.0 - (PixelSize * 0.5));
}

float4 PlasterRawSample(float2 uv)
{
    return tex2D(
        SpriteTextureSampler,
        PlasterClampUv(uv));
}

float4 PlasterOriginal(float2 uv, int profile)
{
    return WorkingAssociatedToLinearSrgb(
        tex2D(PlasterOriginalSampler, PlasterClampUv(uv)),
        profile);
}

float4 PlasterHorizontalMoments(float2 uv, int profile)
{
    float radius = clamp(FilterOptions9.x, 1.0, 12.0);
    float3 moments = 0.0;
    float count = 0.0;
    [loop]
    for (int offset = -12; offset <= 12; offset++)
    {
        if (abs((float)offset) <= radius)
        {
            float4 sample = CatalogLinearSample(
                uv + float2(PixelSize.x * offset, 0.0),
                profile);
            float luminance = CatalogLuminance(sample);
            moments += float3(
                sample.a * luminance,
                sample.a * luminance * luminance,
                sample.a);
            count += 1.0;
        }
    }
    return float4(moments / max(count, 1.0), 1.0);
}

float4 PlasterVerticalCoefficients(float2 uv)
{
    float radius = clamp(FilterOptions9.y, 1.0, 12.0);
    float3 moments = 0.0;
    float count = 0.0;
    [loop]
    for (int offset = -12; offset <= 12; offset++)
    {
        if (abs((float)offset) <= radius)
        {
            moments += PlasterRawSample(
                uv + float2(0.0, PixelSize.y * offset)).rgb;
            count += 1.0;
        }
    }
    moments /= max(count, 1.0);
    if (moments.z <= 0.000001)
    {
        return float4(0.0, 0.0, 0.0, 1.0);
    }

    float mean = moments.x / moments.z;
    float variance = max(
        (moments.y / moments.z) - (mean * mean),
        0.0);
    float coefficient = variance /
        max(variance + FilterOptions5.z, 0.000001);
    return float4(
        coefficient,
        mean - (coefficient * mean),
        0.0,
        1.0);
}

float4 PlasterHorizontalCoefficients(float2 uv)
{
    float radius = clamp(FilterOptions9.x, 1.0, 12.0);
    float2 coefficients = 0.0;
    float count = 0.0;
    [loop]
    for (int offset = -12; offset <= 12; offset++)
    {
        if (abs((float)offset) <= radius)
        {
            coefficients += PlasterRawSample(
                uv + float2(PixelSize.x * offset, 0.0)).rg;
            count += 1.0;
        }
    }
    return float4(
        coefficients / max(count, 1.0),
        0.0,
        1.0);
}

float4 PlasterReconstructHeight(float2 uv, int profile)
{
    float radius = clamp(FilterOptions9.y, 1.0, 12.0);
    float2 coefficients = 0.0;
    float count = 0.0;
    [loop]
    for (int offset = -12; offset <= 12; offset++)
    {
        if (abs((float)offset) <= radius)
        {
            coefficients += PlasterRawSample(
                uv + float2(0.0, PixelSize.y * offset)).rg;
            count += 1.0;
        }
    }
    coefficients /= max(count, 1.0);
    float luminance = CatalogLuminance(PlasterOriginal(uv, profile));
    float heightValue = 1.0 - saturate(
        (coefficients.x * luminance) + coefficients.y);
    return float4(heightValue, heightValue, heightValue, 1.0);
}

float3 PlasterLightDirection(int code)
{
    const float diagonal = 0.70710678;
    float2 direction;
    if (code == 0)
    {
        direction = float2(0.0, -1.0);
    }
    else if (code == 1)
    {
        direction = float2(diagonal, -diagonal);
    }
    else if (code == 2)
    {
        direction = float2(1.0, 0.0);
    }
    else if (code == 3)
    {
        direction = float2(diagonal, diagonal);
    }
    else if (code == 4)
    {
        direction = float2(0.0, 1.0);
    }
    else if (code == 5)
    {
        direction = float2(-diagonal, diagonal);
    }
    else if (code == 6)
    {
        direction = float2(-1.0, 0.0);
    }
    else
    {
        direction = float2(-diagonal, -diagonal);
    }
    return normalize(float3(direction * 0.65, 0.76));
}

float PlasterHeight(float2 uv)
{
    return PlasterRawSample(uv).r;
}

float4 PlasterComposite(float2 uv, float4 original)
{
    if (original.a <= 0.000001)
    {
        return 0.0;
    }

    float center = PlasterHeight(uv);
    float horizontal =
        PlasterHeight(uv + float2(PixelSize.x, 0.0)) -
        PlasterHeight(uv - float2(PixelSize.x, 0.0));
    float vertical =
        PlasterHeight(uv + float2(0.0, PixelSize.y)) -
        PlasterHeight(uv - float2(0.0, PixelSize.y));
    float3 normal = normalize(float3(
        -horizontal * FilterOptions5.w,
        -vertical * FilterOptions5.w,
        1.0));
    float3 light = PlasterLightDirection(
        (int)(FilterOptions6.x + 0.5));
    float shade = (dot(normal, light) - light.z) * 0.75;
    float threshold = lerp(0.22, 0.78, saturate(FilterOptions5.x));
    float smoothness = saturate(FilterOptions4.x / 15.0);
    float transition = lerp(0.08, 0.18, smoothness);
    float surface = smoothstep(
        threshold - transition,
        threshold + transition,
        center);
    float3 color = saturate(
        lerp(FilterOptions0.rgb, FilterOptions1.rgb, surface) +
        shade);
    return float4(color * original.a, original.a);
}

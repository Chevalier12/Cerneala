float4 CatalogLinearSample(float2 uv, int profile)
{
    return WorkingAssociatedToLinearSrgb(
        tex2D(
            SpriteTextureSampler,
            clamp(
                uv,
                PixelSize * 0.5,
                1.0 - (PixelSize * 0.5))),
        profile);
}

float CatalogLuminance(float4 color)
{
    return dot(
        saturate(Unpremultiply(color)),
        float3(0.2126, 0.7152, 0.0722));
}

float CatalogParameterMagnitude()
{
    return dot(abs(FilterOptions0), 1.0) +
        dot(abs(FilterOptions1), 1.0) +
        dot(abs(FilterOptions2), 1.0) +
        dot(abs(FilterOptions3), 1.0) +
        dot(abs(FilterOptions4), 1.0) +
        dot(abs(FilterOptions5), 1.0) +
        dot(abs(FilterOptions6), 1.0) +
        dot(abs(FilterOptions7), 1.0) +
        dot(abs(FilterOptions8), 1.0);
}

float CatalogSeed()
{
    return dot(
        float4(
            FilterOptions0.x,
            FilterOptions1.x,
            FilterOptions2.x,
            FilterOptions3.x),
        float4(1.0, 17.0, 257.0, 4099.0)) +
        dot(
            float4(
                FilterOptions4.x,
                FilterOptions5.x,
                FilterOptions6.x,
                FilterOptions7.x),
            float4(65537.0, 31.0, 521.0, 8191.0)) +
        FilterOptions8.x;
}

float CatalogSobel(float2 uv, int profile)
{
    float topLeft = CatalogLuminance(
        CatalogLinearSample(
            uv + float2(-PixelSize.x, -PixelSize.y),
            profile));
    float top = CatalogLuminance(
        CatalogLinearSample(
            uv + float2(0.0, -PixelSize.y),
            profile));
    float topRight = CatalogLuminance(
        CatalogLinearSample(
            uv + float2(PixelSize.x, -PixelSize.y),
            profile));
    float left = CatalogLuminance(
        CatalogLinearSample(
            uv + float2(-PixelSize.x, 0.0),
            profile));
    float right = CatalogLuminance(
        CatalogLinearSample(
            uv + float2(PixelSize.x, 0.0),
            profile));
    float bottomLeft = CatalogLuminance(
        CatalogLinearSample(
            uv + float2(-PixelSize.x, PixelSize.y),
            profile));
    float bottom = CatalogLuminance(
        CatalogLinearSample(
            uv + float2(0.0, PixelSize.y),
            profile));
    float bottomRight = CatalogLuminance(
        CatalogLinearSample(
            uv + PixelSize,
            profile));
    float horizontal =
        -topLeft + topRight -
        (2.0 * left) + (2.0 * right) -
        bottomLeft + bottomRight;
    float vertical =
        -topLeft - (2.0 * top) - topRight +
        bottomLeft + (2.0 * bottom) + bottomRight;
    return saturate(length(float2(horizontal, vertical)));
}


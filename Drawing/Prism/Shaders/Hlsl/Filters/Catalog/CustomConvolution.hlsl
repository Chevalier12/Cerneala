float CustomConvolutionWrapCoordinate(
    float value,
    float length)
{
    return value - (floor(value / length) * length);
}

float CustomConvolutionMirrorCoordinate(
    float value,
    float length)
{
    if (length <= 1.0)
    {
        return 0.0;
    }

    float period = (length * 2.0) - 2.0;
    float mirrored = CustomConvolutionWrapCoordinate(
        value,
        period);
    return mirrored < length
        ? mirrored
        : period - mirrored;
}

float4 CatalogCustomConvolutionSample(
    float2 uv,
    int profile,
    int edgeMode)
{
    float2 textureSize = floor((1.0 / PixelSize) + 0.5);
    float2 pixel = floor(uv / PixelSize);
    if (edgeMode == 1 &&
        (any(pixel < 0.0) || any(pixel >= textureSize)))
    {
        return 0.0;
    }

    if (edgeMode == 2)
    {
        pixel = float2(
            CustomConvolutionWrapCoordinate(
                pixel.x,
                textureSize.x),
            CustomConvolutionWrapCoordinate(
                pixel.y,
                textureSize.y));
    }
    else if (edgeMode == 3)
    {
        pixel = float2(
            CustomConvolutionMirrorCoordinate(
                pixel.x,
                textureSize.x),
            CustomConvolutionMirrorCoordinate(
                pixel.y,
                textureSize.y));
    }
    else
    {
        pixel = clamp(pixel, 0.0, textureSize - 1.0);
    }

    return WorkingAssociatedToLinearSrgb(
        tex2D(
            SpriteTextureSampler,
            (pixel + 0.5) * PixelSize),
        profile);
}


float4 CatalogCustomConvolution(
    float2 uv,
    float4 source,
    int profile)
{
    float4 total = 0.0;
    int edgeMode = (int)(FilterOptions1.x + 0.5);
    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            float2 kernelUv =
                (float2(x, y) + 1.5) / 3.0;
            float weight = tex2D(
                SecondaryTextureSampler,
                kernelUv).r;
            total += CatalogCustomConvolutionSample(
                uv + (float2(x, y) * PixelSize),
                profile,
                edgeMode) * weight;
        }
    }

    float4 result =
        (total * FilterOptions4.x) +
        FilterOptions3.x;
    result.a = lerp(
        source.a,
        result.a,
        step(0.5, FilterOptions0.x));
    return result;
}

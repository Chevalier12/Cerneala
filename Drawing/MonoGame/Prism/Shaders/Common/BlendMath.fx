float BlendLuminosity(float3 color)
{
    return dot(color, float3(0.3, 0.59, 0.11));
}

float BlendSaturation(float3 color)
{
    return max(color.r, max(color.g, color.b))
        - min(color.r, min(color.g, color.b));
}

float3 ClipBlendColor(float3 color)
{
    float luminosity = BlendLuminosity(color);
    float minimum = min(color.r, min(color.g, color.b));
    float maximum = max(color.r, max(color.g, color.b));
    if (minimum < 0.0)
    {
        color = luminosity +
            ((color - luminosity) * luminosity)
            / (luminosity - minimum);
    }
    if (maximum > 1.0)
    {
        color = luminosity +
            ((color - luminosity) * (1.0 - luminosity))
            / (maximum - luminosity);
    }
    return color;
}

float3 SetBlendLuminosity(float3 color, float luminosity)
{
    return ClipBlendColor(
        color + (luminosity - BlendLuminosity(color)));
}

float3 SetBlendSaturation(float3 color, float saturation)
{
    float red = color.r;
    float green = color.g;
    float blue = color.b;
    if (max(red, max(green, blue)) == min(red, min(green, blue)))
    {
        return 0.0;
    }

    if (red <= green)
    {
        if (green <= blue)
        {
            return float3(
                0.0,
                ((green - red) * saturation) / (blue - red),
                saturation);
        }
        if (red <= blue)
        {
            return float3(
                0.0,
                saturation,
                ((blue - red) * saturation) / (green - red));
        }
        return float3(
            ((red - blue) * saturation) / (green - blue),
            saturation,
            0.0);
    }

    if (red <= blue)
    {
        return float3(
            ((red - green) * saturation) / (blue - green),
            0.0,
            saturation);
    }
    if (green <= blue)
    {
        return float3(
            saturation,
            0.0,
            ((blue - green) * saturation) / (red - green));
    }
    return float3(
        saturation,
        ((green - blue) * saturation) / (red - blue),
        0.0);
}

float3 BlendColorBurn(float3 backdrop, float3 source)
{
    float3 denominator = max(source, 0.000001);
    float3 result = 1.0 - min(1.0, (1.0 - backdrop) / denominator);
    return lerp(result, 0.0, step(source, 0.0));
}

float3 BlendColorDodge(float3 backdrop, float3 source)
{
    float3 denominator = max(1.0 - source, 0.000001);
    float3 result = min(1.0, backdrop / denominator);
    return lerp(result, 1.0, step(1.0, source));
}

float3 BlendOverlay(float3 backdrop, float3 source)
{
    float3 low = 2.0 * backdrop * source;
    float3 high =
        1.0 - (2.0 * (1.0 - backdrop) * (1.0 - source));
    return lerp(low, high, step(0.5, backdrop));
}

float3 BlendSoftLight(float3 backdrop, float3 source)
{
    float3 low = backdrop -
        ((1.0 - (2.0 * source)) * backdrop * (1.0 - backdrop));
    float3 polynomial =
        (((16.0 * backdrop) - 12.0) * backdrop + 4.0) * backdrop;
    float3 curve = lerp(
        polynomial,
        sqrt(max(backdrop, 0.0)),
        step(0.25, backdrop));
    float3 high = backdrop +
        (((2.0 * source) - 1.0) * (curve - backdrop));
    return lerp(low, high, step(0.5, source));
}

float3 BlendVividLight(float3 backdrop, float3 source)
{
    float3 low = BlendColorBurn(backdrop, 2.0 * source);
    float3 high = BlendColorDodge(
        backdrop,
        2.0 * (source - 0.5));
    return lerp(low, high, step(0.5, source));
}

float3 BlendPinLight(float3 backdrop, float3 source)
{
    float3 low = min(backdrop, 2.0 * source);
    float3 high = max(backdrop, (2.0 * source) - 1.0);
    return lerp(low, high, step(0.5, source));
}

float3 EvaluateBlendMode(
    int mode,
    float3 backdrop,
    float3 source)
{
    backdrop = saturate(backdrop);
    source = saturate(source);
    float3 result = source;
    if (mode == 2)
    {
        result = min(backdrop, source);
    }
    else if (mode == 3)
    {
        result = backdrop * source;
    }
    else if (mode == 4)
    {
        result = BlendColorBurn(backdrop, source);
    }
    else if (mode == 5)
    {
        result = backdrop + source - 1.0;
    }
    else if (mode == 6)
    {
        result = BlendLuminosity(backdrop)
                <= BlendLuminosity(source)
            ? backdrop
            : source;
    }
    else if (mode == 7)
    {
        result = max(backdrop, source);
    }
    else if (mode == 8)
    {
        result = backdrop + source - (backdrop * source);
    }
    else if (mode == 9)
    {
        result = BlendColorDodge(backdrop, source);
    }
    else if (mode == 10)
    {
        result = backdrop + source;
    }
    else if (mode == 11)
    {
        result = BlendLuminosity(backdrop)
                >= BlendLuminosity(source)
            ? backdrop
            : source;
    }
    else if (mode == 12)
    {
        result = BlendOverlay(backdrop, source);
    }
    else if (mode == 13)
    {
        result = BlendSoftLight(backdrop, source);
    }
    else if (mode == 14)
    {
        result = BlendOverlay(source, backdrop);
    }
    else if (mode == 15)
    {
        result = BlendVividLight(backdrop, source);
    }
    else if (mode == 16)
    {
        result = backdrop + (2.0 * source) - 1.0;
    }
    else if (mode == 17)
    {
        result = BlendPinLight(backdrop, source);
    }
    else if (mode == 18)
    {
        result = step(
            0.5,
            BlendVividLight(backdrop, source));
    }
    else if (mode == 19)
    {
        result = abs(backdrop - source);
    }
    else if (mode == 20)
    {
        result = backdrop + source -
            (2.0 * backdrop * source);
    }
    else if (mode == 21)
    {
        result = backdrop - source;
    }
    else if (mode == 22)
    {
        result = lerp(
            backdrop / max(source, 0.000001),
            1.0,
            step(source, 0.0));
    }
    else if (mode == 23)
    {
        result = SetBlendLuminosity(
            SetBlendSaturation(
                source,
                BlendSaturation(backdrop)),
            BlendLuminosity(backdrop));
    }
    else if (mode == 24)
    {
        result = SetBlendLuminosity(
            SetBlendSaturation(
                backdrop,
                BlendSaturation(source)),
            BlendLuminosity(backdrop));
    }
    else if (mode == 25)
    {
        result = SetBlendLuminosity(
            source,
            BlendLuminosity(backdrop));
    }
    else if (mode == 26)
    {
        result = SetBlendLuminosity(
            backdrop,
            BlendLuminosity(source));
    }
    return saturate(result);
}

float ResolveBlendIfValue(float3 color)
{
    if (BlendIfChannel < 0.5)
    {
        return BlendLuminosity(color);
    }
    if (BlendIfChannel < 1.5)
    {
        return color.r;
    }
    if (BlendIfChannel < 2.5)
    {
        return color.g;
    }
    return color.b;
}

float EvaluateBlendIfRange(float value, float4 range)
{
    float black = range.y > range.x
        ? saturate((value - range.x) / (range.y - range.x))
        : value >= range.x ? 1.0 : 0.0;
    float white = range.w > range.z
        ? 1.0 -
            saturate((value - range.z) / (range.w - range.z))
        : value <= range.z ? 1.0 : 0.0;
    return black * white;
}

float4 CompositeAssociated(
    float4 source,
    float4 backdrop,
    float3 blended)
{
    float overlap = source.a * backdrop.a;
    return float4(
        (source.rgb * (1.0 - backdrop.a)) +
            (backdrop.rgb * (1.0 - source.a)) +
            (blended * overlap),
        source.a + backdrop.a - overlap);
}


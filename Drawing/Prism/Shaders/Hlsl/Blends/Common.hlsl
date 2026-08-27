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

float4 BlendPixelShader(VertexShaderOutput input, int mode);

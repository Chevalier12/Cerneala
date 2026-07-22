float3 DecodeSrgb(float3 value)
{
    float3 low = value / 12.92;
    float3 high = pow(max((value + 0.055) / 1.055, 0.0), 2.4);
    return lerp(high, low, step(value, 0.04045));
}

float3 EncodeSrgb(float3 value)
{
    float3 low = value * 12.92;
    float3 high = (1.055 * pow(max(value, 0.0), 1.0 / 2.4)) - 0.055;
    return lerp(high, low, step(value, 0.0031308));
}

float3 LinearSrgbToLinearDisplayP3(float3 value)
{
    return float3(
        dot(value, float3(0.822592735, 0.177533954, 0.000000027)),
        dot(value, float3(0.033199601, 0.966783523, -0.000000002)),
        dot(value, float3(0.017085349, 0.072395741, 0.910301476)));
}

float3 LinearDisplayP3ToLinearSrgb(float3 value)
{
    return float3(
        dot(value, float3(1.224745485, -0.224904439, -0.000000037)),
        dot(value, float3(-0.042058082, 1.042080996, 0.000000003)),
        dot(value, float3(-0.019642260, -0.078654881, 1.098537162)));
}

float3 Unpremultiply(float4 source)
{
    return
        (source.rgb / max(source.a, 0.000001)) *
        step(0.000001, source.a);
}

float4 FinishColorConversion(
    VertexShaderOutput input,
    float4 source,
    float3 straight)
{
    float3 associated = source.a > 0.0
        ? straight * source.a
        : 0.0;
    return float4(associated, source.a)
        * input.Color
        * Opacity;
}

float4 InputToLinearSrgbPixelShader(VertexShaderOutput input) : COLOR0
{
    float4 source = SampleSource(input);
    float3 straight = saturate(Unpremultiply(source));
    return FinishColorConversion(
        input,
        source,
        DecodeSrgb(straight));
}

float4 InputToSrgbPixelShader(VertexShaderOutput input) : COLOR0
{
    float4 source = SampleSource(input);
    return FinishColorConversion(
        input,
        source,
        saturate(Unpremultiply(source)));
}

float4 InputToLinearDisplayP3PixelShader(
    VertexShaderOutput input) : COLOR0
{
    float4 source = SampleSource(input);
    float3 straight = saturate(Unpremultiply(source));
    return FinishColorConversion(
        input,
        source,
        saturate(LinearSrgbToLinearDisplayP3(
            DecodeSrgb(straight))));
}

float4 InputToDisplayP3PixelShader(VertexShaderOutput input) : COLOR0
{
    float4 source = SampleSource(input);
    float3 straight = saturate(Unpremultiply(source));
    return FinishColorConversion(
        input,
        source,
        saturate(EncodeSrgb(
            LinearSrgbToLinearDisplayP3(
                DecodeSrgb(straight)))));
}

float4 InputToScRgbPixelShader(VertexShaderOutput input) : COLOR0
{
    float4 source = SampleSource(input);
    float3 straight = saturate(Unpremultiply(source));
    return FinishColorConversion(
        input,
        source,
        DecodeSrgb(straight));
}

float4 LinearSrgbToOutputPixelShader(VertexShaderOutput input) : COLOR0
{
    float4 source = SampleSource(input);
    return FinishColorConversion(
        input,
        source,
        saturate(EncodeSrgb(Unpremultiply(source))));
}

float4 SrgbToOutputPixelShader(VertexShaderOutput input) : COLOR0
{
    float4 source = SampleSource(input);
    return FinishColorConversion(
        input,
        source,
        saturate(Unpremultiply(source)));
}

float4 LinearDisplayP3ToOutputPixelShader(
    VertexShaderOutput input) : COLOR0
{
    float4 source = SampleSource(input);
    return FinishColorConversion(
        input,
        source,
        saturate(EncodeSrgb(
            LinearDisplayP3ToLinearSrgb(
                Unpremultiply(source)))));
}

float4 DisplayP3ToOutputPixelShader(VertexShaderOutput input) : COLOR0
{
    float4 source = SampleSource(input);
    float3 straight = saturate(Unpremultiply(source));
    return FinishColorConversion(
        input,
        source,
        saturate(EncodeSrgb(
            LinearDisplayP3ToLinearSrgb(
                DecodeSrgb(straight)))));
}

float4 ScRgbToOutputPixelShader(VertexShaderOutput input) : COLOR0
{
    float4 source = SampleSource(input);
    return FinishColorConversion(
        input,
        source,
        saturate(EncodeSrgb(Unpremultiply(source))));
}

float AdjustmentLuminance(float3 color)
{
    return dot(color, float3(0.2126, 0.7152, 0.0722));
}

float3 WorkingToLinearSrgb(float3 color, int profile)
{
    if (profile == 174)
    {
        return DecodeSrgb(saturate(color));
    }
    if (profile == 175)
    {
        return LinearDisplayP3ToLinearSrgb(color);
    }
    if (profile == 176)
    {
        return LinearDisplayP3ToLinearSrgb(
            DecodeSrgb(saturate(color)));
    }
    return color;
}

float3 LinearSrgbToWorking(float3 color, int profile)
{
    if (profile == 174)
    {
        return EncodeSrgb(color);
    }
    if (profile == 175)
    {
        return LinearSrgbToLinearDisplayP3(color);
    }
    if (profile == 176)
    {
        return EncodeSrgb(
            LinearSrgbToLinearDisplayP3(color));
    }
    return color;
}

float4 WorkingAssociatedToLinearSrgb(
    float4 color,
    int profile)
{
    float3 straight = WorkingToLinearSrgb(
        Unpremultiply(color),
        profile);
    return float4(
        straight * color.a,
        color.a);
}

float4 LinearSrgbAssociatedToWorking(
    float4 color,
    int profile)
{
    float3 straight = LinearSrgbToWorking(
        Unpremultiply(color),
        profile);
    return float4(
        straight * color.a,
        color.a);
}

float4 BackdropColorConversionPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int sourceProfile = (int)(FilterHeader.x + 0.5);
    int targetProfile = (int)(FilterHeader.y + 0.5);
    float4 linearColor = WorkingAssociatedToLinearSrgb(
        SampleSource(input),
        sourceProfile);
    return LinearSrgbAssociatedToWorking(
        linearColor,
        targetProfile) * input.Color * Opacity;
}


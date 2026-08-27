float3 DecodeSrgb(float3 value)
{
    float3 magnitude = abs(value);
    float3 low = value / 12.92;
    float3 high = sign(value) *
        pow((magnitude + 0.055) / 1.055, 2.4);
    return lerp(high, low, step(magnitude, 0.04045));
}

float3 EncodeSrgb(float3 value)
{
    float3 magnitude = abs(value);
    float3 low = value * 12.92;
    float3 high = sign(value) *
        ((1.055 * pow(magnitude, 1.0 / 2.4)) - 0.055);
    return lerp(high, low, step(magnitude, 0.0031308));
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

float AdjustmentLuminance(float3 color)
{
    return dot(color, float3(0.2126, 0.7152, 0.0722));
}

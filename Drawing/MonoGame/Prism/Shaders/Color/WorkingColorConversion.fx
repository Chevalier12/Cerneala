float3 WorkingToLinearSrgb(float3 color, int profile)
{
    if (profile == 174)
    {
        return DecodeSrgb(color);
    }
    if (profile == 175)
    {
        return LinearDisplayP3ToLinearSrgb(color);
    }
    if (profile == 176)
    {
        return LinearDisplayP3ToLinearSrgb(
            DecodeSrgb(color));
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

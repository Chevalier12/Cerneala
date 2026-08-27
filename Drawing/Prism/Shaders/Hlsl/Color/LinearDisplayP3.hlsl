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

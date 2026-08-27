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

float4 InputToLinearSrgbPixelShader(VertexShaderOutput input) : COLOR0
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

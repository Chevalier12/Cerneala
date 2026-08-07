float4 InputToScRgbPixelShader(VertexShaderOutput input) : COLOR0
{
    float4 source = SampleSource(input);
    return FinishColorConversion(
        input,
        source,
        DecodeSrgb(Unpremultiply(source)));
}

float4 ScRgbToOutputPixelShader(VertexShaderOutput input) : COLOR0
{
    float4 source = SampleSource(input);
    return FinishColorConversion(
        input,
        source,
        EncodeSrgb(Unpremultiply(source)));
}

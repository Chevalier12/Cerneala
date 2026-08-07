float4 CopyCompositePixelShader(VertexShaderOutput input) : COLOR0
{
    return SampleSource(input) * input.Color * Opacity;
}

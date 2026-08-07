float4 ClipAlphaPixelShader(VertexShaderOutput input) : COLOR0
{
    float4 content = SampleSource(input);
    float clipAlpha = SampleSecondary(input).a;
    return content * clipAlpha * input.Color * Opacity;
}

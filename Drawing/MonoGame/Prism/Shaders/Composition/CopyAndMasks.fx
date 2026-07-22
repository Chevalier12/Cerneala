float4 CopyCompositePixelShader(VertexShaderOutput input) : COLOR0
{
    return SampleSource(input) * input.Color * Opacity;
}

float2 ResolveMaskUv(VertexShaderOutput input)
{
    float3 position = float3(input.Position.xy, 1.0);
    return float2(
        dot(position, MaskUvRowX),
        dot(position, MaskUvRowY));
}

float4 MaskExtractPixelShader(VertexShaderOutput input) : COLOR0
{
    float2 uv = ResolveMaskUv(input);
    float inside =
        step(0.0, uv.x) *
        step(uv.x, 1.0) *
        step(0.0, uv.y) *
        step(uv.y, 1.0);
    float4 sample = tex2D(
        SpriteTextureSampler,
        saturate(uv));
    float3 straight = sample.a > 0.0
        ? sample.rgb / sample.a
        : 0.0;
    float luminance = dot(
        saturate(straight),
        float3(0.2126, 0.7152, 0.0722));
    float value = lerp(
        sample.a,
        luminance,
        step(0.5, MaskChannel));
    value *= inside;
    value = lerp(
        value,
        1.0 - value,
        step(0.5, MaskInvert));
    value = lerp(
        1.0,
        value,
        saturate(MaskDensity));
    return float4(value, value, value, value)
        * input.Color
        * Opacity;
}

float4 BackdropCropPixelShader(VertexShaderOutput input) : COLOR0
{
    float2 uv = ResolveMaskUv(input);
    float inside =
        step(0.0, uv.x) *
        step(uv.x, 1.0) *
        step(0.0, uv.y) *
        step(uv.y, 1.0);
    float4 sample = tex2D(
        SpriteTextureSampler,
        clamp(
            uv,
            PixelSize * 0.5,
            1.0 - (PixelSize * 0.5)));
    if (MaskChannel < 0.5)
    {
        sample.a = 1.0;
    }
    else if (MaskChannel > 1.5)
    {
        sample.rgb *= sample.a;
    }
    return sample * inside * input.Color * Opacity;
}

float SampleMaskAlpha(float2 uv)
{
    float2 clampedUv = clamp(
        uv,
        PixelSize * 0.5,
        1.0 - (PixelSize * 0.5));
    return tex2D(SpriteTextureSampler, clampedUv).a;
}

float4 MaskFeatherPixelShader(VertexShaderOutput input) : COLOR0
{
    float2 uv = ResolveUv(input);
    float value =
        SampleMaskAlpha(uv - MaskFeatherStep) +
        (4.0 * SampleMaskAlpha(
            uv - (MaskFeatherStep * 0.75))) +
        (7.0 * SampleMaskAlpha(
            uv - (MaskFeatherStep * 0.5))) +
        (10.0 * SampleMaskAlpha(
            uv - (MaskFeatherStep * 0.25))) +
        (12.0 * SampleMaskAlpha(uv)) +
        (10.0 * SampleMaskAlpha(
            uv + (MaskFeatherStep * 0.25))) +
        (7.0 * SampleMaskAlpha(
            uv + (MaskFeatherStep * 0.5))) +
        (4.0 * SampleMaskAlpha(
            uv + (MaskFeatherStep * 0.75))) +
        SampleMaskAlpha(uv + MaskFeatherStep);
    value /= 56.0;
    value = lerp(
        1.0,
        value,
        saturate(MaskDensity));
    return float4(value, value, value, value)
        * input.Color
        * Opacity;
}

float4 MaskAlphaPixelShader(VertexShaderOutput input) : COLOR0
{
    float4 content = SampleSource(input);
    float maskAlpha = SampleSecondary(input).a;
    return content * maskAlpha * input.Color * Opacity;
}

float4 ClipAlphaPixelShader(VertexShaderOutput input) : COLOR0
{
    float4 content = SampleSource(input);
    float clipAlpha = SampleSecondary(input).a;
    return content * clipAlpha * input.Color * Opacity;
}


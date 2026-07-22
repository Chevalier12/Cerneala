float4 ApplyBlendChannelMask(
    float4 composite,
    float4 backdrop)
{
    float3 compositeStraight = composite.a > 0.0
        ? composite.rgb / composite.a
        : 0.0;
    float3 backdropStraight = backdrop.a > 0.0
        ? backdrop.rgb / backdrop.a
        : 0.0;
    float alpha = lerp(
        backdrop.a,
        composite.a,
        BlendChannels.a);
    float3 straight = lerp(
        backdropStraight,
        compositeStraight,
        BlendChannels.rgb);
    return float4(straight * alpha, alpha);
}

float DissolveValue(float2 position)
{
    float value = fmod(
        (floor(position.x) * 17.0) +
        (floor(position.y) * 131.0) +
        (DissolveSeed * 13.0),
        256.0);
    return value / 256.0;
}

float4 BlendPixelShader(
    VertexShaderOutput input,
    int mode) : COLOR0
{
    float4 source = SampleSource(input);
    float4 backdrop = BackgroundAvailable > 0.5
        ? SampleSecondary(input)
        : 0.0;
    float3 sourceStraight = source.a > 0.0
        ? source.rgb / source.a
        : 0.0;
    float3 backdropStraight = backdrop.a > 0.0
        ? backdrop.rgb / backdrop.a
        : 0.0;
    float blendIf = EvaluateBlendIfRange(
            ResolveBlendIfValue(sourceStraight),
            ThisLayerRange) *
        EvaluateBlendIfRange(
            ResolveBlendIfValue(backdropStraight),
            UnderlyingRange);
    source *= blendIf;

    float4 composite;
    if (mode == 1)
    {
        float selected = DissolveValue(input.Position.xy) < source.a
            ? 1.0
            : 0.0;
        float4 dissolved = float4(
            sourceStraight * selected,
            selected);
        composite = CompositeAssociated(
            dissolved,
            backdrop,
            sourceStraight);
    }
    else
    {
        float3 blended = KnockoutMode > 0.5
            ? sourceStraight
            : EvaluateBlendMode(
                mode == 27 ? 0 : mode,
                backdropStraight,
                sourceStraight);
        composite = CompositeAssociated(
            source,
            backdrop,
            blended);
    }

    return ApplyBlendChannelMask(composite, backdrop)
        * input.Color
        * Opacity;
}

float4 NormalBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 0);
}

float4 DissolveBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 1);
}

float4 DarkenBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 2);
}

float4 MultiplyBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 3);
}

float4 ColorBurnBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 4);
}

float4 LinearBurnBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 5);
}

float4 DarkerColorBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 6);
}

float4 LightenBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 7);
}

float4 ScreenBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 8);
}

float4 ColorDodgeBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 9);
}

float4 LinearDodgeBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 10);
}

float4 LighterColorBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 11);
}

float4 OverlayBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 12);
}

float4 SoftLightBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 13);
}

float4 HardLightBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 14);
}

float4 VividLightBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 15);
}

float4 LinearLightBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 16);
}

float4 PinLightBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 17);
}

float4 HardMixBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 18);
}

float4 DifferenceBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 19);
}

float4 ExclusionBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 20);
}

float4 SubtractBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 21);
}

float4 DivideBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 22);
}

float4 HueBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 23);
}

float4 SaturationBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 24);
}

float4 ColorBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 25);
}

float4 LuminosityBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 26);
}

float4 PassThroughBlendPixelShader(VertexShaderOutput input) : COLOR0
{
    return BlendPixelShader(input, 27);
}


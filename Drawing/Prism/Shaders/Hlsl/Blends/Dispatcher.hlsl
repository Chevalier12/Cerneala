float3 EvaluateBlendMode(
    int mode,
    float3 backdrop,
    float3 source)
{
    backdrop = saturate(backdrop);
    source = saturate(source);
    float3 result = BlendNormal(backdrop, source);
    if (mode == 2)
    {
        result = BlendDarken(backdrop, source);
    }
    else if (mode == 3)
    {
        result = BlendMultiply(backdrop, source);
    }
    else if (mode == 4)
    {
        result = BlendColorBurn(backdrop, source);
    }
    else if (mode == 5)
    {
        result = BlendLinearBurn(backdrop, source);
    }
    else if (mode == 6)
    {
        result = BlendDarkerColor(backdrop, source);
    }
    else if (mode == 7)
    {
        result = BlendLighten(backdrop, source);
    }
    else if (mode == 8)
    {
        result = BlendScreen(backdrop, source);
    }
    else if (mode == 9)
    {
        result = BlendColorDodge(backdrop, source);
    }
    else if (mode == 10)
    {
        result = BlendLinearDodge(backdrop, source);
    }
    else if (mode == 11)
    {
        result = BlendLighterColor(backdrop, source);
    }
    else if (mode == 12)
    {
        result = BlendOverlay(backdrop, source);
    }
    else if (mode == 13)
    {
        result = BlendSoftLight(backdrop, source);
    }
    else if (mode == 14)
    {
        result = BlendHardLight(backdrop, source);
    }
    else if (mode == 15)
    {
        result = BlendVividLight(backdrop, source);
    }
    else if (mode == 16)
    {
        result = BlendLinearLight(backdrop, source);
    }
    else if (mode == 17)
    {
        result = BlendPinLight(backdrop, source);
    }
    else if (mode == 18)
    {
        result = BlendHardMix(backdrop, source);
    }
    else if (mode == 19)
    {
        result = BlendDifference(backdrop, source);
    }
    else if (mode == 20)
    {
        result = BlendExclusion(backdrop, source);
    }
    else if (mode == 21)
    {
        result = BlendSubtract(backdrop, source);
    }
    else if (mode == 22)
    {
        result = BlendDivide(backdrop, source);
    }
    else if (mode == 23)
    {
        result = BlendHue(backdrop, source);
    }
    else if (mode == 24)
    {
        result = BlendSaturationMode(backdrop, source);
    }
    else if (mode == 25)
    {
        result = BlendColorMode(backdrop, source);
    }
    else if (mode == 26)
    {
        result = BlendLuminosityMode(backdrop, source);
    }
    else if (mode == 27)
    {
        result = BlendPassThrough(backdrop, source);
    }
    return saturate(result);
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

    float4 originalBackdrop = KnockoutBackdropAvailable > 0.5
        ? SampleKnockoutBackdrop(input)
        : 0.0;
    float3 originalBackdropStraight = originalBackdrop.a > 0.0
        ? originalBackdrop.rgb / originalBackdrop.a
        : 0.0;
    float sourceShape = SampleKnockoutShape(input) * blendIf;

    float4 composite;
    if (mode == 1)
    {
        float selected = DissolveValue(input.Position.xy) < source.a
            ? 1.0
            : 0.0;
        float4 dissolved = float4(
            sourceStraight * selected,
            selected);
        composite = KnockoutMode > 0.5
            ? CompositeKnockout(
                dissolved,
                backdrop,
                originalBackdrop,
                selected,
                sourceStraight)
            : CompositeAssociated(
                dissolved,
                backdrop,
                sourceStraight);
    }
    else
    {
        float3 blended = EvaluateBlendMode(
            mode,
            KnockoutMode > 0.5
                ? originalBackdropStraight
                : backdropStraight,
            sourceStraight);
        composite = KnockoutMode > 0.5
            ? CompositeKnockout(
                source,
                backdrop,
                originalBackdrop,
                sourceShape,
                blended)
            : CompositeAssociated(
                source,
                backdrop,
                blended);
    }

    return ApplyBlendChannelMask(composite, backdrop)
        * input.Color
        * Opacity;
}

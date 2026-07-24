float4 LayerStylePixelShader(
    VertexShaderOutput input) : COLOR0
{
    float4 content = SampleSource(input);
    float2 uv = ResolveUv(input);
    int kind = (int)(StyleModes0.x + 0.5);
    int blendMode = (int)(StyleModes0.y + 0.5);
    int secondaryBlendMode =
        (int)(StyleModes0.z + 0.5);
    float alpha = SampleStyleAlpha(uv);
    float2 offset = StyleGeometry0.xy;
    float styleTechnique = StyleModes1.z;
    float techniqueScale = styleTechnique < 0.5
        ? 1.0
        : styleTechnique < 1.5 ? 0.65 : 0.8;
    float size = max(
        StyleGeometry0.z * techniqueScale,
        0.5);
    float spread = max(StyleGeometry0.w, 0.0);
    float shifted = alpha;
    float local = alpha;
    if (kind == 0 || kind == 1)
    {
        shifted = StyleBlurAlpha(
            uv - offset,
            size);
    }
    else if (kind == 2)
    {
        local = tex2D(
            StyleMaskTextureSampler,
            uv).a;
        shifted = local;
    }
    else if (kind == 3)
    {
        local = StyleBlurAlpha(uv, size);
        shifted = local;
    }
    float spreadRatio =
        spread / max(size + spread, 0.0001);
    float grown = saturate(
        shifted + (spreadRatio * (1.0 - shifted)));
    float innerEdge = saturate(
        alpha * (1.0 - local));
    float outerEdge = saturate(grown - alpha);
    float3 pixelPosition = float3(input.Position.xy, 1.0);
    float2 boundsUv = float2(
        dot(pixelPosition, StyleBoundsUvRowX),
        dot(pixelPosition, StyleBoundsUvRowY));
    float insideBounds =
        step(0.0, boundsUv.x) *
        step(boundsUv.x, 1.0) *
        step(0.0, boundsUv.y) *
        step(boundsUv.y, 1.0);
    float mask = alpha;

    if (kind == 0)
    {
        mask = EvaluateDropShadowMask(grown, alpha);
    }
    else if (kind == 1)
    {
        mask = EvaluateInnerShadowMask(
            alpha,
            shifted,
            spreadRatio);
    }
    else if (kind == 2)
    {
        mask = EvaluateOuterGlowMask(
            local,
            alpha,
            insideBounds);
    }
    else if (kind == 3)
    {
        mask = EvaluateInnerGlowMask(
            alpha,
            local,
            innerEdge);
    }
    else if (kind == 5)
    {
        mask = EvaluateSatinMask(
            uv,
            alpha,
            offset,
            size);
    }
    else if (kind == 6)
    {
        mask = EvaluateColorOverlayMask(alpha);
    }
    else if (kind == 7)
    {
        mask = EvaluateGradientOverlayMask(alpha);
    }
    else if (kind == 8)
    {
        mask = EvaluatePatternOverlayMask(alpha);
    }
    else if (kind == 9)
    {
        mask = EvaluateStrokeMask(uv, alpha, size);
    }

    if (kind == 2 || kind == 3)
    {
        mask = saturate(
            mask / max(StyleModes3.z, 0.0001));
    }
    mask = ApplyStyleContour(
        mask,
        StyleModes1.x);
    mask = lerp(
        mask,
        smoothstep(0.0, 1.0, mask),
        StyleFlag(1.0));
    float noise = saturate(StyleOptions0.z);
    mask *= lerp(
        1.0,
        step(
            StyleRandom(input.Position.xy),
            mask),
        noise);
    mask = saturate(
        mask +
        ((StyleRandom(
            input.Position.xy + 37.0) - 0.5) *
            StyleOptions0.w *
            mask));

    if (kind == 4)
    {
        return CompositeBevelEmbossStyle(
            input,
            content,
            uv,
            alpha,
            styleTechnique,
            blendMode,
            secondaryBlendMode);
    }

    float4 paint = SampleStylePaint(input, mask);
    float styleAlpha = saturate(
        mask *
        StyleOptions0.x *
        paint.a);
    float4 style = float4(
        paint.rgb * styleAlpha,
        styleAlpha);
    float4 result;
    if (kind == 0)
    {
        result = CompositeDropShadowStyle(
            content,
            style);
    }
    else if (kind == 2)
    {
        result = CompositeOuterGlowStyle(
            content,
            style);
    }
    else
    {
        result = CompositeStyleOver(
            style,
            content,
            blendMode);
    }

    return result * input.Color * Opacity;
}


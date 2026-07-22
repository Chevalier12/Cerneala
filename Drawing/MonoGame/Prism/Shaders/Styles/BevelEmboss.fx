float4 CompositeBevelEmbossStyle(
    VertexShaderOutput input,
    float4 content,
    float2 uv,
    float alpha,
    float styleTechnique,
    int blendMode,
    int secondaryBlendMode)
{
    float lightAngle = StyleGeometry1.x;
    float altitude = StyleGeometry1.y;
    float2 light = float2(
        cos(lightAngle),
        -sin(lightAngle)) *
        PixelSize *
        max(
            StyleGeometry0.z + StyleGeometry1.w,
            1.0);
    float signedEdge =
        SampleStyleAlpha(uv - light) -
        SampleStyleAlpha(uv + light);
    float edgeSign = sign(signedEdge);
    float edgeAmount = ApplyStyleContour(
        abs(signedEdge),
        StyleModes1.x);
    edgeAmount = styleTechnique < 0.5
        ? edgeAmount
        : styleTechnique < 1.5
            ? saturate(edgeAmount * 1.5)
            : step(0.2, edgeAmount);
    if (StyleFlag(256.0) > 0.5)
    {
        edgeAmount = ApplyStyleContour(
            saturate(
                edgeAmount /
                max(StyleModes3.z, 0.0001)),
            StyleModes1.y);
        edgeAmount = lerp(
            edgeAmount,
            smoothstep(0.0, 1.0, edgeAmount),
            StyleFlag(1024.0));
    }

    float bevelStyle = StyleModes3.x;
    float edgeSupport = alpha;
    if (bevelStyle > 0.5 && bevelStyle < 1.5)
    {
        edgeSupport = 1.0 - alpha;
    }
    else if (bevelStyle > 1.5 && bevelStyle < 3.5)
    {
        edgeSupport = 1.0;
    }
    edgeSign *= lerp(
        1.0,
        -1.0,
        step(0.5, StyleModes2.y));
    signedEdge =
        edgeSign *
        edgeAmount *
        edgeSupport *
        max(StyleGeometry1.z, 0.0) *
        max(sin(altitude), 0.05);
    float textureValue = 1.0;
    if (StyleFlag(64.0) > 0.5 &&
        StyleResourceAvailable > 0.5)
    {
        float2 textureUv = lerp(
            input.Position.xy * PixelSize,
            uv,
            StyleFlag(512.0));
        float4 sample = tex2D(
            StyleTextureSampler,
            (textureUv / max(StyleOptions1.x, 0.0001)) +
                StyleOptions1.zw);
        textureValue = lerp(
            sample.a,
            1.0 - sample.a,
            StyleFlag(128.0));
        signedEdge +=
            (((textureValue * 2.0) - 1.0) *
                StyleOptions1.y *
                0.25);
    }

    float highlightAlpha =
        saturate(signedEdge) *
        alpha *
        StyleOptions0.x *
        StyleColor.a;
    float shadowAlpha =
        saturate(-signedEdge) *
        alpha *
        StyleOptions0.y *
        StyleSecondaryColor.a;
    float4 shadow = float4(
        StyleSecondaryColor.rgb * shadowAlpha,
        shadowAlpha);
    float4 highlight = float4(
        StyleColor.rgb * highlightAlpha,
        highlightAlpha);
    float4 beveled = CompositeStyleOver(
        shadow,
        content,
        secondaryBlendMode);
    return CompositeStyleOver(
        highlight,
        beveled,
        blendMode) *
        input.Color *
        Opacity;
}

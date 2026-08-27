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

float4 CompositeAssociated(
    float4 source,
    float4 backdrop,
    float3 blended)
{
    float overlap = source.a * backdrop.a;
    return float4(
        (source.rgb * (1.0 - backdrop.a)) +
            (backdrop.rgb * (1.0 - source.a)) +
            (blended * overlap),
        source.a + backdrop.a - overlap);
}


float4 CompositeKnockout(
    float4 source,
    float4 currentBackdrop,
    float4 originalBackdrop,
    float sourceShape,
    float3 blended)
{
    sourceShape = saturate(max(sourceShape, source.a));
    float remainingBackdropAlpha = 1.0 - originalBackdrop.a;
    float previousGroupAlpha = remainingBackdropAlpha > 0.000001
        ? saturate(
            (currentBackdrop.a - originalBackdrop.a) /
                remainingBackdropAlpha)
        : 0.0;
    float groupAlpha =
        ((1.0 - sourceShape) * previousGroupAlpha) +
        source.a;
    float alpha = originalBackdrop.a + groupAlpha -
        (originalBackdrop.a * groupAlpha);
    float3 sourceStraight = source.a > 0.0
        ? source.rgb / source.a
        : 0.0;
    float3 contribution =
        ((sourceShape - source.a) * originalBackdrop.rgb) +
        (source.a *
            (((1.0 - originalBackdrop.a) * sourceStraight) +
                (originalBackdrop.a * blended)));
    return float4(
        ((1.0 - sourceShape) * currentBackdrop.rgb) +
            contribution,
        alpha);
}

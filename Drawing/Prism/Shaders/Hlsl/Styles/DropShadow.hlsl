float EvaluateDropShadowMask(
    float grown,
    float alpha)
{
    return grown *
        lerp(1.0, 1.0 - alpha, StyleFlag(16.0));
}

float4 CompositeDropShadowStyle(
    float4 content,
    float4 style,
    int blendMode,
    float4 backdrop,
    float backdropAvailable)
{
    if (backdropAvailable > 0.5)
    {
        float3 styleStraight = style.a > 0.0
            ? style.rgb / style.a
            : 0.0;
        float3 backdropStraight = backdrop.a > 0.0
            ? backdrop.rgb / backdrop.a
            : 0.0;
        style.rgb = EvaluateBlendMode(
            blendMode,
            backdropStraight,
            styleStraight) * style.a;
    }
    return content + (style * (1.0 - content.a));
}

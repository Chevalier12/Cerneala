float EvaluateDropShadowMask(
    float grown,
    float alpha)
{
    return grown *
        lerp(1.0, 1.0 - alpha, StyleFlag(16.0));
}

float4 CompositeDropShadowStyle(
    float4 content,
    float4 style)
{
    return content + (style * (1.0 - content.a));
}
